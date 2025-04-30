using NobleTech.Products.PathEditor.Utils;
using NobleTech.Products.PathEditor.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NobleTech.Products.PathEditor;

internal class Views(Canvas canvas)
{
    private readonly Dictionary<DrawablePath, View> views = [];

    public void Add(DrawablePath path) => views.Add(path, new View(path, canvas));

    public void AddRange(IEnumerable<DrawablePath> paths) =>
        views.AddRange(paths.ToDictionary(path => path, path => new View(path, canvas)));

    public void Remove(DrawablePath path)
    {
        if (views.Remove(path, out View? view))
            view.Remove();
    }

    public void Clear()
    {
        views.Clear();
        canvas.Children.Clear();
    }

    private class View
    {
        private readonly PolyLineSegment segment;
        private readonly Path path;
        private readonly Canvas canvas;

        public View(DrawablePath drawablePath, Canvas canvas)
        {
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
                };
            this.canvas = canvas;
            canvas.Children.Add(path);
            drawablePath.Points.Added += OnPointAdded;
        }

        public void Remove() => canvas.Children.Remove(path);

        private void OnPointAdded(object sender, Point point) => segment.Points.Add(point);
    }
}
