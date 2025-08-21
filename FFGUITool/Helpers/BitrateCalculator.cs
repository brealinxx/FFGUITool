using System;
using FFGUITool.Models;

namespace FFGUITool.Helpers
{
    public static class BitrateCalculator
    {
        public static int CalculateOptimalBitrate(VideoInfo videoInfo, int compressionPercentage, string codec)
        {
            if (videoInfo == null || videoInfo.Bitrate == 0)
                return 2000; // Default bitrate

            // Calculate base target bitrate
            var targetBitrate = (int)(videoInfo.Bitrate * compressionPercentage / 100.0);

            // Adjust for codec efficiency
            targetBitrate = AdjustForCodec(targetBitrate, codec);

            // Clamp to reasonable limits based on resolution
            targetBitrate = ClampByResolution(targetBitrate, videoInfo.Resolution);

            return targetBitrate;
        }

        private static int AdjustForCodec(int baseBitrate, string codec)
        {
            return codec switch
            {
                "libx265" => (int)(baseBitrate * 0.7),    // H.265 is ~30% more efficient
                "libvpx-vp9" => (int)(baseBitrate * 0.8), // VP9 is ~20% more efficient
                "av1" => (int)(baseBitrate * 0.6),        // AV1 is ~40% more efficient
                _ => baseBitrate                          // H.264 baseline
            };
        }

        private static int ClampByResolution(int bitrate, string resolution)
        {
            var (min, max) = resolution switch
            {
                var r when r.Contains("3840") || r.Contains("2160") => (3000, 50000), // 4K
                var r when r.Contains("2560") || r.Contains("1440") => (2000, 30000), // 1440p
                var r when r.Contains("1920") || r.Contains("1080") => (1000, 20000), // 1080p
                var r when r.Contains("1280") || r.Contains("720") => (500, 10000),   // 720p
                var r when r.Contains("854") || r.Contains("480") => (300, 5000),     // 480p
                _ => (200, 5000)
            };

            return Math.Max(min, Math.Min(bitrate, max));
        }

        public static long EstimateFileSize(int bitrateKbps, TimeSpan duration)
        {
            // File size = (video bitrate + audio bitrate) * duration / 8
            // Assume audio bitrate is ~128 kbps
            var totalBitrateKbps = bitrateKbps + 128;
            return (long)(totalBitrateKbps * duration.TotalSeconds * 1024 / 8);
        }

        public static string GetRecommendedBitrateRange(string resolution)
        {
            return resolution switch
            {
                var r when r.Contains("3840") || r.Contains("2160") => "15000-25000 kbps",
                var r when r.Contains("2560") || r.Contains("1440") => "8000-16000 kbps",
                var r when r.Contains("1920") || r.Contains("1080") => "4000-8000 kbps",
                var r when r.Contains("1280") || r.Contains("720") => "2000-4000 kbps",
                var r when r.Contains("854") || r.Contains("480") => "1000-2000 kbps",
                _ => "500-1500 kbps"
            };
        }
    }
}