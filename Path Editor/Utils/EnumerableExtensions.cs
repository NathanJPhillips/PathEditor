namespace NobleTech.Products.PathEditor.Utils;

internal static class EnumerableExtensions
{
    public static IEnumerable<TResult> SelectFromPairs<T, TResult>(this IEnumerable<T> source, Func<T, T, TResult> projection)
    {
        IEnumerator<T> e = source.GetEnumerator();
        if (!e.MoveNext())
            yield break;
        T current = e.Current;
        while (e.MoveNext())
        {
            yield return projection(current, e.Current);
            current = e.Current;
        }
    }

    public static IEnumerable<TResult> SelectWithNext<T, TResult>(this IEnumerable<T> source, Func<T, T?, TResult> projection)
    {
        IEnumerator<T> e = source.GetEnumerator();
        if (!e.MoveNext())
            yield break;
        T current = e.Current;
        while (e.MoveNext())
        {
            yield return projection(current, e.Current);
            current = e.Current;
        }
        yield return projection(current, default);
    }

    public static void DisposeAll(this IEnumerable<IDisposable> disposables)
    {
        foreach (IDisposable disposable in disposables)
            disposable.Dispose();
    }
}
