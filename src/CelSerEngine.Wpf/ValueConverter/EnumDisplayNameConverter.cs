using CelSerEngine.Core.Extensions;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CelSerEngine.ValueConverter;

/// <summary>
/// Inspired by this post
/// https://brianlagunas.com/a-better-way-to-data-bind-enums-in-wpf/
/// </summary>
[ValueConversion(typeof(Enum), typeof(string))]
public class EnumDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var enumValue = value as Enum;

        return enumValue?.GetDisplayName() ?? "No Value";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
