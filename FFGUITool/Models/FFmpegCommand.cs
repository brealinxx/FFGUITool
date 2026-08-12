using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public string InputVideoCodec { get; set; } = "";
        public bool PreferDav1dDecoder { get; set; }
        public string TrimStart { get; set; } = "";
        public string TrimEnd { get; set; } = "";
        public string AudioTrackMode { get; set; } = "transcode";
        public int ImageQuality { get; set; } = 80;
        public double ImageTargetSizeKB { get; set; }
        public string ImageFormat { get; set; } = "jpg";
        public List<int> IconSizes { get; set; } = new();
        public bool ClearMetadata { get; set; }
        public string AdditionalParameters { get; set; } = "";

        public string BuildCommand()
        {
            if (string.IsNullOrEmpty(InputPath))
            {
                return LocalizationService.T("Command.SelectInput");
            }

            var command = new StringBuilder();
            command.Append("ffmpeg ");
            AppendTrimInputOptions(command);
            AppendDecoderOptions(command);

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

                if (IsIconFormat(ImageFormat) && IconSizes.Count > 0)
                {
                    AppendIconFileParameters(command);
                    AppendOutputPath(command);
                    return command.ToString();
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

            AppendAudioParameters(command);
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

        private void AppendTrimInputOptions(StringBuilder command)
        {
            var start = NormalizeTimeArgument(TrimStart);
            var end = NormalizeTimeArgument(TrimEnd);
            if (!string.IsNullOrWhiteSpace(start))
            {
                command.Append($"-ss {start} ");
            }

            if (!string.IsNullOrWhiteSpace(end))
            {
                command.Append($"-to {end} ");
            }
        }

        private void AppendDecoderOptions(StringBuilder command)
        {
            if (PreferDav1dDecoder && string.Equals(InputVideoCodec, "av1", System.StringComparison.OrdinalIgnoreCase))
            {
                command.Append("-c:v libdav1d ");
            }
        }

        private void AppendAudioParameters(StringBuilder command)
        {
            switch (AudioTrackMode)
            {
                case "remove":
                    command.Append("-an ");
                    return;
                case "copy":
                    command.Append("-c:a copy ");
                    return;
                default:
                    command.Append($"-c:a {AudioCodec} ");
                    command.Append($"-b:a {AudioBitrate}k ");
                    return;
            }
        }

        private static string NormalizeTimeArgument(string value)
        {
            value = value.Trim();
            return Regex.IsMatch(value, @"^\d+(?::\d{1,2}){0,2}(?:[\.,]\d+)?$")
                ? value.Replace(',', '.')
                : "";
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
                case "ico":
                case "icns":
                    command.Append("-frames:v 1 ");
                    break;
                default:
                    command.Append("-frames:v 1 ");
                    break;
            }
        }

        private void AppendIconFileParameters(StringBuilder command)
        {
            var sizes = IconSizes.Distinct().OrderBy(size => size).ToList();
            if (sizes.Count == 1)
            {
                var size = sizes[0];
                command.Append($"-vf \"{BuildIconScaleFilter(size)}\" ");
                command.Append("-frames:v 1 ");
                return;
            }

            var splitOutputs = string.Join("", Enumerable.Range(0, sizes.Count).Select(index => $"[icon{index}]"));
            var filters = new List<string> { $"[0:v]split={sizes.Count}{splitOutputs}" };
            for (var index = 0; index < sizes.Count; index++)
            {
                filters.Add($"[icon{index}]{BuildIconScaleFilter(sizes[index])}[iconout{index}]");
            }

            command.Append($"-filter_complex \"{string.Join(";", filters)}\" ");
            for (var index = 0; index < sizes.Count; index++)
            {
                command.Append($"-map \"[iconout{index}]\" ");
            }

            command.Append("-frames:v 1 ");
        }

        private static bool IsIconFormat(string format)
        {
            return format.Equals("ico", System.StringComparison.OrdinalIgnoreCase)
                   || format.Equals("icns", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildIconScaleFilter(int size)
        {
            return $"scale={size}:{size}:force_original_aspect_ratio=decrease,pad={size}:{size}:(ow-iw)/2:(oh-ih)/2:color=0x00000000";
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
