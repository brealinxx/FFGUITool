using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FFGUITool.Converters
{
    /// <summary>
    /// 字符串转颜色转换器
    /// </summary>
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string colorName)
            {
                return colorName.ToLower() switch
                {
                    "green" => Brushes.Green,
                    "red" => Brushes.Red,
                    "orange" => Brushes.Orange,
                    "blue" => Brushes.Blue,
                    "gray" or "grey" => Brushes.Gray,
                    "yellow" => Brushes.Yellow,
                    "black" => Brushes.Black,
                    _ => Brushes.Black
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ISolidColorBrush brush)
            {
                if (brush == Brushes.Green) return "Green";
                if (brush == Brushes.Red) return "Red";
                if (brush == Brushes.Orange) return "Orange";
                if (brush == Brushes.Blue) return "Blue";
                if (brush == Brushes.Gray) return "Gray";
                if (brush == Brushes.Yellow) return "Yellow";
            }
            return "Black";
        }
    }
}