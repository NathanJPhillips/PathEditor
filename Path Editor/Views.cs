using NobleTech.Products.PathEditor.Utils;
using NobleTech.Products.PathEditor.ViewModels;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NobleTech.Products.PathEditor;

internal class Views(Canvas Canvas)
{
    private readonly Dictionary<DrawablePath, (PolyLineSegment segment, Path view)> segmentAndViews = [];

    public PolyLineSegment Add(DrawablePath path)
    {
        if (segmentAndViews.TryGetValue(path, out (PolyLineSegment segment, Path view) segmentAndView))
            return segmentAndView.segment;
        (PolyLineSegment segment, Path view) = CreateView(path);
        segmentAndViews.Add(path, (segment, view));
        Canvas.Children.Add(view);
        return segment;
    }

    public void AddRange(IEnumerable<DrawablePath> paths)
    {
        segmentAndViews.AddRange(paths.ToDictionary(path => path, CreateView));
        Canvas.Children.AddRange(segmentAndViews.Values.Select(segmentAndView => segmentAndView.view));
    }

    public void Remove(DrawablePath path)
    {
        segmentAndViews.Remove(path, out (PolyLineSegment segment, Path view) segmentAndView);
        Canvas.Children.Remove(segmentAndView.view);
    }

    public void Clear()
    {
        segmentAndViews.Clear();
        Canvas.Children.Clear();
    }

    private static (PolyLineSegment segment, Path view) CreateView(DrawablePath path)
    {
        PolyLineSegment segment = new(path.Points.ToArray()[1..], true);
        return (
            segment,
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
                                        Segments = [segment],
                                    },
                                ],
                        },
            });
    }
}
