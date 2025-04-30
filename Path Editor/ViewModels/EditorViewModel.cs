using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NobleTech.Products.PathEditor.Collections;
using NobleTech.Products.PathEditor.Utils;
using System.IO;
using System.Text.RegularExpressions;
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
    /// The paths that have been undone and can be redone.
    /// </summary>
    private readonly ObservableList<DrawablePath, List<DrawablePath>> undonePaths = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorViewModel"/> class.
    /// </summary>
    public EditorViewModel()
    {
        Paths.CollectionChanged += (sender, args) => OnPathsChanged();
        undonePaths.CollectionChanged += (sender, args) => RedoCommand?.NotifyCanExecuteChanged();
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
    [ObservableProperty]
    private Size canvasSize = new(800, 600);

    /// <summary>
    /// The color of the stroke used to draw new paths.
    /// </summary>
    [ObservableProperty]
    private Color currentStrokeColor = Colors.Black;

    /// <summary>
    /// The thickness of the stroke used to draw new paths.
    /// </summary>
    [ObservableProperty]
    private double currentStrokeThickness = 5;

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
    /// Create a new, empty canvas.
    /// </summary>
    [RelayCommand]
    private void New()
    {
        paths.Clear();
        currentPath = null;
        undonePaths.Clear();
        FilePath = null;
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

        switch (Path.GetExtension(filePath))
        {
        case ".cs":
            LoadFromCSharp(filePath);
            break;
        case ".path":
            LoadFromBinary(filePath);
            break;
        }
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
    /// Close the editor window.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        Navigation.Close();
        navigation = null;
    }

    /// <summary>
    /// Undo the last drawn path.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (Paths.Count == 0)
            return;
        undonePaths.Add(paths.RemoveAt(^1));
        currentPath = null;
    }
    private bool CanUndo() => Paths.Count != 0;

    /// <summary>
    /// Redo the last undone path.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (undonePaths.Count == 0)
            return;
        paths.Add(undonePaths.RemoveAt(^1));
        currentPath = null;
    }
    private bool CanRedo() => undonePaths.Count != 0;

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
            paths.Add(currentPath);
            undonePaths.Clear();
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

        CanvasSize = bounds.Size;
        MapPaths(
            path =>
            new(
                points: [.. path.Points.Select(point => (Point)(point - bounds.TopLeft))],
                path.StrokeColor,
                path.StrokeThickness));
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
                path.StrokeThickness));
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

        MapPaths(
            path =>
            new(
                points: [.. path.Points.Select(point => point + offset)],
                path.StrokeColor,
                path.StrokeThickness));
    }

    private bool BoundsArentEmpty() => !GetBounds().IsZeroSize();

    private void OnPathsChanged()
    {
        UndoCommand?.NotifyCanExecuteChanged();
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
                SaveAsCSharp(filePath);
                break;
            case ".path":
                SaveAsBinary(filePath);
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

    private void LoadFromBinary(string filePath)
    {
        using FileStream stream = new(filePath, FileMode.Open);
        using BinaryReader reader = new(stream);
        double width = reader.ReadDouble();
        double height = reader.ReadDouble();
        CanvasSize = new(width, height);
        int pathCount = reader.ReadInt32();
        New();
        var paths = new DrawablePath[pathCount];
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            int pointCount = reader.ReadInt32();
            var points = new Point[pointCount];
            for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
                points[pointIndex] = new(reader.ReadDouble(), reader.ReadDouble());
            byte a = reader.ReadByte();
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            double strokeThickness = reader.ReadDouble();
            paths[pathIndex] = new(points, Color.FromArgb(a, r, g, b), strokeThickness);
        }
        this.paths.ResetTo(paths);
        FilePath = filePath;
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
        List<DrawablePath> paths = [];
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
            paths.Add(new(points, color, strokeThickness));
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
        this.paths.ResetTo(paths);
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
        foreach (DrawnPaths.DrawnPath path in DrawnPaths.drawnPaths)
        {
            writer.WriteLine(
                $"                new([{string.Join(", ", path.Points.Select(pt => $"new({pt.X}, {pt.Y})"))}], new() {{ A = {path.StrokeColor.A}, R = {path.StrokeColor.R}, G = {path.StrokeColor.G}, B = {path.StrokeColor.B} }}, StrokeThickness: {path.StrokeThickness}),");
        }
        writer.WriteLine("            ],");
        writer.WriteLine($"            new({CanvasSize.Width}, {CanvasSize.Height}));");
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
