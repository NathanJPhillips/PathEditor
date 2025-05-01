using System.Windows;
using System.Windows.Media.Animation;

namespace NobleTech.Products.PathEditor;

partial class AnimationWindow : Window
{
    private Storyboard? storyboard;

    public AnimationWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        RestartAnimation();

    private void Restart_Click(object sender, RoutedEventArgs e) => RestartAnimation();

    private void RestartAnimation()
    {
        storyboard?.Stop();
        Canvas.Children.Clear();
        storyboard = (DataContext as DrawnPaths)?.Animate(Canvas, Duration);
    }
}
