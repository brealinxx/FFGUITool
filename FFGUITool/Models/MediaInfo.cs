using System;
using System.Collections.Generic;

namespace FFGUITool.Models
{
    public abstract class MediaInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public double FrameRate { get; set; }
        public string VideoCodec { get; set; } = "";
        public string AudioCodec { get; set; } = "";

        public string FilePath { get; set; } = "";
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public long FileSize { get; set; }
        public TimeSpan Duration { get; set; }
        public int Bitrate { get; set; }
        public string Format { get; set; } = "";
        public Dictionary<string, string> Metadata { get; set; } = new();

        public string FileSizeFormatted => FormatFileSize(FileSize);
        public string DurationFormatted => FormatDuration(Duration);

        protected string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        protected string FormatDuration(TimeSpan duration)
        {
            return duration.Hours > 0
                ? $"{duration:hh\\:mm\\:ss}"
                : $"{duration:mm\\:ss}";
        }
    }
}