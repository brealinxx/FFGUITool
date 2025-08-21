namespace FFGUITool.Models
{
    public class VideoInfo : MediaInfo
    {
        public string Resolution { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public double FrameRate { get; set; }
        public string VideoCodec { get; set; } = "";
        public string AudioCodec { get; set; } = "";
        public string AspectRatio { get; set; } = "";
        public int VideoBitrate { get; set; }
        public int AudioBitrate { get; set; }
    }
}