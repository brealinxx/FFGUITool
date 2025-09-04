using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FFGUITool.Models;

namespace FFGUITool.Services
{
    /// <summary>
    /// 视频分析服务
    /// </summary>
    public class VideoAnalyzer
    {
        private readonly FFmpegManager _ffmpegManager;

        public VideoAnalyzer(FFmpegManager ffmpegManager)
        {
            _ffmpegManager = ffmpegManager;
        }

        /// <summary>
        /// 分析视频文件
        /// </summary>
        public async Task<VideoInfo?> AnalyzeVideo(string videoPath)
        {
            if (!_ffmpegManager.IsFFmpegAvailable || !File.Exists(videoPath))
                return null;

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegManager.FFmpegPath,
                    Arguments = $"-i \"{videoPath}\" -hide_banner",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                return ParseVideoInfo(output, videoPath);
            }
            catch
            {
                return null;
            }
        }

        private VideoInfo? ParseVideoInfo(string ffmpegOutput, string filePath)
        {
            try
            {
                var videoInfo = new VideoInfo { FilePath = filePath };

                // 解析时长
                var durationMatch = Regex.Match(ffmpegOutput, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
                if (durationMatch.Success)
                {
                    var hours = int.Parse(durationMatch.Groups[1].Value);
                    var minutes = int.Parse(durationMatch.Groups[2].Value);
                    var seconds = double.Parse(durationMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                    videoInfo.Duration = hours * 3600 + minutes * 60 + seconds;
                }

                // 解析比特率
                var bitrateMatch = Regex.Match(ffmpegOutput, @"bitrate: (\d+) kb/s");
                if (bitrateMatch.Success)
                {
                    videoInfo.Bitrate = int.Parse(bitrateMatch.Groups[1].Value);
                }

                // 解析分辨率
                var resolutionMatch = Regex.Match(ffmpegOutput, @"(\d{3,4}x\d{3,4})");
                if (resolutionMatch.Success)
                {
                    videoInfo.Resolution = resolutionMatch.Groups[1].Value;
                }

                // 解析帧率
                var framerateMatch = Regex.Match(ffmpegOutput, @"(\d+(?:\.\d+)?) fps");
                if (framerateMatch.Success)
                {
                    videoInfo.Framerate = double.Parse(framerateMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }

                // 获取文件大小
                videoInfo.FileSize = new FileInfo(filePath).Length;

                return videoInfo;
            }
            catch
            {
                return null;
            }
        }
    }
}