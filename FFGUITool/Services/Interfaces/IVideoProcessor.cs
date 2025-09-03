using System;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Models;
using FFGUITool.ViewModels;

namespace FFGUITool.Services.Interfaces
{
    public interface IVideoProcessor
    {
        Task<bool> CompressVideoAsync(VideoCompressionOptions options, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default);
        Task<bool> ConvertFormatAsync(string input, string output, VideoFormat format, CancellationToken cancellationToken = default);
        string GenerateCommand(VideoCompressionOptions options);
        long EstimateOutputSize(VideoCompressionOptions options);
        Task CompressVideoAsync(VideoCompressionSettings settings, IProgress<double>? progress = null);
    }

    public class ProcessingProgress
    {
        public double Percentage { get; set; }
        public TimeSpan TimeElapsed { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public string CurrentOperation { get; set; } = "";
        public long ProcessedBytes { get; set; }
        public long TotalBytes { get; set; }
    }
}