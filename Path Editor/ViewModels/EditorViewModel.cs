using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NobleTech.Products.PathEditor.Utils;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class EditorViewModel : ObservableObject, INavigationViewModel
{
    private const string fileFilter = "Paths Files|*.path|C# Source Files|*.cs|All Files|*.*";

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

    public List<DrawablePath> CompletePaths { get; } = [];

    [ObservableProperty]
    private DrawablePath? currentPath;

    private readonly List<DrawablePath> undonePaths = [];

    public DrawnPaths DrawnPaths =>
        new(
            [.. (CurrentPath is null ? CompletePaths : CompletePaths.Append(CurrentPath)).Select(DrawablePath.ToDrawnPath)],
            CanvasSize);

    public event Action<Point>? CurrentPathExtended;

    public event Action<DrawablePath>? CompletedPathAdded;

    public event Action<DrawablePath>? CompletedPathRemoved;

    public event Action? RedrawRequired;

    [RelayCommand]
    private void New()
    {
        CompletePaths.Clear();
        CurrentPath = null;
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
        if (Navigation.ShowDialog("Open", viewModel) == true && viewModel.FilePath is not null)
        {
            string extension = Path.GetExtension(viewModel.FilePath);
            if (extension == ".cs")
                LoadFromCSharp(viewModel.FilePath);
            else if (extension == ".path")
                LoadFromBinary(viewModel.FilePath);
        }
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
        if (CompletePaths.Count != 0)
        {
            DrawablePath removedPath = CompletePaths[^1];
            CompletePaths.RemoveAt(CompletePaths.Count - 1);
            CompletedPathRemoved?.Invoke(removedPath);
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
        CompletePaths.Add(addedPath);
        CompletedPathAdded?.Invoke(addedPath);
        UndoCommand?.NotifyCanExecuteChanged();
        RedoCommand?.NotifyCanExecuteChanged();
    }
    private bool CanRedo() => undonePaths.Count != 0;

    public void ProcessPoint(Point point, InputAction action)
    {
        if (action == InputAction.Down)
            CompleteCurrentPath();

        if (CurrentPath is not { Points: List<Point> currentPoints })
        {
            CurrentPath = new([point], CurrentStrokeColor, CurrentStrokeThickness);
            UndoCommand?.NotifyCanExecuteChanged();
        }
        else if (currentPoints[^1] != point)
            currentPoints.Add(point);
        CurrentPathExtended?.Invoke(point);

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
                SaveAsCSharp(filePath);
            else if (extension == ".path")
                SaveAsBinary(filePath);
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

    private void LoadFromBinary(string filePath)
    {
        using FileStream stream = new(filePath, FileMode.Open);
        using BinaryReader reader = new(stream);
        double width = reader.ReadDouble();
        double height = reader.ReadDouble();
        CanvasSize = new(width, height);
        int pathCount = reader.ReadInt32();
        New();
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            int pointCount = reader.ReadInt32();
            List<Point> points = [];
            for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
                points.Add(new(reader.ReadDouble(), reader.ReadDouble()));
            byte a = reader.ReadByte();
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            double strokeThickness = reader.ReadDouble();
            CompletePaths.Add(new(points, Color.FromArgb(a, r, g, b), strokeThickness));
        }
        FilePath = filePath;
        RedrawRequired?.Invoke();
    }

    private void SaveAsBinary(string filePath)
    {
        using FileStream stream = new(filePath, FileMode.Create);
        using BinaryWriter writer = new(stream);
        writer.Write(CanvasSize.Width);
        writer.Write(CanvasSize.Height);
        writer.Write(DrawnPaths.drawnPaths.Length);
        foreach (DrawnPaths.DrawnPath path in DrawnPaths.drawnPaths)
        {
            writer.Write(path.Points.Length);
            foreach (Point point in path.Points)
            {
                writer.Write(point.X);
                writer.Write(point.Y);
            }
            writer.Write(path.StrokeColor.A);
            writer.Write(path.StrokeColor.R);
            writer.Write(path.StrokeColor.G);
            writer.Write(path.StrokeColor.B);
            writer.Write(path.StrokeThickness);
        }
        FilePath = filePath;
    }

    private void LoadFromCSharp(string filePath)
    {
        using StreamReader reader = new(filePath);
        string? line;
        do
        {
            if ((line = reader.ReadLine()) is null)
                return;
        } while (!CSharpDefinitionRegex().IsMatch(line));
        if ((line = reader.ReadLine()) is null || !CSharpNewRegex().IsMatch(line))
            return;
        if ((line = reader.ReadLine()) is null || !CSharpListStartRegex().IsMatch(line))
            return;
        New();
        Regex pathRegex = CSharpPathRegex();
        Regex pointsRegex = CSharpPointsRegex();
        while (true)
        {
            if ((line = reader.ReadLine()) is null)
                return;
            Match pathMatch = pathRegex.Match(line);
            if (!pathMatch.Success)
                break;
            MatchCollection pointsMatches = pointsRegex.Matches(pathMatch.Groups["points"].Value);
            if (pointsMatches.Count == 0)
                return;
            List<Point> points = [];
            foreach (Match pointMatch in pointsMatches)
            {
                double x = double.Parse(pointMatch.Groups["x"].Value);
                double y = double.Parse(pointMatch.Groups["y"].Value);
                points.Add(new(x, y));
            }
            var color = Color.FromArgb(
                byte.Parse(pathMatch.Groups["a"].Value),
                byte.Parse(pathMatch.Groups["r"].Value),
                byte.Parse(pathMatch.Groups["g"].Value),
                byte.Parse(pathMatch.Groups["b"].Value));
            double strokeThickness = double.Parse(pathMatch.Groups["thickness"].Value);
            CompletePaths.Add(new(points, color, strokeThickness));
        }
        if (!CSharpListEndRegex().IsMatch(line))
            return;
        if ((line = reader.ReadLine()) is null)
            return;
        Match canvasSizeMatch = CSharpCanvasSizeRegex().Match(line);
        if (!canvasSizeMatch.Success)
            return;
        CanvasSize =
            new(
                double.Parse(canvasSizeMatch.Groups["width"].Value),
                double.Parse(canvasSizeMatch.Groups["height"].Value));
        FilePath = filePath;
        RedrawRequired?.Invoke();
    }

    private void SaveAsCSharp(string filePath)
    {
        using StreamWriter writer = new(filePath);
        FilePath = filePath;
        writer.WriteLine($"    // Created {DateTime.Now} by NobleTech Path Editor");
        writer.WriteLine($"    DrawnPaths {FileName.Replace(' ', '_')} =");
        writer.WriteLine("        new(");
        writer.WriteLine("            [");
        foreach (DrawnPaths.DrawnPath path in DrawnPaths.drawnPaths)
        {
            writer.WriteLine(
                $"                new([{string.Join(", ", path.Points.Select(pt => $"new({pt.X}, {pt.Y})"))}], new() {{ A = {path.StrokeColor.A}, R = {path.StrokeColor.R}, G = {path.StrokeColor.G}, B = {path.StrokeColor.B} }}, StrokeThickness: {path.StrokeThickness}),");
        }
        writer.WriteLine("            ],");
        writer.WriteLine($"            new({CanvasSize.Width}, {CanvasSize.Height}));");
    }

    private void CompleteCurrentPath()
    {
        if (CurrentPath is not DrawablePath path)
            return;

        Debug.Assert(path.Points.Count != 0);
        if (path.Points.Count > 1)
            CompletePaths.Add(path);
        else
            CompletedPathRemoved?.Invoke(path);

        CurrentPath = null;
        undonePaths.Clear();
        RedoCommand?.NotifyCanExecuteChanged();
    }

    private Rect GetBounds()
    {
        if (DrawnPaths.drawnPaths.Length == 0)
            return Rect.Empty;
        Rect bounds = new(DrawnPaths.drawnPaths[0].Points[0], DrawnPaths.drawnPaths[0].Points[0]);
        foreach (DrawnPaths.DrawnPath path in DrawnPaths.drawnPaths)
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
        DrawablePath[] completePaths = [.. CompletePaths];
        CompletePaths.Clear();
        CompletePaths.AddRange(completePaths.Select(projection));
        CurrentPath = CurrentPath is null ? null : projection(CurrentPath);
        RedrawRequired?.Invoke();
    }

    [GeneratedRegex("""^\s*DrawnPaths\s+(?<name>[\w_\d]+)\s*=\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpDefinitionRegex();
    [GeneratedRegex("""^\s*new\s*\(\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpNewRegex();
    [GeneratedRegex("""^\s*\[\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpListStartRegex();
    [GeneratedRegex("""^\s*new\s*\(\s*\[\s*(?<points>.+?)\],\s*new\(\)\s*\{\s*A\s*=\s*(?<a>\d+),\s*R\s*=\s*(?<r>\d+),\s*G\s*=\s*(?<g>\d+),\s*B\s*=\s*(?<b>\d+)\s*\},\s*StrokeThickness:\s*(?<thickness>\d+(?:\.\d+)?)\s*\)\s*,?\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpPathRegex();
    [GeneratedRegex("""\s*new\s*\(\s*(?<x>\d+(?:\.\d+)?),\s*(?<y>\d+(?:\.\d+)?)\s*\)\s*,?\s*""", RegexOptions.Compiled)]
    private static partial Regex CSharpPointsRegex();
    [GeneratedRegex("""^\s*\]\s*,\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpListEndRegex();
    [GeneratedRegex("""^\s*new\s*\(\s*(?<width>\d+(?:\.\d+)?),\s*(?<height>\d+(?:\.\d+)?)\s*\)\s*\)\s*;\s*$""", RegexOptions.Compiled)]
    private static partial Regex CSharpCanvasSizeRegex();
}
