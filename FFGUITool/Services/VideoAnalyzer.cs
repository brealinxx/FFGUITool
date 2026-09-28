using System;
using System.Threading;
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
        private readonly MediaInfoCache _cache = new();

        public async Task<VideoInfo?> AnalyzeVideo(string videoPath, CancellationToken cancellationToken = default)
        {
            if (!_ffmpegManager.IsFFmpegAvailable || !File.Exists(videoPath)) return null;
            var file = new FileInfo(videoPath);
            var key = Path.GetFullPath(videoPath);
            if (_cache.Get(key, file.Length, file.LastWriteTimeUtc.Ticks) is { } cached)
                return cached;
            var directory = Path.GetDirectoryName(_ffmpegManager.FFmpegPath);
            var probe = string.IsNullOrWhiteSpace(directory) ? "ffprobe" :
                Path.Combine(directory, OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe");
            VideoInfo? info;
            try
            {
                var result = await ProcessRunner.RunAsync(probe,
                    new[] { "-v", "error", "-show_format", "-show_streams", "-of", "json", videoPath },
                    cancellationToken, TimeSpan.FromSeconds(20));
                if (result.ExitCode != 0) throw new InvalidDataException(result.Error);
                info = MediaProbe.Parse(result.Output, videoPath, file.Length);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                var result = await ProcessRunner.RunAsync(_ffmpegManager.FFmpegPath,
                    new[] { "-hide_banner", "-i", videoPath }, cancellationToken, TimeSpan.FromSeconds(20));
                info = ParseVideoInfo(result.Error, videoPath);
                if (info != null) info.HasAudio = result.Error.Contains("Audio:", StringComparison.Ordinal);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { AppLogger.Warn($"Media analysis failed: {ex.Message}"); return null; }
            if (info != null)
            {
                _cache.Put(key, file.Length, file.LastWriteTimeUtc.Ticks, info);
            }
            return info;
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
                var codecMatch = Regex.Match(ffmpegOutput, @"Video:\s*([^,\s]+)", RegexOptions.IgnoreCase);
                if (codecMatch.Success)
                {
                    videoInfo.VideoCodec = codecMatch.Groups[1].Value.Trim().ToLowerInvariant();
                }

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
