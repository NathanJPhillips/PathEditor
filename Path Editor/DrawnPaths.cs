using NobleTech.Products.PathEditor.Utils;
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

public partial class DrawnPaths(DrawnPaths.DrawnPath[] drawnPaths, Size canvasSize)
{
    public class DrawnPath(Point[] points, Color strokeColor, double strokeThickness)
    {
        public Point[] Points = points;
        public Color StrokeColor = strokeColor;
        public double StrokeThickness = strokeThickness;
    }

    internal readonly DrawnPath[] drawnPaths = drawnPaths;
    internal readonly Size canvasSize = canvasSize;

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
                if (!pointMatch.Success)
                    return null;
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

    public void SaveAsBitmap(BitmapEncoder encoder, Stream stream)
    {
        Canvas canvas = new() { Background = Brushes.Transparent };
        Draw(canvas);
        BitmapUtils.SaveAs(canvas, (int)canvasSize.Width, (int)canvasSize.Height, encoder, stream);
    }

    public void Draw(Canvas canvas)
    {
        canvas.Width = canvasSize.Width;
        canvas.Height = canvasSize.Height;
        canvas.Children.AddRange(drawnPaths.Select(CreatePath));
    }

    public Storyboard Animate(Canvas canvas) => Animate(canvas, TimeSpan.FromSeconds(0.5));

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
