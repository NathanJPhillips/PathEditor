using System.Globalization;
using System.Windows.Data;

namespace NobleTech.Products.PathEditor.Converters;

public class EqualValuesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Equals(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool isChecked && isChecked ? parameter : Binding.DoNothing;
}
