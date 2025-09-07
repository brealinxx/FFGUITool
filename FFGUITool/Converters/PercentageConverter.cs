using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FFGUITool.Converters
{
    /// <summary>
    /// 百分比转换器
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return $"{doubleValue:F1}%";
            }
            else if (value is int intValue)
            {
                return $"{intValue}%";
            }
            return "0%";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                str = str.Replace("%", "").Trim();
                if (double.TryParse(str, out double result))
                {
                    return result;
                }
            }
            return 0.0;
        }
    }
}