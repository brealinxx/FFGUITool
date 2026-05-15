using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Linq;
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
                var resolutionMatch = Regex.Match(ffmpegOutput, @"(\d{2,5}x\d{2,5})");
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
                videoInfo.MetadataSummary = ParseSensitiveMetadata(ffmpegOutput);

                return videoInfo;
            }
            catch
            {
                return null;
            }
        }

        private static string ParseSensitiveMetadata(string ffmpegOutput)
        {
            var sensitiveKeys = new[]
            {
                "album",
                "artist",
                "author",
                "camera",
                "composer",
                "copyright",
                "comment",
                "description",
                "creation_time",
                "date",
                "device",
                "encoded_by",
                "encoder",
                "firmware",
                "gps",
                "handler_name",
                "keywords",
                "latitude",
                "lens",
                "location",
                "location-eng",
                "longitude",
                "make",
                "model",
                "owner",
                "producer",
                "publisher",
                "serial",
                "software",
                "synopsis",
                "writer"
            };

            var lines = ffmpegOutput
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Contains(':'))
                .Select(line => line.Split(':', 2))
                .Select(parts => new
                {
                    Key = parts[0].Trim(),
                    Value = parts.Length > 1 ? parts[1].Trim() : ""
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Where(item => sensitiveKeys.Any(key => item.Key.Contains(key, System.StringComparison.OrdinalIgnoreCase)))
                .Select(item => $"{NormalizeMetadataKey(item.Key)}: {item.Value}")
                .Distinct()
                .Take(12)
                .ToList();

            return lines.Count == 0 ? "" : string.Join("\n", lines);
        }

        private static string NormalizeMetadataKey(string key)
        {
            return key switch
            {
                "artist" => "Author",
                "author" => "Author",
                "com.apple.quicktime.author" => "Author",
                "creation_time" => "Creation time",
                "com.apple.quicktime.creationdate" => "Creation time",
                "com.apple.quicktime.location.ISO6709" => "Location",
                "com.apple.quicktime.make" => "Device maker",
                "com.apple.quicktime.model" => "Device model",
                _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key.Replace('_', ' '))
            };
        }
    }
}
