using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FFGUITool.Converters
{
    /// <summary>
    /// 文件大小格式化转换器
    /// </summary>
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return FormatFileSize(bytes);
            }
            else if (value is int intBytes)
            {
                return FormatFileSize(intBytes);
            }
            else if (value is double doubleBytes)
            {
                return FormatFileSize((long)doubleBytes);
            }
            return "0 B";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}