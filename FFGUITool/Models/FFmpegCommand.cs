namespace FFGUITool.Models
{
    /// <summary>
    /// FFmpeg命令模型
    /// </summary>
    public class FFmpegCommand
    {
        public string InputPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public string Codec { get; set; } = "libx264";
        public int Bitrate { get; set; } = 2000;
        public string AudioCodec { get; set; } = "aac";
        public string AdditionalParameters { get; set; } = "";

        /// <summary>
        /// 构建完整的FFmpeg命令
        /// </summary>
        public string BuildCommand()
        {
            if (string.IsNullOrEmpty(InputPath))
                return "请先选择输入文件或文件夹";

            var command = new System.Text.StringBuilder();
            command.Append("ffmpeg ");

            // 输入文件
            if (System.IO.File.Exists(InputPath))
            {
                command.Append($"-i \"{InputPath}\" ");
            }
            else if (System.IO.Directory.Exists(InputPath))
            {
                command.Append($"-i \"{InputPath}/*.mp4\" ");
            }

            // 视频编码器
            command.Append($"-c:v {Codec} ");

            // 比特率
            command.Append($"-b:v {Bitrate}k ");

            // 音频编码器
            command.Append($"-c:a {AudioCodec} ");

            // 额外参数
            if (!string.IsNullOrEmpty(AdditionalParameters))
            {
                command.Append($"{AdditionalParameters} ");
            }

            // 输出文件
            if (!string.IsNullOrEmpty(OutputPath))
            {
                command.Append($"\"{OutputPath}\"");
            }
            else
            {
                command.Append("\"[输出路径]/output.mp4\"");
            }

            return command.ToString();
        }
    }
}