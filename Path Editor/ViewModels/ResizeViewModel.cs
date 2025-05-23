using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NobleTech.Products.PathEditor.Geometry;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class ResizeViewModel : ObservableObject, INavigationViewModel
{
    private readonly Size originalSize;
    private readonly Action<Size, bool> resizeCanvas;

    public ResizeViewModel(Size originalSize, Action<Size, bool> resizeCanvas)
    {
        this.originalSize = originalSize;
        Width = originalSize.Width;
        Height = originalSize.Height;
        this.resizeCanvas = resizeCanvas;
    }

    private INavigationService? navigation;
    public INavigationService Navigation
    {
        private get => navigation ?? throw new InvalidOperationException("Navigation used before it was set");
        set => navigation = value;
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(KeepPathsPropertional))]
    private bool isProportional = true;
    partial void OnIsProportionalChanged(bool value)
    {
        if (value)
            Height = Width / originalSize.Width * originalSize.Height;
    }

    [ObservableProperty]
    private double width;
    partial void OnWidthChanged(double value)
    {
        if (IsProportional)
            Height = value / originalSize.Width * originalSize.Height;
    }

    [ObservableProperty]
    private double height;
    partial void OnHeightChanged(double value)
    {
        if (IsProportional)
            Width = value / originalSize.Height * originalSize.Width;
    }

    private bool keepPathsPropertional = false;
    public bool KeepPathsPropertional
    {
        get => keepPathsPropertional || IsProportional;
        set => SetProperty(ref keepPathsPropertional, value);
    }

    [RelayCommand]
    public void Apply()
    {
        resizeCanvas(new(Width, Height), KeepPathsPropertional);
    }

    [RelayCommand]
    public void OK()
    {
        Apply();
        Navigation.DialogResult = true;
    }
}
