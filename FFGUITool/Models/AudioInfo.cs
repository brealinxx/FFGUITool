namespace FFGUITool.Models
{
    public class AudioInfo : MediaInfo
    {
        public string AudioCodec { get; set; } = "";
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public string ChannelLayout { get; set; } = "";
        public int BitsPerSample { get; set; }
    }
}