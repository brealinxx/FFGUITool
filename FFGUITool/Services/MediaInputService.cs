using System.Collections.Generic;
using System.Threading;
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

        public async Task<VideoInfo?> AnalyzeAsync(string inputPath, bool fallbackToFileInfo = false, CancellationToken cancellationToken = default)
        {
            var info = await _videoAnalyzer.AnalyzeVideo(inputPath, cancellationToken);
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
            bool includeSubfolders, CancellationToken cancellationToken = default, MediaImportReport? report = null)
        {
            return MediaFileSupport.GetBatchInputFiles(
                inputPath,
                imageMode,
                enableAudioConversion,
                includeSubfolders, cancellationToken, report);
        }
    }
}
