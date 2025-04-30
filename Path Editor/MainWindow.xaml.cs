using NobleTech.Products.PathEditor.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace NobleTech.Products.PathEditor;

partial class MainWindow : Window, IDisposable
{
    private readonly Views views;

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
            oldViewModel.Paths.Added -= AddPath;
            oldViewModel.Paths.Removed -= RemovePath;
            oldViewModel.Paths.Reset -= Redraw;
        }
        if (e.NewValue is EditorViewModel newViewModel)
        {
            newViewModel.Paths.Added += AddPath;
            newViewModel.Paths.Removed += RemovePath;
            newViewModel.Paths.Reset += Redraw;
        }
        Redraw();
    }

    private void AddPath(object sender, DrawablePath path) => views.Add(path);

    private void RemovePath(object sender, DrawablePath path) => views.Remove(path);

    private void Redraw(object sender) => Redraw();
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

    public void Dispose()
    {
        views.Dispose();
        DataContextChanged -= OnDataContextChanged;
    }
}
