using System.Globalization;
using System.Windows.Data;

namespace NobleTech.Products.PathEditor.Converters;

internal class IsNullOrEmptyConverter : IValueConverter
{
    public bool IsNegated { get; set; } = false;
    public bool IsInverted { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        IsInverted
            ? throw new NotSupportedException("Can only convert in the forward direction when not inverted.")
            : IsNullOrEmpty(value, targetType) ^ IsNegated;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        IsInverted
            ? IsNullOrEmpty(value, targetType) ^ IsNegated
            : throw new NotSupportedException("Can only convert in the backward direction when inverted.");

    private bool IsNullOrEmpty(object value, Type targetType) =>
        targetType != typeof(bool)
            ? throw new InvalidOperationException("IsNullOrEmptyConverter can only convert to bool.")
            : value
                switch
                {
                    null => true,
                    string str => string.IsNullOrEmpty(str),
                    _ => false,
                };
}
