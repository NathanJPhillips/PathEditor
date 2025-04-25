using NobleTech.Products.PathEditor.Utils;
using NobleTech.Products.PathEditor.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NobleTech.Products.PathEditor;

partial class MainWindow : Window
{
    private PolyLineSegment? currentSegment;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is EditorViewModel oldViewModel)
        {
            oldViewModel.CompletePaths.CollectionChanged -= Redraw;
            oldViewModel.PropertyChanging -= ViewModel_PropertyChanging;
            oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
        if (e.NewValue is EditorViewModel newViewModel)
        {
            newViewModel.CompletePaths.CollectionChanged += Redraw;
            newViewModel.PropertyChanging += ViewModel_PropertyChanging;
            newViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        Redraw();
    }

    private void Redraw(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();
    private void Redraw()
    {
        Canvas.Children.Clear();
        if (DataContext is not EditorViewModel viewModel)
            return;
        Canvas.Children.AddRange(viewModel.CompletePaths.Select(GetView));
        DrawCurrentPath(viewModel.CurrentPath);
    }

    private void ViewModel_PropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (sender is not EditorViewModel viewModel)
            return;
        if (e.PropertyName == nameof(viewModel.CurrentPath))
        {
            if (viewModel.CurrentPath is not null)
                viewModel.CurrentPath.Points.Added -= ExtendCurrentPath;
            currentSegment = null;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is EditorViewModel viewModel && e.PropertyName == nameof(viewModel.CurrentPath))
            DrawCurrentPath(viewModel.CurrentPath);
    }

    private void DrawCurrentPath(DrawablePath? currentPath)
    {
        if (currentPath is null)
        {
            currentSegment = null;
            return;
        }
        currentSegment = new(currentPath.Points.Skip(1), true);
        Canvas.Children.Add(GetView(
            currentPath.Points.First(),
            currentPath.StrokeColor,
            currentPath.StrokeThickness,
            currentSegment));
        currentPath.Points.Added += ExtendCurrentPath;
    }

    private void ExtendCurrentPath(object sender, Point point) => currentSegment?.Points.Add(point);

    private static Path GetView(DrawablePath drawnPath) =>
        GetView(
            drawnPath.Points.First(),
            drawnPath.StrokeColor,
            drawnPath.StrokeThickness,
            new PolyLineSegment(drawnPath.Points.Skip(1), true));

    private static Path GetView(Point startPoint, Color strokeColor, double strokeThickness, PathSegment segment) =>
        new()
        {
            Stroke = new SolidColorBrush(strokeColor),
            StrokeThickness = strokeThickness,
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
                                        StartPoint = startPoint,
                                        Segments = [segment],
                                    },
                            ],
                    },
        };

    private void Canvas_TouchEvent(object sender, TouchEventArgs e)
    {
        if (DataContext is not EditorViewModel viewModel)
            return;
        foreach (TouchPoint touchPoint in e.GetIntermediateTouchPoints(Canvas))
            ProcessPoint(viewModel, touchPoint);
        ProcessPoint(viewModel, e.GetTouchPoint(Canvas));
        e.Handled = true;
    }

    private static void ProcessPoint(EditorViewModel viewModel, TouchPoint touchPoint) =>
        viewModel.ProcessPoint(
            touchPoint.Position,
            touchPoint.Action switch
            {
                TouchAction.Up => InputEvents.Up,
                TouchAction.Down => InputEvents.Down,
                _ => InputEvents.Move,
            });

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ProcessPoint(e, InputEvents.Move);
            e.Handled = true;
        }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ProcessPoint(e, InputEvents.Down);
        e.Handled = true;
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ProcessPoint(e, InputEvents.Up);
        e.Handled = true;
    }

    private void ProcessPoint(MouseEventArgs e, InputEvents inputEvent)
    {
        if (DataContext is not EditorViewModel viewModel)
            return;
        viewModel.ProcessPoint(e.GetPosition(Canvas), inputEvent);
    }
}
