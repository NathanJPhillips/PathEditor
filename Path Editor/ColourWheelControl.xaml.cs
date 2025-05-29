using NobleTech.Products.PathEditor.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Point = NobleTech.Products.PathEditor.Geometry.Point;
using Size = NobleTech.Products.PathEditor.Geometry.Size;
using Vector = NobleTech.Products.PathEditor.Geometry.Vector;

namespace NobleTech.Products.PathEditor;

partial class ColourWheelControl : UserControl, INotifyPropertyChanged
{
    public ColourWheelControl()
    {
        InitializeComponent();
    }

    public Colour SelectedColour
    {
        get => (Colour)GetValue(SelectedColourProperty);
        set => SetValue(SelectedColourProperty, value);
    }
    public static readonly DependencyProperty SelectedColourProperty =
        DependencyProperty.Register(
            nameof(SelectedColour),
            typeof(Colour),
            typeof(ColourWheelControl),
            new FrameworkPropertyMetadata(new Colour(Colors.White), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColourChanged));
    private static void OnSelectedColourChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColourWheelControl control && e.NewValue is Colour newColour)
            control.OnSelectedColourChanged(newColour);
    }
    private void OnSelectedColourChanged(Colour newColour)
    {
        SetCurrentValue(ValueProperty, newColour.Value);
        DrawColorWheel();
        PropertyChanged?.Invoke(this, new(nameof(SelectedPoint)));
    }

    public double Value
    {
        get { return (double)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(ColourWheelControl),
            new PropertyMetadata(1.0, OnValueChanged));
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColourWheelControl control && e.NewValue is double newValue)
            control.OnValueChanged(newValue);
    }
    private void OnValueChanged(double newValue)
    {
        SetCurrentValue(SelectedColourProperty, new Colour(SelectedColour.Hue, SelectedColour.Saturation, newValue));
        DrawColorWheel();
    }

    public Point SelectedPoint
    {
        get
        {
            ColourCalculator colourCalculator = new(WheelCanvas);
            return !colourCalculator.IsValid ? new(0, 0)
                : colourCalculator.GetPointFromColour(SelectedColour);
        }
    }

    private void DrawColorWheel()
    {
        ColourCalculator colourCalculator = new(WheelCanvas);
        if (!colourCalculator.IsValid)
            return;
        BitmapDirect bmp = new(colourCalculator.Width, colourCalculator.Height);
        for (int y = 0; y < colourCalculator.Height; y++)
        {
            for (int x = 0; x < colourCalculator.Width; x++)
            {
                // Outside the wheel, set to transparent
                bmp[x, y] =
                    colourCalculator.GetColourFromPoint(new(x, y), Value)?.Color
                        ?? Colors.Transparent;
            }
        }
        WheelCanvas.Background = new ImageBrush(bmp.UnlockedBitmap);
    }

    private void WheelCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!WheelCanvas.CaptureMouse())
            Debug.WriteLine("Couldn't capture mouse");
        SetColorFromPoint(e.GetPosition(WheelCanvas));
    }

    private void WheelCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (WheelCanvas.IsMouseCaptured)
            SetColorFromPoint(e.GetPosition(WheelCanvas));
    }

    private void WheelCanvas_MouseUp(object sender, MouseButtonEventArgs e) =>
        WheelCanvas.ReleaseMouseCapture();

    private void WheelCanvas_Loaded(object sender, RoutedEventArgs e) => DrawColorWheel();
    private void WheelCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawColorWheel();

    private void SetColorFromPoint(Point pt)
    {
        ColourCalculator colourCalculator = new(WheelCanvas);
        if (colourCalculator.IsValid && colourCalculator.GetColourFromPoint(pt, Value) is Colour colour)
            SelectedColour = colour;
    }

    private class ColourCalculator
    {
        private readonly Size size;
        private readonly Point centre;
        private readonly double radius;

        public ColourCalculator(Canvas canvas)
        {
            size = new(canvas.ActualWidth, canvas.ActualHeight);
            centre = Point.Origin + size / 2;
            radius = Math.Min(size.Width / 2, size.Height / 2) - 1;
        }

        public bool IsValid => radius > 0;

        public int Width => (int)size.Width;
        public int Height => (int)size.Height;

        public Colour? GetColourFromPoint(Point pt, double value)
        {
            Vector v = pt - centre;
            double saturation = v.Length / radius;
            return saturation > 1 ? null
                : new((v.Angle + Math.Tau / 4) % Math.Tau / Math.Tau * 360, saturation, value);
        }

        public Point GetPointFromColour(Colour colour)
        {
            double length = colour.Saturation * radius;
            double angle = colour.Hue / 360 * Math.Tau - Math.Tau / 4;
            return centre + new Vector(Math.Cos(angle) * length, Math.Sin(angle) * length);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
