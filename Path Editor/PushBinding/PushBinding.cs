using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace NobleTech.Products.PathEditor.PushBinding;

public class PushBinding : FreezableBinding
{
    public PushBinding()
    {
        Mode = BindingMode.OneWayToSource;
    }

    public object TargetPropertyMirror
    {
        get { return GetValue(TargetPropertyMirrorProperty); }
        set { SetValue(TargetPropertyMirrorProperty, value); }
    }
    public static readonly DependencyProperty TargetPropertyMirrorProperty =
        DependencyProperty.Register(
            "TargetPropertyMirror",
            typeof(object),
            typeof(PushBinding));

    public object TargetPropertyListener
    {
        get { return GetValue(TargetPropertyListenerProperty); }
        set { SetValue(TargetPropertyListenerProperty, value); }
    }
    public static readonly DependencyProperty TargetPropertyListenerProperty =
        DependencyProperty.Register(
            "TargetPropertyListener",
            typeof(object),
            typeof(PushBinding),
            new UIPropertyMetadata(null, OnTargetPropertyListenerChanged));
    private static void OnTargetPropertyListenerChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        ((PushBinding)sender).TargetPropertyValueChanged();

    [DefaultValue(null)]
    public string? TargetProperty { get; set; }

    [DefaultValue(null)]
    public DependencyProperty? TargetDependencyProperty { get; set; }

    public void SetupTargetBinding(DependencyObject? targetObject)
    {
        if (targetObject is null)
            return;

        // Prevent the designer from reporting exceptions since
        // changes will be made of a Binding in use if it is set
        if (DesignerProperties.GetIsInDesignMode(this))
            return;

        // Bind to the selected TargetProperty, e.g. ActualHeight and get
        // notified about changes in OnTargetPropertyListenerChanged
        Binding listenerBinding =
            new()
            {
                Source = targetObject,
                Mode = BindingMode.OneWay,
                Path =
                    TargetDependencyProperty is not null ? new(TargetDependencyProperty)
                        : new(TargetProperty)
            };
        BindingOperations.SetBinding(this, TargetPropertyListenerProperty, listenerBinding);

        // Set up a OneWayToSource Binding with the Binding declared in XAML from
        // the Mirror property of this class. The mirror property will be updated
        // everytime the Listener property gets updated
        BindingOperations.SetBinding(this, TargetPropertyMirrorProperty, Binding);
        
        TargetPropertyValueChanged();
        if (targetObject is FrameworkElement frameworkElement)
            frameworkElement.Loaded += (object sender, RoutedEventArgs e) => TargetPropertyValueChanged();
        else if (targetObject is FrameworkContentElement frameworkContentElement)
            frameworkContentElement.Loaded += (object sender, RoutedEventArgs e) => TargetPropertyValueChanged();
    }

    private void TargetPropertyValueChanged() =>
        SetValue(TargetPropertyMirrorProperty, GetValue(TargetPropertyListenerProperty));

    #region Freezable overrides

    protected override void CloneCore(Freezable sourceFreezable)
    {
        var pushBinding = (PushBinding)sourceFreezable;
        TargetProperty = pushBinding.TargetProperty;
        TargetDependencyProperty = pushBinding.TargetDependencyProperty;
        base.CloneCore(sourceFreezable);
    }

    protected override Freezable CreateInstanceCore() => new PushBinding();

    #endregion // Freezable overrides
}
