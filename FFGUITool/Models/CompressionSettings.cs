namespace FFGUITool.Models
{
    /// <summary>
    /// 压缩设置模型
    /// </summary>
    public class CompressionSettings
    {
        public int CompressionPercentage { get; set; } = 70;
        public double TargetSizeMB { get; set; }
        public int Bitrate { get; set; } = 2000;
        public string Codec { get; set; } = "libx264";
        public bool UseCrf { get; set; }
        public int Crf { get; set; } = 23;
        public int AudioBitrate { get; set; } = 96;
        public int MaxHeight { get; set; }
        public int MaxFramerate { get; set; }
        public bool EnableFormatConversion { get; set; }
        public string OutputFormat { get; set; } = "mp4";
        public bool EnableAudioConversion { get; set; }
        public string AudioOutputFormat { get; set; } = "mp3";
        public bool EnableResolutionConversion { get; set; }
        public int ResolutionHeight { get; set; } = 720;
        public string InputPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
        
        /// <summary>
        /// 验证设置是否有效
        /// </summary>
        public bool IsValid => 
            !string.IsNullOrEmpty(InputPath) && 
            Bitrate > 0;
    }
}
