using System.Windows.Data;

namespace NobleTech.Products.PathEditor.Converters;

internal class BooleanNotConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        value is bool booleanValue ? !booleanValue : throw new InvalidOperationException("Value is not a boolean");

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        Convert(value, targetType, parameter, culture);
}
