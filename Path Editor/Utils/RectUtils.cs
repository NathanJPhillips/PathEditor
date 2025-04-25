using System.Windows;

namespace NobleTech.Products.PathEditor.Utils;

internal static class RectUtils
{
    public static bool IsZeroSize(this Rect rect) => rect.IsEmpty || (rect.Width == 0 && rect.Height == 0);

    public static Point Center(this Rect rect) =>
        new(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);

    public static Rect AroundPoint(Point centre, Vector inflation) =>
        new(centre - inflation, centre + inflation);

    public static Rect UnionWith(this Rect rect, Point point)
    {
        rect.Union(point);
        return rect;
    }

    public static Rect UnionWith(this Rect rect, Rect other)
    {
        rect.Union(other);
        return rect;
    }
}
