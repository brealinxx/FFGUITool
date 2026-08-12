using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Models;

namespace FFGUITool.Services
{
    /// <summary>
    /// Executes prepared processing tasks and reports lifecycle changes.
    /// UI selection and dialogs deliberately stay outside this service.
    /// </summary>
    public sealed class ProcessingExecutor
    {
        private readonly FFmpegManager _ffmpegManager;
        private readonly ExifToolManager _exifToolManager;
        private readonly VideoAnalyzer _videoAnalyzer;
        private readonly CommandBuilder _commandBuilder;

        public ProcessingExecutor(
            FFmpegManager ffmpegManager,
            ExifToolManager exifToolManager,
            VideoAnalyzer videoAnalyzer,
            CommandBuilder commandBuilder)
        {
            _ffmpegManager = ffmpegManager;
            _exifToolManager = exifToolManager;
            _videoAnalyzer = videoAnalyzer;
            _commandBuilder = commandBuilder;
        }

        public IReadOnlyList<string> GetPlannedOutputPaths(
            IEnumerable<ProcessingTask> tasks,
            ProcessingExecutionOptions options)
        {
            return tasks
                .Select(task => BuildCommand(task, options).OutputPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();
        }

        public FFmpegCommand BuildCommand(
            ProcessingTask task,
            ProcessingExecutionOptions options,
            VideoInfo? inputInfo = null)
        {
            var settings = ResolveSettings(task, inputInfo, options.AvailableVideoDecoders);
            return _commandBuilder.BuildCommand(settings, inputInfo);
        }

        public async Task<ProcessingExecutionSummary> ExecuteAsync(
            IReadOnlyList<ProcessingTask> tasks,
            ProcessingExecutionOptions options,
            IProgress<ProcessingProgress>? progress,
            CancellationToken cancellationToken)
        {
            if (!_ffmpegManager.IsFFmpegAvailable)
            {
                throw new InvalidOperationException(LocalizationService.T("Status.NotConfigured"));
            }

            var stopwatch = Stopwatch.StartNew();
            var results = new List<ProcessingResult>();
            var failures = new List<ProcessingFailure>();
            var reservedOutputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var completed = 0;
            var wasCancelled = false;

            foreach (var task in tasks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }

                progress?.Report(new ProcessingProgress(task, ProcessingTaskState.Running, completed, tasks.Count));
                try
                {
                    var inputInfo = await GetInputInfo(task.InputPath);
                    cancellationToken.ThrowIfCancellationRequested();
                    var command = BuildCommand(task, options, inputInfo);
                    PrepareOutputPath(command, options.OutputConflictPolicy, reservedOutputPaths);
                    var result = await RunFFmpegCommand(command, inputInfo, cancellationToken);
                    results.Add(result);
                    completed++;
                    progress?.Report(new ProcessingProgress(
                        task,
                        ProcessingTaskState.Completed,
                        completed,
                        tasks.Count,
                        result.OutputPath));
                }
                catch (OperationCanceledException)
                {
                    completed++;
                    wasCancelled = true;
                    progress?.Report(new ProcessingProgress(
                        task,
                        ProcessingTaskState.Cancelled,
                        completed,
                        tasks.Count));
                    break;
                }
                catch (Exception ex)
                {
                    failures.Add(new ProcessingFailure(task, ex));
                    completed++;
                    progress?.Report(new ProcessingProgress(
                        task,
                        ProcessingTaskState.Failed,
                        completed,
                        tasks.Count,
                        Message: ex.Message));
                }
            }

            stopwatch.Stop();
            return new ProcessingExecutionSummary(results, failures, stopwatch.Elapsed, wasCancelled);
        }

        private CompressionSettings ResolveSettings(
            ProcessingTask task,
            VideoInfo? inputInfo,
            IReadOnlySet<string> availableVideoDecoders)
        {
            var settings = task.Settings.Clone();
            settings.InputPath = task.InputPath;
            settings.InputVideoCodec = inputInfo?.VideoCodec ?? settings.InputVideoCodec;
            settings.PreferDav1dDecoder = string.Equals(settings.InputVideoCodec, "av1", StringComparison.OrdinalIgnoreCase) &&
                                         availableVideoDecoders.Contains("libdav1d");

            if (task.UsesRelativeTarget)
            {
                ApplyRelativeTarget(settings, task, inputInfo);
            }

            return settings;
        }

        private void ApplyRelativeTarget(
            CompressionSettings settings,
            ProcessingTask task,
            VideoInfo? inputInfo)
        {
            var sourceBytes = inputInfo?.FileSize > 0
                ? inputInfo.FileSize
                : File.Exists(task.InputPath)
                    ? new FileInfo(task.InputPath).Length
                    : 0;
            var percentage = Math.Clamp(task.RelativeTargetPercentage, 1, 100);
            var ratio = percentage / 100.0;

            if (settings.IsImageProcessing)
            {
                settings.ImageTargetSizeKB = sourceBytes > 0
                    ? Math.Max(1, sourceBytes / 1024.0 * ratio)
                    : Math.Max(1, percentage);
                settings.OutputLabel = $"ratio{percentage:0.0}pct";
                return;
            }

            settings.TargetSizeMB = sourceBytes > 0
                ? Math.Max(0.1, sourceBytes / 1024.0 / 1024.0 * ratio)
                : Math.Max(0.1, percentage);
            settings.OutputLabel = settings.UseCrf ? $"crf{settings.Crf}" : $"ratio{percentage:0.0}pct";

            if (settings.UseCrf || inputInfo == null || inputInfo.Duration <= 0)
            {
                return;
            }

            var targetBitrate = _commandBuilder.CalculateBitrateForTargetSize(inputInfo, settings.TargetSizeMB);
            if (task.MinimumVideoBitrateKbps > 0)
            {
                targetBitrate = Math.Max(targetBitrate, task.MinimumVideoBitrateKbps);
            }

            if (task.MaximumVideoBitrateKbps > 0)
            {
                targetBitrate = Math.Min(targetBitrate, task.MaximumVideoBitrateKbps);
            }

            settings.Bitrate = targetBitrate;
        }

        private static void PrepareOutputPath(
            FFmpegCommand command,
            OutputConflictPolicy conflictPolicy,
            ISet<string> reservedOutputPaths)
        {
            var outputPath = command.OutputPath;
            if (conflictPolicy == OutputConflictPolicy.AutoRename)
            {
                var directory = Path.GetDirectoryName(outputPath) ?? "";
                var name = Path.GetFileNameWithoutExtension(outputPath);
                var extension = Path.GetExtension(outputPath);
                var suffix = 1;
                while (File.Exists(outputPath) || reservedOutputPaths.Contains(outputPath))
                {
                    outputPath = Path.Combine(directory, $"{name} ({suffix++}){extension}");
                }

                command.OutputPath = outputPath;
            }

            reservedOutputPaths.Add(command.OutputPath);
        }

        private async Task<VideoInfo?> GetInputInfo(string inputPath)
        {
            var inputInfo = await _videoAnalyzer.AnalyzeVideo(inputPath);
            if (inputInfo == null && File.Exists(inputPath))
            {
                inputInfo = new VideoInfo
                {
                    FilePath = inputPath,
                    FileSize = new FileInfo(inputPath).Length
                };
            }

            return inputInfo;
        }

        private async Task<ProcessingResult> RunFFmpegCommand(
            FFmpegCommand command,
            VideoInfo? inputInfo,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var expectedOutputBytes = EstimateExpectedOutputBytes(command, inputInfo);
            var outputDiagnostics = OutputDiagnostics.Check(command, expectedOutputBytes);
            AppLogger.Info($"Output diagnostics before FFmpeg run:{Environment.NewLine}{outputDiagnostics.Summary}");
            if (!outputDiagnostics.CanWrite)
            {
                throw new ProcessingExecutionException(
                    "Output path check failed before running FFmpeg.",
                    1,
                    outputDiagnostics.Summary,
                    command.BuildCommand(),
                    command.OutputPath,
                    outputDiagnostics.Summary);
            }

            var output = command.ImageOutput && command.ImageTargetSizeKB > 0 && !IsIconFormat(command.ImageFormat)
                ? await RunImageCommandWithTargetSize(command, inputInfo, cancellationToken)
                : await RunSingleFFmpegCommand(command, cancellationToken);

            if (output.ExitCode != 0)
            {
                var av1Message = BuildAv1DecodeFailureMessage(command, output.Error);
                throw new ProcessingExecutionException(
                    av1Message ?? $"FFmpeg执行失败，退出代码: {output.ExitCode}",
                    output.ExitCode,
                    output.Error,
                    command.BuildCommand(),
                    command.OutputPath,
                    outputDiagnostics.Summary);
            }

            if (command.ClearMetadata && _exifToolManager.IsExifToolAvailable)
            {
                var clearResult = await _exifToolManager.ClearMetadata(command.OutputPath);
                if (!clearResult.Success)
                {
                    throw new ProcessingExecutionException(
                        "ExifTool元数据清除失败",
                        1,
                        clearResult.Error,
                        command.BuildCommand(),
                        command.OutputPath,
                        outputDiagnostics.Summary);
                }
            }

            var outputInfo = await _videoAnalyzer.AnalyzeVideo(command.OutputPath);
            if (outputInfo == null && File.Exists(command.OutputPath))
            {
                outputInfo = new VideoInfo
                {
                    FilePath = command.OutputPath,
                    FileSize = new FileInfo(command.OutputPath).Length
                };
            }

            return new ProcessingResult(command.InputPath, command.OutputPath, command, inputInfo, outputInfo);
        }

        private static long EstimateExpectedOutputBytes(FFmpegCommand command, VideoInfo? inputInfo)
        {
            if (command.ImageOutput && command.ImageTargetSizeKB > 0)
            {
                return (long)(command.ImageTargetSizeKB * 1024);
            }

            if (!command.UseCrf && command.Bitrate > 0 && inputInfo?.Duration > 0)
            {
                var totalKbps = command.AudioOnly ? command.AudioBitrate : command.Bitrate + command.AudioBitrate;
                return (long)(totalKbps * 1024.0 * inputInfo.Duration / 8.0);
            }

            return inputInfo?.FileSize > 0 ? inputInfo.FileSize : 0;
        }

        private static string? BuildAv1DecodeFailureMessage(FFmpegCommand command, string ffmpegOutput)
        {
            if (!string.Equals(command.InputVideoCodec, "av1", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var output = ffmpegOutput ?? "";
            var hasKnownFailure = output.Contains("libaom-av1", StringComparison.OrdinalIgnoreCase) ||
                                  output.Contains("frame=    0", StringComparison.OrdinalIgnoreCase) ||
                                  output.Contains("frame=0", StringComparison.OrdinalIgnoreCase) ||
                                  output.Contains("Nothing was written into output file", StringComparison.OrdinalIgnoreCase);

            return hasKnownFailure
                ? "源视频 AV1 解码失败，可能是录屏文件损坏或当前 FFmpeg 构建兼容性不足，建议更换 full build FFmpeg 或重新导出/修复源视频。"
                : null;
        }

        private async Task<(int ExitCode, string Error)> RunImageCommandWithTargetSize(
            FFmpegCommand command,
            VideoInfo? inputInfo,
            CancellationToken cancellationToken)
        {
            var targetBytes = (long)Math.Max(1, command.ImageTargetSizeKB * 1024);
            var baseHeight = command.MaxHeight > 0 ? command.MaxHeight : TryParseHeight(inputInfo?.Resolution);
            var lastResult = (ExitCode: 0, Error: "");

            foreach (var quality in BuildImageQualityAttempts(command.ImageQuality))
            {
                cancellationToken.ThrowIfCancellationRequested();
                command.ImageQuality = quality;
                lastResult = await RunSingleFFmpegCommand(command, cancellationToken);
                if (lastResult.ExitCode != 0 || IsOutputWithinTarget(command.OutputPath, targetBytes))
                {
                    return lastResult;
                }
            }

            if (baseHeight <= 0)
            {
                return lastResult;
            }

            var currentHeight = Math.Max(128, (int)Math.Round(baseHeight * 0.85));
            while (currentHeight >= 128)
            {
                command.MaxHeight = currentHeight;
                foreach (var quality in new[] { 35, 25, 15, 8, 1 })
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    command.ImageQuality = quality;
                    lastResult = await RunSingleFFmpegCommand(command, cancellationToken);
                    if (lastResult.ExitCode != 0 || IsOutputWithinTarget(command.OutputPath, targetBytes))
                    {
                        return lastResult;
                    }
                }

                currentHeight = (int)Math.Round(currentHeight * 0.85);
            }

            return lastResult;
        }

        private async Task<(int ExitCode, string Error)> RunSingleFFmpegCommand(
            FFmpegCommand command,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(command.OutputPath))
            {
                File.Delete(command.OutputPath);
            }

            var commandText = command.BuildCommand();
            AppLogger.Info($"Running FFmpeg command:{Environment.NewLine}{commandText}");
            var arguments = commandText.StartsWith("ffmpeg ", StringComparison.OrdinalIgnoreCase)
                ? commandText["ffmpeg ".Length..]
                : commandText;
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegManager.FFmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            using var cancellationRegistration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Failed to stop FFmpeg process.", ex);
                }
            });

            string error;
            try
            {
                await process.WaitForExitAsync(cancellationToken);
                error = await errorTask;
            }
            catch (OperationCanceledException)
            {
                TryDeletePartialOutput(command.OutputPath);
                throw;
            }

            AppLogger.Info($"FFmpeg exited with code {process.ExitCode}.");
            var errorSummary = AppLogger.Summarize(error);
            if (!string.IsNullOrWhiteSpace(errorSummary))
            {
                AppLogger.Info($"FFmpeg stderr summary:{Environment.NewLine}{errorSummary}");
            }

            return (process.ExitCode, error);
        }

        private static void TryDeletePartialOutput(string outputPath)
        {
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to delete partial output: {outputPath}", ex);
            }
        }

        private static IEnumerable<int> BuildImageQualityAttempts(int startQuality)
        {
            var quality = Math.Clamp(startQuality, 1, 100);
            for (var current = quality; current >= 1; current -= 8)
            {
                yield return current;
            }

            if (quality != 1)
            {
                yield return 1;
            }
        }

        private static bool IsOutputWithinTarget(string outputPath, long targetBytes)
        {
            return File.Exists(outputPath) && new FileInfo(outputPath).Length <= targetBytes;
        }

        private static int TryParseHeight(string? resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
            {
                return 0;
            }

            var parts = resolution.Split('x', 'X');
            return parts.Length == 2 && int.TryParse(parts[1], out var height) ? height : 0;
        }

        private static bool IsIconFormat(string format)
        {
            return format.Equals("ico", StringComparison.OrdinalIgnoreCase) ||
                   format.Equals("icns", StringComparison.OrdinalIgnoreCase);
        }
    }
}
