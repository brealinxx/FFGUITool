using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Helpers;
using FFGUITool.Models;
using FFGUITool.Services.Interfaces;
using FFGUITool.ViewModels;

namespace FFGUITool.Services
{
    public class VideoProcessor : IVideoProcessor
    {
        private readonly IFFmpegService _ffmpegService;
        private readonly IMediaAnalyzer _mediaAnalyzer;

        public VideoProcessor(IFFmpegService ffmpegService, IMediaAnalyzer mediaAnalyzer)
        {
            _ffmpegService = ffmpegService;
            _mediaAnalyzer = mediaAnalyzer;
        }

        public async Task<bool> CompressVideoAsync(
            VideoCompressionOptions options, 
            IProgress<ProcessingProgress>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            if (!_ffmpegService.IsAvailable)
                throw new InvalidOperationException("FFmpeg is not available");

            // Analyze source video if not already done
            if (options.SourceVideoInfo == null)
            {
                options.SourceVideoInfo = await _mediaAnalyzer.AnalyzeVideoAsync(options.InputFile);
            }

            var command = GenerateCommand(options);
            
            // Create progress reporter
            var progressReporter = progress != null && options.SourceVideoInfo != null
                ? new FFmpegProgressParser(options.SourceVideoInfo.Duration, progress)
                : null;

            var result = await _ffmpegService.ExecuteAsync(
                command, 
                progressReporter, 
                cancellationToken);

            return result.Success;
        }

        public async Task<bool> ConvertFormatAsync(
            string input, 
            string output, 
            VideoFormat format, 
            CancellationToken cancellationToken = default)
        {
            var builder = new FFmpegCommandBuilder()
                .AddInput(input)
                .SetVideoCodec("copy")
                .SetAudioCodec("copy")
                .SetOutput(output);

            var result = await _ffmpegService.ExecuteAsync(
                builder.Build(), 
                null, 
                cancellationToken);

            return result.Success;
        }

        public string GenerateCommand(VideoCompressionOptions options)
        {
            var builder = new FFmpegCommandBuilder()
                .AddInput(options.InputFile)
                .SetVideoCodec(options.VideoCodec)
                .SetVideoBitrate(options.TargetBitrate)
                .SetAudioCodec(options.AudioCodec);

            // Add resolution if specified
            if (!string.IsNullOrEmpty(options.Resolution))
            {
                builder.SetScale(options.Resolution);
            }

            // Add framerate if specified
            if (options.FrameRate.HasValue)
            {
                builder.SetFrameRate(options.FrameRate.Value);
            }

            // Add preset if using x264/x265
            if (options.VideoCodec.Contains("x264") || options.VideoCodec.Contains("x265"))
            {
                var preset = options.Profile?.PresetSpeed ?? "medium";
                builder.AddParameter("-preset", preset);
            }

            builder.SetOutput(options.OutputFile);

            return builder.Build();
        }

        public long EstimateOutputSize(VideoCompressionOptions options)
        {
            if (options.SourceVideoInfo == null)
                return 0;

            // Calculate estimated size: (bitrate * duration) / 8
            // Add 12% for audio and overhead
            var videoSizeBytes = (long)(options.TargetBitrate * 1024 * options.SourceVideoInfo.Duration.TotalSeconds / 8);
            var totalSizeBytes = (long)(videoSizeBytes * 1.12);

            return totalSizeBytes;
        }

        public Task CompressVideoAsync(VideoCompressionSettings settings, IProgress<double>? progress = null)
        {
            throw new NotImplementedException();
        }
    }

    // Progress parser helper class
    internal class FFmpegProgressParser : IProgress<double>
    {
        private readonly TimeSpan _totalDuration;
        private readonly IProgress<ProcessingProgress> _progress;
        private DateTime _startTime;

        public FFmpegProgressParser(TimeSpan totalDuration, IProgress<ProcessingProgress> progress)
        {
            _totalDuration = totalDuration;
            _progress = progress;
            _startTime = DateTime.Now;
        }

        public void Report(double currentSeconds)
        {
            var percentage = (_totalDuration.TotalSeconds > 0) 
                ? (currentSeconds / _totalDuration.TotalSeconds) * 100 
                : 0;

            var elapsed = DateTime.Now - _startTime;
            var estimatedTotal = percentage > 0 
                ? TimeSpan.FromSeconds(elapsed.TotalSeconds * 100 / percentage) 
                : TimeSpan.Zero;
            var remaining = estimatedTotal - elapsed;

            _progress.Report(new ProcessingProgress
            {
                Percentage = Math.Min(100, percentage),
                TimeElapsed = elapsed,
                TimeRemaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero,
                CurrentOperation = "Compressing video..."
            });
        }
    }
}