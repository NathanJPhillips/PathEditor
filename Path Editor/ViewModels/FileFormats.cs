using NobleTech.Products.PathEditor.Utils;
using System.IO;
using System.Windows.Media.Imaging;

namespace NobleTech.Products.PathEditor.ViewModels;

internal class FileFormats(INavigationService navigation)
{
    private const string encoderPostscript = " Encoder";

    /// <summary>
    /// Represents a file format used for saving and loading drawn paths.
    /// </summary>
    /// <param name="Name">The name of the file format.</param>
    /// <param name="Extensions">The file extensions associated with the format.</param>
    /// <param name="Save">The action to save drawn paths to a stream.</param>
    /// <param name="Load">The function to load drawn paths from a stream. If null then loading is not supported for this format.</param>
    private record FileFormat(
           string Name,
           string[] Extensions,
           Action<DrawnPaths, Stream, string> Save,
           Func<Stream, DrawnPaths?>? Load = null)
       : IFileFormat;

    /// <summary>
    /// The native file formats supported by the application. These exclude those generated from bitmap encoders.
    /// </summary>
    private static readonly FileFormat[] nativeFileFormats =
        [
            new(
                "Paths Files",
                [".path"],
                (paths, stream, name) => paths.SaveAsBinary(stream),
                DrawnPaths.LoadFromBinary),
            new FileFormat(
                "SVG Files",
                [".svg"],
                (paths, stream, name) => paths.SaveAsSvg(stream),
                DrawnPaths.LoadFromSvg),
            new(
                "C# Source Files",
                [".cs"],
                (paths, stream, name) => paths.SaveAsCSharp(stream, name),
                DrawnPaths.LoadFromCSharp),
        ];

    private readonly INavigationService navigation = navigation;

    /// <summary>
    /// Ask the user for a file path and load DrawnPaths from it.
    /// </summary>
    public (FileInformation fileInfo, Func<Stream, DrawnPaths?> load)? Open()
    {
        FileDialogViewModel viewModel = new() { FileFormats = [.. nativeFileFormats.Where(format => format.Load is not null)] };
        if (navigation.ShowDialog(NavigationDestinations.Open, viewModel) != true || viewModel.FilePath is not string filePath)
            return null;
        if (viewModel.SelectedFileFormat is not FileFormat fileFormat)
        {
            navigation.ShowDialog(
                NavigationDestinations.MessageBox,
                new MessageBoxViewModel()
                {
                    Title = "Unknown extension",
                    Message = $"A file format couldn't be determined for the selected file extension so the file has not been opened.",
                    Image = MessageBoxViewModel.Images.Warning,
                });
            return null;
        }
        return (new(filePath, fileFormat.Save), fileFormat.Load!);
    }

    /// <summary>
    /// Ask the user for a file to which to save DrawnPaths.
    /// </summary>
    public FileInformation? SaveAs(string? originalPath)
    {
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
                FilePath = originalPath,
            };
        if (navigation.ShowDialog(NavigationDestinations.Save, viewModel) != true || viewModel.FilePath is not string filePath)
            return null;
        if (viewModel.SelectedFileFormat is not FileFormat fileFormat)
        {
            navigation.ShowDialog(
                NavigationDestinations.MessageBox,
                new MessageBoxViewModel()
                {
                    Title = "Unknown extension",
                    Message = $"A file format couldn't be determined for the selected file extension so the file has not been saved.",
                    Image = MessageBoxViewModel.Images.Warning,
                });
            return null;
        }
        return new(filePath, fileFormat.Save);
    }
}
