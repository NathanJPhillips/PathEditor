using System.Windows.Controls;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal class SolidColorBackground(Color color) : IBackground
{
    public Color Color { get; } = color;

    public void DrawTo(Canvas canvas) => canvas.Background = new SolidColorBrush(Color);
}
