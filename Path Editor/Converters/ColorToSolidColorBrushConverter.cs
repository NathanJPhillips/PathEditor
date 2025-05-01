using System.Windows.Media;

namespace NobleTech.Products.PathEditor.Converters;

public class ColorToSolidColorBrushConverter : ValueConverterBase<Color, SolidColorBrush>
{
    protected override object ConvertTo(Color color) => new SolidColorBrush(color);

    protected override object ConvertFrom(SolidColorBrush brush) => brush.Color;
}
