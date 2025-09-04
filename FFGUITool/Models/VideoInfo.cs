using System;

namespace FFGUITool.Models
{
    /// <summary>
    /// 视频信息模型
    /// </summary>
    public class VideoInfo
    {
        public int Bitrate { get; set; }
        public double Duration { get; set; }
        public string Resolution { get; set; } = "";
        public double Framerate { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; } = "";

        /// <summary>
        /// 获取格式化的文件大小文本
        /// </summary>
        public string FormattedFileSize
        {
            get
            {
                var sizeMB = FileSize / 1024.0 / 1024.0;
                return sizeMB < 1024
                    ? $"{sizeMB:F1} MB"
                    : $"{sizeMB / 1024.0:F2} GB";
            }
        }

        /// <summary>
        /// 获取格式化的时长文本
        /// </summary>
        public string FormattedDuration
        {
            get
            {
                var duration = TimeSpan.FromSeconds(Duration);
                return duration.Hours > 0
                    ? $"{duration:hh\\:mm\\:ss}"
                    : $"{duration:mm\\:ss}";
            }
        }
    }
}