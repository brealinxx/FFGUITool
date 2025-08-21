namespace FFGUITool.Models
{
    public class ProcessingOptions
    {
        public ProcessingType Type { get; set; } = ProcessingType.VideoCompression;
        public VideoCompressionOptions? VideoOptions { get; set; }
        public AudioConversionOptions? AudioOptions { get; set; }
        public bool OverwriteExisting { get; set; }
        public string OutputDirectory { get; set; } = "";
        public string OutputFilePattern { get; set; } = "{name}_processed{ext}";
    }

    public enum ProcessingType
    {
        VideoCompression,
        AudioConversion,
        FormatConversion,
        ExtractAudio,
        Merge
    }

    public class VideoCompressionOptions
    {
        public string InputFile { get; set; } = "";
        public string OutputFile { get; set; } = "";
        public int TargetBitrate { get; set; } = 2000;
        public string VideoCodec { get; set; } = "libx264";
        public string AudioCodec { get; set; } = "aac";
        public int CompressionPercentage { get; set; } = 70;
        public string? Resolution { get; set; }
        public double? FrameRate { get; set; }
        public CompressionProfile? Profile { get; set; }
        public VideoInfo? SourceVideoInfo { get; set; }
    }

    public class AudioConversionOptions
    {
        public string InputFile { get; set; } = "";
        public string OutputFile { get; set; } = "";
        public string Codec { get; set; } = "aac";
        public int Bitrate { get; set; } = 128;
        public int SampleRate { get; set; } = 44100;
        public int Channels { get; set; } = 2;
        public AudioFormat OutputFormat { get; set; } = AudioFormat.MP3;
    }

    public enum VideoFormat
    {
        MP4,
        AVI,
        MKV,
        MOV,
        WMV,
        FLV,
        WEBM
    }

    public enum AudioFormat
    {
        MP3,
        AAC,
        WAV,
        FLAC,
        OGG,
        M4A,
        WMA
    }
}