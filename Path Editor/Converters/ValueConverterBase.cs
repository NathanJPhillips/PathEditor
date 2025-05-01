using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NobleTech.Products.PathEditor.Converters;

public abstract class ValueConverterBase<TFrom, TTo> : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ((Func<object, object>)(!Invert ? DoConvertTo : DoConvertFrom))(value);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        ((Func<object, object>)(!Invert ? DoConvertFrom : DoConvertTo))(value);

    private object DoConvertTo(object value) =>
        DoConversion<TFrom>(value, ConvertTo);
    private object DoConvertFrom(object value) =>
        DoConversion<TTo>(value, ConvertFrom);

    protected abstract object ConvertTo(TFrom value);
    protected abstract object ConvertFrom(TTo value);

    private static object DoConversion<TConvertFrom>(object value, Func<TConvertFrom, object> converter) =>
        value is null ? DependencyProperty.UnsetValue
            : value is TConvertFrom fromValue ? converter(fromValue)
            : throw new InvalidOperationException($"Unsupported type ({value.GetType().Name})");
}
