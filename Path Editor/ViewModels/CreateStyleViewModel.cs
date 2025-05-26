using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class CreateStyleViewModel(Color strokeColor, double strokeThickness, IEnumerable<Style> styles)
    : ObservableObject, INavigationViewModel
{
    private INavigationService? navigation;
    public INavigationService Navigation
    {
        private get => navigation ?? throw new InvalidOperationException("Navigation used before it was set");
        set => navigation = value;
    }

    public IEnumerable<Style> Styles { get; } = styles;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OKCommand))]
    private string name = string.Empty;

    public Color StrokeColor { get; } = strokeColor;
    public double StrokeThickness { get; } = strokeThickness;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OKCommand))]
    private bool useStrokeColor = true;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(OKCommand))]
    private bool useStrokeThickness = true;

    [RelayCommand(CanExecute = "CanOK")]
    public void OK() => Navigation.DialogResult = true;
    private bool CanOK() => !string.IsNullOrWhiteSpace(Name) && (UseStrokeColor || UseStrokeThickness);
}
