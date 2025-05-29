using System.Runtime.CompilerServices;
using System.Text;

namespace NobleTech.Products.PathEditor.Geometry;

internal readonly record struct Vector(double X, double Y)
{
    /// <summary>
    /// The square of the length of the <see cref="Vector"/>, useful for some computations.
    /// </summary>
    public double LengthSquared => X * X + Y * Y;

    /// <summary>
    /// The length of the <see cref="Vector"/>.
    /// </summary>
    public double Length => Math.Sqrt(LengthSquared);

    /// <summary>
    /// The angle of the <see cref="Vector"/> in radians, measured from the positive X-axis.
    /// </summary>
    public double Angle
    {
        get
        {
            double angle = Math.Atan2(Y, X);
            return angle < 0 ? angle + Math.Tau : angle;
        }
    }

    /// <summary>
    /// A normalised copy of the <see cref="Vector"/> with a <see cref="Length"/> of 1
    /// (or 0 if the original vector was zero-length).
    /// </summary>
    public Vector Normalised
    {
        get
        {
            double length = Length;
            return length == 0 ? this : new(X / length, Y / length);
        }
    }

    public static double DotProduct(Vector a, Vector b) => a.X * b.X + a.Y * b.Y;

    public static Vector operator -(Vector s) => new(-s.X, -s.Y);

    public static Vector operator *(Vector s, double x) => new(s.X * x, s.Y * x);
    public static Vector operator /(Vector s, double x) => new(s.X / x, s.Y / x);
    public static Vector operator *(double x, Vector s) => s * x;

    public static Matrix operator /(Vector s1, Vector s2) => Matrix.CreateScale(s1.X / s2.X, s1.Y / s2.Y);

    public static bool operator <(Vector s1, Vector s2) => s1.X < s2.X && s1.Y < s2.Y;
    public static bool operator >(Vector s1, Vector s2) => s1.X > s2.X && s1.Y > s2.Y;
    public static bool operator <=(Vector s1, Vector s2) => s1.X <= s2.X && s1.Y <= s2.Y;
    public static bool operator >=(Vector s1, Vector s2) => s1.X >= s2.X && s1.Y >= s2.Y;

    public static implicit operator System.Windows.Vector(Vector s) => new(s.X, s.Y);
    public static implicit operator Vector(System.Windows.Vector s) => new(s.X, s.Y);

    public static implicit operator Size(Vector s) => new(s.X, s.Y);

    private bool PrintMembers(StringBuilder builder)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        builder.Append("X = ");
        builder.Append(X);
        builder.Append(", Y = ");
        builder.Append(Y);
        builder.Append(", Length = ");
        builder.Append(Length);
        return true;
    }
}
