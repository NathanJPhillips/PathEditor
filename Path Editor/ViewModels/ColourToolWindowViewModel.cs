using CommunityToolkit.Mvvm.ComponentModel;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class ColourToolWindowViewModel(EditorViewModel editor) : ObservableObject
{
    public EditorViewModel Editor { get; } = editor;
}
