﻿using NobleTech.Products.PathEditor.ViewModels;
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
        navigation.RegisterWindow<MainWindow>("PathEditor");
        navigation.RegisterWindow<ResizeDialog>("Resize");
        navigation.RegisterWindow<BabyPaintWindow>("BabyPaint");
        navigation.RegisterWindow<AnimationWindow>("Animation");
        MainWindow = navigation.ShowWindow("PathEditor", new EditorViewModel());
    }
}
