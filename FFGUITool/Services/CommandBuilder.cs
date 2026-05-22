using System;
using System.IO;
using FFGUITool.Models;

namespace FFGUITool.Services
{
    /// <summary>
    /// FFmpeg命令构建服务
    /// </summary>
    public class CommandBuilder
    {
        /// <summary>
        /// 根据设置构建FFmpeg命令
        /// </summary>
        public FFmpegCommand BuildCommand(CompressionSettings settings, VideoInfo? videoInfo = null)
        {
            var command = new FFmpegCommand
            {
                InputPath = settings.InputPath,
                Codec = GetVideoCodec(settings),
                HardwareEncoder = settings.HardwareEncoder,
                Bitrate = settings.Bitrate,
                AudioBitrate = settings.AudioBitrate,
                UseCrf = settings.UseCrf,
                Crf = settings.Crf,
                MaxHeight = GetEffectiveMaxHeight(settings),
                MaxFramerate = settings.EnableFormatConversion && settings.OutputFormat == "gif" && settings.MaxFramerate <= 0
                    ? 12
                    : settings.MaxFramerate,
                AudioOnly = settings.EnableAudioConversion,
                GifOutput = settings.EnableFormatConversion && settings.OutputFormat == "gif",
                AudioCodec = GetCommandAudioCodec(settings),
                ImageOutput = settings.IsImageProcessing,
                ImageQuality = settings.ImageQuality,
                ImageTargetSizeKB = settings.ImageTargetSizeKB,
                ImageFormat = settings.IsImageProcessing ? settings.ImageOutputFormat : GetOutputFormat(settings)
            };

            command.OutputPath = BuildOutputPath(settings);

            return command;
        }

        private static string BuildOutputPath(CompressionSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.InputPath))
            {
                return "";
            }

            var outputDirectory = !string.IsNullOrWhiteSpace(settings.OutputPath)
                ? settings.OutputPath
                : GetDefaultOutputDirectory(settings.InputPath);

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return "";
            }

            var inputFileName = Path.GetFileNameWithoutExtension(settings.InputPath);
            var outputFormat = GetOutputFormat(settings);
            var targetSize = !string.IsNullOrWhiteSpace(settings.OutputLabel)
                ? settings.OutputLabel
                : settings.UseCrf
                ? $"crf{settings.Crf}"
                : settings.IsImageProcessing && settings.ImageTargetSizeKB > 0
                ? $"{settings.ImageTargetSizeKB:F0}KB"
                : settings.TargetSizeMB > 0
                ? $"{settings.TargetSizeMB:F0}MB"
                : $"{settings.CompressionPercentage}%";
            var outputFileName = $"{inputFileName}_FFGUIToolOutPut_{SanitizeFileNameToken(targetSize)}.{outputFormat}";

            return Path.Combine(outputDirectory, outputFileName);
        }

        private static string SanitizeFileNameToken(string token)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                token = token.Replace(invalidChar, '_');
            }

            return token.Replace(' ', '_');
        }

        private static string GetDefaultOutputDirectory(string inputPath)
        {
            if (File.Exists(inputPath))
            {
                return Path.GetDirectoryName(inputPath) ?? "";
            }

            if (Directory.Exists(inputPath))
            {
                return inputPath;
            }

            return Path.GetDirectoryName(inputPath) ?? "";
        }

        private static string GetOutputFormat(CompressionSettings settings)
        {
            if (settings.IsImageProcessing)
            {
                return settings.EnableFormatConversion
                    ? settings.ImageOutputFormat
                    : GetImageOutputFormatFromInput(settings.InputPath);
            }

            if (settings.EnableAudioConversion)
            {
                return settings.AudioOutputFormat;
            }

            if (settings.EnableFormatConversion)
            {
                return settings.OutputFormat;
            }

            return "mp4";
        }

        private static string GetImageOutputFormatFromInput(string inputPath)
        {
            var extension = Path.GetExtension(inputPath).TrimStart('.').ToLowerInvariant();
            return extension switch
            {
                "jpeg" => "jpg",
                "jpg" or "png" or "webp" => extension,
                "bmp" => "png",
                "heic" or "heif" or "gif" or "tif" or "tiff" or "ico" or "tga" or "avif" => "jpg",
                _ => "jpg"
            };
        }

        private static string GetVideoCodec(CompressionSettings settings)
        {
            if (settings.EnableFormatConversion && settings.OutputFormat == "webm")
            {
                return "libvpx-vp9";
            }

            return settings.Codec;
        }

        private static string GetAudioCodec(string format)
        {
            return format switch
            {
                "mp3" => "libmp3lame",
                "aac" or "m4a" => "aac",
                "wav" => "pcm_s16le",
                "flac" => "flac",
                "ogg" => "libvorbis",
                _ => "aac"
            };
        }

        private static string GetCommandAudioCodec(CompressionSettings settings)
        {
            if (settings.EnableAudioConversion)
            {
                return GetAudioCodec(settings.AudioOutputFormat);
            }

            if (settings.EnableFormatConversion && settings.OutputFormat == "webm")
            {
                return "libopus";
            }

            return "aac";
        }

        private static int GetEffectiveMaxHeight(CompressionSettings settings)
        {
            if (!settings.EnableResolutionConversion)
            {
                return settings.MaxHeight;
            }

            if (settings.MaxHeight <= 0)
            {
                return settings.ResolutionHeight;
            }

            return Math.Min(settings.MaxHeight, settings.ResolutionHeight);
        }

        /// <summary>
        /// 计算推荐的比特率
        /// </summary>
        public int CalculateRecommendedBitrate(VideoInfo videoInfo, int compressionPercentage, string codec)
        {
            // 基于压缩百分比计算目标比特率
            var targetBitrate = (int)(videoInfo.Bitrate * compressionPercentage / 100.0);

            // 根据编码器调整比特率
            targetBitrate = AdjustBitrateForCodec(targetBitrate, codec);

            // 确保比特率在合理范围内
            return Math.Max(1, Math.Min(targetBitrate, videoInfo.Bitrate));
        }

        public int CalculateBitrateForTargetSize(VideoInfo videoInfo, double targetSizeMB)
        {
            if (videoInfo.Duration <= 0 || targetSizeMB <= 0)
            {
                return Math.Max(1, videoInfo.Bitrate);
            }

            var targetBytes = targetSizeMB * 1024 * 1024;
            var totalBitrateKbps = targetBytes * 8 / videoInfo.Duration / 1024;
            var videoBitrateKbps = totalBitrateKbps / 1.12;

            return Math.Max(80, (int)Math.Round(videoBitrateKbps));
        }

        private int AdjustBitrateForCodec(int baseBitrate, string codec)
        {
            return codec switch
            {
                "libx265" => (int)(baseBitrate * 0.7), // H.265效率更高
                "libvpx-vp9" => (int)(baseBitrate * 0.8), // VP9效率较高
                "libaom-av1" => (int)(baseBitrate * 0.65), // AV1效率更高
                _ => baseBitrate // H.264基准
            };
        }

        /// <summary>
        /// 计算预估文件大小
        /// </summary>
        public long CalculateEstimatedFileSize(int bitrateKbps, double durationSeconds)
        {
            // 文件大小 = 比特率 * 时长 / 8 (转换为字节)
            // 考虑音频轨道大约占总比特率的10-15%
            var totalBitrateKbps = bitrateKbps + (bitrateKbps * 0.12); // 视频+音频
            return (long)(totalBitrateKbps * 1024 * durationSeconds / 8);
        }
    }
}
