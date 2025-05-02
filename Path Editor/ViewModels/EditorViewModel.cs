using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NobleTech.Products.PathEditor.Utils;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class EditorViewModel : ObservableObject, INavigationViewModel
{
    private record FileFormat(
        string Name,
        string[] Extensions,
        Action<DrawnPaths, Stream, string> Save,
        Func<Stream, DrawnPaths?>? Load = null) : IFileFormat;

    private record FileInformation(string Path, Action<DrawnPaths, Stream, string> Save);

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

    private readonly List<DrawablePath> completePaths = [];
    private DrawablePath? currentPath;

    private FileInformation? fileInfo;
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

    public EditorViewModel()
    {
        LoadAutoSave();
    }

    private INavigationService? navigation;
    public INavigationService Navigation
    {
        private get => navigation ?? throw new InvalidOperationException("Navigation used before it was set");
        set => navigation = value;
    }

    public string FileName =>
        FileInfo?.Path is string filePath ? Path.GetFileNameWithoutExtension(filePath) : "[Untitled]";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanvasWidth), nameof(CanvasHeight))]
    private Size canvasSize = new(1920, 1080);

    public double CanvasWidth
    {
        get => CanvasSize.Width;
        set => CanvasSize = CanvasSize with { Width = value };
    }

    public double CanvasHeight
    {
        get => CanvasSize.Height;
        set => CanvasSize = CanvasSize with { Height = value };
    }

    [ObservableProperty]
    private Color currentStrokeColor = Colors.Blue;

    [ObservableProperty]
    private double currentStrokeThickness = 50;

    public IEnumerable<DrawablePath> Paths =>
        currentPath is null || currentPath.Points.Count < 2 ? completePaths : completePaths.Append(currentPath);

    public DrawnPaths DrawnPaths =>
        new([.. Paths.Select(DrawablePath.ToDrawnPath)], CanvasSize);

    public UndoViewModel UndoStack { get; } = new();

    public event Action<DrawablePath>? PathAdded;

    public event Action<DrawablePath>? PathRemoved;

    public event Action<DrawablePath, Point>? PathExtended;

    public event Action? RedrawRequired;

    [RelayCommand]
    private void SetColor(Color color) => CurrentStrokeColor = color;

    [RelayCommand]
    private void SetThickness(double thickness) => CurrentStrokeThickness = thickness;

    [RelayCommand]
    private void New()
    {
        CompleteCurrentPath();
        DrawablePath[] oldPaths = [.. Paths];
        FileInformation? oldFileInfo = FileInfo;
        UndoStack.Do(
            "New Picture",
            () =>
            {
                Debug.Assert(currentPath is null);
                completePaths.Clear();
                FileInfo = null;
                RedrawRequired?.Invoke();
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                FileInfo = oldFileInfo;
            });
    }

    [RelayCommand]
    private void Open()
    {
        FileDialogViewModel viewModel = new() { FileFormats = [.. nativeFileFormats.Where(format => format.Load is not null)] };
        if (Navigation.ShowDialog("Open", viewModel) != true
            || viewModel.FilePath is not string filePath
            || viewModel.SelectedFileFormat is not FileFormat fileFormat)
        {
            return;
        }

        DrawnPaths? paths;
        using (FileStream stream = new(filePath, FileMode.Open))
            paths = fileFormat.Load!(stream);
        if (paths is null)
        {
            Navigation.ShowDialog(
                "MessageBox",
                new MessageBoxViewModel()
                {
                    Title = "Error Loading File",
                    Message = "The file could not be loaded. Please check the file format.",
                    ButtonsToShow = MessageBoxViewModel.Buttons.OK,
                    Image = MessageBoxViewModel.Images.Error,
                });
            return;
        }

        CompleteCurrentPath();
        DrawablePath[] oldPaths = [.. Paths];
        FileInformation? oldFileInfo = FileInfo;
        Size oldCanvasSize = CanvasSize;
        UndoStack.Do(
            "Open Picture",
            () =>
            {
                Debug.Assert(currentPath is null);
                CanvasSize = paths.canvasSize;
                completePaths.Clear();
                completePaths.AddRange(paths.drawnPaths.Select(DrawablePath.FromDrawnPath));
                FileInfo = new(filePath, fileFormat.Save);
                RedrawRequired?.Invoke();
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                CanvasSize = oldCanvasSize;
                FileInfo = oldFileInfo;
            });
    }

    [RelayCommand]
    private void Save()
    {
        if (FileInfo is not null)
            DoSave(FileInfo);
        else
            SaveAs();
    }

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
        if (Navigation.ShowDialog("Save", viewModel) == true
            && viewModel.FilePath is string filePath
            && viewModel.SelectedFileFormat is FileFormat fileFormat)
        {
            DoSave(new(filePath, fileFormat.Save));
        }
    }

    [RelayCommand]
    private void Print() => DrawnPaths.Print(FileName);

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
        completePaths.Clear();
        completePaths.AddRange(paths.drawnPaths.Select(DrawablePath.FromDrawnPath));
    }

    [RelayCommand]
    private void Exit()
    {
        Navigation.Close();
        navigation = null;
    }

    [RelayCommand]
    private void BabyPaintView() => Navigation.ReplaceWindow("BabyPaint", this);

    [RelayCommand]
    private void ExitBabyPaintView() => Navigation.ReplaceWindow("PathEditor", this);

    [RelayCommand]
    private void PreviewAnimation() => Navigation.ShowWindow("Animation", DrawnPaths);

    public void ProcessPoint(Point point, InputAction action)
    {
        if (action == InputAction.Down)
            CompleteCurrentPath();

        if (currentPath is not { Points: List<Point> currentPoints })
        {
            UndoStack.StartToDo();
            currentPath = new([point], CurrentStrokeColor, CurrentStrokeThickness);
            PathAdded?.Invoke(currentPath);
        }
        else if (currentPoints[^1] != point)
        {
            currentPoints.Add(point);
            PathExtended?.Invoke(currentPath, point);
        }

        if (action == InputAction.Up)
            CompleteCurrentPath();
    }

    [RelayCommand]
    private void CropToPaths()
    {
        Rect bounds = GetBounds();
        if (bounds.IsEmpty)
            return;

        CompleteCurrentPath();
        DrawablePath[] oldPaths = [.. Paths];
        Size oldCanvasSize = CanvasSize;
        UndoStack.Do(
            "Crop to Paths",
            () =>
            {
                Debug.Assert(currentPath is null);
                CanvasSize = bounds.Size;
                MapPaths(
                    path =>
                    path with { Points = [.. path.Points.Select(point => (Point)(point - bounds.TopLeft))] });
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                CanvasSize = oldCanvasSize;
            });
    }

    [RelayCommand]
    private void ResizeCanvas()
    {
        ResizeViewModel resize = new(CanvasSize, ResizeCanvas);
        Navigation.ShowDialog("Resize", resize);
    }

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
        CompleteCurrentPath();
        DrawablePath[] oldPaths = [.. Paths];
        Size oldCanvasSize = CanvasSize;
        UndoStack.Do(
            "Resize Canvas",
            () =>
            {
                Debug.Assert(currentPath is null);
                MapPaths(
                    path =>
                    path with
                    {
                        Points =
                            [..
                                path.Points.Select(
                                    point =>
                                    new Point(
                                        point.X * scale.X + offset.X,
                                        point.Y * scale.Y + offset.Y))
                            ],
                        StrokeThickness = path.StrokeThickness * thicknessScale,
                    });
                CanvasSize = canvasSize;
            },
            () =>
            {
                UndoPathChanges(oldPaths);
                CanvasSize = oldCanvasSize;
            });
    }

    [RelayCommand]
    private void FitToCanvas()
    {
        Rect bounds = GetBounds();
        if (bounds.IsEmpty)
            return;

        CompleteCurrentPath();
        DrawablePath[] oldPaths = [.. Paths];
        UndoStack.Do(
            "Fit to Canvas",
            () =>
            {
                Debug.Assert(currentPath is null);
                MapPaths(
                    path =>
                    path with
                    {
                        Points =
                            [..
                                path.Points.Select(
                                    point =>
                                    new Point(
                                        (point.X  - bounds.Left) / bounds.Width * CanvasSize.Width,
                                        (point.Y - bounds.Top) / bounds.Height * CanvasSize.Height))
                            ],
                    });
            },
            () => UndoPathChanges(oldPaths));
    }

    [RelayCommand]
    private void CenterOnCanvas()
    {
        Rect bounds = GetBounds();
        if (bounds.IsEmpty)
            return;

        Point pathCenter = new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        Point canvasCenter = new(CanvasSize.Width / 2, CanvasSize.Height / 2);
        Vector offset = canvasCenter - pathCenter;

        CompleteCurrentPath();
        DrawablePath[] oldPaths = [.. Paths];
        UndoStack.Do(
            "Center on Canvas",
            () =>
            {
                Debug.Assert(currentPath is null);
                MapPaths(path => (path with { Points = [.. path.Points.Select(point => point + offset)] }));
            },
            () => UndoPathChanges(oldPaths));
    }

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
            if (Navigation.ShowDialog("MessageBox", viewModel) == true && viewModel.SelectedButton == MessageBoxViewModel.Buttons.Yes)
                DoSave(fileInfo);
        }
    }

    /// <summary>
    /// Completes the current path and adds it to the list of completed paths.
    /// Undoing this action will remove the path from the list of completed paths.
    /// Redoing this action will add the path back to the list of completed paths.
    /// </summary>
    /// <remarks>
    /// Undoing while a new path is being drawn will remove the new path from the view as well as this one.
    /// Starting to draw a new path will remove the option to redo this action.
    /// </remarks>
    private void CompleteCurrentPath()
    {
        if (currentPath is not DrawablePath path)
            return;

        currentPath = null;

        Debug.Assert(path.Points.Count != 0);
        if (path.Points.Count < 2)
        {
            // Remove the path currently being drawn from the view.
            PathRemoved?.Invoke(path);
            return;
        }

        UndoStack.Do(
            "Add Path",
            () =>
            {
                Debug.Assert(currentPath is null);
                completePaths.Add(path);
                PathAdded?.Invoke(path);
            },
            () =>
            {
                if (currentPath is not null)
                {
                    // If a new path is being drawn then undo it too
                    PathRemoved?.Invoke(currentPath);
                    currentPath = null;
                }
                completePaths.Remove(path);
                PathRemoved?.Invoke(path);
            });
    }

    private Rect GetBounds()
    {
        DrawablePath[] paths = [.. Paths];
        if (paths.Length == 0)
            return Rect.Empty;
        Rect bounds = new(paths[0].Points[0], paths[0].Points[0]);
        foreach (DrawablePath path in paths)
        {
            Rect pathBounds = new(path.Points[0], path.Points[0]);
            foreach (Point point in path.Points)
                pathBounds.Union(point);
            pathBounds.Inflate(path.StrokeThickness / 2, path.StrokeThickness / 2);
            bounds.Union(pathBounds);
        }
        return bounds;
    }

    private void MapPaths(Func<DrawablePath, DrawablePath> projection)
    {
        DrawablePath[] oldPaths = [.. completePaths];
        completePaths.Clear();
        completePaths.AddRange(oldPaths.Select(projection));
        currentPath = currentPath is null ? null : projection(currentPath);
        RedrawRequired?.Invoke();
    }

    private void UndoPathChanges(DrawablePath[] oldPaths)
    {
        currentPath = null;     // Undo any partially drawn path
        completePaths.Clear();
        completePaths.AddRange(oldPaths);
        RedrawRequired?.Invoke();
    }
}
