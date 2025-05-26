using System.Collections.Specialized;

namespace NobleTech.Products.PathEditor.Collections;

internal class ObservableSortedSet<T> : ObservableCollection<T, SortedSet<T>>
{
    public ObservableSortedSet()
    {
    }

    public ObservableSortedSet(SortedSet<T> items)
        : base(items)
    {
    }

    public ObservableSortedSet(IEnumerable<T> items)
        : base(items)
    {
    }

    public ObservableSortedSet(IComparer<T> comparer)
        : this(new SortedSet<T>(comparer))
    {
    }

    public ObservableSortedSet(IEnumerable<T> items, IComparer<T> comparer)
        : this(new SortedSet<T>(items, comparer))
    {
    }

    public event EventHandler<CancelEventArgs<(T, T)>>? Replacing;
    public event EventHandler<T, T>? Replaced;

    public bool AddOrUpdate(T item)
    {
        if (items.TryGetValue(item, out T? existingItem))
        {
            if (!RaiseReplacing(existingItem, item))
                return false;
            int index =
                items.Index()
                    .First(item => items.Comparer.Compare(item.Item, existingItem) == 0)
                    .Index;
            items.Remove(existingItem);
            items.Add(item);
            RaiseReplaced(index, existingItem, item);
            return true;
        }
        else
        {
            if (!RaiseAdding(item))
                return false;
            if (!items.Add(item))
                return false;
            RaiseAdded(item);
            return true;
        }
    }

    protected bool RaiseReplacing(T oldItem, T newItem)
    {
        CancelEventArgs<(T, T)> args = new((oldItem, newItem));
        Replacing?.Invoke(this, args);
        return !args.Cancel;
    }

    protected void RaiseReplaced(int index, T oldItem, T newItem)
    {
        Replaced?.Invoke(this, oldItem, newItem);
        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Replace, newItem, oldItem, index));
    }
}
