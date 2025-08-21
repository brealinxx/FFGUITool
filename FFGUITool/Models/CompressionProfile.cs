using System.Collections.Generic;

namespace FFGUITool.Models
{
    public class CompressionProfile
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int TargetBitrate { get; set; }
        public string VideoCodec { get; set; } = "libx264";
        public string AudioCodec { get; set; } = "aac";
        public string? Resolution { get; set; }
        public double? FrameRate { get; set; }
        public string? PresetSpeed { get; set; }
        public bool IsCustom { get; set; }
        public bool IsDefault { get; set; }
        
        // Predefined profiles
        public static CompressionProfile UltraHighQuality => new()
        {
            Id = "ultra_high",
            Name = "超高质量",
            Description = "几乎无损压缩，适合存档",
            TargetBitrate = 10000,
            VideoCodec = "libx264",
            AudioCodec = "aac",
            PresetSpeed = "slow"
        };
        
        public static CompressionProfile HighQuality => new()
        {
            Id = "high",
            Name = "高质量",
            Description = "高质量输出，适合重要视频",
            TargetBitrate = 6000,
            VideoCodec = "libx264",
            AudioCodec = "aac",
            PresetSpeed = "medium"
        };
        
        public static CompressionProfile Balanced => new()
        {
            Id = "balanced",
            Name = "平衡",
            Description = "质量和文件大小的最佳平衡",
            TargetBitrate = 3000,
            VideoCodec = "libx264",
            AudioCodec = "aac",
            PresetSpeed = "medium"
        };
        
        public static CompressionProfile SmallSize => new()
        {
            Id = "small",
            Name = "小文件",
            Description = "最大程度压缩，适合网络分享",
            TargetBitrate = 1500,
            VideoCodec = "libx265",
            AudioCodec = "aac",
            PresetSpeed = "medium"
        };
        
        public static CompressionProfile Mobile => new()
        {
            Id = "mobile",
            Name = "移动设备",
            Description = "优化移动设备播放",
            TargetBitrate = 1000,
            VideoCodec = "libx264",
            AudioCodec = "aac",
            Resolution = "1280x720",
            PresetSpeed = "fast"
        };
        
        public static List<CompressionProfile> GetDefaultProfiles()
        {
            return new List<CompressionProfile>
            {
                UltraHighQuality,
                HighQuality,
                Balanced,
                SmallSize,
                Mobile
            };
        }
    }
}