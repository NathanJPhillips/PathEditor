namespace NobleTech.Products.PathEditor.Collections;

internal class ObservableList<T, TCollection>
    : ObservableCollection<T, TCollection>, IObservableList<T>
    where TCollection : System.Collections.Generic.IList<T>, new()
{
    public event EventHandler<int, T, T>? Updated;
    public event EventHandler<int, T>? Inserted;

    public ObservableList() : base() { }
    public ObservableList(IEnumerable<T> items) : base(items) { }

    public T this[int index]
    {
        get => items[index];
        set
        {
            T oldValue = items[index];
            if (Equals(oldValue, value))
                return;
            items[index] = value;
            Updated?.Invoke(this, index, oldValue, items[index]);
        }
    }

    public int IndexOf(T item) => items.IndexOf(item);

    public virtual bool Insert(int index, T item)
    {
        items.Insert(index, item);
        Inserted?.Invoke(this, index, item);
        return true;
    }

    public bool Insert(Index index, T item) => Insert(index.GetOffset(Count), item);

    public T RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Index out of range: 0 to {Count - 1}");
        T item = items[index];
        items.RemoveAt(index);
        RaiseRemoved(item);
        return item;
    }

    public T RemoveAt(Index index) => RemoveAt(index.GetOffset(Count));
}
