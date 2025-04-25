namespace NobleTech.Products.PathEditor.Collections;

internal interface IReadOnlyList<T> : IReadOnlyCollection<T>
{
    T this[int index] { get; }
    int IndexOf(T item);
}
