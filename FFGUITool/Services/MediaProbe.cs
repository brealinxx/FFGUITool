using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using FFGUITool.Models;

namespace FFGUITool.Services;

public static class MediaProbe
{
    public static VideoInfo Parse(string json, string path, long length)
    {
        using var document = JsonDocument.Parse(json);
        var info = new VideoInfo { FilePath = path, FileSize = length };
        var root = document.RootElement;
        var metadata = new List<string>();
        if (root.TryGetProperty("format", out var format))
        {
            info.Duration = Number(format, "duration");
            info.Bitrate = (int)(Number(format, "bit_rate") / 1000);
            ReadTags(format, metadata);
        }
        if (root.TryGetProperty("streams", out var streams))
        {
            foreach (var stream in streams.EnumerateArray())
            {
                ReadTags(stream, metadata);
                var type = Text(stream, "codec_type");
                if (type == "audio")
                {
                    info.HasAudio = true;
                    info.AudioBitrate += (int)(Number(stream, "bit_rate") / 1000);
                    info.AudioCodec = Text(stream, "codec_name");
                }
                if (type != "video" || !string.IsNullOrEmpty(info.VideoCodec)) continue;
                if (stream.TryGetProperty("disposition", out var disposition) && Number(disposition, "attached_pic") == 1) continue;
                info.VideoCodec = Text(stream, "codec_name");
                info.Resolution = $"{Number(stream, "width"):0}x{Number(stream, "height"):0}";
                info.PixelFormat = Text(stream, "pix_fmt");
                info.ColorSpace = Text(stream, "color_space") + "/" + Text(stream, "color_transfer");
                var rate = Text(stream, "avg_frame_rate").Split('/');
                if (rate.Length == 2 && double.TryParse(rate[0], CultureInfo.InvariantCulture, out var n) &&
                    double.TryParse(rate[1], CultureInfo.InvariantCulture, out var d) && d != 0) info.Framerate = n / d;
                info.Duration = Math.Max(info.Duration, Number(stream, "duration"));
                info.IsAnimated = Number(stream, "nb_frames") > 1;
                if (stream.TryGetProperty("side_data_list", out var sideData))
                    foreach (var item in sideData.EnumerateArray()) info.Rotation = (int)Number(item, "rotation");
            }
        }
        info.MetadataSummary = string.Join(Environment.NewLine, metadata.Distinct().Take(20));
        return info;
    }

    private static string Text(JsonElement element, string key) => element.TryGetProperty(key, out var value) ? value.ToString() : "";
    private static double Number(JsonElement element, string key) => double.TryParse(Text(element, key), NumberStyles.Float,
        CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) ? value : 0;
    private static void ReadTags(JsonElement element, List<string> tags)
    {
        if (element.TryGetProperty("tags", out var values))
            foreach (var tag in values.EnumerateObject()) tags.Add($"{tag.Name}: {tag.Value}");
    }
}
