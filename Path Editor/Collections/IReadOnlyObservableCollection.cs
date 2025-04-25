using System.Collections.Specialized;

namespace NobleTech.Products.PathEditor.Collections;

internal interface IReadOnlyObservableCollection<T> : IReadOnlyCollection<T>, INotifyCollectionChanged
{
    event EventHandler<CancelEventArgs<T>>? Adding;
    event EventHandler<T>? Added;
    event EventHandler<CancelEventArgs<T>>? Removing;
    event EventHandler<T>? Removed;
    event EventHandler? Reset;
}
