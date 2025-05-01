using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NobleTech.Products.PathEditor.Collections;
using NobleTech.Products.PathEditor.Utils;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

/// <summary>
/// The view model for editing windows.
/// </summary>
internal partial class EditorViewModel : ObservableObject, INavigationViewModel
{
    /// <summary>
    /// The filter used to select files in the file dialog.
    /// </summary>
    private const string fileFilter = "Paths Files|*.path|C# Source Files|*.cs|All Files|*.*";

    /// <summary>
    /// The current path being drawn on the canvas.
    /// </summary>
    private DrawablePath? currentPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorViewModel"/> class.
    /// </summary>
    public EditorViewModel()
    {
        if (AutoSaver.Open() is DrawnPaths paths)
            Open(paths);
        Paths.CollectionChanged += (sender, args) => OnPathsChanged();
    }

    private INavigationService? navigation;
    /// <summary>
    /// The navigation service used to open dialogs and close the application.
    /// </summary>
    /// <remarks>
    /// This is set by the navigation service when the view model is attached to the view.
    /// </remarks>
    public INavigationService Navigation
    {
        private get => navigation ?? throw new InvalidOperationException("Navigation used before it was set");
        set => navigation = value;
    }

    /// <summary>
    /// The path to the file being edited, or null if the file has not yet been saved.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    private string? filePath = null;

    /// <summary>
    /// The name of the file being edited, or "[Untitled]" if no file is open.
    /// </summary>
    public string FileName =>
        FilePath is null ? "[Untitled]" : Path.GetFileNameWithoutExtension(FilePath);

    /// <summary>
    /// The size of the drawing canvas.
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanvasWidth), nameof(CanvasHeight))]
    private Size canvasSize = new(1920, 1080);

    /// <summary>
    /// The width of the drawing canvas.
    /// </summary>
    public double CanvasWidth
    {
        get => CanvasSize.Width;
        set => CanvasSize = CanvasSize with { Width = value };
    }

    /// <summary>
    /// The height of the drawing canvas.
    /// </summary>
    public double CanvasHeight
    {
        get => CanvasSize.Height;
        set => CanvasSize = CanvasSize with { Height = value };
    }

    /// <summary>
    /// The color of the stroke used to draw new paths.
    /// </summary>
    [ObservableProperty]
    private Color currentStrokeColor = Colors.Blue;

    /// <summary>
    /// The thickness of the stroke used to draw new paths.
    /// </summary>
    [ObservableProperty]
    private double currentStrokeThickness = 50;

    private readonly ObservableList<DrawablePath, List<DrawablePath>> paths = [];
    /// <summary>
    /// The paths that have been drawn or are being drawn on the canvas.
    /// </summary>
    public IReadOnlyObservableCollection<DrawablePath> Paths => paths;

    /// <summary>
    /// The current state of this canvas exported to a <see cref="DrawnPaths"/> object.
    /// </summary>
    public DrawnPaths DrawnPaths => new([.. Paths.Select(DrawablePath.ToDrawnPath)], CanvasSize);

    /// <summary>
    /// The undo stack used to manage undo and redo actions.
    /// </summary>
    public UndoViewModel UndoStack { get; } = new();

    /// <summary>
    /// Set the current stroke color to the specified color.
    /// </summary>
    /// <param name="color">The color to set the stroke to.</param>
    [RelayCommand]
    private void SetColor(Color color) => CurrentStrokeColor = color;

    /// <summary>
    /// Set the current stroke thickness to the specified value.
    /// </summary>
    /// <param name="thickness">The thickness to set the stroke to.</param>
    [RelayCommand]
    private void SetThickness(double thickness) => CurrentStrokeThickness = thickness;

    /// <summary>
    /// Create a new, empty canvas.
    /// </summary>
    [RelayCommand]
    private void New()
    {
        DrawablePath[] oldPaths = [.. Paths];
        string? oldFilePath = FilePath;
        UndoStack.Do(
            "New Picture",
            () =>
            {
                paths.Clear();
                currentPath = null;
                FilePath = null;
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                FilePath = oldFilePath;
            });
    }

    /// <summary>
    /// Load a canvas from a file.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        FileDialogViewModel viewModel = new() { Filter = fileFilter };
        if (Navigation.ShowDialog(NavigationDestinations.Open, viewModel) != true || viewModel.FilePath is not string filePath)
            return;

        DrawnPaths? paths =
            Path.GetExtension(filePath) switch
            {
                ".cs" => DrawnPaths.LoadFromCSharp(filePath),
                ".path" => DrawnPaths.LoadFromBinary(filePath),
                _ => null,
            };
        if (paths is null)
        {
            Navigation.ShowDialog(
                NavigationDestinations.MessageBox,
                new MessageBoxViewModel()
                {
                    Title = "Error Loading File",
                    Message = "The file could not be loaded. Please check the file format.",
                    ButtonsToShow = MessageBoxViewModel.Buttons.OK,
                    Image = MessageBoxViewModel.Images.Error,
                });
            return;
        }

        DrawablePath[] oldPaths = [.. Paths];
        string? oldFilePath = FilePath;
        Size oldCanvasSize = CanvasSize;
        UndoStack.Do(
            "Open Picture",
            () =>
            {
                Open(paths);
                FilePath = filePath;
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                CanvasSize = oldCanvasSize;
                FilePath = oldFilePath;
            });
    }

    /// <summary>
    /// Save the current canvas to a file.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (FilePath is not null)
            DoSave(FilePath);
        else
        {
            FileDialogViewModel viewModel = new() { Filter = fileFilter };
            if (Navigation.ShowDialog(NavigationDestinations.Save, viewModel) == true && viewModel.FilePath is not null)
                DoSave(viewModel.FilePath);
        }
    }

    /// <summary>
    /// Save the current canvas to a file with a new name.
    /// </summary>
    [RelayCommand]
    private void SaveAs()
    {
        FileDialogViewModel viewModel = new() { Filter = fileFilter, FilePath = FilePath };
        if (Navigation.ShowDialog(NavigationDestinations.Save, viewModel) == true && viewModel.FilePath is not null)
            DoSave(viewModel.FilePath);
    }

    /// <summary>
    /// Perform an auto-save of the current canvas.
    /// </summary>
    public void AutoSave() => AutoSaver.Save(DrawnPaths);

    /// <summary>
    /// Close the editor window.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        Navigation.Close();
        navigation = null;
    }

    /// <summary>
    /// Open the Baby Paint full-screen view.
    /// </summary>
    [RelayCommand]
    private void BabyPaintView() => Navigation.ReplaceWindow(NavigationDestinations.BabyPaint, this);

    /// <summary>
    /// Open the default Path Editor window.
    /// </summary>
    [RelayCommand]
    private void ExitBabyPaintView() => Navigation.ReplaceWindow(NavigationDestinations.PathEditor, this);

    /// <summary>
    /// Open the animation preview window.
    /// </summary>
    [RelayCommand]
    private void PreviewAnimation() => Navigation.ShowWindow(NavigationDestinations.Animation, DrawnPaths);

    /// <summary>
    /// Processes a mouse or touch event at a point on the canvas.
    /// </summary>
    /// <param name="point">The point on the canvas that received a mouse or touch event.</param>
    /// <param name="e">The event that occured.</param>
    public void ProcessPoint(Point point, InputEvents e)
    {
        if (e == InputEvents.Down || currentPath is null)
        {
            currentPath = new([point], CurrentStrokeColor, CurrentStrokeThickness);
        }
        else if (currentPath.Points.Add(point) && currentPath.SegmentCount == 1)
        {
            DrawablePath path = currentPath;    // Capture the current path
            UndoStack.Do("Add Path", () => paths.Add(path), () => paths.Remove(path));
        }

        if (e == InputEvents.Up)
            currentPath = null;
    }

    /// <summary>
    /// Crop the canvas to the bounds of the drawn paths.
    /// </summary>
    [RelayCommand(CanExecute = nameof(BoundsArentEmpty))]
    private void CropToPaths()
    {
        Rect bounds = GetBounds();
        if (bounds.IsZeroSize())
            return;

        DrawablePath[] oldPaths = [.. Paths];
        Size oldCanvasSize = CanvasSize;
        UndoStack.Do(
            "Crop to Paths",
            () =>
            {
                CanvasSize = bounds.Size;
                MapPaths(
                    path =>
                    new(
                        points: [.. path.Points.Select(point => (Point)(point - bounds.TopLeft))],
                        path.StrokeColor,
                        path.StrokeThickness));
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                CanvasSize = oldCanvasSize;
            });
    }

    /// <summary>
    /// Show a dialog to retrieve parameters for resizing the canvas.
    /// </summary>
    [RelayCommand]
    private void ResizeCanvas() =>
        Navigation.ShowDialog(NavigationDestinations.Resize, new ResizeViewModel(CanvasSize, ResizeCanvas));

    /// <summary>
    /// Resize the canvas to the specified size.
    /// </summary>
    /// <param name="canvasSize">The new size of the canvas.</param>
    /// <param name="keepPathsProportional">Whether to keep the paths proportional to their original size.</param>
    private void ResizeCanvas(Size canvasSize, bool keepPathsProportional)
    {
        Vector scale = new(canvasSize.Width / CanvasSize.Width, canvasSize.Height / CanvasSize.Height);
        Vector offset = new();
        if (keepPathsProportional)
        {
            scale.X = scale.Y = Math.Min(scale.X, scale.Y);
            offset.X = (canvasSize.Width - CanvasSize.Width * scale.X) / 2;
            offset.Y = (canvasSize.Height - CanvasSize.Height * scale.Y) / 2;
        }
        double thicknessScale = (scale.X + scale.Y) / 2;

        DrawablePath[] oldPaths = [.. Paths];
        Size oldCanvasSize = CanvasSize;
        UndoStack.Do(
            "Resize Canvas",
            () =>
            {
                CanvasSize = canvasSize;
                MapPaths(
                    path =>
                    new(
                        points:
                            [..
                                path.Points.Select(
                                    point =>
                                    new Point(
                                        point.X * scale.X + offset.X,
                                        point.Y * scale.Y + offset.Y))
                            ],
                        path.StrokeColor,
                        path.StrokeThickness * thicknessScale));
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                CanvasSize = oldCanvasSize;
            });
    }

    /// <summary>
    /// Resize the paths so that the bounds of the paths fit the canvas.
    /// </summary>
    [RelayCommand(CanExecute = nameof(BoundsArentEmpty))]
    private void FitToCanvas()
    {
        Rect bounds = GetBounds();
        if (bounds.IsZeroSize())
            return;

        DrawablePath[] oldPaths = [.. Paths];
        UndoStack.Do(
            "Fit to Canvas",
            () =>
            MapPaths(
                path =>
                new(
                    points:
                        [..
                            path.Points.Select(
                                point =>
                                new Point(
                                    (point.X  - bounds.Left) / bounds.Width * CanvasSize.Width,
                                    (point.Y - bounds.Top) / bounds.Height * CanvasSize.Height))
                        ],
                    path.StrokeColor,
                    path.StrokeThickness)),
            () => UndoPathChanges(oldPaths));
    }

    /// <summary>
    /// Center the paths on the canvas.
    /// </summary>
    [RelayCommand(CanExecute = nameof(BoundsArentEmpty))]
    private void CenterOnCanvas()
    {
        Rect bounds = GetBounds();
        if (bounds.IsZeroSize())
            return;

        Point pathCenter = bounds.Center();
        Point canvasCenter = new(CanvasSize.Width / 2, CanvasSize.Height / 2);
        Vector offset = canvasCenter - pathCenter;

        DrawablePath[] oldPaths = [.. Paths];
        UndoStack.Do(
            "Center on Canvas",
            () =>
            MapPaths(
                path =>
                new(
                    points: [.. path.Points.Select(point => point + offset)],
                    path.StrokeColor,
                    path.StrokeThickness)),
            () => UndoPathChanges(oldPaths));
    }

    private void Open(DrawnPaths paths)
    {
        CanvasSize = paths.canvasSize;
        this.paths.ResetTo(paths.drawnPaths.Select(DrawablePath.FromDrawnPath));
        currentPath = null;
    }

    private bool BoundsArentEmpty() => !GetBounds().IsZeroSize();

    private void OnPathsChanged()
    {
        CropToPathsCommand?.NotifyCanExecuteChanged();
        FitToCanvasCommand?.NotifyCanExecuteChanged();
        CenterOnCanvasCommand?.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Save the current canvas to a file.
    /// </summary>
    /// <param name="filePath">The file path to which to save.</param>
    private void DoSave(string filePath)
    {
        try
        {
            switch (Path.GetExtension(filePath))
            {
            case ".cs":
                DrawnPaths.SaveAsCSharp(filePath);
                break;
            case ".path":
                DrawnPaths.SaveAsBinary(filePath);
                break;
            default:
                Navigation.ShowDialog(
                    NavigationDestinations.MessageBox,
                    new MessageBoxViewModel()
                    {
                        Title = "Unknown extension",
                        Message = $"A file format couldn't be determined for the selected file extension so the file has not been saved.",
                        Image = MessageBoxViewModel.Images.Warning,
                    });
                break;
            }
            FilePath = filePath;
        }
        catch (IOException ex)
        {
            MessageBoxViewModel viewModel =
                new()
                {
                    Title = "Error Saving File",
                    Message = $"An error occurred while saving the file: {ex.Message}\nShall we try to save again?",
                    ButtonsToShow = MessageBoxViewModel.Buttons.Yes | MessageBoxViewModel.Buttons.No,
                    DefaultButton = MessageBoxViewModel.Buttons.No,
                    Image = MessageBoxViewModel.Images.Warning,
                };
            if (Navigation.ShowDialog(NavigationDestinations.MessageBox, viewModel) == true
                && viewModel.SelectedButton == MessageBoxViewModel.Buttons.Yes)
            {
                DoSave(filePath);
            }
        }
    }

    /// <summary>
    /// Gets the bounds of the drawn paths.
    /// </summary>
    /// <returns>The bounds of the drawn paths.</returns>
    private Rect GetBounds()
    {
        if (Paths.Count == 0)
            return Rect.Empty;
        Rect bounds = paths[0].Bounds;
        foreach (DrawablePath path in Paths)
            bounds.Union(path.Bounds);
        return bounds;
    }

    /// <summary>
    /// Applies a transformation to all paths on the canvas.
    /// </summary>
    /// <param name="projection"></param>
    private void MapPaths(Func<DrawablePath, DrawablePath> projection)
    {
        paths.ResetTo((DrawablePath[])([.. Paths.Select(projection)]));
        currentPath = null;
    }

    /// <summary>
    /// Reverts the paths on the canvas to a previously saved state.
    /// </summary>
    /// <param name="oldPaths">The paths to revert to.</param>
    private void UndoPathChanges(DrawablePath[] oldPaths)
    {
        currentPath = null;     // Cancel current drawing operation
        paths.ResetTo(oldPaths);
    }
}
