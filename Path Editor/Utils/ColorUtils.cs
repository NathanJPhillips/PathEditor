using System.Windows.Media;

namespace NobleTech.Products.PathEditor.Utils;

internal static class ColorUtils
{
    public static Color GetContrastingColour(this Color color) =>
        new()
        {
            A = color.A,
            R = (byte)(255 - color.R),
            G = (byte)(255 - color.G),
            B = (byte)(255 - color.B),
        };
}
