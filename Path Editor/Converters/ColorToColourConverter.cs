using NobleTech.Products.PathEditor.ViewModels;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.Converters;

internal class ColorToColourConverter : ValueConverterBase<Color, Colour>
{
    protected override object ConvertTo(Color color) => new Colour(color);
    protected override object ConvertFrom(Colour colour) => colour.Color;
}
