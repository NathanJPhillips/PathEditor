using NobleTech.Products.PathEditor.Utils;
using NobleTech.Products.PathEditor.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor;

partial class BabyPaintWindow : Window, IDisposable
{
    private static readonly TimeSpan autoSaveInterval = TimeSpan.FromMinutes(3);

    private readonly Timer timer;
    private readonly Views views;

    private readonly DisableTouchConversionToMouse disableTouchConversionToMouse = new(); // Prevents touch events from being converted to mouse events

    public BabyPaintWindow()
    {
        InitializeComponent();
        views = new(Canvas);
        DataContextChanged += OnDataContextChanged;
        Closing += OnClosing;
        timer = new(state => Dispatcher.Invoke(AutoSave), null, autoSaveInterval, autoSaveInterval);
        Loaded += (sender, e) => SetCursor();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is EditorViewModel oldViewModel)
        {
            oldViewModel.Paths.Added -= AddPath;
            oldViewModel.Paths.Removed -= RemovePath;
            oldViewModel.Paths.Reset -= Redraw;
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        if (e.NewValue is EditorViewModel newViewModel)
        {
            newViewModel.Paths.Added += AddPath;
            newViewModel.Paths.Removed += RemovePath;
            newViewModel.Paths.Reset += Redraw;
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        Redraw();
        SetCursor();
    }

    private void AddPath(object sender, DrawablePath path) => views.Add(path);

    private void RemovePath(object sender, DrawablePath path) => views.Remove(path);

    private void Redraw(object sender) => Redraw();

    private void Redraw()
    {
        views.Clear();
        if (DataContext is not EditorViewModel viewModel)
            return;
        viewModel.Background?.DrawTo(Canvas);
        views.AddRange(viewModel.Paths);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
        case nameof(EditorViewModel.Background):
            Redraw();
            break;
        case nameof(EditorViewModel.CurrentStrokeThickness):
            SetCursor();
            break;
        }
    }

    private void Canvas_TouchEvent(object sender, TouchEventArgs e)
    {
        if (DataContext is not EditorViewModel viewModel)
            return;
        foreach (TouchPoint touchPoint in e.GetIntermediateTouchPoints(Canvas))
            ProcessPoint(viewModel, touchPoint, e.TouchDevice);
        ProcessPoint(viewModel, e.GetTouchPoint(Canvas), e.TouchDevice);
        e.Handled = true;
    }

    private static void ProcessPoint(EditorViewModel viewModel, TouchPoint touchPoint, TouchDevice device) =>
        viewModel.ProcessPoint(
            touchPoint.Position,
            touchPoint.Action switch
            {
                TouchAction.Up => InputEvents.Up,
                TouchAction.Down => InputEvents.Down,
                _ => InputEvents.Move,
            },
            device);

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ProcessPoint(e, InputEvents.Move, e.MouseDevice);
            e.Handled = true;
        }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ProcessPoint(e, InputEvents.Down, e.MouseDevice);
        e.Handled = true;
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ProcessPoint(e, InputEvents.Up, e.MouseDevice);
        e.Handled = true;
    }

    private void ProcessPoint(MouseEventArgs e, InputEvents inputEvent, MouseDevice device)
    {
        if (DataContext is not EditorViewModel viewModel)
            return;
        viewModel.ProcessPoint(e.GetPosition(Canvas), inputEvent, device);
    }

    private void SetCursor()
    {
        Canvas.Cursor =
            DataContext is not EditorViewModel viewModel || !IsAncestorOf(Canvas)
                ? Cursors.Arrow
                : CursorUtils.CreateCircle(
                        viewModel.CurrentStrokeThickness
                            * VisualTreeHelper.GetDpi(Canvas).PixelsPerDip
                            * (Canvas.TransformToAncestor(this) is MatrixTransform transform
                                ? transform.Matrix.M11 // M11 is the X-axis scale
                                : 1),
                        viewModel.CurrentStrokeColor);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        timer.Dispose();
        AutoSave();
        disableTouchConversionToMouse.Dispose();
    }

    private void AutoSave()
    {
        if (DataContext is EditorViewModel viewModel)
            viewModel.AutoSave();
    }

    public void Dispose()
    {
        timer.Dispose();
        views.Dispose();
        DataContextChanged -= OnDataContextChanged;
        Closing -= OnClosing;
    }
}
