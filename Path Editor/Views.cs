using NobleTech.Products.PathEditor.Utils;
using NobleTech.Products.PathEditor.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NobleTech.Products.PathEditor;

internal sealed class Views(Canvas canvas) : IDisposable
{
    private readonly Dictionary<DrawablePath, View> views = [];

    public void Add(DrawablePath path) => views.Add(path, new View(path, canvas));

    public void AddRange(IEnumerable<DrawablePath> paths) =>
        views.AddRange(paths.ToDictionary(path => path, path => new View(path, canvas)));

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

        public View(DrawablePath drawablePath, Canvas canvas)
        {
            this.drawablePath = drawablePath;
            this.canvas = canvas;
            segment = new(drawablePath.Points.Skip(1), isStroked: true);
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
            canvas.Children.Add(path);
            drawablePath.Points.Added += OnPointAdded;
        }

        private void OnPointAdded(object sender, Point point) => segment.Points.Add(point);

        public void Dispose()
        {
            drawablePath.Points.Added -= OnPointAdded;
            canvas.Children.Remove(path);
        }
    }
}
