using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class BabyPaintWindowViewModel : ObservableObject, INavigationViewModel, IDisposable
{
    public BabyPaintWindowViewModel(EditorViewModel editor)
    {
        Editor = editor;
        Editor.PropertyChanged += Editor_PropertyChanged;
    }

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

    public EditorViewModel Editor { get; }

    /// <summary>
    /// The width of the drawing canvas.
    /// </summary>
    public double CanvasWidth
    {
        get => Editor.CanvasSize.Width;
        set => Editor.CanvasSize = Editor.CanvasSize with { Width = value };
    }

    /// <summary>
    /// The height of the drawing canvas.
    /// </summary>
    public double CanvasHeight
    {
        get => Editor.CanvasSize.Height;
        set => Editor.CanvasSize = Editor.CanvasSize with { Height = value };
    }

    /// <summary>
    /// Set the current stroke color to the specified color.
    /// </summary>
    /// <param name="color">The color to set the stroke to.</param>
    [RelayCommand]
    private void SetColor(Color color) => Editor.CurrentStrokeColor = color;

    /// <summary>
    /// Set the current stroke thickness to the specified value.
    /// </summary>
    /// <param name="thickness">The thickness to set the stroke to.</param>
    [RelayCommand]
    private void SetThickness(double thickness) => Editor.CurrentStrokeThickness = thickness;

    /// <summary>
    /// Open the default Path Editor window.
    /// </summary>
    [RelayCommand]
    private void ExitBabyPaintView() =>
        Navigation.ReplaceWindow(NavigationDestinations.PathEditor, new MainWindowViewModel(Editor));

    private void Editor_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Editor.CanvasSize))
        {
            OnPropertyChanged(nameof(CanvasWidth));
            OnPropertyChanged(nameof(CanvasHeight));
        }
    }

    public void Dispose() =>
        // Unsubscribe from the editor's property changed event to prevent memory leaks.
        Editor.PropertyChanged -= Editor_PropertyChanged;
}
