namespace NobleTech.Products.PathEditor.Collections;

internal interface IList<T> : IReadOnlyList<T>, ICollection<T>
{
    new T this[int index] { get; set; }
    bool Insert(int index, T item);
    bool Insert(Index index, T item);
    T? RemoveAt(int index);
    T? RemoveAt(Index index);
}
