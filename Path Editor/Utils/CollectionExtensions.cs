using System.Collections;

namespace NobleTech.Products.PathEditor.Utils;

internal static class CollectionExtensions
{
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (T item in items)
            collection.Add(item);
    }

    public static void AddRange(this IList list, IEnumerable<object> items)
    {
        foreach (object item in items)
            list.Add(item);
    }

    public static T RemoveAt<T>(this IList<T> list, Index index)
    {
        T item = list[index];
        list.RemoveAt(index.GetOffset(list.Count));
        return item;
    }
}
