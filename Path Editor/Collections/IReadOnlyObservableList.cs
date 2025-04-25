namespace NobleTech.Products.PathEditor.Collections;

internal interface IReadOnlyObservableList<T> : IReadOnlyObservableCollection<T>, IReadOnlyList<T>
{
    event EventHandler<CancelEventArgs<(int index, T oldValue, T newValue)>>? Updating;
    event EventHandler<int, T, T>? Updated;
    event EventHandler<CancelEventArgs<(int index, T value)>>? Inserting;
    event EventHandler<int, T>? Inserted;
}
