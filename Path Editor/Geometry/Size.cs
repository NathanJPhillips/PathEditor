namespace NobleTech.Products.PathEditor.Geometry;

internal readonly record struct Size(double Width, double Height)
{
    public static Size Empty { get; } = new(0, 0);

    public bool IsEmpty => this == Empty;

    public static Size operator -(Size s) => new(-s.Width, -s.Height);

    public static Size operator *(Size s, double x) => new(s.Width * x, s.Height * x);
    public static Size operator /(Size s, double x) => new(s.Width / x, s.Height / x);
    public static Size operator *(double x, Size s) => s * x;

    public static bool operator <(Size s1, Size s2) => s1.Width < s2.Width && s1.Height < s2.Height;
    public static bool operator >(Size s1, Size s2) => s1.Width > s2.Width && s1.Height > s2.Height;
    public static bool operator <=(Size s1, Size s2) => s1.Width <= s2.Width && s1.Height <= s2.Height;
    public static bool operator >=(Size s1, Size s2) => s1.Width >= s2.Width && s1.Height >= s2.Height;

    public static implicit operator System.Windows.Size(Size s) => new(s.Width, s.Height);
    public static implicit operator Size(System.Windows.Size s) => new(s.Width, s.Height);

    public static implicit operator Vector(Size s) => new(s.Width, s.Height);
}
