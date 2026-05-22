using System.Collections.Generic;
using System.Text;
using FFGUITool.Services;

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
        public string HardwareEncoder { get; set; } = "";
        public int Bitrate { get; set; } = 2000;
        public string AudioCodec { get; set; } = "aac";
        public int AudioBitrate { get; set; } = 96;
        public bool UseCrf { get; set; }
        public int Crf { get; set; } = 23;
        public int MaxHeight { get; set; }
        public int MaxFramerate { get; set; }
        public bool AudioOnly { get; set; }
        public bool GifOutput { get; set; }
        public bool ImageOutput { get; set; }
        public int ImageQuality { get; set; } = 80;
        public double ImageTargetSizeKB { get; set; }
        public string ImageFormat { get; set; } = "jpg";
        public string AdditionalParameters { get; set; } = "";

        public string BuildCommand()
        {
            if (string.IsNullOrEmpty(InputPath))
            {
                return LocalizationService.T("Command.SelectInput");
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

            if (ImageOutput)
            {
                if (MaxHeight > 0)
                {
                    command.Append($"-vf \"scale=-2:min({MaxHeight}\\,ih)\" ");
                }

                AppendImageParameters(command);
                AppendAdditionalParameters(command);
                AppendOutputPath(command);
                return command.ToString();
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

            command.Append($"-c:v {GetEffectiveVideoCodec()} ");

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

        private string GetEffectiveVideoCodec()
        {
            return string.IsNullOrWhiteSpace(HardwareEncoder) ? Codec : HardwareEncoder;
        }

        private void AppendImageParameters(StringBuilder command)
        {
            var quality = System.Math.Clamp(ImageQuality, 1, 100);
            switch (ImageFormat.ToLowerInvariant())
            {
                case "jpg":
                case "jpeg":
                    var qscale = System.Math.Clamp(31 - (int)System.Math.Round(quality * 29 / 100.0), 2, 31);
                    command.Append("-frames:v 1 ");
                    command.Append($"-q:v {qscale} ");
                    break;
                case "webp":
                    command.Append("-frames:v 1 -c:v libwebp ");
                    command.Append($"-quality {quality} ");
                    break;
                case "png":
                    var compression = System.Math.Clamp(10 - (int)System.Math.Ceiling(quality / 10.0), 0, 9);
                    command.Append("-frames:v 1 ");
                    command.Append($"-compression_level {compression} ");
                    break;
                default:
                    command.Append("-frames:v 1 ");
                    break;
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
