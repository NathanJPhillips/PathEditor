using System.Windows;

namespace NobleTech.Products.PathEditor.PushBinding;

public class PushBindingManager
{
    public static readonly DependencyProperty PushBindingsProperty =
        DependencyProperty.RegisterAttached(
            "PushBindingsInternal",
            typeof(PushBindingCollection),
            typeof(PushBindingManager),
            new UIPropertyMetadata(null));
    public static PushBindingCollection GetPushBindings(DependencyObject obj)
    {
        if (obj.GetValue(PushBindingsProperty) is not PushBindingCollection value)
        {
            value = new(obj);
            SetPushBindings(obj, value);
        }
        return value;
    }
    public static void SetPushBindings(DependencyObject obj, PushBindingCollection value) =>
        obj.SetValue(PushBindingsProperty, value);

    public static readonly DependencyProperty StylePushBindingsProperty =
        DependencyProperty.RegisterAttached(
            "StylePushBindings",
            typeof(PushBindingCollection),
            typeof(PushBindingManager),
            new UIPropertyMetadata(null, StylePushBindingsChanged));
    public static PushBindingCollection GetStylePushBindings(DependencyObject obj) =>
        (PushBindingCollection)obj.GetValue(StylePushBindingsProperty);
    public static void SetStylePushBindings(DependencyObject obj, PushBindingCollection value) =>
        obj.SetValue(StylePushBindingsProperty, value);
    public static void StylePushBindingsChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is null)
            return;
        PushBindingCollection pushBindingCollection = GetPushBindings(target);
        foreach (PushBinding pushBinding in (PushBindingCollection)e.NewValue)
            pushBindingCollection.Add((PushBinding)pushBinding.Clone());
    }
}
