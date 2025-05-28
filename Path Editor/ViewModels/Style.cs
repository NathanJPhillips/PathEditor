using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal class Style : ObservableObject
{
    private readonly EditorViewModel viewModel;

    public Style(string name, Color? strokeColor, double? strokeThickness, EditorViewModel viewModel)
    {
        this.viewModel = viewModel;
        Name = name;
        StrokeColor = strokeColor;
        StrokeThickness = strokeThickness;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public string Name { get; }
    public Color? StrokeColor { get; }
    public double? StrokeThickness { get; }
    public bool IsSelected =>
        (StrokeColor is null || viewModel.CurrentStrokeColor == StrokeColor)
            && (StrokeThickness is null || viewModel.CurrentStrokeThickness == StrokeThickness);

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
        case nameof(EditorViewModel.CurrentStrokeColor):
        case nameof(EditorViewModel.CurrentStrokeThickness):
            OnPropertyChanged(nameof(IsSelected));
            break;
        }
    }

    public class NameComparer : IEqualityComparer<Style>, IComparer<Style>
    {
        public bool Equals(Style? x, Style? y) =>
            ReferenceEquals(x, y)
                || (x is not null && y is not null
                    && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

        public int Compare(Style? x, Style? y) =>
            ReferenceEquals(x, y) ? 0
                : x is null ? -1
                : y is null ? 1
                : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode([DisallowNull] Style obj) =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
    }
}
