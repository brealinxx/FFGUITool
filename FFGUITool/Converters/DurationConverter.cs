using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FFGUITool.Converters
{
    /// <summary>
    /// 时长格式化转换器
    /// </summary>
    public class DurationConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double seconds)
            {
                var duration = TimeSpan.FromSeconds(seconds);
                return duration.Hours > 0
                    ? $"{duration:hh\\:mm\\:ss}"
                    : $"{duration:mm\\:ss}";
            }
            else if (value is TimeSpan timeSpan)
            {
                return timeSpan.Hours > 0
                    ? $"{timeSpan:hh\\:mm\\:ss}"
                    : $"{timeSpan:mm\\:ss}";
            }
            return "00:00";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}