namespace NobleTech.Products.PathEditor.Geometry;

internal readonly record struct Point(double X, double Y)
{
    public static Point Origin { get; } = new(0, 0);

    public static Point Min(Point p1, Point p2) =>
        new(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y));
    public static Point Max(Point p1, Point p2) =>
        new(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y));

    public static Vector operator -(Point p1, Point p2) =>
        new(p1.X - p2.X, p1.Y - p2.Y);
    public static Point operator +(Point p, Vector s) =>
        new(p.X + s.X, p.Y + s.Y);
    public static Point operator -(Point p, Vector s) => p + -s;

    public static bool operator <(Point p1, Point p2) => p1.X < p2.X && p1.Y < p2.Y;
    public static bool operator >(Point p1, Point p2) => p1.X > p2.X && p1.Y > p2.Y;
    public static bool operator <=(Point p1, Point p2) => p1.X <= p2.X && p1.Y <= p2.Y;
    public static bool operator >=(Point p1, Point p2) => p1.X >= p2.X && p1.Y >= p2.Y;

    public static implicit operator System.Windows.Point(Point p) => new(p.X, p.Y);
    public static implicit operator Point(System.Windows.Point p) => new(p.X, p.Y);
}
