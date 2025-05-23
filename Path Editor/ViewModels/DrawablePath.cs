using CommunityToolkit.Mvvm.ComponentModel;
using NobleTech.Products.PathEditor.Collections;
using NobleTech.Products.PathEditor.Geometry;
using NobleTech.Products.PathEditor.Utils;
using System.Diagnostics;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal class DrawablePath : ObservableObject
{
    private readonly EditorViewModel parent;
    private readonly Size inflation;

    public DrawablePath(IEnumerable<Point> points, Color strokeColor, double strokeThickness, EditorViewModel parent)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));

        this.points = [.. points ?? throw new ArgumentNullException(nameof(points))];
        if (this.points.Count == 0)
            throw new ArgumentException("Path must contain at least one point", nameof(points));
        Points.Adding += OnPointAdding;
        Points.Added += OnPointAdded;

        StrokeColor = strokeColor;
        StrokeThickness = strokeThickness;
        inflation = new(StrokeThickness / 2, StrokeThickness / 2);

        Point firstPoint = this.points[0];
        Rectangle? bounds = null;
        foreach (Point point in Points)
            bounds |= point;
        Bounds = bounds?.Inflate(inflation) ?? Rectangle.Empty;

        parent.SelectedPaths.Added += (paths, path) => SelectedPaths_Changed(path == this);
        parent.SelectedPaths.Removed += (paths, path) => SelectedPaths_Changed(path == this);
        parent.SelectedPaths.Reset += paths => SelectedPaths_Changed(true);
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

    public Rectangle Bounds { get; private set; }

    public bool IsSelected => parent.SelectedPaths.Contains(this);

    public bool HitTest(Point point)
    {
        if (!Bounds.Contains(point))
            return false;
        // Use the squared length to avoid repeated square root calculations
        double strokeHalfThicknessSquared = StrokeThickness * StrokeThickness / 4;
        return Points
                .SelectFromPairs(
                    (a, b) =>
                    {
                        Vector v = b - a;
                        Debug.Assert(v.LengthSquared > 0, "Segment length should not be zero");
                        // Project w onto v to find the closest point on the line segment  
                        double projection = Vector.DotProduct(point - a, v) / v.LengthSquared;
                        Point closestPoint =
                            projection < 0 ? a      // Start point
                            : projection > 1 ? b    // End point
                            : (a + projection * v); // Closest point on the segment
                        // Check if distance to the closest point is less than the stroke thickness
                        return (point - closestPoint).LengthSquared < strokeHalfThicknessSquared;
                    })
                .Any(isHit => isHit);
    }

    public static Func<DrawnPaths.DrawnPath, DrawablePath> FromDrawnPath(EditorViewModel parent) =>
        drawnPath => new DrawablePath([.. drawnPath.Points], drawnPath.StrokeColor, drawnPath.StrokeThickness, parent);

    public static DrawnPaths.DrawnPath ToDrawnPath(DrawablePath drawablePath) =>
        new([.. drawablePath.Points], drawablePath.StrokeColor, drawablePath.StrokeThickness);

    private void OnPointAdding(object sender, CancelEventArgs<Point> args)
    {
        if (args.Value == points[^1])
            args.Cancel = true;
    }

    private void OnPointAdded(object sender, Point point)
    {
        Bounds |= new Rectangle(point, Size.Empty).Inflate(inflation);
        OnPropertyChanged(nameof(Bounds));
        OnPropertyChanged(nameof(SegmentCount));
    }

    private void SelectedPaths_Changed(bool isThisPath)
    {
        if (isThisPath)
            OnPropertyChanged(nameof(IsSelected));
    }
}
