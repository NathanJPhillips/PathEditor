using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.Utils;

public class ColorToSolidColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? DependencyProperty.UnsetValue
            : value is Color color ? new SolidColorBrush(color)
            : throw new InvalidOperationException($"Unsupported type ({value.GetType().Name})");

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        throw new NotImplementedException();
}
