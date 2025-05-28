using NobleTech.Products.PathEditor.Geometry;
using NobleTech.Products.PathEditor.Utils;
using NobleTech.Products.PathEditor.ViewModels;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using AnyGeometry = System.Windows.Media.Geometry;

namespace NobleTech.Products.PathEditor;

internal sealed class Views(Canvas canvas) : IDisposable
{
    private readonly Dictionary<DrawablePath, View> views = [];

    public void Add(DrawablePath path) => views.Add(path, new View(path, canvas, canvas.Children.Count));

    public void Insert(int index, DrawablePath path) => views.Add(path, new View(path, canvas, index));

    public void Remove(DrawablePath path)
    {
        if (views.Remove(path, out View? view))
            view.Dispose();
    }

    public void Clear()
    {
        views.Values.DisposeAll();
        views.Clear();
    }

    public void Dispose() => Clear();

    private sealed class View : IDisposable
    {
        private readonly DrawablePath drawablePath;
        private readonly Canvas canvas;
        private readonly PolyLineSegment segment;
        private readonly Path path;
        private Path? outline;

        private double OutlineThickness => canvas.Width / 150;

        public View(DrawablePath drawablePath, Canvas canvas, int index)
        {
            this.drawablePath = drawablePath;
            this.canvas = canvas;
            segment = new(drawablePath.Points.Skip(1).Select(pt => (System.Windows.Point)pt), isStroked: true);
            path =
                new()
                {
                    Stroke = new SolidColorBrush(drawablePath.StrokeColor),
                    StrokeThickness = drawablePath.StrokeThickness,
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
                                        StartPoint = drawablePath.Points.First(),
                                        Segments = [segment],
                                    }
                                ],
                        },
                    IsHitTestVisible = false,
                };
            canvas.Children.Insert(index, path);
            UpdateSelectionOutline();
            drawablePath.Points.Added += OnPointAdded;
            drawablePath.PropertyChanged += DrawablePath_PropertyChanged;
        }

        private void OnPointAdded(object sender, Point point) => segment.Points.Add(point);

        private void DrawablePath_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DrawablePath.IsSelected))
            {
                UpdateSelectionOutline();
            }
            else if (e.PropertyName == nameof(DrawablePath.StrokeThickness))
            {
                path.StrokeThickness = drawablePath.StrokeThickness;
                if (outline is not null)
                {
                    canvas.Children.Remove(outline);
                    outline = null;
                    UpdateSelectionOutline();
                }
            }
            else if (e.PropertyName == nameof(DrawablePath.StrokeColor))
                path.SetValue(Shape.StrokeProperty, new SolidColorBrush(drawablePath.StrokeColor));
            else if (e.PropertyName == nameof(DrawablePath.Movement))
            {
                SetMovement(path, drawablePath.Movement);
                if (outline is not null)
                    SetMovement(outline, drawablePath.Movement);
            }
        }

        private static void SetMovement(Path path, Vector movement)
        {
            Canvas.SetLeft(path, movement.X);
            Canvas.SetTop(path, movement.Y);
        }

        private void UpdateSelectionOutline()
        {
            if (outline is not null)
                canvas.Children.Remove(outline);
            if (drawablePath.IsSelected)
            {
                outline ??= CreateOutline(drawablePath.StrokeColor.GetContrastingColour());
                canvas.Children.Add(outline);
            }
        }

        private Path CreateOutline(Color outlineColor) =>
            new()
            {
                Data =
                    AnyGeometry.Combine(
                        CreatePathGeometry(drawablePath.Points, drawablePath.StrokeThickness / 2 + OutlineThickness),
                        CreatePathGeometry(drawablePath.Points, drawablePath.StrokeThickness / 2),
                        GeometryCombineMode.Exclude,
                        Transform.Identity),
                Fill = new SolidColorBrush(outlineColor),
                IsHitTestVisible = false,
            };

        private static AnyGeometry CreatePathGeometry(IEnumerable<Point> points, double strokeHalfThickness) =>
            points
                .SelectWithNext<Point, IEnumerable<AnyGeometry>>(
                    (Point begin, Point? next) =>
                    {
                        // Create an ellipse at the start of the line segment
                        EllipseGeometry startCircle = new(begin, strokeHalfThickness, strokeHalfThickness);
                        return next is not Point end ? [startCircle]
                            // Create a line segment between the two points
                            : [startCircle, CreateRotatedRectangle(begin, end, strokeHalfThickness)];
                    })
                .SelectMany(geometries => geometries)
                .Aggregate((geometry1, geometry2) => AnyGeometry.Combine(geometry1, geometry2, GeometryCombineMode.Union, Transform.Identity));

        private static StreamGeometry CreateRotatedRectangle(Point begin, Point end, double width)
        {
            // Calculate the direction vector from begin to end
            Vector direction = (end - begin).Normalised;
            // Calculate the perpendicular vector for the rectangle's width
            Vector widthVector = new Vector(-direction.Y, direction.X) * width;

            StreamGeometry geometry = new();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(begin + widthVector, isFilled: true, isClosed: true);
                ctx.LineTo(end + widthVector, isStroked: true, isSmoothJoin: true);
                ctx.LineTo(end - widthVector, isStroked: true, isSmoothJoin: true);
                ctx.LineTo(begin - widthVector, isStroked: true, isSmoothJoin: true);
            }
            geometry.Freeze(); // Optimize the geometry for performance
            return geometry;
        }

        public void Dispose()
        {
            drawablePath.Points.Added -= OnPointAdded;
            drawablePath.PropertyChanged -= DrawablePath_PropertyChanged;
            canvas.Children.Remove(path);
            if (outline is not null)
                canvas.Children.Remove(outline);
        }
    }
}
