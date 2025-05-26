using System.Windows;
using System.Windows.Controls.Primitives;

namespace NobleTech.Products.PathEditor.Behaviors;

public static class Focus
{
    public static readonly DependencyProperty IsFocusedOnLoadProperty =
        DependencyProperty.RegisterAttached(
            "IsFocusedOnLoad",
            typeof(bool),
            typeof(Focus),
            new PropertyMetadata(false, OnIsFocusedOnLoadChanged));
    public static bool GetIsFocusedOnLoad(DependencyObject obj)
        => (bool)obj.GetValue(IsFocusedOnLoadProperty);
    public static void SetIsFocusedOnLoad(DependencyObject obj, bool value)
        => obj.SetValue(IsFocusedOnLoadProperty, value);
    private static void OnIsFocusedOnLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;
        if ((bool)e.NewValue)
            element.Loaded += Element_Loaded;
        else
            element.Loaded -= Element_Loaded;
    }

    private static void Element_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Loaded -= Element_Loaded;
            element.Focus();
            if (element is TextBoxBase textBox)
                textBox.SelectAll();
        }
    }
}
