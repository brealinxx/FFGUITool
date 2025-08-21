using System;
using System.Threading;
using System.Threading.Tasks;

namespace FFGUITool.Services.Interfaces
{
    public interface IFFmpegService
    {
        bool IsAvailable { get; }
        string FFmpegPath { get; }
        
        Task<bool> InitializeAsync();
        Task<string> GetVersionAsync();
        Task<bool> SetCustomPathAsync(string path);
        Task<bool> InstallFromArchiveAsync(string archivePath);
        Task<ProcessResult> ExecuteAsync(string arguments, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    }

    public class ProcessResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public TimeSpan Duration { get; set; }
    }
}