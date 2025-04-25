using System.ComponentModel;

namespace NobleTech.Products.PathEditor.Collections;

public class CancelEventArgs<T>(T value) : CancelEventArgs
{
    public T Value { get; } = value;
}
