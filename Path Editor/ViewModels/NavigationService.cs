using Microsoft.Win32;
using System.Windows;

namespace NobleTech.Products.PathEditor.ViewModels;

internal class NavigationService : INavigationService
{
    private readonly Dictionary<string, Type> windows;
    private readonly Window? window;

    private NavigationService(Dictionary<string, Type> windows, Window window)
    {
        this.windows = windows;
        this.window = window;
    }

    public NavigationService()
    {
        windows = [];
    }

    public Window ShowWindow(string windowName, object viewModel)
    {
        if (!windows.TryGetValue(windowName, out Type? windowType))
            throw new ArgumentException($"Window with name {windowName} not registered.");
        var newWindow = (Window)Activator.CreateInstance(windowType)!;
        if (viewModel is INavigationViewModel navigationViewModel)
            navigationViewModel.Navigation = new NavigationService(windows, newWindow);
        newWindow.DataContext = viewModel;
        newWindow.Owner = window;
        newWindow.Show();
        return newWindow;
    }

    void INavigationService.ShowWindow(string windowName, object viewModel) =>
        ShowWindow(windowName, viewModel);

    bool? INavigationService.ShowDialog(string windowName, object viewModel)
    {
        switch (windowName)
        {
        case "Open":
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
            return openResult;
        case "Save":
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
            return saveResult;
        default:
            if (!windows.TryGetValue(windowName, out Type? windowType))
                throw new ArgumentException($"Window with name {windowName} not registered.");
            var dialog = (Window)Activator.CreateInstance(windowType)!;
            if (viewModel is INavigationViewModel navigationViewModel)
                navigationViewModel.Navigation = new NavigationService(windows, dialog);
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

    public void RegisterWindow<TWindow>(string name) where TWindow : Window
    {
        if (windows.ContainsKey(name))
            throw new ArgumentException($"Window with name {name} already registered.");
        windows[name] = typeof(TWindow);
    }
}
