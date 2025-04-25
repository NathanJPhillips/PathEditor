using NobleTech.Products.PathEditor.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace NobleTech.Products.PathEditor;

/// <summary>
/// A collection of drawn paths.
/// </summary>
/// <param name="drawnPaths">An array of <see cref="DrawnPath"/> objects representing the paths to be drawn.</param>
/// <param name="canvasSize">The size of the drawing canvas.</param>
public class DrawnPaths(DrawnPaths.DrawnPath[] drawnPaths, Size canvasSize)
{
    /// <summary>
    /// Represents a single drawn path with its points, stroke color, and stroke thickness.
    /// </summary>
    /// <param name="points">An array of <see cref="Point"/> objects representing the points of the path.</param>
    /// <param name="strokeColor">The color of the path's stroke.</param>
    /// <param name="strokeThickness">The thickness of the path's stroke.</param>
    public class DrawnPath(Point[] points, Color strokeColor, double strokeThickness)
    {
        /// <summary>
        /// The points of the path.
        /// </summary>
        public readonly Point[] Points = points;

        /// <summary>
        /// The color of the path's stroke.
        /// </summary>
        public readonly Color StrokeColor = strokeColor;

        /// <summary>
        /// The thickness of the path's stroke.
        /// </summary>
        public readonly double StrokeThickness = strokeThickness;
    }

    /// <summary>
    /// The collection of drawn paths.
    /// </summary>
    internal readonly DrawnPath[] drawnPaths = drawnPaths;

    /// <summary>
    /// The size of the drawing canvas.
    /// </summary>
    internal readonly Size canvasSize = canvasSize;

    /// <summary>
    /// Animates the drawn paths onto the specified canvas with a default duration.
    /// </summary>
    /// <param name="canvas">The canvas onto which the paths will be animated.</param>
    /// <returns>A <see cref="Storyboard"/> that controls the animation.</returns>
    public Storyboard Animate(Canvas canvas) => Animate(canvas, TimeSpan.FromSeconds(0.5));

    /// <summary>
    /// Animates the drawn paths onto the specified canvas over a given duration.
    /// </summary>
    /// <param name="canvas">The canvas onto which the paths will be animated.</param>
    /// <param name="totalDuration">The total duration of the animation.</param>
    /// <returns>A <see cref="Storyboard"/> that controls the animation.</returns>
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

    /// <summary>
    /// Calculates the distance between two points.
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>The distance between the two points.</returns>
    private static double DistanceBetween(Point point1, Point point2) => (point2 - point1).Length;
}
