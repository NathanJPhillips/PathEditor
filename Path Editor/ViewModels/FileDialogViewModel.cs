using System.IO;

namespace NobleTech.Products.PathEditor.ViewModels;

internal class FileDialogViewModel
{
    public string? Title { get; set; }

    public required IFileFormat[] FileFormats { get; set; }

    public string Filter =>
        string.Join(
            "|",
            FileFormats
                .Select(format => $"{format.Name}|{string.Join(';', format.Extensions.Select(ext => $"*{ext}"))}")
                .Append("All Files|*.*"));

    public string? FilePath { get; set; }

    public int? SelectedFilterIndex { get; set; }

    public IFileFormat? SelectedFileFormat =>
        SelectedFilterIndex is not int filterIndex ? null
            : filterIndex < FileFormats.Length ? FileFormats[filterIndex]
            : FileFormats.FirstOrDefault(
                format =>
                format.Extensions.Contains(Path.GetExtension(FilePath), StringComparer.OrdinalIgnoreCase));
}
