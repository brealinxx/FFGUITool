using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FFGUITool.Models;

namespace FFGUITool.Services
{
    public sealed class MediaInputService
    {
        private readonly VideoAnalyzer _videoAnalyzer;

        public MediaInputService(VideoAnalyzer videoAnalyzer)
        {
            _videoAnalyzer = videoAnalyzer;
        }

        public async Task<VideoInfo?> AnalyzeAsync(string inputPath, bool fallbackToFileInfo = false)
        {
            var info = await _videoAnalyzer.AnalyzeVideo(inputPath);
            if (info == null && fallbackToFileInfo && File.Exists(inputPath))
            {
                info = new VideoInfo
                {
                    FilePath = inputPath,
                    FileSize = new FileInfo(inputPath).Length
                };
            }

            return info;
        }

        public IEnumerable<string> DiscoverFolderFiles(
            string inputPath,
            bool imageMode,
            bool enableAudioConversion,
            bool includeSubfolders)
        {
            return MediaFileSupport.GetBatchInputFiles(
                inputPath,
                imageMode,
                enableAudioConversion,
                includeSubfolders);
        }
    }
}
