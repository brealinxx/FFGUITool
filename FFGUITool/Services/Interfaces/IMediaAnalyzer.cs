using System.Threading.Tasks;
using FFGUITool.Models;

namespace FFGUITool.Services.Interfaces
{
    public interface IMediaAnalyzer
    {
        Task<MediaInfo?> AnalyzeAsync(string filePath);
        Task<VideoInfo?> AnalyzeVideoAsync(string filePath);
        Task<AudioInfo?> AnalyzeAudioAsync(string filePath);
        MediaType GetMediaType(string filePath);
    }

    public enum MediaType
    {
        Unknown,
        Video,
        Audio,
        Image
    }
}