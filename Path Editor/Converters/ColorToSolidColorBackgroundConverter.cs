using NobleTech.Products.PathEditor.ViewModels;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.Converters;

internal class ColorToSolidColorBackgroundConverter : ValueConverterBase<Color, SolidColorBackground>
{
    protected override object ConvertTo(Color color) => new SolidColorBackground(color);

    protected override object ConvertFrom(SolidColorBackground background) => background.Color;
}
