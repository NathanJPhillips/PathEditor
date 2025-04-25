namespace NobleTech.Products.PathEditor.ViewModels;

internal class FileDialogViewModel
{
    public string? Title { get; set; }

    public required string Filter { get; set; }

    public string? FilePath { get; set; }
}
