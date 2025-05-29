using NobleTech.Products.PathEditor.ViewModels;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.Converters;

internal class ColorToColourPropertyConverter : ValueConverterBase<Color, double>
{
    public string? PropertyName { get; set; }

    private Colour? cachedColour;

    protected override object ConvertTo(Color color)
    {
        if (PropertyName is null)
            return DependencyProperty.UnsetValue;
        cachedColour = new Colour(color);
        if (typeof(Colour).GetProperty(PropertyName) is not PropertyInfo property)
            throw new InvalidOperationException($"Property {PropertyName} does not exist on Colour");
        return property.GetValue(cachedColour)
            ?? throw new InvalidOperationException($"Property {PropertyName} is null");
    }

    protected override object ConvertFrom(double value)
    {
        if (cachedColour is not Colour colour)
            return DependencyProperty.UnsetValue;
        switch (PropertyName)
        {
        case nameof(Colour.Hue):
            return new Colour(value, colour.Saturation, colour.Value).Color;
        case nameof(Colour.Saturation):
            return new Colour(colour.Hue, value, colour.Value).Color;
        case nameof(Colour.Value):
            return new Colour(colour.Hue, colour.Saturation, value).Color;
        }
        throw new InvalidOperationException($"Property {PropertyName} is not Hue, Saturation or Value");
    }
}
