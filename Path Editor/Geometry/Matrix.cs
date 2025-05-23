namespace NobleTech.Products.PathEditor.Geometry;

internal readonly record struct Matrix(double M11, double M12, double M21, double M22, double OffsetX, double OffsetY)
{
    public static Matrix Identity { get; } = new(1, 0, 0, 1, 0, 0);

    public bool IsIdentity => this == Identity;

    public static Point operator *(Point p, Matrix m) =>
        new(
            p.X * m.M11 + p.Y * m.M21 + m.OffsetX,
            p.X * m.M12 + p.Y * m.M22 + m.OffsetY);

    public static Vector operator *(Vector v, Matrix m) =>
        new(
            v.X * m.M11 + v.Y * m.M21,
            v.X * m.M12 + v.Y * m.M22);

    public static Matrix operator *(Matrix m1, Matrix m2) =>
        new(
            m1.M11 * m2.M11 + m1.M12 * m2.M21,
            m1.M11 * m2.M12 + m1.M12 * m2.M22,
            m1.M21 * m2.M11 + m1.M22 * m2.M21,
            m1.M21 * m2.M12 + m1.M22 * m2.M22,
            m1.OffsetX * m2.M11 + m1.OffsetY * m2.M21 + m2.OffsetX,
            m1.OffsetX * m2.M12 + m1.OffsetY * m2.M22 + m2.OffsetY);

    public static Matrix CreateTranslation(Vector offset) =>
        new(1, 0, 0, 1, offset.X, offset.Y);

    public static Matrix CreateTranslation(double offsetX, double offsetY) =>
        CreateTranslation(new(offsetX, offsetY));

    public static Matrix CreateScale(double scaleX, double scaleY) =>
        new(scaleX, 0, 0, scaleY, 0, 0);

    public static Matrix CreateScale(double scale) => CreateScale(scale, scale);

    public static Matrix CreateRotation(double radians)
    {
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        return new(cos, sin, -sin, cos, 0, 0);
    }
}
