using System.Collections;

namespace NobleTech.Products.PathEditor.Collections;

internal class ObservableCollection<T, TCollection> : IObservableCollection<T>
    where TCollection : System.Collections.Generic.ICollection<T>, new()
{
    protected readonly TCollection items = [];

    public ObservableCollection() { }
    public ObservableCollection(IEnumerable<T> items)
    {
        foreach (T item in items)
            this.items.Add(item);
    }

    public event EventHandler<CancelEventArgs<T>>? Adding;
    public event EventHandler<T>? Added;
    public event EventHandler<CancelEventArgs<T>>? Removing;
    public event EventHandler<T>? Removed;
    public event EventHandler? Reset;

    public int Count => items.Count;

    public bool Contains(T item) => items.Contains(item);

    public bool Add(T item)
    {
        CancelEventArgs<T> args = new(item);
        Adding?.Invoke(this, args);
        if (args.Cancel)
            return false;
        items.Add(item);
        RaiseAdded(item);
        return true;
    }

    public int AddRange(IEnumerable<T> items) => items.Count(Add);

    public bool Remove(T item)
    {
        CancelEventArgs<T> args = new(item);
        Removing?.Invoke(this, args);
        if (args.Cancel || !items.Remove(item))
            return false;
        RaiseRemoved(item);
        return true;
    }

    public int RemoveRange(IEnumerable<T> items) => items.Count(Remove);

    public void Clear()
    {
        items.Clear();
        Reset?.Invoke(this);
    }

    public void ResetTo(IEnumerable<T> newItems)
    {
        items.Clear();
        foreach (var item in newItems)
            items.Add(item);
        Reset?.Invoke(this);
    }

    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected void RaiseAdded(T item) => Added?.Invoke(this, item);
    protected void RaiseRemoved(T item) => Removed?.Invoke(this, item);
}
