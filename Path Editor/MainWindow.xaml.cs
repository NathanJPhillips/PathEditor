﻿using NobleTech.Products.PathEditor.Utils;
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
        if (e.PropertyName == nameof(EditorViewModel.Background))
            Redraw();
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
        if (e.StylusDevice != null)
            return;
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ProcessPoint(e, InputEvents.Move, e.MouseDevice);
            e.Handled = true;
        }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice != null)
            return;
        ProcessPoint(e, InputEvents.Down, e.MouseDevice);
        e.Handled = true;
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice != null)
            return;
        ProcessPoint(e, InputEvents.Up, e.MouseDevice);
        e.Handled = true;
    }

    private void ProcessPoint(MouseEventArgs e, InputEvents inputEvent, MouseDevice device)
    {
        if (DataContext is not EditorViewModel viewModel)
            return;
        viewModel.ProcessPoint(e.GetPosition(Canvas), inputEvent, device);
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
        timer.Dispose();
        AutoSave();
    }

    private void AutoSave()
    {
        if (DataContext is EditorViewModel viewModel)
            viewModel.AutoSave();
    }

    private readonly CommandForwarder newCommand = new(viewModel => viewModel.NewCommand);
    private void OnNewExecuted(object sender, ExecutedRoutedEventArgs e) => newCommand.ExecuteCommand(DataContext, e);
    private void OnNewCanExecute(object sender, CanExecuteRoutedEventArgs e) => newCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder openCommand = new(viewModel => viewModel.OpenCommand);
    private void OnOpenExecuted(object sender, ExecutedRoutedEventArgs e) => openCommand.ExecuteCommand(DataContext, e);
    private void OnOpenCanExecute(object sender, CanExecuteRoutedEventArgs e) => openCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder saveCommand = new(viewModel => viewModel.SaveCommand);
    private void OnSaveExecuted(object sender, ExecutedRoutedEventArgs e) => saveCommand.ExecuteCommand(DataContext, e);
    private void OnSaveCanExecute(object sender, CanExecuteRoutedEventArgs e) => saveCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder saveAsCommand = new(viewModel => viewModel.SaveAsCommand);
    private void OnSaveAsExecuted(object sender, ExecutedRoutedEventArgs e) => saveAsCommand.ExecuteCommand(DataContext, e);
    private void OnSaveAsCanExecute(object sender, CanExecuteRoutedEventArgs e) => saveAsCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder printCommand = new(viewModel => viewModel.PrintCommand);
    private void OnPrintExecuted(object sender, ExecutedRoutedEventArgs e) => printCommand.ExecuteCommand(DataContext, e);
    private void OnPrintCanExecute(object sender, CanExecuteRoutedEventArgs e) => printCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder closeCommand = new(viewModel => viewModel.CloseCommand);
    private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e) => closeCommand.ExecuteCommand(DataContext, e);
    private void OnCloseCanExecute(object sender, CanExecuteRoutedEventArgs e) => closeCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder undoCommand = new(viewModel => viewModel.UndoStack.UndoCommand);
    private void OnUndoExecuted(object sender, ExecutedRoutedEventArgs e) => undoCommand.ExecuteCommand(DataContext, e);
    private void OnUndoCanExecute(object sender, CanExecuteRoutedEventArgs e) => undoCommand.CanExecuteCommand(DataContext, e);

    private readonly CommandForwarder redoCommand = new(viewModel => viewModel.UndoStack.RedoCommand);
    private void OnRedoExecuted(object sender, ExecutedRoutedEventArgs e) => redoCommand.ExecuteCommand(DataContext, e);
    private void OnRedoCanExecute(object sender, CanExecuteRoutedEventArgs e) => redoCommand.CanExecuteCommand(DataContext, e);

    private class CommandForwarder(Func<EditorViewModel, ICommand> getCommand)
    {
        public void ExecuteCommand(object dataContext, ExecutedRoutedEventArgs e)
        {
            ICommand? command = dataContext is EditorViewModel viewModel ? getCommand(viewModel) : null;
            if (command?.CanExecute(e.Parameter) ?? false)
                command.Execute(e.Parameter);
        }

        public void CanExecuteCommand(object dataContext, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = dataContext is EditorViewModel viewModel
                && getCommand(viewModel).CanExecute(e.Parameter);
    }
}
