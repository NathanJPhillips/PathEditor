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
internal partial class EditorViewModel : ObservableObject
{
    private FileInformation? fileInfo;
    /// <summary>
    /// The file path and save function of the currently opened file, or null if the file has not yet been saved.
    /// </summary>
    private FileInformation? FileInfo
    {
        get => fileInfo;
        set
        {
            if (fileInfo == value)
                return;
            fileInfo = value;
            OnPropertyChanged(nameof(FileName));
        }
    }

    /// <summary>
    /// Map from input device to the path currently being drawn on the canvas with that device.
    /// </summary>
    private readonly Dictionary<object, DrawablePath> currentPaths = [];

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
        get => navigation ?? throw new InvalidOperationException("Navigation used before it was set");
        set => navigation = value;
    }

    /// <summary>
    /// The name of the file being edited, or "[Untitled]" if no file is open.
    /// </summary>
    public string FileName =>
        FileInfo?.Path is string filePath ? Path.GetFileNameWithoutExtension(filePath) : "[Untitled]";

    /// <summary>
    /// The size of the drawing canvas.
    /// </summary>
    [ObservableProperty]
    private Size canvasSize = new(1920, 1080);

    /// <summary>
    /// The background of the canvas, which could be a solid colour or an image, for example.
    /// </summary>
    [ObservableProperty]
    public IBackground? background;

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
    /// Create a new, empty canvas.
    /// </summary>
    [RelayCommand]
    private void New()
    {
        DrawablePath[] oldPaths = [.. Paths];
        FileInformation? oldFileInfo = FileInfo;
        UndoStack.Do(
            "New Picture",
            () =>
            {
                paths.Clear();
                currentPaths.Clear();
                FileInfo = null;
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                FileInfo = oldFileInfo;
            });
    }

    /// <summary>
    /// Load a canvas from a file.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        FileFormats fileFormats = new(Navigation);
        if (fileFormats.Open() is not (FileInformation fileInfo, Func<Stream, DrawnPaths?> load))
            return;

        DrawnPaths? paths;
        using (FileStream stream = new(fileInfo.Path, FileMode.Open))
            paths = load(stream);
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
        FileInformation? oldFileInfo = FileInfo;
        Size oldCanvasSize = CanvasSize;
        UndoStack.Do(
            "Open Picture",
            () =>
            {
                Open(paths);
                FileInfo = fileInfo;
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                CanvasSize = oldCanvasSize;
                FileInfo = oldFileInfo;
            });
    }

    /// <summary>
    /// Save the current canvas to a file.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (FileInfo is not null)
            DoSave(FileInfo);
        else
            SaveAs();
    }

    /// <summary>
    /// Save the current canvas to a file with a new name.
    /// </summary>
    [RelayCommand]
    private void SaveAs()
    {
        FileFormats fileFormats = new(Navigation);
        if (fileFormats.SaveAs(FileInfo?.Path) is FileInformation fileInfo)
            DoSave(fileInfo);
    }

    /// <summary>
    /// Print the current canvas to a printer.
    /// </summary>
    [RelayCommand]
    private void Print() => DrawnPaths.Print(FileName);

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
    /// Open the animation preview window.
    /// </summary>
    [RelayCommand]
    private void PreviewAnimation() => Navigation.ShowWindow(NavigationDestinations.Animation, DrawnPaths);

    /// <summary>
    /// Processes a mouse or touch event at a point on the canvas.
    /// </summary>
    /// <param name="point">The point on the canvas that received a mouse or touch event.</param>
    /// <param name="e">The event that occured.</param>
    /// <param name="device">The input device that generated the event.</param>
    public void ProcessPoint(Point point, InputEvents e, object device)
    {
        if (e == InputEvents.Down || !currentPaths.TryGetValue(device, out DrawablePath? currentPath))
            currentPaths[device] = new([point], CurrentStrokeColor, CurrentStrokeThickness);
        else if (currentPath.Points.Add(point) && currentPath.SegmentCount == 1)
            UndoStack.Do("Add Path", () => paths.Add(currentPath), () => paths.Remove(currentPath));

        if (e == InputEvents.Up)
            currentPaths.Remove(device);
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
        currentPaths.Clear();
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
    /// <param name="fileInfo">The file information containing the path and save function to use.</param>
    private void DoSave(FileInformation fileInfo)
    {
        try
        {
            using FileStream stream = new(fileInfo.Path, FileMode.Create);
            fileInfo.Save(DrawnPaths, stream, Path.GetFileNameWithoutExtension(fileInfo.Path));
            FileInfo = fileInfo;
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
                DoSave(fileInfo);
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
        currentPaths.Clear();
    }

    /// <summary>
    /// Reverts the paths on the canvas to a previously saved state.
    /// </summary>
    /// <param name="oldPaths">The paths to revert to.</param>
    private void UndoPathChanges(DrawablePath[] oldPaths)
    {
        currentPaths.Clear();   // Cancel current drawing operation
        paths.ResetTo(oldPaths);
    }
}
