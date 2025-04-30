using System.Windows;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

public record DrawablePath(List<Point> Points, Color StrokeColor, double StrokeThickness)
{
    public static DrawablePath FromDrawnPath(DrawnPaths.DrawnPath drawnPath) =>
        new([.. drawnPath.Points], drawnPath.StrokeColor, drawnPath.StrokeThickness);

    public static DrawnPaths.DrawnPath ToDrawnPath(DrawablePath drawablePath) =>
        new([.. drawablePath.Points], drawablePath.StrokeColor, drawablePath.StrokeThickness);
}
