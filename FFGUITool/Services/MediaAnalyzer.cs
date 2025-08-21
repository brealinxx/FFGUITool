using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FFGUITool.Models;
using FFGUITool.Services.Interfaces;

namespace FFGUITool.Services
{
    public class MediaAnalyzer : IMediaAnalyzer
    {
        private readonly IFFmpegService _ffmpegService;
        
        private readonly string[] _videoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };
        private readonly string[] _audioExtensions = { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".opus" };
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };

        public MediaAnalyzer(IFFmpegService ffmpegService)
        {
            _ffmpegService = ffmpegService;
        }

        public async Task<MediaInfo?> AnalyzeAsync(string filePath)
        {
            var mediaType = GetMediaType(filePath);
            
            return mediaType switch
            {
                MediaType.Video => await AnalyzeVideoAsync(filePath),
                MediaType.Audio => await AnalyzeAudioAsync(filePath),
                _ => null
            };
        }

        public async Task<VideoInfo?> AnalyzeVideoAsync(string filePath)
        {
            if (!File.Exists(filePath) || !_ffmpegService.IsAvailable)
                return null;

            try
            {
                var result = await _ffmpegService.ExecuteAsync($"-i \"{filePath}\" -hide_banner");
                
                // FFmpeg writes media info to stderr, not stdout
                var output = result.Error;
                
                return ParseVideoInfo(output, filePath);
            }
            catch
            {
                return null;
            }
        }

        public async Task<AudioInfo?> AnalyzeAudioAsync(string filePath)
        {
            if (!File.Exists(filePath) || !_ffmpegService.IsAvailable)
                return null;

            try
            {
                var result = await _ffmpegService.ExecuteAsync($"-i \"{filePath}\" -hide_banner");
                var output = result.Error;
                
                return ParseAudioInfo(output, filePath);
            }
            catch
            {
                return null;
            }
        }

        public MediaType GetMediaType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            
            if (Array.IndexOf(_videoExtensions, extension) >= 0)
                return MediaType.Video;
            if (Array.IndexOf(_audioExtensions, extension) >= 0)
                return MediaType.Audio;
            if (Array.IndexOf(_imageExtensions, extension) >= 0)
                return MediaType.Image;
                
            return MediaType.Unknown;
        }

        private VideoInfo? ParseVideoInfo(string ffmpegOutput, string filePath)
        {
            try
            {
                var videoInfo = new VideoInfo
                {
                    FilePath = filePath,
                    FileSize = new FileInfo(filePath).Length
                };

                // Parse duration
                var durationMatch = Regex.Match(ffmpegOutput, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
                if (durationMatch.Success)
                {
                    var hours = int.Parse(durationMatch.Groups[1].Value);
                    var minutes = int.Parse(durationMatch.Groups[2].Value);
                    var seconds = double.Parse(durationMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                    videoInfo.Duration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                }

                // Parse bitrate
                var bitrateMatch = Regex.Match(ffmpegOutput, @"bitrate: (\d+) kb/s");
                if (bitrateMatch.Success)
                {
                    videoInfo.Bitrate = int.Parse(bitrateMatch.Groups[1].Value);
                }

                // Parse video stream info
                var videoStreamMatch = Regex.Match(ffmpegOutput, @"Stream.*Video: (\w+).*?, .*?, (\d+)x(\d+).*?, (\d+(?:\.\d+)?) fps");
                if (videoStreamMatch.Success)
                {
                    videoInfo.VideoCodec = videoStreamMatch.Groups[1].Value;
                    videoInfo.Width = int.Parse(videoStreamMatch.Groups[2].Value);
                    videoInfo.Height = int.Parse(videoStreamMatch.Groups[3].Value);
                    videoInfo.Resolution = $"{videoInfo.Width}x{videoInfo.Height}";
                    videoInfo.FrameRate = double.Parse(videoStreamMatch.Groups[4].Value, CultureInfo.InvariantCulture);
                }

                // Parse audio stream info
                var audioStreamMatch = Regex.Match(ffmpegOutput, @"Stream.*Audio: (\w+).*?, (\d+) Hz");
                if (audioStreamMatch.Success)
                {
                    videoInfo.AudioCodec = audioStreamMatch.Groups[1].Value;
                }

                return videoInfo;
            }
            catch
            {
                return null;
            }
        }

        private AudioInfo? ParseAudioInfo(string ffmpegOutput, string filePath)
        {
            try
            {
                var audioInfo = new AudioInfo
                {
                    FilePath = filePath,
                    FileSize = new FileInfo(filePath).Length
                };

                // Parse duration
                var durationMatch = Regex.Match(ffmpegOutput, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
                if (durationMatch.Success)
                {
                    var hours = int.Parse(durationMatch.Groups[1].Value);
                    var minutes = int.Parse(durationMatch.Groups[2].Value);
                    var seconds = double.Parse(durationMatch.Groups[3].Value, CultureInfo.InvariantCulture);
                    audioInfo.Duration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                }

                // Parse bitrate
                var bitrateMatch = Regex.Match(ffmpegOutput, @"bitrate: (\d+) kb/s");
                if (bitrateMatch.Success)
                {
                    audioInfo.Bitrate = int.Parse(bitrateMatch.Groups[1].Value);
                }

                // Parse audio stream info
                var audioStreamMatch = Regex.Match(ffmpegOutput, @"Stream.*Audio: (\w+).*?, (\d+) Hz, (\w+), .*?, (\d+) kb/s");
                if (audioStreamMatch.Success)
                {
                    audioInfo.AudioCodec = audioStreamMatch.Groups[1].Value;
                    audioInfo.SampleRate = int.Parse(audioStreamMatch.Groups[2].Value);
                    audioInfo.ChannelLayout = audioStreamMatch.Groups[3].Value;
                    audioInfo.Channels = audioInfo.ChannelLayout.Contains("stereo") ? 2 : 1;
                }

                return audioInfo;
            }
            catch
            {
                return null;
            }
        }
    }
}