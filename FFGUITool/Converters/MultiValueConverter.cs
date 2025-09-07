using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace FFGUITool.Converters
{
    /// <summary>
    /// 多值转换器基类
    /// </summary>
    public abstract class MultiValueConverterBase : IMultiValueConverter
    {
        public abstract object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture);

        public virtual object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 所有值为真时返回真的多值转换器
    /// </summary>
    public class AllTrueConverter : MultiValueConverterBase
    {
        public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Count == 0)
                return false;

            return values.All(v => v is bool b && b);
        }
    }

    /// <summary>
    /// 任一值为真时返回真的多值转换器
    /// </summary>
    public class AnyTrueConverter : MultiValueConverterBase
    {
        public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Count == 0)
                return false;

            return values.Any(v => v is bool b && b);
        }
    }
}