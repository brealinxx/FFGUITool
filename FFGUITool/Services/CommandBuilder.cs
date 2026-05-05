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
                AudioCodec = GetCommandAudioCodec(settings)
            };

            // 生成输出路径
            if (!string.IsNullOrEmpty(settings.OutputPath))
            {
                var inputFileName = Path.GetFileNameWithoutExtension(settings.InputPath);
                var outputFormat = GetOutputFormat(settings);
                var targetSize = settings.TargetSizeMB > 0 ? $"{settings.TargetSizeMB:F0}MB" : $"{settings.CompressionPercentage}%";
                var outputFileName = $"{inputFileName}_compressed_{targetSize}.{outputFormat}";
                command.OutputPath = Path.Combine(settings.OutputPath, outputFileName);
            }

            return command;
        }

        private static string GetOutputFormat(CompressionSettings settings)
        {
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
