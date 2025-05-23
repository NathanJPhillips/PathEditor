namespace NobleTech.Products.PathEditor.Geometry;

internal readonly record struct Rectangle(Point Origin, Size Size)
{
    public static Rectangle Empty { get; } = new(Point.Origin, Size.Empty);

    public Rectangle(double x, double y, double width, double height)
        : this(new(x, y), new Size(width, height))
    {
    }

    public Rectangle(Point origin, Point farCorner)
        : this(origin, farCorner - origin)
    {
    }

    public bool IsEmpty => Size.IsEmpty;

    public double X => Origin.X;
    public double Y => Origin.Y;
    public double Width => Size.Width;
    public double Height => Size.Height;

    public Point Center => Origin + (Vector)Size / 2;
    public Point FarCorner => Origin + (Vector)Size;

    public Rectangle Normalised =>
        new(Point.Min(Origin, FarCorner), Point.Max(Origin, FarCorner));

    public bool Contains(Point p)
    {
        Rectangle r = Normalised;
        return p >= r.Origin && p <= r.FarCorner;
    }

    public Rectangle Inflate(Size s)
    {
        Rectangle r = Normalised;
        return new(r.Origin - (Vector)s, r.FarCorner + (Vector)s);
    }

    public static Rectangle? operator |(Rectangle? r1, Rectangle? r2) =>
        r1 is null ? r2 : r2 is null ? r1 : r1.Value | r2.Value;

    public static Rectangle operator |(Rectangle r1, Rectangle r2)
    {
        r1 = r1.Normalised;
        r2 = r2.Normalised;
        return new(
            Point.Min(r1.Origin, r2.Origin),
            Point.Max(r1.FarCorner, r2.FarCorner));
    }

    public static Rectangle operator |(Rectangle? r, Point p) =>
        r is null ? new(p, Size.Empty) : r.Value | p;

    public static Rectangle operator |(Rectangle r, Point p)
    {
        r = r.Normalised;
        return new(Point.Min(r.Origin, p), Point.Max(r.FarCorner, p));
    }
}
