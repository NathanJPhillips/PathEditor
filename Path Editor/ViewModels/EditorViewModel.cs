using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NobleTech.Products.PathEditor.Collections;
using NobleTech.Products.PathEditor.Geometry;
using NobleTech.Products.PathEditor.Utils;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using Clipboard = System.Windows.Clipboard;
using Matrix = NobleTech.Products.PathEditor.Geometry.Matrix;

namespace NobleTech.Products.PathEditor.ViewModels;

/// <summary>
/// The view model for editing windows.
/// </summary>
internal partial class EditorViewModel : ObservableObject
{
    /// <summary>
    /// Information about a move of paths currently in progress.
    /// </summary>
    private record MoveInfo(Point StartPoint, DrawablePath[] OriginalPaths);

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
    /// The amount of movement that is considered a "no move" action, used to determine if a click should be interpreted as a selection or a move.
    /// </summary>
    private Vector NoMoveDelta => CanvasSize / 500;

    /// <summary>
    /// The last point that was clicked in selection mode, used to determine if a click should be interpreted as a selection or a move.
    /// </summary>
    private Point? lastSelectPoint;

    /// <summary>
    /// Information about a move operation currently in progress, or null if no move is in progress.
    /// </summary>
    private MoveInfo? moveInfo;

    /// <summary>
    /// A timer to allow moves to start after a click has been held on a path for an amount of time.
    /// </summary>
    private readonly Timer moveTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorViewModel"/> class.
    /// </summary>
    public EditorViewModel()
    {
        moveTimer = new(OnMoveTimeout);
        if (AutoSaver.Open() is DrawnPaths paths)
            Open(paths);
        Paths.CollectionChanged += (sender, args) => OnPathsChanged();
        SelectedPaths.CollectionChanged += (sender, args) => OnSelectionChanged();
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
    /// The mode of the editor, which determines how touches are interpreted.
    /// </summary>
    [ObservableProperty]
    private EditorModes mode = EditorModes.Draw;
    partial void OnModeChanged(EditorModes oldValue, EditorModes newValue)
    {
        switch (oldValue)
        {
        case EditorModes.Draw:
            // Complete the paths currently being drawn if we are switching to select mode.
            currentPaths.Clear();
            break;
        case EditorModes.Select:
            // Remove the selection if we are switching to draw mode.
            selectedPaths.Clear();
            break;
        }
    }

    /// <summary>
    /// Whether a move operation is currently in progress.
    /// </summary>
    public bool IsMoving
    {
        get => moveInfo is not null;
        set
        {
            if (value == IsMoving)
                return;
            if (value)
                throw new InvalidOperationException("IsMoving cannot be set to true");
            OnPropertyChanging();
            moveInfo = null;
            OnPropertyChanged();
        }
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

    private readonly ObservableCollection<DrawablePath, HashSet<DrawablePath>> selectedPaths = [];
    /// <summary>
    /// The paths that are currently selected on the canvas.
    /// </summary>
    public IReadOnlyObservableCollection<DrawablePath> SelectedPaths => selectedPaths;

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
                selectedPaths.Clear();
                FileInfo = null;
                Mode = EditorModes.Draw;
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
    /// Cut the selected paths to the clipboard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsSomethingSelected))]
    private void Cut()
    {
        Copy();
        Delete("Cut");
    }

    /// <summary>
    /// Copy the selected paths to the clipboard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsSomethingSelected))]
    private void Copy()
    {
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream))
        {
            foreach (DrawnPaths.DrawnPath? path in SelectedPaths.Select(DrawablePath.ToDrawnPath))
                path.SaveAsBinary(writer);
        }
        Clipboard.SetData("PathEditor", stream.ToArray());
    }

    /// <summary>
    /// Paste the paths from the clipboard to the canvas.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPaste))]
    private void Paste()
    {
        if (!Clipboard.ContainsData("PathEditor"))
            return;
        List<DrawablePath> paths = [];
        using (MemoryStream stream = new((byte[])Clipboard.GetData("PathEditor"), false))
        {
            using BinaryReader reader = new(stream);
            while (stream.Position < stream.Length)
                paths.Add(DrawablePath.FromDrawnPath(this)(DrawnPaths.DrawnPath.LoadFromBinary(reader)));
        }
        UndoStack.Do(
            "Paste",
            () =>
            {
                this.paths.AddRange(paths);
                selectedPaths.ResetTo(paths);
                Mode = EditorModes.Select;
            },
            () =>
            {
                this.paths.RemoveRange(paths);
                selectedPaths.RemoveRange(paths);
            });
    }
    private static bool CanPaste() => Clipboard.ContainsData("PathEditor");

    /// <summary>
    /// Select all paths on the canvas.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSelectAll))]
    private void SelectAll()
    {
        selectedPaths.ResetTo(Paths);
        Mode = EditorModes.Select;
    }
    private bool CanSelectAll() => SelectedPaths.Count < Paths.Count;

    /// <summary>
    /// Deselect all paths on the canvas.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsSomethingSelected))]
    private void DeselectAll() => selectedPaths.Clear();

    /// <summary>
    /// Invert the selection of paths on the canvas.
    /// </summary>
    [RelayCommand]
    private void InvertSelection()
    {
        DrawablePath[] previouslySelected = [.. SelectedPaths];
        selectedPaths.ResetTo(Paths.Except(previouslySelected));
        if (SelectedPaths.Count != 0)
            Mode = EditorModes.Select;
    }

    /// <summary>
    /// Delete the selected paths from the canvas.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsSomethingSelected))]
    private void Delete() => Delete("Delete");

    /// <summary>
    /// Duplicate the selected paths on the canvas.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsSomethingSelected))]
    private void Duplicate()
    {
        DrawablePath[] duplicatePaths =
            [.. SelectedPaths.Select(path => new DrawablePath(path.Points, path.StrokeColor, path.StrokeThickness, this))];
        UndoStack.Do(
            "Duplicate",
            () =>
            {
                paths.AddRange(duplicatePaths);
                selectedPaths.ResetTo(duplicatePaths);
            },
            () =>
            {
                paths.RemoveRange(duplicatePaths);
                selectedPaths.RemoveRange(duplicatePaths);
            });
    }

    private bool IsSomethingSelected() => SelectedPaths.Count != 0;

    private void OnSelectionChanged()
    {
        DeleteCommand.NotifyCanExecuteChanged();
        CutCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        SelectAllCommand.NotifyCanExecuteChanged();
        DeselectAllCommand.NotifyCanExecuteChanged();
        InvertSelectionCommand.NotifyCanExecuteChanged();
        DuplicateCommand.NotifyCanExecuteChanged();
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
        switch (Mode)
        {
        case EditorModes.Draw:
            DrawPoint(point, e, device);
            break;
        case EditorModes.Select:
            SelectAtPoint(point, e);
            break;
        default:
            throw new InvalidEnumArgumentException(nameof(Mode), (int)Mode, Mode.GetType());
        }
    }

    /// <summary>
    /// Processes a mouse or touch event at a point on the canvas in drawing mode.
    /// </summary>
    /// <param name="point">The point on the canvas that received a mouse or touch event.</param>
    /// <param name="e">The event that occured.</param>
    /// <param name="device">The input device that generated the event.</param>
    private void DrawPoint(Point point, InputEvents e, object device)
    {
        if (e == InputEvents.Down || !currentPaths.TryGetValue(device, out DrawablePath? currentPath))
            currentPaths[device] = new([point], CurrentStrokeColor, CurrentStrokeThickness, this);
        else if (currentPath.Points.Add(point) && currentPath.SegmentCount == 1)
            UndoStack.Do("Add Path", () => paths.Add(currentPath), () => paths.Remove(currentPath));

        if (e == InputEvents.Up)
            currentPaths.Remove(device);
    }

    /// <summary>
    /// Processes a mouse or touch event at a point on the canvas in selection mode.
    /// If the down and up events occur close together, the path under the point is selected or deselected.
    /// If there is no up event for a while after the down event, move mode is entered.
    /// In move mode the selected paths move with the mouse or touch point.
    /// When the mouse or touch point is released, the paths are moved to the new location.
    /// </summary>
    /// <param name="point">The point on the canvas that received a mouse or touch event.</param>
    /// <param name="e">The event that occured.</param>
    private void SelectAtPoint(Point point, InputEvents e)
    {
        if (e == InputEvents.Down)
        {
            lastSelectPoint = point;
            if (SelectedPaths.Any(path => path.HitTest(point)))
                moveTimer.Start(TimeSpan.FromSeconds(0.5));
        }
        else if (e == InputEvents.Up)
        {
            moveTimer.Stop();
            if (moveInfo is not null)
            {
                // Complete the move operation
                // Capture the moved paths and the move delta
                DrawablePath[] selectedPaths = [.. SelectedPaths];
                Vector delta = point - moveInfo.StartPoint;
                // Capture the original paths to undo the move
                DrawablePath[] originalPaths = moveInfo.OriginalPaths;
                UndoStack.Do(
                    "Move Paths",
                    () =>
                    MapPaths(
                        path =>
                        selectedPaths.Contains(path)
                            ? new(
                                path.Points.Select(point => point + delta),
                                path.StrokeColor,
                                path.StrokeThickness,
                                this)
                            : path),
                    () => UndoPathChanges(originalPaths));
                IsMoving = false;
            }
            else if (point - lastSelectPoint < NoMoveDelta)
            {
                // Select the path under the point
                IEnumerable<DrawablePath> pathsAtPoint = Paths.Reverse().Where(path => path.HitTest(point));
                if (!pathsAtPoint.Any())
                {
                    // No path under the point, deselect all paths
                    DeselectAll();
                    return;
                }
                foreach (DrawablePath path in pathsAtPoint)
                {
                    if (!path.IsSelected)
                    {
                        selectedPaths.Add(path);
                        return;
                    }
                    selectedPaths.Remove(path);
                }
            }
        }
        else if (moveInfo is not null)
        {
            Vector delta = point - moveInfo.StartPoint;
            foreach (DrawablePath path in SelectedPaths)
                path.Movement = delta;
        }
    }

    /// <summary>
    /// Handles the timeout event for the move timer, starting a move. This is triggered when a click has been held for a certain amount of time.
    /// </summary>
    private void OnMoveTimeout(object? _)
    {
        if (this.lastSelectPoint is not Point lastSelectPoint)
            return;
        OnPropertyChanging(nameof(IsMoving));
        moveInfo = new(lastSelectPoint, [..Paths]);
        this.lastSelectPoint = null;
        OnPropertyChanged(nameof(IsMoving));
    }

    /// <summary>
    /// Crop the canvas to the bounds of the drawn paths.
    /// </summary>
    [RelayCommand(CanExecute = nameof(BoundsArentEmpty))]
    private void CropToPaths()
    {
        if (GetBounds() is not Rectangle bounds)
            return;

        DrawablePath[] oldPaths = [.. Paths];
        Size oldCanvasSize = CanvasSize;
        UndoStack.Do(
            "Crop to Paths",
            () =>
            {
                CanvasSize = bounds.Size;
                MovePaths(new Point() - bounds.Origin);
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
        Matrix transformation = canvasSize / (Vector)CanvasSize;
        if (keepPathsProportional)
        {
            double scale = Math.Min(transformation.M11, transformation.M22);
            transformation =
                Matrix.CreateScale(scale)
                    * Matrix.CreateTranslation(
                        (canvasSize.Width - CanvasSize.Width * scale) / 2,
                        (canvasSize.Height - CanvasSize.Height * scale) / 2);
        }
        // Thickness doesn't have X- and Y- components so just multiply it by the average
        double thicknessScale = (transformation.M11 + transformation.M22) / 2;

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
                        path.Points.Select(point => point * transformation),
                        path.StrokeColor,
                        path.StrokeThickness * thicknessScale,
                        this));
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
        if (GetBounds() is not Rectangle bounds)
            return;

        Matrix transformation =
            Matrix.CreateTranslation(Point.Origin - bounds.Origin)
                * (CanvasSize / (Vector)bounds.Size);

        DrawablePath[] oldPaths = [.. Paths];
        UndoStack.Do(
            "Fit to Canvas",
            () => TransformPaths(transformation),
            () => UndoPathChanges(oldPaths));
    }

    /// <summary>
    /// Center the paths on the canvas.
    /// </summary>
    [RelayCommand(CanExecute = nameof(BoundsArentEmpty))]
    private void CenterOnCanvas()
    {
        if (GetBounds() is not Rectangle bounds)
            return;

        Point pathCenter = bounds.Center;
        Point canvasCenter = new(CanvasSize.Width / 2, CanvasSize.Height / 2);
        Vector offset = canvasCenter - pathCenter;

        DrawablePath[] oldPaths = [.. Paths];
        UndoStack.Do(
            "Center on Canvas",
            () => MovePaths(offset),
            () => UndoPathChanges(oldPaths));
    }

    private void Open(DrawnPaths paths)
    {
        CanvasSize = paths.canvasSize;
        this.paths.ResetTo(paths.drawnPaths.Select(DrawablePath.FromDrawnPath(this)));
        currentPaths.Clear();
        selectedPaths.Clear();
    }

    private bool BoundsArentEmpty() => GetBounds() is not null;

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

    private void Delete(string undoTitle)
    {
        DrawablePath[] pathsToDelete = [.. SelectedPaths];
        UndoStack.Do(
            undoTitle,
            () =>
            {
                paths.RemoveRange(pathsToDelete);
                // Don't just remove all selected paths as the user may have selected other paths and then redone this delete.
                selectedPaths.RemoveRange(pathsToDelete);
            },
            () =>
            {
                paths.AddRange(pathsToDelete);
                selectedPaths.ResetTo(pathsToDelete);
            });
    }

    /// <summary>
    /// Gets the bounds of the drawn paths.
    /// </summary>
    /// <returns>The bounds of the drawn paths.</returns>
    private Rectangle? GetBounds()
    {
        Rectangle? bounds = null;
        foreach (DrawablePath path in Paths)
            bounds |= path.Bounds;
        return bounds;
    }

    /// <summary>
    /// Applies a transformation to all paths on the canvas.
    /// </summary>
    /// <param name="projection"></param>
    private void MapPaths(Func<DrawablePath, DrawablePath> projection)
    {
        var updates = Paths.ToDictionary(path => path, projection);
        selectedPaths.ResetTo((DrawablePath[])[.. SelectedPaths.Select(path => updates[path])]);
        paths.ResetTo(updates.Values);
        Debug.Assert(SelectedPaths.All(Paths.Contains));
        currentPaths.Clear();
    }

    private void TransformPaths(Matrix transformation) =>
        MapPaths(
            path =>
            new(
                path.Points.Select(point => point * transformation),
                path.StrokeColor,
                path.StrokeThickness,
                this));

    private void MovePaths(Vector delta) =>
        MapPaths(
            path =>
            new(
                path.Points.Select(point => point + delta),
                path.StrokeColor,
                path.StrokeThickness,
                this));

    /// <summary>
    /// Reverts the paths on the canvas to a previously saved state.
    /// </summary>
    /// <param name="oldPaths">The paths to revert to.</param>
    private void UndoPathChanges(DrawablePath[] oldPaths)
    {
        currentPaths.Clear();   // Cancel current drawing operation
        paths.ResetTo(oldPaths);
        selectedPaths.Clear();
    }
}
