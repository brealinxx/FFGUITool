using System;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Helpers;
using FFGUITool.Models;
using FFGUITool.Services.Interfaces;

namespace FFGUITool.Services
{
    public class AudioProcessor : IAudioProcessor
    {
        private readonly IFFmpegService _ffmpegService;
        private readonly IMediaAnalyzer _mediaAnalyzer;

        public AudioProcessor(IFFmpegService ffmpegService, IMediaAnalyzer mediaAnalyzer)
        {
            _ffmpegService = ffmpegService;
            _mediaAnalyzer = mediaAnalyzer;
        }

        public async Task<bool> ConvertFormatAsync(
            AudioConversionOptions options, 
            IProgress<ProcessingProgress>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            if (!_ffmpegService.IsAvailable)
                throw new InvalidOperationException("FFmpeg is not available");

            var command = GenerateConversionCommand(options);
            var result = await _ffmpegService.ExecuteAsync(command, null, cancellationToken);
            return result.Success;
        }

        public async Task<bool> ExtractAudioFromVideoAsync(
            string videoFile, 
            string outputFile, 
            AudioFormat format, 
            CancellationToken cancellationToken = default)
        {
            var codec = GetCodecForFormat(format);
            var builder = new FFmpegCommandBuilder()
                .AddInput(videoFile)
                .AddParameter("-vn", "")  // No video
                .SetAudioCodec(codec)
                .SetOutput(outputFile);

            var result = await _ffmpegService.ExecuteAsync(builder.Build(), null, cancellationToken);
            return result.Success;
        }

        public async Task<bool> NormalizeAudioAsync(
            string inputFile, 
            string outputFile, 
            CancellationToken cancellationToken = default)
        {
            // Use loudnorm filter for EBU R128 normalization
            var builder = new FFmpegCommandBuilder()
                .AddInput(inputFile)
                .AddParameter("-af", "loudnorm=I=-16:TP=-1.5:LRA=11")
                .SetOutput(outputFile);

            var result = await _ffmpegService.ExecuteAsync(builder.Build(), null, cancellationToken);
            return result.Success;
        }

        public string GenerateConversionCommand(AudioConversionOptions options)
        {
            var builder = new FFmpegCommandBuilder()
                .AddInput(options.InputFile)
                .SetAudioCodec(options.Codec)
                .SetAudioBitrate(options.Bitrate)
                .SetSampleRate(options.SampleRate)
                .SetChannels(options.Channels)
                .SetOutput(options.OutputFile);

            return builder.Build();
        }

        private string GetCodecForFormat(AudioFormat format)
        {
            return format switch
            {
                AudioFormat.MP3 => "libmp3lame",
                AudioFormat.AAC => "aac",
                AudioFormat.WAV => "pcm_s16le",
                AudioFormat.FLAC => "flac",
                AudioFormat.OGG => "libvorbis",
                AudioFormat.M4A => "aac",
                AudioFormat.WMA => "wmav2",
                _ => "copy"
            };
        }
    }
}