using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Models;

namespace FFGUITool.Services.Interfaces
{
    public interface IBatchProcessor
    {
        int MaxParallelTasks { get; set; }
        
        Task<BatchProcessResult> ProcessFolderAsync(string folderPath, ProcessingOptions options, IProgress<BatchProgress>? progress = null, CancellationToken cancellationToken = default);
        Task<BatchProcessResult> ProcessFileListAsync(IEnumerable<string> files, ProcessingOptions options, IProgress<BatchProgress>? progress = null, CancellationToken cancellationToken = default);
        IEnumerable<string> ScanFolder(string folderPath, string[] extensions, bool recursive = true);
    }

    public class BatchProgress
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks;
        public int FailedTasks;
        public ProcessingTask? CurrentTask { get; set; }
        public double OverallProgress { get; set; }
        public string CurrentFile { get; set; } = "";
    }

    public class BatchProcessResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount;
        public int FailureCount;
        public TimeSpan TotalDuration { get; set; }
        public List<ProcessingError> Errors { get; set; } = new();
    }

    public class ProcessingError
    {
        public string FilePath { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}