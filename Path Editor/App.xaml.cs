using NobleTech.Products.PathEditor.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NobleTech.Products.PathEditor;

public partial class App : Application
{
    public App()
    {
        EventManager.RegisterClassHandler(
            typeof(TextBox),
            UIElement.GotKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus));

        static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsReadOnly
                && (e.KeyboardDevice.IsKeyDown(Key.Tab) || e.KeyboardDevice.IsKeyDown(Key.LeftAlt)))
            {
                textBox.SelectAll();
            }
        }

        NavigationService navigation = new();
        navigation.RegisterWindow<MainWindow>(NavigationDestinations.PathEditor);
        navigation.RegisterWindow<ResizeDialog>(NavigationDestinations.Resize);
        navigation.RegisterWindow<ApplyStyleDialog>(NavigationDestinations.ApplyStyle);
        navigation.RegisterWindow<SetStyleDialog>(NavigationDestinations.SetStyle);
        navigation.RegisterWindow<CreateStyleDialog>(NavigationDestinations.CreateStyle);
        navigation.RegisterWindow<BabyPaintWindow>(NavigationDestinations.BabyPaint);
        navigation.RegisterWindow<AnimationWindow>(NavigationDestinations.Animation);
        navigation.RegisterWindow<ColourToolWindow>(NavigationDestinations.ColourToolWindow);
        MainWindow = navigation.ShowWindow(NavigationDestinations.PathEditor, new MainWindowViewModel(new()));
    }
}
