using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class SetStyleViewModel : ObservableObject, INavigationViewModel
{
    public SetStyleViewModel(IEnumerable<Style> styles)
    {
        Styles = styles;
        SelectedStyle = styles.FirstOrDefault() ?? throw new ArgumentException("Styles collection cannot be empty", nameof(styles));
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
        OKCommand.NotifyCanExecuteChanged();
    }

    private bool useStrokeColor = true;
    public bool UseStrokeColor
    {
        get => SelectedStyle.StrokeColor.HasValue && useStrokeColor;
        set
        {
            if (SetProperty(ref useStrokeColor, value))
                OKCommand.NotifyCanExecuteChanged();
        }
    }

    private bool useStrokeThickness = true;
    public bool UseStrokeThickness
    {
        get => SelectedStyle.StrokeThickness.HasValue && useStrokeThickness;
        set
        {
            if (SetProperty(ref useStrokeThickness, value))
                OKCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = "CanOK")]
    public void OK() => Navigation.DialogResult = true;
    private bool CanOK() => UseStrokeColor || UseStrokeThickness;
}
