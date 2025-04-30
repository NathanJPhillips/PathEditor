using NobleTech.Products.PathEditor.Utils;
using NobleTech.Products.PathEditor.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor;

partial class MainWindow : Window
{
    private static readonly TimeSpan autoSaveInterval = TimeSpan.FromMinutes(3);

    private readonly Timer timer;
    private readonly Views views;

    public MainWindow()
    {
        InitializeComponent();
        views = new(Canvas);
        DataContextChanged += OnDataContextChanged;
        Closing += OnClosing;
        timer = new(state => Dispatcher.Invoke(AutoSave), null, autoSaveInterval, autoSaveInterval);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is EditorViewModel oldViewModel)
        {
            oldViewModel.PathAdded -= AddCompletedPath;
            oldViewModel.PathRemoved -= RemoveCompletedPath;
            oldViewModel.PathExtended -= ExtendPath;
            oldViewModel.RedrawRequired -= Redraw;
        }
        if (e.NewValue is EditorViewModel newViewModel)
        {
            newViewModel.PathAdded += AddCompletedPath;
            newViewModel.PathRemoved += RemoveCompletedPath;
            newViewModel.PathExtended += ExtendPath;
            newViewModel.RedrawRequired += Redraw;
        }
        Redraw();
    }

    private void AddCompletedPath(DrawablePath path) => views.Add(path);

    private void RemoveCompletedPath(DrawablePath path) => views.Remove(path);

    private void ExtendPath(DrawablePath path, Point point) => views.Add(path).Points.Add(point);

    private void Redraw()
    {
        views.Clear();
        if (DataContext is not EditorViewModel viewModel)
            return;
        views.AddRange(viewModel.Paths);
    }

    private void Canvas_TouchEvent(object sender, TouchEventArgs e)
    {
        if (DataContext is not EditorViewModel viewModel)
            return;
        foreach (TouchPoint touchPoint in e.GetIntermediateTouchPoints(Canvas))
            ProcessPoint(viewModel, touchPoint);
        ProcessPoint(viewModel, e.GetTouchPoint(Canvas));
    }

    private static void ProcessPoint(EditorViewModel viewModel, TouchPoint touchPoint) =>
        viewModel.ProcessPoint(
            touchPoint.Position,
            touchPoint.Action switch
            {
                TouchAction.Up => InputAction.Up,
                TouchAction.Down => InputAction.Down,
                _ => InputAction.Move,
            });

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            ProcessPoint(e, InputAction.Move);
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        ProcessPoint(e, InputAction.Down);

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        ProcessPoint(e, InputAction.Up);

    private void ProcessPoint(MouseEventArgs e, InputAction action)
    {
        if (DataContext is not EditorViewModel viewModel)
            return;
        viewModel.ProcessPoint(e.GetPosition(Canvas), action);
    }

    private void Canvas_MouseEnter(object sender, MouseEventArgs e)
    {
        Canvas.Cursor =
            DataContext is not EditorViewModel viewModel
                ? Cursors.Arrow
                : CursorUtils.CreateCircle(
                        viewModel.CurrentStrokeThickness
                            * VisualTreeHelper.GetDpi(Canvas).PixelsPerDip
                            * (Canvas.TransformToAncestor(this) is MatrixTransform transform
                                ? transform.Matrix.M11 // M11 is the X-axis scale
                                : 1),
                        viewModel.CurrentStrokeColor);
    }

    private void Canvas_MouseLeave(object sender, MouseEventArgs e) => Canvas.Cursor = Cursors.Arrow;

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        AutoSave();
        timer.Dispose();
    }

    private void AutoSave()
    {
        if (DataContext is EditorViewModel viewModel)
            viewModel.AutoSave();
    }
}
