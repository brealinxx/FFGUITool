using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFGUITool.Models;

namespace FFGUITool.Services
{
    public static class ProcessingResultFormatter
    {
        public static string FormatSummary(ProcessingExecutionSummary summary)
        {
            var lines = new List<string>
            {
                LocalizationService.Format(
                    "Queue.Summary",
                    summary.Results.Count,
                    summary.Failures.Count,
                    summary.WasCancelled ? 1 : 0),
                LocalizationService.Format("Result.Elapsed", FormatDuration(summary.Elapsed))
            };

            foreach (var result in summary.Results)
            {
                lines.Add("");
                lines.Add(Path.GetFileName(result.InputPath));
                lines.Add(LocalizationService.Format("Result.Output", result.OutputPath));

                if (result.InputInfo != null && result.OutputInfo != null)
                {
                    lines.Add(LocalizationService.Format(
                        "Result.SizeCompare",
                        VideoInfo.FormatFileSize(result.InputInfo.FileSize),
                        VideoInfo.FormatFileSize(result.OutputInfo.FileSize),
                        FormatSizeChange(result.InputInfo.FileSize, result.OutputInfo.FileSize)));

                    if (result.Command.ImageOutput)
                    {
                        lines.Add(LocalizationService.Format(
                            "Result.ImageFormat",
                            GetDisplayExtension(result.InputPath),
                            GetDisplayExtension(result.OutputPath)));
                        if (!string.IsNullOrWhiteSpace(result.InputInfo.Resolution) ||
                            !string.IsNullOrWhiteSpace(result.OutputInfo.Resolution))
                        {
                            lines.Add(LocalizationService.Format(
                                "Result.Resolution",
                                string.IsNullOrWhiteSpace(result.InputInfo.Resolution)
                                    ? LocalizationService.T("Result.Unknown")
                                    : result.InputInfo.Resolution,
                                string.IsNullOrWhiteSpace(result.OutputInfo.Resolution)
                                    ? LocalizationService.T("Result.Unknown")
                                    : result.OutputInfo.Resolution));
                        }

                        continue;
                    }

                    if (result.Command.AudioOnly)
                    {
                        lines.Add(LocalizationService.Format(
                            "Result.AudioFormat",
                            GetDisplayExtension(result.InputPath),
                            GetDisplayExtension(result.OutputPath)));
                        lines.Add(LocalizationService.Format("Result.AudioBitrate", $"{result.Command.AudioBitrate} kb/s"));
                        continue;
                    }

                    if (result.InputInfo.Bitrate > 0 || result.OutputInfo.Bitrate > 0)
                    {
                        lines.Add(LocalizationService.Format(
                            "Result.BitrateCompare",
                            FormatBitrate(result.InputInfo.Bitrate),
                            FormatBitrate(result.OutputInfo.Bitrate)));
                    }
                }
                else if (result.OutputInfo != null)
                {
                    lines.Add(LocalizationService.Format(
                        "Result.OutputSize",
                        VideoInfo.FormatFileSize(result.OutputInfo.FileSize)));
                }
            }

            foreach (var failure in summary.Failures)
            {
                lines.Add("");
                lines.Add($"{failure.Task.FileName} — {LocalizationService.T("SourceTabs.Failed")}");
                lines.Add(failure.Exception.Message);
            }

            if (summary.WasCancelled)
            {
                lines.Add("");
                lines.Add(LocalizationService.T("Queue.RemainingNotProcessed"));
            }

            return string.Join(Environment.NewLine, lines);
        }

        public static string FormatFailureDetails(ProcessingExecutionException exception)
        {
            var lines = new List<string>
            {
                exception.Message,
                $"Exit code: {exception.ExitCode}"
            };

            if (!string.IsNullOrWhiteSpace(exception.CommandText))
            {
                lines.Add("");
                lines.Add("Command:");
                lines.Add(exception.CommandText);
            }

            if (!string.IsNullOrWhiteSpace(exception.OutputPath))
            {
                lines.Add("");
                lines.Add($"Output path: {exception.OutputPath}");
            }

            if (!string.IsNullOrWhiteSpace(exception.OutputDiagnostics))
            {
                lines.Add("");
                lines.Add("Diagnostics:");
                lines.Add(exception.OutputDiagnostics);
            }

            if (!string.IsNullOrWhiteSpace(exception.FFmpegOutput))
            {
                lines.Add("");
                lines.Add("stderr summary:");
                lines.Add(TrimFFmpegOutput(exception.FFmpegOutput));
                lines.Add("");
                lines.Add("stderr full:");
                lines.Add(exception.FFmpegOutput);
            }

            lines.Add("");
            lines.Add($"Log file: {AppLogger.CurrentLogPath}");
            return string.Join(Environment.NewLine, lines);
        }

        public static string GetDisplayExtension(string path)
        {
            var extension = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
            return string.IsNullOrWhiteSpace(extension) ? LocalizationService.T("Result.Unknown") : extension;
        }

        private static string TrimFFmpegOutput(string output)
        {
            var lines = output
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .TakeLast(12);
            return string.Join(Environment.NewLine, lines);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return duration.TotalHours >= 1 ? duration.ToString(@"hh\:mm\:ss") : duration.ToString(@"mm\:ss");
        }

        private static string FormatSizeChange(long before, long after)
        {
            if (before <= 0)
            {
                return LocalizationService.T("Result.ChangeUnavailable");
            }

            var percentage = (after - before) * 100.0 / before;
            return percentage <= 0
                ? LocalizationService.Format("Result.Reduced", Math.Abs(percentage))
                : LocalizationService.Format("Result.Increased", percentage);
        }

        private static string FormatBitrate(int bitrate)
        {
            return bitrate > 0 ? $"{bitrate} kb/s" : LocalizationService.T("Result.Unknown");
        }
    }
}
