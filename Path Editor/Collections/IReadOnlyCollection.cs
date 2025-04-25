namespace NobleTech.Products.PathEditor.Collections;

internal interface IReadOnlyCollection<T> : IEnumerable<T>
{
    int Count { get; }
    bool Contains(T item);
}
