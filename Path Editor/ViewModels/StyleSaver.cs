using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace NobleTech.Products.PathEditor.ViewModels;

internal static class StyleSaver
{
    private static readonly string stylesPath = Path.Combine(AutoSaver.temporaryFolder, "Styles.json");

    private record SerialisableStyle(string Name, Color? StrokeColor, double? StrokeThickness)
    {
        public static SerialisableStyle FromStyle(Style style) => new(style.Name, style.StrokeColor, style.StrokeThickness);
        public Style ToStyle(EditorViewModel viewModel) => new(Name, StrokeColor, StrokeThickness, viewModel);
    }

    /// <summary>
    /// Saves a list of styles to a JSON file at a predefined path.
    /// </summary>
    /// <param name="styles">The styles to save.</param>
    public static void Save(IEnumerable<Style> styles)
    {
        try
        {
            // Create Path Editor temporary folder if it doesn't exist
            Directory.CreateDirectory(AutoSaver.temporaryFolder);
            // Save the styles to a temporary file
            using FileStream stylesStream = new(stylesPath, FileMode.Create);
            JsonSerializer.Serialize(stylesStream, styles.Select(SerialisableStyle.FromStyle).ToList());
        }
        catch (IOException)
        {
        }
    }

    /// <summary>
    /// Opens a list of styles from a JSON file at a predefined path.
    /// </summary>
    /// <remarks>If the file cannot be accessed due to an I/O error or the file does not contain 
    /// styles in the valid format, the method returns <see langword="null"/>.</remarks>
    /// <returns>A list of <see cref="Style"/> objects if the file is successfully read.</returns>
    public static List<Style>? Open(EditorViewModel viewModel)
    {
        try
        {
            using FileStream stylesStream = new(stylesPath, FileMode.Open);
            List<SerialisableStyle>? loadedStyles = JsonSerializer.Deserialize<List<SerialisableStyle>>(stylesStream);
            return loadedStyles is null ? null
                : [.. loadedStyles.Select(style => style.ToStyle(viewModel))];
        }
        catch (IOException)
        {
            return null;
        }
    }
}
