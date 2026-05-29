using System;
using System.Collections.Generic;
using System.IO;
using FFGUITool.Models;

namespace FFGUITool.Services
{
    public sealed class OutputDiagnosticsResult
    {
        public OutputDiagnosticsResult(bool canWrite, string summary)
        {
            CanWrite = canWrite;
            Summary = summary;
        }

        public bool CanWrite { get; }
        public string Summary { get; }
    }

    public static class OutputDiagnostics
    {
        public static OutputDiagnosticsResult Check(FFmpegCommand command, long expectedBytes = 0)
        {
            var lines = new List<string>();

            if (string.IsNullOrWhiteSpace(command.OutputPath))
            {
                return new OutputDiagnosticsResult(false, "Output path is empty.");
            }

            var outputDirectory = Path.GetDirectoryName(command.OutputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                outputDirectory = Directory.GetCurrentDirectory();
            }

            lines.Add($"Output: {command.OutputPath}");
            lines.Add($"Output directory: {outputDirectory}");

            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                lines.Add($"Directory check failed: {ex.Message}");
                return new OutputDiagnosticsResult(false, string.Join(Environment.NewLine, lines));
            }

            try
            {
                var probePath = Path.Combine(outputDirectory, $".ffguitool-write-test-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probePath, "test");
                File.Delete(probePath);
                lines.Add("Permission: writable");
            }
            catch (Exception ex)
            {
                lines.Add($"Permission: not writable ({ex.Message})");
                return new OutputDiagnosticsResult(false, string.Join(Environment.NewLine, lines));
            }

            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(outputDirectory));
                if (!string.IsNullOrWhiteSpace(root))
                {
                    var drive = new DriveInfo(root);
                    lines.Add($"Free space: {VideoInfo.FormatFileSize(drive.AvailableFreeSpace)}");

                    if (expectedBytes > 0 && drive.AvailableFreeSpace < expectedBytes)
                    {
                        lines.Add($"Warning: free space is below expected output size ({VideoInfo.FormatFileSize(expectedBytes)}).");
                    }
                }
            }
            catch (Exception ex)
            {
                lines.Add($"Free space check unavailable: {ex.Message}");
            }

            return new OutputDiagnosticsResult(true, string.Join(Environment.NewLine, lines));
        }
    }
}
