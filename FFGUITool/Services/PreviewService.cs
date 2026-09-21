using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Models;

namespace FFGUITool.Services;

public sealed class PreviewArtifact : IDisposable
{
    public string DirectoryPath { get; init; } = "";
    public string BeforeImage { get; set; } = "";
    public string AfterImage { get; set; } = "";
    public string SamplePath { get; set; } = "";
    public string Summary { get; set; } = "";
    public void Dispose()
    {
        // This directory is created exclusively for this preview; no source files live here.
        if (!Path.GetFileName(DirectoryPath).StartsWith("FFGUITool-Preview-", StringComparison.Ordinal)) return;
        try { Directory.Delete(DirectoryPath, true); }
        catch (IOException ex) { AppLogger.Warn(ex.Message); }
        catch (UnauthorizedAccessException ex) { AppLogger.Warn(ex.Message); }
    }
}

public sealed class PreviewService
{
    private readonly FFmpegManager _ffmpeg;
    private readonly ProcessingExecutor _executor;
    private readonly VideoAnalyzer _analyzer;
    public PreviewService(FFmpegManager ffmpeg, ProcessingExecutor executor, VideoAnalyzer analyzer)
    { _ffmpeg = ffmpeg; _executor = executor; _analyzer = analyzer; }

    public async Task<PreviewArtifact> CreateAsync(CompressionSettings source, IReadOnlySet<string> decoders, CancellationToken token)
    {
        if (source.EnableAudioConversion) throw new ArgumentException(LocalizationService.T("Improve.NoAudioPreview"));
        var artifact = new PreviewArtifact { DirectoryPath = Path.Combine(Path.GetTempPath(), "FFGUITool-Preview-" + Guid.NewGuid().ToString("N")) };
        Directory.CreateDirectory(artifact.DirectoryPath);
        try
        {
            var info = await _analyzer.AnalyzeVideo(source.InputPath, token) ?? throw new InvalidDataException(LocalizationService.T("Improve.AnalysisFailed"));
            var settings = source.Clone();
            settings.OutputPath = artifact.DirectoryPath;
            settings.InputRoot = ""; settings.PreserveFolderStructure = false;
            settings.OutputNamePattern = "sample_{name}";
            if (!source.IsImageProcessing)
            {
                var duration = EncodingPolicy.OutputDuration(source, info);
                var start = source.EnableTrim ? EncodingPolicy.ParseTime(source.TrimStart) : 0;
                var sampleDuration = Math.Min(10, duration);
                settings.EnableTrim = true;
                settings.TrimStart = start.ToString(System.Globalization.CultureInfo.InvariantCulture);
                settings.TrimEnd = (start + sampleDuration).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (duration > 0) settings.TargetSizeMB *= sampleDuration / duration;
            }
            var task = new ProcessingTask(settings.InputPath, settings, ProcessingSettingsScope.Independent);
            var summary = await _executor.ExecuteAsync(new[] { task }, new ProcessingExecutionOptions { AvailableVideoDecoders = decoders }, null, token);
            token.ThrowIfCancellationRequested();
            if (summary.Failures.Count > 0) throw summary.Failures[0].Exception;
            var result = summary.Results.Single();
            artifact.SamplePath = result.OutputPath;
            artifact.BeforeImage = Path.Combine(artifact.DirectoryPath, "before.png");
            artifact.AfterImage = Path.Combine(artifact.DirectoryPath, "after.png");
            var seek = settings.EnableTrim ? settings.TrimStart : "0";
            await ThumbnailAsync(settings.InputPath, artifact.BeforeImage, seek, token);
            await ThumbnailAsync(result.OutputPath, artifact.AfterImage, "0", token);
            artifact.Summary = source.IsImageProcessing ? ProcessingResultFormatter.FormatSummary(summary) :
                LocalizationService.T("Improve.SampleNotice") + Environment.NewLine + VideoInfo.FormatFileSize(result.OutputInfo?.FileSize ?? 0) + " · " + result.Warning;
            return artifact;
        }
        catch { artifact.Dispose(); throw; }
    }

    private async Task ThumbnailAsync(string input, string output, string seek, CancellationToken token)
    {
        var result = await ProcessRunner.RunAsync(_ffmpeg.FFmpegPath, new[] { "-v", "error", "-y", "-ss", seek, "-i", input,
            "-frames:v", "1", "-vf", "scale=w='min(4096,iw)':h='min(4096,ih)':force_original_aspect_ratio=decrease", output }, token, TimeSpan.FromSeconds(30));
        if (result.ExitCode != 0) throw new IOException(result.Error);
    }
}
