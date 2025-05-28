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
        if (e.OldValue is BabyPaintWindowViewModel oldViewModel)
        {
            oldViewModel.Editor.Paths.Added -= AddPath;
            oldViewModel.Editor.Paths.Inserted -= InsertPath;
            oldViewModel.Editor.Paths.Removed -= RemovePath;
            oldViewModel.Editor.Paths.Reset -= Redraw;
            oldViewModel.Editor.PropertyChanged -= OnViewModelPropertyChanged;
        }
        if (e.NewValue is BabyPaintWindowViewModel newViewModel)
        {
            newViewModel.Editor.Paths.Added += AddPath;
            newViewModel.Editor.Paths.Inserted += InsertPath;
            newViewModel.Editor.Paths.Removed += RemovePath;
            newViewModel.Editor.Paths.Reset += Redraw;
            newViewModel.Editor.PropertyChanged += OnViewModelPropertyChanged;
        }
        Redraw();
        SetCursor();
    }

    private void AddPath(object sender, DrawablePath path) => views.Add(path);

    private void InsertPath(object sender, int index, DrawablePath path) => views.Insert(index, path);

    private void RemovePath(object sender, DrawablePath path) => views.Remove(path);

    private void Redraw(object sender) => Redraw();
    private void Redraw()
    {
        views.Clear();
        if (DataContext is not BabyPaintWindowViewModel viewModel)
            return;
        viewModel.Editor.Background?.DrawTo(Canvas);
        foreach (DrawablePath path in viewModel.Editor.Paths)
            views.Add(path);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
        case nameof(EditorViewModel.Background):
            Redraw();
            break;
        case nameof(EditorViewModel.CurrentStrokeThickness):
        case nameof(EditorViewModel.Mode):
            SetCursor();
            break;
        }
    }

    private void Canvas_TouchEvent(object sender, TouchEventArgs e)
    {
        if (DataContext is not BabyPaintWindowViewModel viewModel)
            return;
        foreach (TouchPoint touchPoint in e.GetIntermediateTouchPoints(Canvas))
            ProcessPoint(viewModel.Editor, touchPoint, e.TouchDevice);
        ProcessPoint(viewModel.Editor, e.GetTouchPoint(Canvas), e.TouchDevice);
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
        if (DataContext is not BabyPaintWindowViewModel viewModel)
            return;
        viewModel.Editor.ProcessPoint(e.GetPosition(Canvas), inputEvent, device);
    }

    private void SetCursor()
    {
        Canvas.Cursor =
            DataContext is not BabyPaintWindowViewModel viewModel || !IsAncestorOf(Canvas)
                ? Cursors.Arrow
                : CursorUtils.CreateCircle(
                        viewModel.Editor.CurrentStrokeThickness
                            * VisualTreeHelper.GetDpi(Canvas).PixelsPerDip
                            * (Canvas.TransformToAncestor(this) is MatrixTransform transform
                                ? transform.Matrix.M11 // M11 is the X-axis scale
                                : 1),
                        viewModel.Editor.CurrentStrokeColor);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        timer.Dispose();
        AutoSave();
        disableTouchConversionToMouse.Dispose();
    }

    private void AutoSave()
    {
        if (DataContext is BabyPaintWindowViewModel viewModel)
            viewModel.Editor.AutoSave();
    }

    private class CommandForwarder(Func<BabyPaintWindowViewModel, ICommand> getCommand) : CommandForwarder<BabyPaintWindowViewModel>(getCommand) { }

    private readonly CommandForwarder newCommand = new(viewModel => viewModel.Editor.NewCommand);
    private void OnNewExecuted(object sender, ExecutedRoutedEventArgs e) => newCommand.ExecuteCommand(DataContext, e);
    private void OnNewCanExecute(object sender, CanExecuteRoutedEventArgs e) => newCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder openCommand = new(viewModel => viewModel.Editor.OpenCommand);
    private void OnOpenExecuted(object sender, ExecutedRoutedEventArgs e) => openCommand.ExecuteCommand(DataContext, e);
    private void OnOpenCanExecute(object sender, CanExecuteRoutedEventArgs e) => openCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder saveCommand = new(viewModel => viewModel.Editor.SaveCommand);
    private void OnSaveExecuted(object sender, ExecutedRoutedEventArgs e) => saveCommand.ExecuteCommand(DataContext, e);
    private void OnSaveCanExecute(object sender, CanExecuteRoutedEventArgs e) => saveCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder saveAsCommand = new(viewModel => viewModel.Editor.SaveAsCommand);
    private void OnSaveAsExecuted(object sender, ExecutedRoutedEventArgs e) => saveAsCommand.ExecuteCommand(DataContext, e);
    private void OnSaveAsCanExecute(object sender, CanExecuteRoutedEventArgs e) => saveAsCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder printCommand = new(viewModel => viewModel.Editor.PrintCommand);
    private void OnPrintExecuted(object sender, ExecutedRoutedEventArgs e) => printCommand.ExecuteCommand(DataContext, e);
    private void OnPrintCanExecute(object sender, CanExecuteRoutedEventArgs e) => printCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder closeCommand = new(viewModel => viewModel.Editor.CloseCommand);
    private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e) => closeCommand.ExecuteCommand(DataContext, e);
    private void OnCloseCanExecute(object sender, CanExecuteRoutedEventArgs e) => closeCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder undoCommand = new(viewModel => viewModel.Editor.UndoStack.UndoCommand);
    private void OnUndoExecuted(object sender, ExecutedRoutedEventArgs e) => undoCommand.ExecuteCommand(DataContext, e);
    private void OnUndoCanExecute(object sender, CanExecuteRoutedEventArgs e) => undoCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder redoCommand = new(viewModel => viewModel.Editor.UndoStack.RedoCommand);
    private void OnRedoExecuted(object sender, ExecutedRoutedEventArgs e) => redoCommand.ExecuteCommand(DataContext, e);
    private void OnRedoCanExecute(object sender, CanExecuteRoutedEventArgs e) => redoCommand.CanExecuteCommand(DataContext, e);

    public void Dispose()
    {
        timer.Dispose();
        views.Dispose();
        DataContextChanged -= OnDataContextChanged;
        Closing -= OnClosing;
    }
}
