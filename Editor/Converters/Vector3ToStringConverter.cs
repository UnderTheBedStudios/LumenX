using System;
using System.Globalization;
using System.Numerics;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace LumenX.Converters;

public class Vector3ToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Vector3 v)
            return $"{v.X.ToString(CultureInfo.InvariantCulture)}, " +
                   $"{v.Y.ToString(CultureInfo.InvariantCulture)}, " +
                   $"{v.Z.ToString(CultureInfo.InvariantCulture)}";
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s)
            return BindingOperations.DoNothing;

        var parts = s.Trim('<', '>', ' ')
                      .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 3 &&
            float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            return new Vector3(x, y, z);
        }

        // Invalid/partial input (e.g. mid-typing "-") - leave the bound value alone
        // instead of throwing or clobbering it with garbage.
        return BindingOperations.DoNothing;
    }
}