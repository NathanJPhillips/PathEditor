using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NobleTech.Products.PathEditor.Collections;
using NobleTech.Products.PathEditor.Utils;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NobleTech.Products.PathEditor.ViewModels;

/// <summary>
/// The view model for editing windows.
/// </summary>
internal partial class EditorViewModel : ObservableObject, INavigationViewModel
{
    /// <summary>
    /// Represents a file format used for saving and loading drawn paths.
    /// </summary>
    /// <param name="Name">The name of the file format.</param>
    /// <param name="Extensions">The file extensions associated with the format.</param>
    /// <param name="Save">The action to save drawn paths to a stream.</param>
    /// <param name="Load">The function to load drawn paths from a stream. If null then loading is not supported for this format.</param>
    private record FileFormat(
           string Name,
           string[] Extensions,
           Action<DrawnPaths, Stream, string> Save,
           Func<Stream, DrawnPaths?>? Load = null)
       : IFileFormat;

    /// <summary>
    /// The combination of a file path and a save function that either is about to be or was last used to save this file.
    /// </summary>
    /// <param name="Path">The path to the file.</param>
    /// <param name="Save">The function to save drawn paths to a stream.</param>
    private record FileInformation(string Path, Action<DrawnPaths, Stream, string> Save);

    /// <summary>
    /// The native file formats supported by the application. These exclude those generated from bitmap encoders.
    /// </summary>
    private static readonly FileFormat[] nativeFileFormats =
        [
            new(
                "Paths Files",
                [".path"],
                (paths, stream, name) => paths.SaveAsBinary(stream),
                DrawnPaths.LoadFromBinary),
            new(
                "C# Source Files",
                [".cs"],
                (paths, stream, name) => paths.SaveAsCSharp(stream, name),
                DrawnPaths.LoadFromCSharp),
        ];

    private static readonly string autoSaveFolder = Path.Combine(Path.GetTempPath(), "Path Editor");
    private static readonly string autoSavePath = Path.Combine(autoSaveFolder, "AutoSave.path");

    /// <summary>
    /// The current path being drawn on the canvas.
    /// </summary>
    private DrawablePath? currentPath;

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
    /// Initializes a new instance of the <see cref="EditorViewModel"/> class.
    /// </summary>
    public EditorViewModel()
    {
        LoadAutoSave();
        Paths.Added += (paths, path) => OnPathsChanged();
        Paths.Removed += (paths, path) => OnPathsChanged();
        Paths.Reset += paths => OnPathsChanged();
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
    /// The name of the file being edited, or "[Untitled]" if no file is open.
    /// </summary>
    public string FileName =>
        FileInfo?.Path is string filePath ? Path.GetFileNameWithoutExtension(filePath) : "[Untitled]";

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
        FileInformation? oldFileInfo = FileInfo;
        UndoStack.Do(
            "New Picture",
            () =>
            {
                paths.Clear();
                currentPath = null;
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
        FileDialogViewModel viewModel = new() { FileFormats = [.. nativeFileFormats.Where(format => format.Load is not null)] };
        if (Navigation.ShowDialog(NavigationDestinations.Open, viewModel) != true || viewModel.FilePath is not string filePath)
            return;
        if (viewModel.SelectedFileFormat is not FileFormat fileFormat)
        {
            Navigation.ShowDialog(
                NavigationDestinations.MessageBox,
                new MessageBoxViewModel()
                {
                    Title = "Unknown extension",
                    Message = $"A file format couldn't be determined for the selected file extension so the file has not been opened.",
                    Image = MessageBoxViewModel.Images.Warning,
                });
            return;
        }

        DrawnPaths? paths;
        using (FileStream stream = new(filePath, FileMode.Open))
            paths = fileFormat.Load!(stream);
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
                CanvasSize = paths.canvasSize;
                this.paths.ResetTo(paths.drawnPaths.Select(DrawablePath.FromDrawnPath));
                currentPath = null;
                FileInfo = new(filePath, fileFormat.Save);
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
        string encoderPostscript = " Encoder";
        FileDialogViewModel viewModel =
            new()
            {
                FileFormats =
                    [
                        .. nativeFileFormats,
                        .. BitmapUtils.AllEncoders
                            .Select(
                                encoderFactory =>
                                {
                                    BitmapCodecInfo encoderInfo = encoderFactory().CodecInfo;
                                    return
                                        new FileFormat(
                                            encoderInfo.FriendlyName.EndsWith(encoderPostscript)
                                                ? encoderInfo.FriendlyName[0 .. ^encoderPostscript.Length]
                                                : encoderInfo.FriendlyName,
                                            encoderInfo.FileExtensions.Split(','),
                                            (paths, stream, name) => paths.SaveAsBitmap(encoderFactory(), stream));
                                })],
                FilePath = FileInfo?.Path,
            };
        if (Navigation.ShowDialog(NavigationDestinations.Save, viewModel) != true || viewModel.FilePath is not string filePath)
            return;
        if (viewModel.SelectedFileFormat is not FileFormat fileFormat)
        {
            Navigation.ShowDialog(
                NavigationDestinations.MessageBox,
                new MessageBoxViewModel()
                {
                    Title = "Unknown extension",
                    Message = $"A file format couldn't be determined for the selected file extension so the file has not been saved.",
                    Image = MessageBoxViewModel.Images.Warning,
                });
            return;
        }
        DoSave(new(filePath, fileFormat.Save));
    }

    /// <summary>
    /// Print the current canvas to a printer.
    /// </summary>
    [RelayCommand]
    private void Print() => DrawnPaths.Print(FileName);

    /// <summary>
    /// Perform an auto-save of the current canvas.
    /// </summary>
    public void AutoSave()
    {
        try
        {
            Directory.CreateDirectory(autoSaveFolder);
            using FileStream stream = new(autoSavePath, FileMode.Create);
            DrawnPaths.SaveAsBinary(stream);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>
    /// Load the auto-saved canvas from the temporary file.
    /// </summary>
    private void LoadAutoSave()
    {
        DrawnPaths paths;
        try
        {
            using FileStream stream = new(autoSavePath, FileMode.Open);
            paths = DrawnPaths.LoadFromBinary(stream);
        }
        catch (IOException)
        {
            return;
        }
        CanvasSize = paths.canvasSize;
        this.paths.ResetTo(paths.drawnPaths.Select(DrawablePath.FromDrawnPath));
        currentPath = null;
    }

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
    [RelayCommand(CanExecute = "BoundsArentEmpty")]
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
    private void ResizeCanvas()
    {
        ResizeViewModel resize = new(CanvasSize, ResizeCanvas);
        Navigation.ShowDialog(NavigationDestinations.Resize, resize);
    }

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
    [RelayCommand(CanExecute = "BoundsArentEmpty")]
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
    [RelayCommand(CanExecute = "BoundsArentEmpty")]
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
