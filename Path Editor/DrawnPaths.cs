using NobleTech.Products.PathEditor.Utils;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace NobleTech.Products.PathEditor;

/// <summary>
/// A collection of drawn paths.
/// </summary>
/// <param name="drawnPaths">An array of <see cref="DrawnPath"/> objects representing the paths to be drawn.</param>
/// <param name="canvasSize">The size of the drawing canvas.</param>
public partial class DrawnPaths(DrawnPaths.DrawnPath[] drawnPaths, Size canvasSize)
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
    /// Load a <see cref="DrawnPaths"/> object from a binary ".paths" file.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The loaded <see cref="DrawnPaths"/> object.</returns>
    public static DrawnPaths LoadFromBinary(Stream stream)
    {
        using BinaryReader reader = new(stream);
        double width = reader.ReadDouble();
        double height = reader.ReadDouble();
        int pathCount = reader.ReadInt32();
        var paths = new DrawnPath[pathCount];
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            int pointCount = reader.ReadInt32();
            var points = new Point[pointCount];
            for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
                points[pointIndex] = new(reader.ReadDouble(), reader.ReadDouble());
            byte a = reader.ReadByte();
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            double strokeThickness = reader.ReadDouble();
            paths[pathIndex] = new(points, Color.FromArgb(a, r, g, b), strokeThickness);
        }
        return new(paths, new(width, height));
    }

    /// <summary>
    /// Saves the current <see cref="DrawnPaths"/> object to a binary ".paths" file.
    /// </summary>
    /// <param name="stream">The stream to which to write.</param>
    /// <remarks>
    /// The binary file will include the canvas size, the number of paths, and for each path:
    /// the points, stroke color (ARGB), and stroke thickness.
    /// </remarks>
    public void SaveAsBinary(Stream stream)
    {
        using BinaryWriter writer = new(stream);
        writer.Write(canvasSize.Width);
        writer.Write(canvasSize.Height);
        writer.Write(drawnPaths.Length);
        foreach (DrawnPath path in drawnPaths)
        {
            writer.Write(path.Points.Length);
            foreach (Point point in path.Points)
            {
                writer.Write(point.X);
                writer.Write(point.Y);
            }
            writer.Write(path.StrokeColor.A);
            writer.Write(path.StrokeColor.R);
            writer.Write(path.StrokeColor.G);
            writer.Write(path.StrokeColor.B);
            writer.Write(path.StrokeThickness);
        }
    }

    /// <summary>
    /// Loads a <see cref="DrawnPaths"/> object from a C# source file.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>
    /// A <see cref="DrawnPaths"/> object if the file is successfully parsed; otherwise, null.
    /// </returns>
    /// <remarks>
    /// The method reads a C# source file containing a representation of a <see cref="DrawnPaths"/> object.
    /// It parses the file to extract the paths, their points, stroke colors, stroke thicknesses,
    /// and the canvas size. If the file format is invalid or parsing fails, the method returns null.
    /// </remarks>
    public static DrawnPaths? LoadFromCSharp(Stream stream)
    {
        using StreamReader reader = new(stream);
        string? line;
        do
        {
            if ((line = reader.ReadLine()) is null)
                return null;
        } while (!CSharpDefinitionRegex().IsMatch(line));
        if ((line = reader.ReadLine()) is null || !CSharpNewRegex().IsMatch(line))
            return null;
        if ((line = reader.ReadLine()) is null || !CSharpListStartRegex().IsMatch(line))
            return null;
        Regex pathRegex = CSharpPathRegex();
        Regex pointsRegex = CSharpPointsRegex();
        List<DrawnPath> paths = [];
        while (true)
        {
            if ((line = reader.ReadLine()) is null)
                return null;
            Match pathMatch = pathRegex.Match(line);
            if (!pathMatch.Success)
                break;
            MatchCollection pointsMatches = pointsRegex.Matches(pathMatch.Groups["points"].Value);
            if (pointsMatches.Count == 0)
                return null;
            var points = new Point[pointsMatches.Count];
            for (int pointIndex = 0; pointIndex < pointsMatches.Count; pointIndex++)
            {
                Match pointMatch = pointsMatches[pointIndex];
                Debug.Assert(pointMatch.Success);
                double x = double.Parse(pointMatch.Groups["x"].Value);
                double y = double.Parse(pointMatch.Groups["y"].Value);
                points[pointIndex] = new(x, y);
            }
            var color = Color.FromArgb(
                byte.Parse(pathMatch.Groups["a"].Value),
                byte.Parse(pathMatch.Groups["r"].Value),
                byte.Parse(pathMatch.Groups["g"].Value),
                byte.Parse(pathMatch.Groups["b"].Value));
            double strokeThickness = double.Parse(pathMatch.Groups["thickness"].Value);
            paths.Add(new(points, color, strokeThickness));
        }
        if (!CSharpListEndRegex().IsMatch(line))
            return null;
        if ((line = reader.ReadLine()) is null)
            return null;
        Match canvasSizeMatch = CSharpCanvasSizeRegex().Match(line);
        return !canvasSizeMatch.Success ? null
            : new(
                [.. paths],
                new(double.Parse(canvasSizeMatch.Groups["width"].Value), double.Parse(canvasSizeMatch.Groups["height"].Value)));
    }

    /// <summary>
    /// Saves the current <see cref="DrawnPaths"/> object as a C# source file.
    /// </summary>
    /// <param name="stream">The stream to which to write.</param>
    /// <param name="name">The name of the file, used as the basis for name of the <see cref="DrawnPaths"/> object in the generated C# file.</param>
    /// <remarks>
    /// The generated C# file will include a representation of the <see cref="DrawnPaths"/> object, 
    /// including all paths, their points, stroke colors, and stroke thicknesses, 
    /// as well as the canvas size.
    /// </remarks>
    public void SaveAsCSharp(Stream stream, string name)
    {
        using StreamWriter writer = new(stream);
        writer.WriteLine($"    // Created {DateTime.Now} by NobleTech Path Editor");
        writer.WriteLine($"    DrawnPaths {name.Replace(' ', '_')} =");
        writer.WriteLine("        new(");
        writer.WriteLine("            [");
        foreach (DrawnPath path in drawnPaths)
        {
            writer.WriteLine(
                $"                new([{string.Join(", ", path.Points.Select(pt => $"new({pt.X}, {pt.Y})"))}], new() {{ A = {path.StrokeColor.A}, R = {path.StrokeColor.R}, G = {path.StrokeColor.G}, B = {path.StrokeColor.B} }}, StrokeThickness: {path.StrokeThickness}),");
        }
        writer.WriteLine("            ],");
        writer.WriteLine($"            new({canvasSize.Width}, {canvasSize.Height}));");
    }

    /// <summary>
    /// Saves the current <see cref="DrawnPaths"/> object as a bitmap image.
    /// </summary>
    /// <param name="encoder">The bitmap encoder to use to save the image.</param>
    /// <param name="stream">The stream to which to write the image.</param>
    public void SaveAsBitmap(BitmapEncoder encoder, Stream stream)
    {
        Canvas canvas = new() { Background = Brushes.Transparent };
        Draw(canvas);
        BitmapUtils.SaveAs(canvas, (int)canvasSize.Width, (int)canvasSize.Height, encoder, stream);
    }

    /// <summary>
    /// Draw the paths on the given canvas.
    /// </summary>
    /// <param name="canvas">The canvas on which to draw the paths.</param>
    public void Draw(Canvas canvas)
    {
        canvas.Width = canvasSize.Width;
        canvas.Height = canvasSize.Height;
        canvas.Children.AddRange(drawnPaths.Select(CreatePath));
    }

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

        static Path CreatePath(DrawnPath drawnPath, double strokeLength)
        {
            Path path = DrawnPaths.CreatePath(drawnPath);
            path.StrokeDashCap = PenLineCap.Round;
            path.StrokeDashArray = [strokeLength, strokeLength + 2];
            path.StrokeDashOffset = strokeLength + 1;
            return path;
        }

        var paths = (
            from drawnPath in
                drawnPaths.SelectWithNext(
                    (path, next) =>
                    new
                    {
                        Path = path,
                        DistanceToNext = next is null ? 0 : DistanceBetween(path.Points[^1], path.Points[0]),
                    })
            let length = drawnPath.Path.Points.SelectFromPairs(DistanceBetween).Sum()
            select
                new
                {
                    Path = CreatePath(drawnPath.Path, length / drawnPath.Path.StrokeThickness),
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
    /// Creates a <see cref="Path"/> object from a <see cref="DrawnPath"/> object.
    /// </summary>
    /// <param name="path">The <see cref="DrawnPath"/> object to convert.</param>
    /// <returns>A <see cref="Path"/> object representing the <see cref="DrawnPath"/>.</returns>
    private static Path CreatePath(DrawnPath path) =>
        new()
        {
            Stroke = new SolidColorBrush(path.StrokeColor),
            StrokeThickness = path.StrokeThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Data =
                new PathGeometry()
                {
                    Figures =
                        [
                            new PathFigure()
                            {
                                StartPoint = path.Points[0],
                                Segments = [ new PolyLineSegment(path.Points[1..], true) ],
                            },
                        ],
                },
        };

    /// <summary>
    /// Calculates the distance between two points.
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>The distance between the two points.</returns>
    private static double DistanceBetween(Point point1, Point point2) => (point2 - point1).Length;

    [GeneratedRegex("""^\s*DrawnPaths\s+(?<name>[\w_\d]+)\s*=\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpDefinitionRegex();
    [GeneratedRegex("""^\s*new\s*\(\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpNewRegex();
    [GeneratedRegex("""^\s*\[\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpListStartRegex();
    [GeneratedRegex("""^\s*new\s*\(\s*\[\s*(?<points>.+?)\],\s*new\(\)\s*\{\s*A\s*=\s*(?<a>\d+),\s*R\s*=\s*(?<r>\d+),\s*G\s*=\s*(?<g>\d+),\s*B\s*=\s*(?<b>\d+)\s*\},\s*StrokeThickness:\s*(?<thickness>\d+(?:\.\d+)?)\s*\)\s*,?\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpPathRegex();
    [GeneratedRegex("""\s*new\s*\(\s*(?<x>\d+(?:\.\d+)?),\s*(?<y>\d+(?:\.\d+)?)\s*\)\s*,?\s*""", RegexOptions.Compiled)]
    private static partial Regex CSharpPointsRegex();
    [GeneratedRegex("""^\s*\]\s*,\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpListEndRegex();
    [GeneratedRegex("""^\s*new\s*\(\s*(?<width>\d+(?:\.\d+)?),\s*(?<height>\d+(?:\.\d+)?)\s*\)\s*\)\s*;\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpCanvasSizeRegex();
}
