using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor;

/// <summary>
/// A circle that when touched or clicked fires a command without waiting for a release.
/// This allows it to be touched and then dragged off and the command will still fire.
/// </summary>
/// <remarks>
/// The fill color is set through a dependency property.
/// The width and height of the circle are set to the minimum of the width and height of the control.
/// When the control is clicked or touched a border is drawn around the circle.
/// When the mouse moves over the control a different border is drawn around the circle.
/// </remarks>
public partial class EasyPressButton : UserControl
{
    private partial class ViewProperties(Brush fill, double size, EasyPressButton parent)
        : ObservableObject
    {
        private readonly EasyPressButton parent = parent;
        private readonly HashSet<object> miceOver = [];
        private readonly HashSet<object> inputDevicesPressed = [];

        [ObservableProperty]
        private Brush fill = fill;

        public Brush Stroke =>
            inputDevicesPressed.Count != 0 ? parent.ClickBorderBrush
            : miceOver.Count != 0 ? parent.HoverBorderBrush
            : Brushes.Transparent;

        [ObservableProperty]
        private double size = size;

        public void MouseEnter(object device)
        {
            miceOver.Add(device);
            OnPropertyChanged(nameof(Stroke));
        }

        public void MouseLeave(object device)
        {
            miceOver.Remove(device);
            inputDevicesPressed.Remove(device);
            OnPropertyChanged(nameof(Stroke));
        }

        public void MouseDown(object device)
        {
            inputDevicesPressed.Add(device);
            OnPropertyChanged(nameof(Stroke));
        }

        public void MouseUp(object device)
        {
            inputDevicesPressed.Remove(device);
            OnPropertyChanged(nameof(Stroke));
        }
    }

    public EasyPressButton()
    {
        InitializeComponent();
        Ellipse.DataContext = new ViewProperties(Fill, Math.Min(ActualWidth, ActualHeight), this);
    }

    private ViewProperties CurrentViewProperties => (ViewProperties)Ellipse.DataContext;

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }
    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(EasyPressButton),
            new PropertyMetadata(Brushes.LightGray, OnFillChanged));
    private static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EasyPressButton button)
            button.CurrentViewProperties.Fill = button.Fill;
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(EasyPressButton),
            new PropertyMetadata(null));

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(EasyPressButton),
            new PropertyMetadata(null));

    public Brush HoverBorderBrush
    {
        get => (Brush)GetValue(HoverBorderBrushProperty);
        set => SetValue(HoverBorderBrushProperty, value);
    }
    public static readonly DependencyProperty HoverBorderBrushProperty =
        DependencyProperty.Register(
            nameof(HoverBorderBrush),
            typeof(Brush),
            typeof(EasyPressButton),
            new PropertyMetadata(Brushes.Gray));

    public Brush ClickBorderBrush
    {
        get => (Brush)GetValue(ClickBorderBrushProperty);
        set => SetValue(ClickBorderBrushProperty, value);
    }
    public static readonly DependencyProperty ClickBorderBrushProperty =
        DependencyProperty.Register(
            nameof(ClickBorderBrush),
            typeof(Brush),
            typeof(EasyPressButton),
            new PropertyMetadata(Brushes.DodgerBlue));

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        CurrentViewProperties.Size = Math.Min(ActualWidth, ActualHeight);

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice is null)
            OnMouseDown(e);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice is null)
            CurrentViewProperties.MouseUp(e.Device);
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (e.StylusDevice is null)
            CurrentViewProperties.MouseEnter(e.Device);
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (e.StylusDevice is null)
            CurrentViewProperties.MouseLeave(e.Device);
    }

    private void OnTouchDown(object sender, TouchEventArgs e) => OnMouseDown(e);

    private void OnTouchUp(object sender, TouchEventArgs e) =>
        CurrentViewProperties.MouseUp(e.Device);

    private void OnTouchLeave(object sender, TouchEventArgs e) =>
        CurrentViewProperties.MouseLeave(e.Device);

    private void OnMouseDown(InputEventArgs e)
    {
        CurrentViewProperties.MouseDown(e.Device);
        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);
        e.Handled = true;
    }
}
