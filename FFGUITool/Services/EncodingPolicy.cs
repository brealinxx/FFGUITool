using System;
using System.Globalization;
using FFGUITool.Models;

namespace FFGUITool.Services;

public static class EncodingPolicy
{
    public static double ParseTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var parts = value.Replace(',', '.').Split(':');
        if (parts.Length > 3) throw new ArgumentException("Invalid time.");
        double seconds = 0;
        foreach (var part in parts)
        {
            if (!double.TryParse(part, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number) || number < 0)
                throw new ArgumentException("Invalid time.");
            seconds = seconds * 60 + number;
        }
        return seconds;
    }

    public static double OutputDuration(CompressionSettings settings, VideoInfo info)
    {
        if (!settings.EnableTrim) return info.Duration;
        var start = ParseTime(settings.TrimStart);
        var end = string.IsNullOrWhiteSpace(settings.TrimEnd) ? info.Duration : Math.Min(ParseTime(settings.TrimEnd), info.Duration);
        if (start >= end) throw new ArgumentException(LocalizationService.T("Improve.InvalidTrim"));
        return end - start;
    }

    public static int AudioBudget(CompressionSettings settings, VideoInfo info) =>
        !info.HasAudio || settings.AudioTrackMode == "remove" ? 0 :
        settings.AudioTrackMode == "copy" ? (info.AudioBitrate > 0 ? info.AudioBitrate : 192) : settings.AudioBitrate;

    public static int TargetBitrate(double targetMiB, double duration, int audioKbps)
    {
        if (duration <= 0 || targetMiB <= 0) throw new ArgumentException(LocalizationService.T("Improve.InvalidTarget"));
        var available = targetMiB * 1024 * 1024 * 8 * 0.97 / duration / 1000 - audioKbps;
        if (available < 16) throw new ArgumentException(LocalizationService.T("Improve.ImpossibleTarget"));
        return (int)Math.Floor(available);
    }

    public static string CompatibleHardware(string encoder, string format) =>
        format is "webm" or "gif" ? "" : encoder;

    public static string QualityArguments(string encoder, int quality)
    {
        quality = Math.Clamp(quality, 0, 51);
        if (encoder.EndsWith("_nvenc", StringComparison.Ordinal)) return $"-rc vbr -cq {quality} -b:v 0";
        if (encoder.EndsWith("_qsv", StringComparison.Ordinal)) return $"-global_quality {quality}";
        if (encoder.EndsWith("_amf", StringComparison.Ordinal)) return $"-rc cqp -qp_i {quality} -qp_p {quality}";
        if (encoder.EndsWith("_vaapi", StringComparison.Ordinal)) return $"-qp {quality}";
        if (encoder.EndsWith("_videotoolbox", StringComparison.Ordinal))
            return "-q:v " + Math.Clamp(100 - quality * 2, 1, 100).ToString(CultureInfo.InvariantCulture);
        return $"-crf {quality}";
    }
}
