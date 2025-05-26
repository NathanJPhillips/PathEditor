using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace NobleTech.Products.PathEditor.Behaviors;

public static class DynamicMenuItems
{
    private const string dynamicMenuItemTag = "DynamicMenuItem";
    private const string dynamicSeparatorTag = "DynamicMenuItemSeparator";

    public static IEnumerable GetSource(DependencyObject element)
        => (IEnumerable)element.GetValue(SourceProperty);
    public static void SetSource(DependencyObject element, IEnumerable value)
        => element.SetValue(SourceProperty, value);
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.RegisterAttached(
            "Source",
            typeof(IEnumerable),
            typeof(DynamicMenuItems),
            new PropertyMetadata(null, OnSourceChanged));
    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MenuItem menuItem)
            return;
        if (e.OldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= (s, args) => RefreshMenuItems(menuItem);
        if (e.NewValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += (s, args) => RefreshMenuItems(menuItem);
        RefreshMenuItems(menuItem);
    }

    public static Style GetContainerStyle(DependencyObject element)
        => (Style)element.GetValue(ContainerStyleProperty);
    public static void SetContainerStyle(DependencyObject element, Style value)
        => element.SetValue(ContainerStyleProperty, value);
    public static readonly DependencyProperty ContainerStyleProperty =
        DependencyProperty.RegisterAttached(
            "ContainerStyle",
            typeof(Style),
            typeof(DynamicMenuItems),
            new PropertyMetadata(null, OnContainerStyleChanged));
    private static void OnContainerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuItem menuItem)
            RefreshMenuItems(menuItem);
    }

    public static string GetInsertBefore(DependencyObject element)
        => (string)element.GetValue(InsertBeforeProperty);
    public static void SetInsertBefore(DependencyObject element, string value)
        => element.SetValue(InsertBeforeProperty, value);
    public static readonly DependencyProperty InsertBeforeProperty =
        DependencyProperty.RegisterAttached(
            "InsertBefore",
            typeof(string),
            typeof(DynamicMenuItems),
            new PropertyMetadata(null, OnInsertBeforeChanged));
    private static void OnInsertBeforeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuItem menuItem)
            RefreshMenuItems(menuItem);
    }

    public static bool GetAddSeparatorBefore(DependencyObject element)
        => (bool)element.GetValue(AddSeparatorBeforeProperty);
    public static void SetAddSeparatorBefore(DependencyObject element, bool value)
        => element.SetValue(AddSeparatorBeforeProperty, value);
    public static readonly DependencyProperty AddSeparatorBeforeProperty =
        DependencyProperty.RegisterAttached(
            "AddSeparatorBefore",
            typeof(bool),
            typeof(DynamicMenuItems),
            new PropertyMetadata(false, OnAddSeparatorBeforeChanged));
    private static void OnAddSeparatorBeforeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuItem menuItem)
            RefreshMenuItems(menuItem);
    }

    private static void RefreshMenuItems(MenuItem menuItem)
    {
        // Remove previously generated dynamic items and separator (tagged for identification)
        for (int i = menuItem.Items.Count - 1; i >= 0; i--)
        {
            if (menuItem.Items[i] is MenuItem mi && (mi.Tag as string) == dynamicMenuItemTag)
                menuItem.Items.RemoveAt(i);
            else if (menuItem.Items[i] is Separator sep && (sep.Tag as string) == dynamicSeparatorTag)
                menuItem.Items.RemoveAt(i);
        }

        IEnumerable itemsSource = GetSource(menuItem);
        if (itemsSource == null)
            return;

        Style? itemStyle = GetContainerStyle(menuItem);
        string insertBeforeName = GetInsertBefore(menuItem);
        bool addSeparator = GetAddSeparatorBefore(menuItem);

        // Materialize items to check count
        List<object> items = [.. itemsSource.Cast<object>()];

        // Find the index to insert before
        int? insertIndex = null;
        if (!string.IsNullOrEmpty(insertBeforeName))
        {
            for (int i = 0; i < menuItem.Items.Count; i++)
            {
                if (menuItem.Items[i] is MenuItem mi && mi.Name == insertBeforeName)
                {
                    insertIndex = i;
                    break;
                }
            }
        }

        // Add separator if requested and there is at least one item
        if (addSeparator && items.Count > 0)
            menuItem.Items.Insert(ref insertIndex, new Separator { Tag = dynamicSeparatorTag });

        foreach (var item in items)
        {
            MenuItem dynamicMenuItem =
                new()
                {
                    Tag = dynamicMenuItemTag,
                    DataContext = item,
                };

            if (itemStyle != null)
                dynamicMenuItem.Style = itemStyle;

            menuItem.Items.Insert(ref insertIndex, dynamicMenuItem);
        }
    }

    public static void Insert(this ItemCollection items, ref int? insertIndex, FrameworkElement element)
    {
        if (insertIndex is int i)
        {
            items.Insert(i, element);
            ++insertIndex;  // Keep inserting before the same named item
        }
        else
        {
            items.Add(element);
        }
    }
}
