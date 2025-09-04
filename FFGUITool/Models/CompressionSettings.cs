namespace FFGUITool.Models
{
    /// <summary>
    /// 压缩设置模型
    /// </summary>
    public class CompressionSettings
    {
        public int CompressionPercentage { get; set; } = 70;
        public int Bitrate { get; set; } = 2000;
        public string Codec { get; set; } = "libx264";
        public string InputPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
        
        /// <summary>
        /// 验证设置是否有效
        /// </summary>
        public bool IsValid => 
            !string.IsNullOrEmpty(InputPath) && 
            !string.IsNullOrEmpty(OutputPath) &&
            Bitrate > 0;
    }
}