using NobleTech.Products.PathEditor.Collections;
using NobleTech.Products.PathEditor.Utils;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal class DrawablePath
{
    private readonly Vector inflation;

    public DrawablePath(IEnumerable<Point> points, Color strokeColor, double strokeThickness)
    {
        this.points = [.. points ?? throw new ArgumentNullException(nameof(points))];
        if (this.points.Count == 0)
            throw new ArgumentException("Path must contain at least one point", nameof(points));
        Points.Adding += OnPointAdding;
        Points.Added += OnPointAdded;

        StrokeColor = strokeColor;
        StrokeThickness = strokeThickness;
        inflation = new Vector(StrokeThickness / 2, StrokeThickness / 2);

        Point firstPoint = this.points[0];
        Rect bounds = new(firstPoint, firstPoint);
        foreach (Point point in Points)
            bounds.Union(point);
        bounds.Inflate(inflation.X, inflation.Y);
        Bounds = bounds;
    }

    private readonly ObservableList<Point, List<Point>> points;
    public IObservableCollection<Point> Points => points;

    public int SegmentCount
    {
        get
        {
            Debug.Assert(Points.Count != 0, "No points in the path");
            return Points.Count - 1;
        }
    }

    public Color StrokeColor { get; }

    public double StrokeThickness { get; }

    public Rect Bounds { get; private set; }

    public static DrawnPaths.DrawnPath ToDrawnPath(DrawablePath drawablePath) =>
        new([.. drawablePath.Points], drawablePath.StrokeColor, drawablePath.StrokeThickness);

    private void OnPointAdding(object sender, CancelEventArgs<Point> args)
    {
        if (args.Value == points[^1])
            args.Cancel = true;
    }

    private void OnPointAdded(object sender, Point point) =>
        Bounds = Bounds.UnionWith(RectUtils.AroundPoint(point, inflation));
}
