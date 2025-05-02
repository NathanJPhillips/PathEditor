using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NobleTech.Products.PathEditor.Utils;

internal class BitmapUtils
{
    /// <summary>
    /// Saves a Visual to a file in the specified format.
    /// </summary>
    /// <param name="visual">The WPF Visual to save.</param>
    /// <param name="width">The width of the bitmap to create.</param>
    /// <param name="height">The height of the bitmap to create.</param>
    /// <param name="encoder">The encoder to use to save the bitmap.</param>
    /// <param name="stream">The stream to which to save the Visual.</param>
    /// <param name="dpi">The DPI of the bitmap to create.</param>
    public static void SaveAs(Visual visual, int width, int height, BitmapEncoder encoder, Stream stream, int dpi = 96)
    {
        if (visual is UIElement element)
        {
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(0, 0, width, height));
            element.UpdateLayout();
        }
        RenderTargetBitmap renderBitmap = new(width, height, dpi, dpi, PixelFormats.Pbgra32);
        renderBitmap.Render(visual);
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
        encoder.Save(stream);
    }

    /// <summary>
    /// Saves a Visual to a file in the specified format.
    /// </summary>
    /// <param name="visual">The WPF Visual to save.</param>
    /// <param name="width">The width of the bitmap to create.</param>
    /// <param name="height">The height of the bitmap to create.</param>
    /// <param name="encoder">The encoder to use to save the bitmap.</param>
    /// <param name="path">The path of the file to save, including the extension.</param>
    /// <param name="dpi">The DPI of the bitmap to create.</param>
    public static void SaveAs(Visual visual, int width, int height, BitmapEncoder encoder, string path, int dpi = 96)
    {
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write);
        SaveAs(visual, width, height, encoder, stream, dpi);
    }

    /// <summary>
    /// Saeves a Visual to a file in a format determined by the file extension.
    /// </summary>
    /// <param name="visual">The WPF Visual to save.</param>
    /// <param name="width">The width of the bitmap to create.</param>
    /// <param name="height">The height of the bitmap to create.</param>
    /// <param name="path">The path of the file to save, including the extension.</param>
    /// <param name="dpi">The DPI of the bitmap to create.</param>
    /// <exception cref="NotSupportedException">Thrown if the file format is not supported by one of the available encoders.</exception>
    public static void SaveAs(Visual visual, int width, int height, string path, int dpi = 96)
    {
        Func<BitmapEncoder>? encoderFactory = GetEncoderForPath(path)
            ?? throw new NotSupportedException($"Unsupported file format: {Path.GetExtension(path)}");
        SaveAs(visual, width, height, encoderFactory(), path, dpi);
    }

    /// <summary>
    /// Gets a factory for the appropriate BitmapEncoder based on a file's extension.
    /// </summary>
    /// <param name="path">The file path, including the extension.</param>
    /// <returns>A BitmapEncoder factory for the specified format or null if no encoder is found.</returns>
    public static Func<BitmapEncoder>? GetEncoderForPath(string path) => GetEncoderForExtension(Path.GetExtension(path));

    /// <summary>
    /// Gets a factory for the appropriate BitmapEncoder for a given file extension.
    /// </summary>
    /// <param name="extension">The file extension, including the leading dot (e.g., ".png").</param>
    /// <returns>A BitmapEncoder factory for the specified format, or null if no encoder is found.</returns>
    public static Func<BitmapEncoder>? GetEncoderForExtension(string extension) =>
        AllEncoders
            .Where(
                codecFactory =>
                codecFactory().CodecInfo.FileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

    /// <summary>
    /// Gets factories for all available BitmapEncoders ordered by their friendly name.
    /// </summary>
    public static IEnumerable<Func<BitmapEncoder>> AllEncoders =>
        TypeUtils.AllDerivedClassesDefaultConstructors<BitmapEncoder>()
            .OrderBy(codecFactory => codecFactory().CodecInfo.FriendlyName);

    /// <summary>
    /// Gets factories for all available BitmapDecoders ordered by their friendly name.
    /// </summary>
    public static IEnumerable<Func<BitmapDecoder>> AllDecoders =>
        TypeUtils.AllDerivedClassesDefaultConstructors<BitmapDecoder>()
            .OrderBy(codecFactory => codecFactory().CodecInfo.FriendlyName);
}
