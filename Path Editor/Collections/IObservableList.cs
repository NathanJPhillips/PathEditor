namespace NobleTech.Products.PathEditor.Collections;

internal interface IObservableList<T> : IReadOnlyObservableList<T>, IObservableCollection<T>, IList<T>
{
}
