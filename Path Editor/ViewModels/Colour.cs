using System.Diagnostics;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

public readonly struct Colour
{
    public Colour(double hue, double saturation, double value)
    {
        Hue = hue;
        Saturation = saturation;
        Value = value;
    }

    public Colour(Color color)
    {
        // Calculate HSV from RGB
        double r = color.R / 255.0,
            g = color.G / 255.0,
            b = color.B / 255.0;
        Value = Math.Max(r, Math.Max(g, b));
        if (Value == 0)
        {
            Hue = 0;
            Saturation = 0;
            return;
        }
        double delta = Value - Math.Min(r, Math.Min(g, b));
        Saturation = delta / Value;
        if (delta == 0)
        {
            Hue = 0; // achromatic
            return;
        }
        Hue =
            r == Value ? (g - b) / delta + (g < b ? 6 : 0)
            : g == Value ? (b - r) / delta + 2
            : (r - g) / delta + 4;
        Hue *= 60; // convert to degrees
    }

    public double Hue { get; }
    public double Saturation { get; }
    public double Value { get; }

    public Color Color
    {
        get
        {
            int hi = Convert.ToInt32(Math.Floor(Hue / 60)) % 6;
            double f = Hue / 60 - Math.Floor(Hue / 60);

            double value = Value * 255;
            byte v = (byte)value;
            byte p = (byte)(value * (1 - Saturation));
            byte q = (byte)(value * (1 - f * Saturation));
            byte t = (byte)(value * (1 - (1 - f) * Saturation));

            return
                hi switch
                {
                    0 => Color.FromRgb(v, t, p),
                    1 => Color.FromRgb(q, v, p),
                    2 => Color.FromRgb(p, v, t),
                    3 => Color.FromRgb(p, q, v),
                    4 => Color.FromRgb(t, p, v),
                    _ => Color.FromRgb(v, p, q),
                };
        }
    }
}
