using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class EditorViewModel : ObservableObject, INavigationViewModel
{
    private const string fileFilter = "Paths Files|*.path|C# Source Files|*.cs|All Files|*.*";

    private readonly List<DrawablePath> completePaths = [];
    private DrawablePath? currentPath;

    private INavigationService? navigation;
    public INavigationService Navigation
    {
        private get => navigation ?? throw new InvalidOperationException("Navigation used before it was set");
        set => navigation = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    private string? filePath = null;

    public string FileName =>
        FilePath is null ? "[Untitled]" : Path.GetFileNameWithoutExtension(FilePath);

    [ObservableProperty]
    private Size canvasSize = new(800, 600);

    [ObservableProperty]
    private Color currentStrokeColor = Colors.Black;

    [ObservableProperty]
    private double currentStrokeThickness = 5;

    public IEnumerable<DrawablePath> Paths =>
        currentPath is null || currentPath.Points.Count < 2 ? completePaths : completePaths.Append(currentPath);

    private readonly List<DrawablePath> undonePaths = [];

    public DrawnPaths DrawnPaths =>
        new([.. Paths.Select(DrawablePath.ToDrawnPath)], CanvasSize);

    public event Action<DrawablePath>? PathAdded;

    public event Action<DrawablePath>? PathRemoved;

    public event Action<DrawablePath, Point>? PathExtended;

    public event Action? RedrawRequired;

    [RelayCommand]
    private void New()
    {
        completePaths.Clear();
        currentPath = null;
        undonePaths.Clear();
        FilePath = null;
        RedrawRequired?.Invoke();
        UndoCommand?.NotifyCanExecuteChanged();
        RedoCommand?.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Open()
    {
        FileDialogViewModel viewModel = new() { Filter = fileFilter };
        if (Navigation.ShowDialog("Open", viewModel) != true || viewModel.FilePath is not string filePath)
            return;

        DrawnPaths? paths;
        string extension = Path.GetExtension(filePath);
        paths =
            extension switch
            {
                ".cs" => DrawnPaths.LoadFromCSharp(filePath),
                ".path" => DrawnPaths.LoadFromBinary(filePath),
                _ => null,
            };
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
        CanvasSize = paths.canvasSize;
        completePaths.Clear();
        completePaths.AddRange(paths.drawnPaths.Select(DrawablePath.FromDrawnPath));
        FilePath = filePath;
        RedrawRequired?.Invoke();
    }

    [RelayCommand]
    private void Save()
    {
        if (FilePath is not null)
            DoSave(FilePath);
        else
        {
            FileDialogViewModel viewModel = new() { Filter = fileFilter };
            if (Navigation.ShowDialog("Save", viewModel) == true && viewModel.FilePath is not null)
                DoSave(viewModel.FilePath);
        }
    }

    [RelayCommand]
    private void SaveAs()
    {
        FileDialogViewModel viewModel = new() { Filter = fileFilter, FilePath = FilePath };
        if (Navigation.ShowDialog("Save", viewModel) == true && viewModel.FilePath is not null)
            DoSave(viewModel.FilePath);
    }

    [RelayCommand]
    private void Exit() => Navigation.Close();

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        CompleteCurrentPath();
        if (completePaths.Count != 0)
        {
            DrawablePath removedPath = completePaths[^1];
            completePaths.RemoveAt(completePaths.Count - 1);
            PathRemoved?.Invoke(removedPath);
            undonePaths.Add(removedPath);
            UndoCommand?.NotifyCanExecuteChanged();
            RedoCommand?.NotifyCanExecuteChanged();
        }
    }
    private bool CanUndo() => DrawnPaths.drawnPaths.Length != 0;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        CompleteCurrentPath();
        if (undonePaths.Count == 0)
            return;
        DrawablePath addedPath = undonePaths[^1];
        undonePaths.RemoveAt(undonePaths.Count - 1);
        completePaths.Add(addedPath);
        PathAdded?.Invoke(addedPath);
        UndoCommand?.NotifyCanExecuteChanged();
        RedoCommand?.NotifyCanExecuteChanged();
    }
    private bool CanRedo() => undonePaths.Count != 0;

    public void ProcessPoint(Point point, InputAction action)
    {
        if (action == InputAction.Down)
            CompleteCurrentPath();

        if (currentPath is not { Points: List<Point> currentPoints })
        {
            currentPath = new([point], CurrentStrokeColor, CurrentStrokeThickness);
            PathAdded?.Invoke(currentPath);
            UndoCommand?.NotifyCanExecuteChanged();
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
        CanvasSize = bounds.Size;
        MapPaths(
            path =>
            path with { Points = [.. path.Points.Select(point => (Point)(point - bounds.TopLeft))] });
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
    }

    [RelayCommand]
    private void FitToCanvas()
    {
        Rect bounds = GetBounds();
        if (bounds.IsEmpty)
            return;
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
        MapPaths(path => path with { Points = [.. path.Points.Select(point => point + offset)] });
    }

    private void DoSave(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        try
        {
            if (extension == ".cs")
                DrawnPaths.SaveAsCSharp(filePath);
            else if (extension == ".path")
                DrawnPaths.SaveAsBinary(filePath);
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
            if (Navigation.ShowDialog("MessageBox", viewModel) == true && viewModel.SelectedButton == MessageBoxViewModel.Buttons.Yes)
                DoSave(filePath);
        }
    }

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

        completePaths.Add(path);
        PathAdded?.Invoke(path);
        undonePaths.Clear();
        RedoCommand?.NotifyCanExecuteChanged();
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
}
