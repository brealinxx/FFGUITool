using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Models;

namespace FFGUITool.Services;

public sealed class ProcessingExecutor
{
    private readonly FFmpegManager _ffmpeg;
    private readonly ExifToolManager _exif;
    private readonly VideoAnalyzer _analyzer;
    private readonly CommandBuilder _builder;
    private readonly Dictionary<string, bool> _hardwareChecks = new();

    public ProcessingExecutor(FFmpegManager ffmpeg, ExifToolManager exif, VideoAnalyzer analyzer, CommandBuilder builder)
    { _ffmpeg = ffmpeg; _exif = exif; _analyzer = analyzer; _builder = builder; }

    public IReadOnlyList<string> GetPlannedOutputPaths(IEnumerable<ProcessingTask> tasks, ProcessingExecutionOptions options) =>
        tasks.Select(task => BuildCommand(task, options).OutputPath).ToList();

    public FFmpegCommand BuildCommand(ProcessingTask task, ProcessingExecutionOptions options, VideoInfo? inputInfo = null)
    {
        var settings = task.Settings.Clone();
        settings.InputPath = task.InputPath;
        settings.InputVideoCodec = inputInfo?.VideoCodec ?? settings.InputVideoCodec;
        settings.PreferDav1dDecoder = settings.InputVideoCodec == "av1" && options.AvailableVideoDecoders.Contains("libdav1d");
        if (task.UsesRelativeTarget)
        {
            var bytes = inputInfo?.FileSize ?? (File.Exists(task.InputPath) ? new FileInfo(task.InputPath).Length : 0);
            var ratio = Math.Clamp(task.RelativeTargetPercentage, 1, 100) / 100;
            if (settings.IsImageProcessing) settings.ImageTargetSizeKB = Math.Max(1, bytes * ratio / 1024);
            else settings.TargetSizeMB = Math.Max(0.01, bytes * ratio / 1024 / 1024);
            settings.OutputLabel = settings.UseCrf ? $"crf{settings.Crf}" : $"ratio{task.RelativeTargetPercentage:0.#}pct";
        }
        return _builder.BuildCommand(settings, inputInfo);
    }

    public async Task<ProcessingExecutionSummary> ExecuteAsync(IReadOnlyList<ProcessingTask> tasks,
        ProcessingExecutionOptions options, IProgress<ProcessingProgress>? progress, CancellationToken cancellationToken)
    {
        if (!_ffmpeg.IsFFmpegAvailable) throw new InvalidOperationException(LocalizationService.T("Status.NotConfigured"));
        var watch = Stopwatch.StartNew();
        var results = new List<ProcessingResult>();
        var failures = new List<ProcessingFailure>();
        var reserved = new HashSet<string>(tasks.Select(t => Path.GetFullPath(t.InputPath)), PathIdentity.Comparer);
        var inputPaths = new HashSet<string>(reserved, PathIdentity.Comparer);
        var completed = 0;
        var cancelled = false;
        var snapshots = tasks.ToDictionary(task => task, task => new ProcessingTask(task.InputPath, task.Settings.Clone(), task.SettingsScope)
        {
            UsesRelativeTarget = task.UsesRelativeTarget, RelativeTargetPercentage = task.RelativeTargetPercentage,
            MinimumVideoBitrateKbps = task.MinimumVideoBitrateKbps, MaximumVideoBitrateKbps = task.MaximumVideoBitrateKbps
        });
        // Reserve all destinations before launching work, including source files belonging to other tasks.
        var planned = new Dictionary<ProcessingTask, string>();
        foreach (var task in tasks)
        {
            try
            {
                var path = Path.GetFullPath(BuildCommand(snapshots[task], options).OutputPath);
                if (inputPaths.Contains(path)) throw new IOException(LocalizationService.T("Improve.SourceConflict"));
                if (options.OutputConflictPolicy == OutputConflictPolicy.AutoRename || reserved.Contains(path))
                {
                    var basePath = path;
                    var suffix = 1;
                    while (File.Exists(path) || reserved.Contains(path))
                        path = Path.Combine(Path.GetDirectoryName(basePath)!, $"{Path.GetFileNameWithoutExtension(basePath)} ({suffix++}){Path.GetExtension(basePath)}");
                }
                reserved.Add(path);
                planned[task] = path;
            }
            catch (Exception ex)
            {
                failures.Add(new ProcessingFailure(task, ex));
                progress?.Report(new ProcessingProgress(task, ProcessingTaskState.Failed, ++completed, tasks.Count, Message: ex.Message));
            }
        }
        var concurrency = tasks.All(t => t.Settings.IsImageProcessing) ? Math.Clamp(options.ImageParallelism, 1, 4) : 1;
        using var slots = new SemaphoreSlim(concurrency);
        var gate = new object();
        await Task.WhenAll(planned.Select(async entry =>
        {
            var task = entry.Key;
            try { await slots.WaitAsync(cancellationToken); }
            catch (OperationCanceledException) { lock (gate) cancelled = true; return; }
            try
            {
                progress?.Report(new ProcessingProgress(task, ProcessingTaskState.Running, Volatile.Read(ref completed), tasks.Count,
                    Message: LocalizationService.T("Improve.Analyzing")));
                var input = await _analyzer.AnalyzeVideo(task.InputPath, cancellationToken);
                if (input == null) throw new InvalidDataException(LocalizationService.T("Improve.AnalysisFailed"));
                var command = BuildCommand(snapshots[task], options, input);
                command.OutputPath = entry.Value;
                var result = await RunAsync(command, input, options.OutputConflictPolicy == OutputConflictPolicy.Overwrite,
                    (fraction, speed, remaining, stage) => progress?.Report(new ProcessingProgress(task, ProcessingTaskState.Running,
                        Volatile.Read(ref completed), tasks.Count, Message: stage, Fraction: fraction, Speed: speed, RemainingSeconds: remaining)), cancellationToken);
                lock (gate) results.Add(result);
                var count = Interlocked.Increment(ref completed);
                progress?.Report(new ProcessingProgress(task, string.IsNullOrEmpty(result.Warning) ? ProcessingTaskState.Completed : ProcessingTaskState.Warning,
                    count, tasks.Count, result.OutputPath, result.Warning, 1));
            }
            catch (OperationCanceledException)
            {
                lock (gate) cancelled = true;
                progress?.Report(new ProcessingProgress(task, ProcessingTaskState.Cancelled, Volatile.Read(ref completed), tasks.Count));
            }
            catch (Exception ex)
            {
                lock (gate) failures.Add(new ProcessingFailure(task, ex));
                progress?.Report(new ProcessingProgress(task, ProcessingTaskState.Failed, Interlocked.Increment(ref completed), tasks.Count, Message: ex.Message));
            }
            finally { slots.Release(); }
        }));
        return new ProcessingExecutionSummary(results, failures, watch.Elapsed, cancelled || cancellationToken.IsCancellationRequested);
    }

    private async Task<ProcessingResult> RunAsync(FFmpegCommand command, VideoInfo input, bool overwrite,
        Action<double, double, double, string> report, CancellationToken token)
    {
        var destination = command.OutputPath;
        using var transaction = new OutputTransaction(destination, overwrite);
        command.OutputPath = transaction.TemporaryPath;
        var warning = "";
        try
        {
            var diagnostics = OutputDiagnostics.Check(command, command.TargetBytes);
            if (!diagnostics.CanWrite) throw new IOException(diagnostics.Summary);
            AppLogger.Info($"Processing {command.InputPath} → {destination}");
            if (command.StreamCopy && Path.GetExtension(destination).Equals(".webm", StringComparison.OrdinalIgnoreCase) &&
                (input.VideoCodec is not ("vp8" or "vp9" or "av1") || input.HasAudio && command.AudioTrackMode != "remove" && input.AudioCodec is not ("opus" or "vorbis")))
                throw new ArgumentException(LocalizationService.T("Improve.CopyContainer"));
            if (!command.ImageOutput && !command.StreamCopy && !command.AudioOnly && !string.IsNullOrEmpty(command.HardwareEncoder))
            {
                report(0, 0, 0, LocalizationService.T("Improve.HardwareCheck"));
                if (!await CheckHardwareAsync(command, token))
                {
                    if (!command.AllowHardwareFallback) throw new InvalidOperationException(LocalizationService.T("Improve.HardwareFailed"));
                    command.HardwareEncoder = "";
                    warning = LocalizationService.T("Improve.SoftwareFallback");
                }
            }
            if (command.ImageOutput && command.ImageTargetSizeKB > 0 && command.ImageFormat is not ("ico" or "icns"))
                await EncodeImageAsync(command, input, report, token);
            else
            {
                if (command.TwoPass && !command.ImageOutput && !command.AudioOnly)
                {
                    command.PassLog = transaction.TemporaryPath + ".pass";
                    command.Pass = 1;
                    command.NullOutput = true;
                    await EncodeAsync(command, (f, s, r, _) => report(f * 0.45, s, r, LocalizationService.T("Improve.Pass1")), token);
                    command.Pass = 2;
                    command.NullOutput = false;
                }
                await EncodeAsync(command, (f, s, r, stage) => report(command.Pass == 2 ? 0.45 + f * 0.5 : f * 0.95, s, r, stage), token);
                command.Pass = 0;
                if (command.TargetBytes > 0 && new FileInfo(command.OutputPath).Length > command.TargetBytes)
                {
                    var ratio = command.TargetBytes / (double)new FileInfo(command.OutputPath).Length;
                    command.Bitrate = Math.Max(16, (int)(command.Bitrate * ratio * 0.92));
                    report(0, 0, 0, LocalizationService.T("Improve.Correcting"));
                    await EncodeAsync(command, report, token);
                }
            }
            token.ThrowIfCancellationRequested();
            if (command.ClearMetadata)
            {
                report(0.96, 0, 0, LocalizationService.T("Improve.Metadata"));
                var clear = await _exif.ClearMetadata(command.OutputPath, token);
                if (!clear.Success) throw new IOException(clear.Error);
            }
            report(0.98, 0, 0, LocalizationService.T("Improve.Validating"));
            var output = await _analyzer.AnalyzeVideo(command.OutputPath, token);
            if (output == null || output.FileSize == 0 || !command.AudioOnly && string.IsNullOrEmpty(output.VideoCodec) || command.AudioOnly && !output.HasAudio)
                throw new InvalidDataException(LocalizationService.T("Improve.InvalidOutput"));
            var target = command.ImageOutput ? (long)(command.ImageTargetSizeKB * 1024) : command.TargetBytes;
            if (command.UsedHardwareFallback) warning = JoinWarning(warning, LocalizationService.T("Improve.SoftwareFallback"));
            if (target > 0 && output.FileSize > target)
                warning = JoinWarning(warning, LocalizationService.T("Improve.OverTarget"));
            token.ThrowIfCancellationRequested();
            transaction.Commit();
            output.FilePath = destination;
            return new ProcessingResult(command.InputPath, destination, command, input, output, warning);
        }
        finally
        {
            command.OutputPath = destination;
            if (!string.IsNullOrWhiteSpace(command.PassLog))
                foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(command.PassLog)!, Path.GetFileName(command.PassLog) + "*"))
                    try { File.Delete(file); } catch (IOException ex) { AppLogger.Warn(ex.Message); }
            command.Pass = 0; command.NullOutput = false;
        }
    }

    private static string JoinWarning(string first, string second) => string.IsNullOrEmpty(first) ? second : first + " · " + second;

    private async Task<bool> CheckHardwareAsync(FFmpegCommand command, CancellationToken token)
    {
        var key = _ffmpeg.FFmpegPath + "|" + command.HardwareEncoder + "|" + command.UseCrf;
        if (_hardwareChecks.TryGetValue(key, out var available)) return available;
        var args = new List<string> { "-v", "error", "-f", "lavfi", "-i", "color=s=128x128:d=0.1", "-frames:v", "1", "-c:v", command.HardwareEncoder };
        if (command.HardwareEncoder.EndsWith("_vaapi", StringComparison.Ordinal))
            args.InsertRange(0, new[] { "-vaapi_device", "/dev/dri/renderD128" });
        if (command.HardwareEncoder.EndsWith("_vaapi", StringComparison.Ordinal)) args.AddRange(new[] { "-vf", "format=nv12,hwupload" });
        if (command.UseCrf) args.AddRange(CommandArguments.Parse(EncodingPolicy.QualityArguments(command.HardwareEncoder, command.Crf)));
        args.AddRange(new[] { "-f", "null", "-" });
        try { available = (await ProcessRunner.RunAsync(_ffmpeg.FFmpegPath, args, token, TimeSpan.FromSeconds(15))).ExitCode == 0; }
        catch (TimeoutException) { available = false; }
        _hardwareChecks[key] = available;
        return available;
    }

    private async Task EncodeAsync(FFmpegCommand command, Action<double, double, double, string> report, CancellationToken token)
    {
        var args = CommandArguments.Parse(command.BuildCommand()).Skip(1).ToList();
        args.InsertRange(0, new[] { "-nostdin", "-y", "-hide_banner", "-nostats", "-progress", "pipe:1" });
        double seconds = 0, speed = 0;
        var result = await ProcessRunner.RunAsync(_ffmpeg.FFmpegPath, args, token, onOutput: line =>
        {
            var pair = line.Split('=', 2);
            if (pair.Length != 2) return;
            if (pair[0] == "out_time_us" && double.TryParse(pair[1], CultureInfo.InvariantCulture, out var micros)) seconds = micros / 1_000_000;
            if (pair[0] == "speed") double.TryParse(pair[1].TrimEnd('x'), CultureInfo.InvariantCulture, out speed);
            if (pair[0] == "progress") report(command.Duration > 0 ? Math.Clamp(seconds / command.Duration, 0, 0.99) : 0,
                speed, speed > 0 ? Math.Max(0, command.Duration - seconds) / speed : 0, LocalizationService.T("Improve.Encoding"));
        });
        if (result.ExitCode != 0)
        {
            if (!string.IsNullOrEmpty(command.HardwareEncoder) && command.AllowHardwareFallback)
            {
                command.HardwareEncoder = "";
                command.UsedHardwareFallback = true;
                await EncodeAsync(command, report, token);
                return;
            }
            throw new ProcessingExecutionException(LocalizationService.T("Improve.EncodeFailed"), result.ExitCode, result.Error, command.BuildCommand(), command.OutputPath);
        }
    }

    private async Task EncodeImageAsync(FFmpegCommand command, VideoInfo input, Action<double, double, double, string> report, CancellationToken token)
    {
        var target = (long)(command.ImageTargetSizeKB * 1024);
        if (command.ImageFormat == "png") command.PngCompressionLevel = 9;
        var originalQuality = command.ImageQuality;
        var originalHeight = command.MaxHeight;
        var best = command.OutputPath + ".best";
        long bestSize = long.MaxValue;
        var bestQuality = originalQuality;
        var bestHeight = originalHeight;
        var attempts = 0;
        var height = originalHeight > 0 ? originalHeight : int.TryParse(input.Resolution.Split('x').Last(), out var h) ? h : 0;
        try
        {
            for (var resize = 0; resize < (command.AllowImageResize && height > 128 ? 4 : 1); resize++)
            {
                var low = 1;
                var high = originalQuality;
                if (resize > 0) command.MaxHeight = Math.Max(128, (int)(height * Math.Pow(0.7, resize)));
                var quality = originalQuality;
                for (var iteration = 0; iteration < (command.ImageFormat == "png" ? 1 : 7) && low <= high; iteration++)
                {
                    command.ImageQuality = command.ImageFormat == "png" ? 1 : quality;
                    report(attempts++ / 28.0, 0, 0, LocalizationService.Format("Improve.ImageAttempt", attempts));
                    await EncodeAsync(command, (_, _, _, _) => { }, token);
                    var size = new FileInfo(command.OutputPath).Length;
                    if (size <= target || bestSize > target && size < bestSize)
                    {
                        File.Copy(command.OutputPath, best, true);
                        bestSize = size; bestQuality = command.ImageQuality; bestHeight = command.MaxHeight;
                    }
                    if (size <= target) low = quality + 1;
                    else high = quality - 1;
                    quality = (low + high) / 2;
                }
                if (bestSize <= target) break;
            }
            File.Move(best, command.OutputPath, true);
            command.ImageQuality = bestQuality;
            command.MaxHeight = bestHeight;
        }
        finally { if (File.Exists(best)) File.Delete(best); }
    }
}
