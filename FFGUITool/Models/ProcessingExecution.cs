using System;
using System.Collections.Generic;

namespace FFGUITool.Models
{
    public enum ProcessingTaskState
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public enum OutputConflictPolicy
    {
        AutoRename,
        Overwrite
    }

    public sealed record ProcessingProgress(
        ProcessingTask Task,
        ProcessingTaskState State,
        int CompletedCount,
        int TotalCount,
        string OutputPath = "",
        string Message = "");

    public sealed record ProcessingResult(
        string InputPath,
        string OutputPath,
        FFmpegCommand Command,
        VideoInfo? InputInfo,
        VideoInfo? OutputInfo);

    public sealed record ProcessingFailure(
        ProcessingTask Task,
        Exception Exception);

    public sealed record ProcessingExecutionSummary(
        IReadOnlyList<ProcessingResult> Results,
        IReadOnlyList<ProcessingFailure> Failures,
        TimeSpan Elapsed,
        bool WasCancelled);

    public sealed class ProcessingExecutionOptions
    {
        public OutputConflictPolicy OutputConflictPolicy { get; init; } = OutputConflictPolicy.AutoRename;
        public IReadOnlySet<string> AvailableVideoDecoders { get; init; } = new HashSet<string>();
    }

    public sealed class ProcessingExecutionException : Exception
    {
        public ProcessingExecutionException(
            string message,
            int exitCode,
            string ffmpegOutput,
            string commandText = "",
            string outputPath = "",
            string outputDiagnostics = "")
            : base(message)
        {
            ExitCode = exitCode;
            FFmpegOutput = ffmpegOutput;
            CommandText = commandText;
            OutputPath = outputPath;
            OutputDiagnostics = outputDiagnostics;
        }

        public int ExitCode { get; }
        public string FFmpegOutput { get; }
        public string CommandText { get; }
        public string OutputPath { get; }
        public string OutputDiagnostics { get; }
    }
}
