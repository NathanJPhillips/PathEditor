using NobleTech.Products.PathEditor.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace NobleTech.Products.PathEditor;

public class DrawnPaths(DrawnPaths.DrawnPath[] drawnPaths, Size canvasSize)
{
    public class DrawnPath(Point[] points, Color strokeColor, double strokeThickness)
    {
        public Point[] Points = points;
        public Color StrokeColor = strokeColor;
        public double StrokeThickness = strokeThickness;
    }

    internal readonly DrawnPath[] drawnPaths = drawnPaths;
    internal readonly Size canvasSize = canvasSize;

    public Storyboard Animate(Canvas canvas) => Animate(canvas, TimeSpan.FromSeconds(0.5));

    public Storyboard Animate(Canvas canvas, TimeSpan totalDuration)
    {
        canvas.Width = canvasSize.Width;
        canvas.Height = canvasSize.Height;

        var paths = (
            from drawnPath in
                drawnPaths.SelectWithNext(
                    (path, next) =>
                    new
                    {
                        path.Points,
                        path.StrokeColor,
                        path.StrokeThickness,
                        DistanceToNext = next is null ? 0 : DistanceBetween(path.Points[^1], path.Points[0]),
                    })
            let length = drawnPath.Points.SelectFromPairs(DistanceBetween).Sum()
            let strokeLength = length / drawnPath.StrokeThickness
            select
                new
                {
                    Path =
                        new Path()
                        {
                            Stroke = new SolidColorBrush(drawnPath.StrokeColor),
                            StrokeThickness = drawnPath.StrokeThickness,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            StrokeLineJoin = PenLineJoin.Round,
                            StrokeDashCap = PenLineCap.Round,
                            StrokeDashArray = [strokeLength, strokeLength + 2],
                            StrokeDashOffset = strokeLength + 1,
                            Data =
                                new PathGeometry()
                                {
                                    Figures =
                                        [
                                            new PathFigure()
                                            {
                                                StartPoint = drawnPath.Points[0],
                                                Segments = [ new PolyLineSegment(drawnPath.Points[1..], true) ],
                                            },
                                        ],
                                },
                        },
                    Length = length,
                    drawnPath.DistanceToNext,
                }).ToArray();

        double totalLength = paths.Sum(shape => shape.Length + shape.DistanceToNext);

        Storyboard storyboard = new();
        double startLength = 0;
        foreach (var path in paths)
        {
            DoubleAnimation animation =
                new()
                {
                    From = (double)path.Length / path.Path.StrokeThickness,
                    To = 0,
                    Duration = new(totalDuration * path.Length / totalLength),
                    BeginTime = totalDuration * startLength / totalLength,
                };
            Storyboard.SetTarget(animation, path.Path);
            Storyboard.SetTargetProperty(animation, new(Shape.StrokeDashOffsetProperty));
            storyboard.Children.Add(animation);
            canvas.Children.Add(path.Path);
            startLength += path.Length + path.DistanceToNext;
        }

        storyboard.Begin();
        return storyboard;
    }

    private static double DistanceBetween(Point point1, Point point2) => (point2 - point1).Length;
}
