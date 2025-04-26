using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NobleTech.Products.PathEditor.Utils;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal partial class EditorViewModel : ObservableObject, INavigationViewModel
{
    private const string fileFilter = "Paths Files|*.path|C# Source Files|*.cs|All Files|*.*";

    public EditorViewModel()
    {
        CompletePaths.CollectionChanged += (sender, e) => undoCommand?.NotifyCanExecuteChanged();
        UndonePaths.CollectionChanged += (sender, e) => redoCommand?.NotifyCanExecuteChanged();
    }

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

    public ObservableCollection<DrawablePath> CompletePaths { get; } = [];

    [ObservableProperty]
    private DrawablePath? currentPath;

    public ObservableCollection<DrawablePath> UndonePaths { get; } = [];

    public DrawnPaths DrawnPaths =>
        new(
            [.. (CurrentPath is null ? CompletePaths : CompletePaths.Append(CurrentPath)).Select(DrawablePath.ToDrawnPath)],
            CanvasSize);

    [RelayCommand]
    private void New()
    {
        CompletePaths.Clear();
        CurrentPath = null;
        FilePath = null;
    }

    [RelayCommand]
    private void Open()
    {
        CompleteCurrentPath();
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
        CompleteCurrentPath();
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
        CompleteCurrentPath();
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
            UndonePaths.Add(CompletePaths[^1]);
            CompletePaths.RemoveAt(CompletePaths.Count - 1);
        }
    }
    private bool CanUndo() => CompletePaths.Count != 0;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        CompleteCurrentPath();
        if (UndonePaths.Count == 0)
            return;
        CompletePaths.Add(UndonePaths[^1]);
        UndonePaths.RemoveAt(UndonePaths.Count - 1);
    }
    private bool CanRedo() => UndonePaths.Count != 0;

    public void ProcessPoint(Point point, InputAction action)
    {
        if (action == InputAction.Down)
            CompleteCurrentPath();

        if (CurrentPath is not { Points: ObservableCollection<Point> currentPoints })
            CurrentPath = new([point], CurrentStrokeColor, CurrentStrokeThickness);
        else if (currentPoints[^1] != point)
            currentPoints.Add(point);

        if (action == InputAction.Up)
            CompleteCurrentPath();
    }

    [RelayCommand]
    private void CropToPaths()
    {
        CompleteCurrentPath();
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
        CompleteCurrentPath();
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
        CompleteCurrentPath();
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
        CompleteCurrentPath();
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
        if (extension == ".cs")
            SaveAsCSharp(filePath);
        else if (extension == ".path")
            SaveAsBinary(filePath);
    }

    private void LoadFromBinary(string filePath)
    {
        using FileStream stream = new(filePath, FileMode.Open);
        using BinaryReader reader = new(stream);
        double width = reader.ReadDouble();
        double height = reader.ReadDouble();
        CanvasSize = new(width, height);
        int pathCount = reader.ReadInt32();
        CompletePaths.Clear();
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            int pointCount = reader.ReadInt32();
            ObservableCollection<Point> points = [];
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
    }

    private void SaveAsBinary(string filePath)
    {
        using FileStream stream = new(filePath, FileMode.Create);
        using BinaryWriter writer = new(stream);
        writer.Write(CanvasSize.Width);
        writer.Write(CanvasSize.Height);
        writer.Write(CompletePaths.Count);
        foreach (DrawablePath path in CompletePaths)
        {
            writer.Write(path.Points.Count);
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
        CompletePaths.Clear();
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
            ObservableCollection<Point> points = [];
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
    }

    private void SaveAsCSharp(string filePath)
    {
        using StreamWriter writer = new(filePath);
        FilePath = filePath;
        writer.WriteLine($"    // Created {DateTime.Now} by NobleTech Path Editor");
        writer.WriteLine($"    DrawnPaths {FileName.Replace(' ', '_')} =");
        writer.WriteLine("        new(");
        writer.WriteLine("            [");
        foreach (DrawablePath path in CompletePaths)
        {
            writer.WriteLine(
                $"                new([{string.Join(", ", path.Points.Select(pt => $"new({pt.X}, {pt.Y})"))}], new() {{ A = {path.StrokeColor.A}, R = {path.StrokeColor.R}, G = {path.StrokeColor.G}, B = {path.StrokeColor.B} }}, StrokeThickness: {path.StrokeThickness}),");
        }
        writer.WriteLine("            ],");
        writer.WriteLine($"            new({CanvasSize.Width}, {CanvasSize.Height}));");
    }

    private void CompleteCurrentPath()
    {
        if (CurrentPath is not DrawablePath drawnPath)
            return;

        if (drawnPath.Points.Count > 1)
            CompletePaths.Add(drawnPath);

        CurrentPath = null;
        UndonePaths.Clear();
    }

    private Rect GetBounds()
    {
        if (CompletePaths.Count == 0)
            return Rect.Empty;
        Rect bounds = new(CompletePaths[0].Points[0], CompletePaths[0].Points[0]);
        foreach (DrawablePath path in CompletePaths)
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
