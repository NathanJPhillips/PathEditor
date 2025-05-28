using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class ApplyStyleViewModel : ObservableObject, INavigationViewModel
{
    private readonly Action<Color?, double?> applyStyle;

    public ApplyStyleViewModel(Style selectedStyle, IEnumerable<Style> styles, Action<Color?, double?> applyStyle)
    {
        this.applyStyle = applyStyle;
        Styles = styles;
        SelectedStyle = selectedStyle;
    }

    private INavigationService? navigation;
    public INavigationService Navigation
    {
        private get => navigation ?? throw new InvalidOperationException("Navigation used before it was set");
        set => navigation = value;
    }

    public IEnumerable<Style> Styles { get; }

    [ObservableProperty]
    private Style selectedStyle;
    partial void OnSelectedStyleChanged(Style value)
    {
        OnPropertyChanged(nameof(UseStrokeColor));
        OnPropertyChanged(nameof(UseStrokeThickness));
        RefreshCommandStates();
    }

    private bool useStrokeColor = true;
    public bool UseStrokeColor
    {
        get => SelectedStyle.StrokeColor.HasValue && useStrokeColor;
        set
        {
            if (SetProperty(ref useStrokeColor, value))
                RefreshCommandStates();
        }
    }

    private bool useStrokeThickness = true;
    public bool UseStrokeThickness
    {
        get => SelectedStyle.StrokeThickness.HasValue && useStrokeThickness;
        set
        {
            if (SetProperty(ref useStrokeThickness, value))
                RefreshCommandStates();
        }
    }

    [RelayCommand(CanExecute = "IsAnAttributeSelected")]
    public void Apply() =>
        applyStyle(
            UseStrokeColor ? SelectedStyle.StrokeColor : null,
            UseStrokeThickness ? SelectedStyle.StrokeThickness : null);

    [RelayCommand(CanExecute = "IsAnAttributeSelected")]
    public void OK()
    {
        Apply();
        Navigation.DialogResult = true;
    }

    private bool IsAnAttributeSelected() => UseStrokeColor || UseStrokeThickness;

    private void RefreshCommandStates()
    {
        ApplyCommand.NotifyCanExecuteChanged();
        OKCommand.NotifyCanExecuteChanged();
    }
}
