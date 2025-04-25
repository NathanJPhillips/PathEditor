namespace NobleTech.Products.PathEditor.Collections;

internal interface ICollection<T> : IReadOnlyCollection<T>
{
    bool Add(T item);
    int AddRange(IEnumerable<T> items);
    bool Remove(T item);
    int RemoveRange(IEnumerable<T> items);
    void Clear();
}
