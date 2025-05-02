using System.IO;

namespace NobleTech.Products.PathEditor.ViewModels;

internal static class AutoSaver
{
    /// <summary>
    /// A temporary folder for saving the Path Editor's auto-saved files.
    /// </summary>
    public static readonly string temporaryFolder = Path.Combine(Path.GetTempPath(), "Path Editor");

    private static readonly string autoSavePath = Path.Combine(temporaryFolder, "AutoSave.path");

    /// <summary>
    /// Save <see cref="DrawnPaths"> to a temporary file.
    /// </summary>
    /// <param name="paths">The <see cref="DrawnPaths"> to save.</param>
    public static void Save(DrawnPaths paths)
    {
        try
        {
            // Create Path Editor temporary folder if it doesn't exist
            Directory.CreateDirectory(temporaryFolder);
            // Save the current canvas to a temporary file
            using FileStream stream = new(autoSavePath, FileMode.Create);
            paths.SaveAsBinary(stream);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>
    /// Load the auto-saved <see cref="DrawnPaths"> from the temporary file.
    /// </summary>
    /// <returns>The loaded <see cref="DrawnPaths">, or null if the file does not exist or cannot be read.</returns>
    public static DrawnPaths? Open()
    {
        try
        {
            using FileStream stream = new(autoSavePath, FileMode.Open);
            return DrawnPaths.LoadFromBinary(stream);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
