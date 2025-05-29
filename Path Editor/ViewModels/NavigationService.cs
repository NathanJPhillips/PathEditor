using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;

namespace NobleTech.Products.PathEditor.ViewModels;

internal class NavigationService : INavigationService
{
    private readonly Dictionary<NavigationDestinations, Type> windowTypes;
    private readonly Dictionary<NavigationDestinations, Window> windows = [];
    private readonly Window? window;

    private NavigationService(Dictionary<NavigationDestinations, Type> windowTypes, Window window)
    {
        this.windowTypes = windowTypes;
        this.window = window;
    }

    public NavigationService()
    {
        windowTypes = [];
    }

    public Window ShowWindow(NavigationDestinations destination, object viewModel, Action? onClosed = null)
    {
        Window newWindow = CreateWindow(destination, viewModel);
        newWindow.Owner = window;
        windows[destination] = newWindow;
        newWindow.Closed +=
            (sender, e) =>
            {
                if (windows.TryGetValue(destination, out Window? currentWindow) && sender == currentWindow)
                    windows.Remove(destination);
                onClosed?.Invoke();
            };
        newWindow.Show();
        return newWindow;
    }

    void INavigationService.ShowWindow(NavigationDestinations destination, object viewModel, Action? onClosed) =>
        ShowWindow(destination, viewModel, onClosed);

    void INavigationService.ReplaceWindow(NavigationDestinations destination, object viewModel)
    {
        if (window is null)
            throw new InvalidOperationException("No window is currently open.");
        CreateWindow(destination, viewModel).Show();
        window.Close();
    }

    bool? INavigationService.ShowDialog(NavigationDestinations destination, object viewModel)
    {
        switch (destination)
        {
        case NavigationDestinations.MessageBox:
            if (viewModel is not MessageBoxViewModel messageBoxViewModel)
                throw new ArgumentException("ViewModel must be of type MessageBoxViewModel.");
            MessageBoxResult messageBoxResult =
                MessageBox.Show(
                    window,
                    messageBoxViewModel.Message,
                    messageBoxViewModel.Title,
                    messageBoxViewModel.ButtonsToShow switch
                    {
                        MessageBoxViewModel.Buttons.OK => MessageBoxButton.OK,
                        MessageBoxViewModel.Buttons.OK | MessageBoxViewModel.Buttons.Cancel => MessageBoxButton.OKCancel,
                        MessageBoxViewModel.Buttons.Yes | MessageBoxViewModel.Buttons.No => MessageBoxButton.YesNo,
                        MessageBoxViewModel.Buttons.Yes | MessageBoxViewModel.Buttons.No | MessageBoxViewModel.Buttons.Cancel => MessageBoxButton.YesNoCancel,
                        _ => throw new ArgumentOutOfRangeException(nameof(viewModel), messageBoxViewModel.ButtonsToShow, "Only certain button combinations are supported on WPF"),
                    },
                    messageBoxViewModel.Image switch
                    {
                        MessageBoxViewModel.Images.None => MessageBoxImage.None,
                        MessageBoxViewModel.Images.Error => MessageBoxImage.Error,
                        MessageBoxViewModel.Images.Warning => MessageBoxImage.Warning,
                        MessageBoxViewModel.Images.Information => MessageBoxImage.Information,
                        _ => throw new InvalidEnumArgumentException(nameof(messageBoxViewModel.Image), (int)messageBoxViewModel.Image, typeof(MessageBoxViewModel.Images)),
                    },
                    messageBoxViewModel.DefaultButton switch
                    {
                        MessageBoxViewModel.Buttons.OK => MessageBoxResult.OK,
                        MessageBoxViewModel.Buttons.Yes => MessageBoxResult.Yes,
                        MessageBoxViewModel.Buttons.No => MessageBoxResult.No,
                        MessageBoxViewModel.Buttons.Cancel => MessageBoxResult.Cancel,
                        _ => throw new InvalidEnumArgumentException(nameof(messageBoxViewModel.DefaultButton), (int)messageBoxViewModel.DefaultButton, typeof(MessageBoxViewModel.Buttons)),
                    });
            messageBoxViewModel.SelectedButton =
                messageBoxResult switch
                {
                    MessageBoxResult.OK => MessageBoxViewModel.Buttons.OK,
                    MessageBoxResult.Yes => MessageBoxViewModel.Buttons.Yes,
                    MessageBoxResult.No => MessageBoxViewModel.Buttons.No,
                    MessageBoxResult.Cancel => MessageBoxViewModel.Buttons.Cancel,
                    _ => null,
                };
            return messageBoxResult != MessageBoxResult.Cancel;
        case NavigationDestinations.Open:
            if (viewModel is not FileDialogViewModel openFileDialogViewModel)
                throw new ArgumentException("ViewModel must be of type FileDialogViewModel.");
            OpenFileDialog openFileDialog =
                new()
                {
                    Title = openFileDialogViewModel.Title,
                    Filter = openFileDialogViewModel.Filter,
                };
            bool? openResult = openFileDialog.ShowDialog(window);
            openFileDialogViewModel.FilePath = openFileDialog.FileName;
            openFileDialogViewModel.SelectedFilterIndex = openFileDialog.FilterIndex - 1;
            return openResult;
        case NavigationDestinations.Save:
            if (viewModel is not FileDialogViewModel saveFileDialogViewModel)
                throw new ArgumentException("ViewModel must be of type FileDialogViewModel.");
            SaveFileDialog saveFileDialog =
                new()
                {
                    Title = saveFileDialogViewModel.Title,
                    Filter = saveFileDialogViewModel.Filter,
                    FileName = saveFileDialogViewModel.FilePath,
                };
            bool? saveResult = saveFileDialog.ShowDialog(window);
            saveFileDialogViewModel.FilePath = saveFileDialog.FileName;
            saveFileDialogViewModel.SelectedFilterIndex = saveFileDialog.FilterIndex - 1;
            return saveResult;
        default:
            if (!windowTypes.TryGetValue(destination, out Type? windowType))
                throw new ArgumentException($"Destination {destination} not registered.", nameof(destination));
            var dialog = (Window)Activator.CreateInstance(windowType)!;
            if (viewModel is INavigationViewModel navigationViewModel)
                navigationViewModel.Navigation = new NavigationService(windowTypes, dialog);
            dialog.DataContext = viewModel;
            dialog.Owner = window;
            return dialog.ShowDialog();
        }
    }

    bool? INavigationService.DialogResult
    {
        get => window?.DialogResult;
        set
        {
            if (window is null)
                throw new InvalidOperationException("No window is currently open.");
            window.DialogResult = value;
        }
    }

    void INavigationService.Close()
    {
        if (window is null)
            throw new InvalidOperationException("No window is currently open.");
        window.Close();
    }

    void INavigationService.CloseWindow(NavigationDestinations destination)
    {
        if (windows.TryGetValue(destination, out Window? window))
            window.Close();
    }

    bool INavigationService.IsWindowOpen(NavigationDestinations destination) =>
        windows.ContainsKey(destination);

    public void RegisterWindow<TWindow>(NavigationDestinations destination) where TWindow : Window
    {
        if (windowTypes.ContainsKey(destination))
            throw new ArgumentException($"Window with name {destination} already registered.");
        windowTypes[destination] = typeof(TWindow);
    }

    private Window CreateWindow(NavigationDestinations destination, object viewModel, bool disposeViewModel = true)
    {
        if (!windowTypes.TryGetValue(destination, out Type? windowType))
            throw new ArgumentException($"Window with name {destination} not registered.");
        var newWindow = (Window)Activator.CreateInstance(windowType)!;
        if (viewModel is INavigationViewModel navigationViewModel)
            navigationViewModel.Navigation = new NavigationService(windowTypes, newWindow);
        newWindow.DataContext = viewModel;
        if (disposeViewModel && viewModel is IDisposable disposableViewModel)
            newWindow.Closed += (_, _) => disposableViewModel.Dispose();
        return newWindow;
    }
}
