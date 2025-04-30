using NobleTech.Products.PathEditor.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor;

partial class MainWindow : Window
{
    private readonly Views views;

    private PolyLineSegment? currentSegment;

    public MainWindow()
    {
        InitializeComponent();
        views = new(Canvas);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is EditorViewModel oldViewModel)
        {
            oldViewModel.CompletedPathAdded -= AddCompletedPath;
            oldViewModel.CompletedPathRemoved -= RemoveCompletedPath;
            oldViewModel.CurrentPathExtended -= ExtendCurrentPath;
            oldViewModel.RedrawRequired -= Redraw;
            oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
        if (e.NewValue is EditorViewModel newViewModel)
        {
            newViewModel.CompletedPathAdded += AddCompletedPath;
            newViewModel.CompletedPathRemoved += RemoveCompletedPath;
            newViewModel.CurrentPathExtended += ExtendCurrentPath;
            newViewModel.RedrawRequired += Redraw;
            newViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        Redraw();
    }

    private void AddCompletedPath(DrawablePath path) => views.Add(path);

    private void RemoveCompletedPath(DrawablePath path) => views.Remove(path);

    private void ExtendCurrentPath(Point point) => currentSegment?.Points.Add(point);

    private void Redraw()
    {
        views.Clear();
        if (DataContext is not EditorViewModel viewModel)
            return;
        views.AddRange(viewModel.CompletePaths);
        DrawCurrentPath(viewModel.CurrentPath);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is EditorViewModel viewModel && e.PropertyName == nameof(viewModel.CurrentPath))
            DrawCurrentPath(viewModel.CurrentPath);
    }

    private void DrawCurrentPath(DrawablePath? currentPath) =>
        currentSegment = currentPath is null ? null : views.Add(currentPath);

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
}
