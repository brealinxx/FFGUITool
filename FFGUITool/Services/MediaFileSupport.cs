using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FFGUITool.Services
{
    public static class MediaFileSupport
    {
        public static readonly string[] VideoExtensions =
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm"
        };

        public static readonly string[] AudioExtensions =
        {
            ".mp3", ".aac", ".m4a", ".wav", ".flac", ".ogg", ".wma"
        };

        public static readonly string[] ImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".bmp",
            ".gif", ".tif", ".tiff", ".ico", ".tga", ".avif"
        };

        public static bool IsVideoExtension(string? extension)
        {
            return ContainsExtension(VideoExtensions, extension);
        }

        public static bool IsAudioExtension(string? extension)
        {
            return ContainsExtension(AudioExtensions, extension);
        }

        public static bool IsImageExtension(string? extension)
        {
            return ContainsExtension(ImageExtensions, extension);
        }

        public static bool IsSupportedDroppedPath(string? path, bool imageMode)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (Directory.Exists(path))
            {
                return true;
            }

            return imageMode
                ? IsImageExtension(Path.GetExtension(path))
                : IsVideoExtension(Path.GetExtension(path)) || IsAudioExtension(Path.GetExtension(path));
        }

        public static IEnumerable<string> GetBatchInputFiles(
            string inputPath,
            bool imageMode,
            bool enableAudioConversion,
            bool includeSubfolders = false)
        {
            if (!Directory.Exists(inputPath))
            {
                return Array.Empty<string>();
            }

            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = includeSubfolders,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };
            return Directory.EnumerateFiles(inputPath, "*.*", enumerationOptions)
                .Where(file =>
                {
                    var extension = Path.GetExtension(file);
                    if (imageMode)
                    {
                        return IsImageExtension(extension);
                    }

                    return enableAudioConversion
                        ? IsVideoExtension(extension) || IsAudioExtension(extension)
                        : IsVideoExtension(extension);
                })
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);
        }

        private static bool ContainsExtension(IEnumerable<string> extensions, string? extension)
        {
            return extensions.Contains((extension ?? "").ToLowerInvariant());
        }
    }
}
