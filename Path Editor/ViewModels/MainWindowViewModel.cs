using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class MainWindowViewModel(EditorViewModel editor) : ObservableObject, INavigationViewModel
{
    /// <summary>
    /// The navigation service used to open dialogs and close the application.
    /// </summary>
    /// <remarks>
    /// This is set by the navigation service when the view model is attached to the view.
    /// </remarks>
    public INavigationService Navigation
    {
        private get => Editor.Navigation;
        set => Editor.Navigation = value;
    }

    public EditorViewModel Editor { get; } = editor;

    /// <summary>
    /// Open the Baby Paint full-screen view.
    /// </summary>
    [RelayCommand]
    private void BabyPaintView()
    {
        Editor.Mode = EditorModes.Draw;
        Navigation.ReplaceWindow(NavigationDestinations.BabyPaint, new BabyPaintWindowViewModel(Editor));
    }
}
