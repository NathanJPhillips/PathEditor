namespace NobleTech.Products.PathEditor.Collections;

internal interface IReadOnlyObservableList<T> : IReadOnlyObservableCollection<T>, IReadOnlyList<T>
{
    event EventHandler<int, T, T>? Updated;
    event EventHandler<int, T>? Inserted;
}
