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
                Codec = settings.Codec,
                Bitrate = settings.Bitrate,
                AudioCodec = "aac"
            };

            // 生成输出路径
            if (!string.IsNullOrEmpty(settings.OutputPath))
            {
                var inputFileName = Path.GetFileNameWithoutExtension(settings.InputPath);
                var outputFileName = $"{inputFileName}_compressed_{settings.CompressionPercentage}%.mp4";
                command.OutputPath = Path.Combine(settings.OutputPath, outputFileName);
            }

            return command;
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