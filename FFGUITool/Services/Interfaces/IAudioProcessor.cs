using System;
using System.Threading;
using System.Threading.Tasks;
using FFGUITool.Models;

namespace FFGUITool.Services.Interfaces
{
    public interface IAudioProcessor
    {
        Task<bool> ConvertFormatAsync(AudioConversionOptions options, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default);
        Task<bool> ExtractAudioFromVideoAsync(string videoFile, string outputFile, AudioFormat format, CancellationToken cancellationToken = default);
        Task<bool> NormalizeAudioAsync(string inputFile, string outputFile, CancellationToken cancellationToken = default);
        string GenerateConversionCommand(AudioConversionOptions options);
    }
}