using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FFGUITool.Converters
{
    /// <summary>
    /// 比特率格式化转换器
    /// </summary>
    public class BitrateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int bitrate)
            {
                return $"{bitrate}k";
            }
            else if (value is double doubleBitrate)
            {
                return $"{doubleBitrate:F0}k";
            }
            return "0k";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                str = str.Replace("k", "").Replace("K", "").Trim();
                if (int.TryParse(str, out int result))
                {
                    return result;
                }
            }
            return 0;
        }
    }
}