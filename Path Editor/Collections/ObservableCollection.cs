using System.Collections;
using System.Collections.Specialized;

namespace NobleTech.Products.PathEditor.Collections;

internal class ObservableCollection<T, TCollection> : IObservableCollection<T>
    where TCollection : System.Collections.Generic.ICollection<T>, new()
{
    protected readonly TCollection items;

    public ObservableCollection()
    {
        items = [];
    }

    public ObservableCollection(TCollection items)
    {
        this.items = items;
    }

    public ObservableCollection(IEnumerable<T> items)
        : this()
    {
        foreach (T item in items)
            this.items.Add(item);
    }

    public event EventHandler<CancelEventArgs<T>>? Adding;
    public event EventHandler<T>? Added;
    public event EventHandler<CancelEventArgs<T>>? Removing;
    public event EventHandler<T>? Removed;
    public event EventHandler? Reset;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => items.Count;

    public bool Contains(T item) => items.Contains(item);

    public bool Add(T item)
    {
        if (!RaiseAdding(item))
            return false;
        items.Add(item);
        RaiseAdded(item);
        return true;
    }

    public int AddRange(IEnumerable<T> items) => items.Count(Add);

    public bool Remove(T item)
    {
        if (!RaiseRemoving(item) || !items.Remove(item))
            return false;
        RaiseRemoved(item);
        return true;
    }

    public int RemoveRange(IEnumerable<T> items) => items.Count(Remove);

    public void Clear()
    {
        items.Clear();
        RaiseReset();
    }

    public void ResetTo(IEnumerable<T> newItems)
    {
        items.Clear();
        foreach (var item in newItems)
            items.Add(item);
        RaiseReset();
    }

    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected bool RaiseAdding(T item)
    {
        CancelEventArgs<T> args = new(item);
        Adding?.Invoke(this, args);
        return !args.Cancel;
    }

    protected void RaiseAdded(T item)
    {
        Added?.Invoke(this, item);
        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, item));
    }

    protected bool RaiseRemoving(T item)
    {
        CancelEventArgs<T> args = new(item);
        Removing?.Invoke(this, args);
        return !args.Cancel;
    }

    protected void RaiseRemoved(T item)
    {
        Removed?.Invoke(this, item);
        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Remove, item));
    }

    protected void RaiseReset()
    {
        Reset?.Invoke(this);
        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, this.ToList()));
    }

    protected void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args) =>
        CollectionChanged?.Invoke(this, args);
}
