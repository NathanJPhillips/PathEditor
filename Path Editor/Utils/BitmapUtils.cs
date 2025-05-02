using System.IO;
using System.Reflection;
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
    public static void SaveAs(Visual visual, int width, int height, BitmapEncoder encoder, string path, int dpi = 96)
    {
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write);
        SaveAs(visual, width, height, encoder, stream, dpi);
    }

    public static void SaveAs(Visual visual, int width, int height, string path, int dpi = 96)
    {
        Func<BitmapEncoder>? encoderFactory = GetEncoderForPath(path)
            ?? throw new NotSupportedException($"Unsupported file format: {Path.GetExtension(path)}");
        SaveAs(visual, width, height, encoderFactory(), path, dpi);
    }

    /// <summary>
    /// Determines the appropriate BitmapEncoder based on the file extension.
    /// </summary>
    /// <param name="path">The file path, including the extension.</param>
    /// <returns>A BitmapEncoder for the specified format.</returns>
    private static Func<BitmapEncoder>? GetEncoderForPath(string path) => GetEncoderForExtension(Path.GetExtension(path));

    private static Func<BitmapEncoder>? GetEncoderForExtension(string extension) =>
        AllEncoders
            .Where(
                codecFactory =>
                codecFactory().CodecInfo.FileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

    public static IEnumerable<Func<BitmapEncoder>> AllEncoders =>
        TypeUtils.AllDerivedClassesDefaultConstructors<BitmapEncoder>()
            .OrderBy(codecFactory => codecFactory().CodecInfo.FriendlyName);

    public static IEnumerable<Func<BitmapDecoder>> AllDecoders =>
        TypeUtils.AllDerivedClassesDefaultConstructors<BitmapDecoder>()
            .OrderBy(codecFactory => codecFactory().CodecInfo.FriendlyName);
}

public static class TypeUtils
{
    public static IEnumerable<Type> AllDerivedClasses<T>(params Assembly[] additionalAssemblies) =>
        AllDerivedClasses<T>(true, true, true, true, additionalAssemblies);

    public static IEnumerable<Type> AllDerivedClasses<T>(
        bool searchInDefiningAssembly,
        bool searchInExecutingAssembly,
        bool searchInEntryAssembly,
        bool searchInCallingAssembly,
        params Assembly[] additionalAssemblies)
    {
        Type baseClass = typeof(T);
        IEnumerable<Assembly> assemblies = additionalAssemblies;
        if (searchInDefiningAssembly)
            assemblies = assemblies.Prepend(baseClass.Assembly);
        if (searchInExecutingAssembly)
            assemblies = assemblies.Prepend(Assembly.GetExecutingAssembly());
        if (searchInEntryAssembly && Assembly.GetEntryAssembly() is Assembly entryAssembly)
            assemblies = assemblies.Prepend(entryAssembly);
        if (searchInCallingAssembly)
            assemblies = assemblies.Prepend(Assembly.GetCallingAssembly());
        return assemblies.Distinct().SelectMany(assembly => assembly.GetExportedTypes()).Where(type => type.IsSubclassOf(baseClass));
    }

    public static IEnumerable<Func<T>> AllDerivedClassesDefaultConstructors<T>() =>
        AllDerivedClasses<T>()
            .Select(type => type.GetConstructor([]))
            .Where(constructor => constructor is not null)
            .Select<ConstructorInfo?, Func<T>>(constructor => () => (T)constructor!.Invoke([]));
}
