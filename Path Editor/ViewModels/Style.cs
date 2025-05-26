using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal record Style(string Name, Color? StrokeColor, double? StrokeThickness)
{
    public class NameComparer : IEqualityComparer<Style>, IComparer<Style>
    {
        public bool Equals(Style? x, Style? y) =>
            ReferenceEquals(x, y)
                || (x is not null && y is not null
                    && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

        public int Compare(Style? x, Style? y) =>
            ReferenceEquals(x, y) ? 0
                : x is null ? -1
                : y is null ? 1
                : string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode([DisallowNull] Style obj) =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
    }
}
