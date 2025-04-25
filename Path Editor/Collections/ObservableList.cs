using System.Collections.Specialized;

namespace NobleTech.Products.PathEditor.Collections;

internal class ObservableList<T, TCollection>
    : ObservableCollection<T, TCollection>, IObservableList<T>
    where TCollection : System.Collections.Generic.IList<T>, new()
{
    public event EventHandler<CancelEventArgs<(int index, T oldValue, T newValue)>>? Updating;
    public event EventHandler<int, T, T>? Updated;
    public event EventHandler<CancelEventArgs<(int index, T value)>>? Inserting;
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
            if (!RaiseUpdating(index, oldValue, value))
                return;
            items[index] = value;
            RaiseUpdated(index, oldValue, items[index]);
        }
    }

    public int IndexOf(T item) => items.IndexOf(item);

    public virtual bool Insert(int index, T item)
    {
        if (index < 0 || index > Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Index out of range: 0 to {Count}");
        if (!RaiseInserting(index, item))
            return false;
        items.Insert(index, item);
        RaiseInserted(index, item);
        return true;
    }

    public bool Insert(Index index, T item) => Insert(index.GetOffset(Count), item);

    public T? RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Index out of range: 0 to {Count - 1}");
        T item = items[index];
        if (!RaiseRemoving(item))
            return default;
        items.RemoveAt(index);
        RaiseRemoved(item);
        return item;
    }

    public T? RemoveAt(Index index) => RemoveAt(index.GetOffset(Count));

    protected bool RaiseUpdating(int index, T oldValue, T newValue)
    {
        CancelEventArgs<(int index, T oldValue, T newValue)> args = new((index, oldValue, newValue));
        Updating?.Invoke(this, args);
        return !args.Cancel;
    }

    protected void RaiseUpdated(int index, T oldValue, T newValue)
    {
        Updated?.Invoke(this, index, oldValue, newValue);
        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Replace, newValue, oldValue, index));
    }

    protected bool RaiseInserting(int index, T item)
    {
        CancelEventArgs<(int index, T value)> args = new((index, item));
        Inserting?.Invoke(this, args);
        return !args.Cancel;
    }

    protected void RaiseInserted(int index, T item)
    {
        Inserted?.Invoke(this, index, item);
        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, item, index));
    }
}
