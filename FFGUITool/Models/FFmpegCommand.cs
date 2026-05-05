using System.Collections.Generic;
using System.Text;

namespace FFGUITool.Models
{
    /// <summary>
    /// FFmpeg command model.
    /// </summary>
    public class FFmpegCommand
    {
        public string InputPath { get; set; } = "";
        public string OutputPath { get; set; } = "";
        public string Codec { get; set; } = "libx264";
        public int Bitrate { get; set; } = 2000;
        public string AudioCodec { get; set; } = "aac";
        public int AudioBitrate { get; set; } = 96;
        public bool UseCrf { get; set; }
        public int Crf { get; set; } = 23;
        public int MaxHeight { get; set; }
        public int MaxFramerate { get; set; }
        public bool AudioOnly { get; set; }
        public bool GifOutput { get; set; }
        public string AdditionalParameters { get; set; } = "";

        public string BuildCommand()
        {
            if (string.IsNullOrEmpty(InputPath))
            {
                return "请先选择输入文件或文件夹";
            }

            var command = new StringBuilder();
            command.Append("ffmpeg ");

            if (System.IO.File.Exists(InputPath))
            {
                command.Append($"-i \"{InputPath}\" ");
            }
            else if (System.IO.Directory.Exists(InputPath))
            {
                command.Append($"-i \"{InputPath}/*.mp4\" ");
            }

            if (AudioOnly)
            {
                command.Append("-vn ");
                command.Append($"-c:a {AudioCodec} ");
                command.Append($"-b:a {AudioBitrate}k ");
                AppendAdditionalParameters(command);
                AppendOutputPath(command);
                return command.ToString();
            }

            var filters = new List<string>();
            if (MaxHeight > 0)
            {
                filters.Add($"scale=-2:min({MaxHeight}\\,ih)");
            }

            if (MaxFramerate > 0)
            {
                filters.Add($"fps={MaxFramerate}");
            }

            if (filters.Count > 0)
            {
                command.Append($"-vf \"{string.Join(",", filters)}\" ");
            }

            if (GifOutput)
            {
                command.Append("-an ");
                AppendAdditionalParameters(command);
                AppendOutputPath(command);
                return command.ToString();
            }

            command.Append($"-c:v {Codec} ");

            if (UseCrf)
            {
                command.Append($"-crf {Crf} ");
            }
            else
            {
                command.Append($"-b:v {Bitrate}k ");
            }

            command.Append($"-c:a {AudioCodec} ");
            command.Append($"-b:a {AudioBitrate}k ");
            AppendAdditionalParameters(command);
            AppendOutputPath(command);

            return command.ToString();
        }

        private void AppendAdditionalParameters(StringBuilder command)
        {
            if (!string.IsNullOrEmpty(AdditionalParameters))
            {
                command.Append($"{AdditionalParameters} ");
            }
        }

        private void AppendOutputPath(StringBuilder command)
        {
            if (!string.IsNullOrEmpty(OutputPath))
            {
                command.Append($"\"{OutputPath}\"");
            }
            else
            {
                command.Append("\"[输出路径]/output.mp4\"");
            }
        }
    }
}
