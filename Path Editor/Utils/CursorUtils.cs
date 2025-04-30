using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PixelFormats = System.Windows.Media.PixelFormats;
using Visual = System.Windows.Media.Visual;
using WindowsColor = System.Windows.Media.Color;

namespace NobleTech.Products.PathEditor.Utils;

internal class CursorUtils
{
    public const int cursorSize = 32;

    public static Cursor CreateCircle(double diameter, WindowsColor color) =>
        CreateCircle((int)diameter, Color.FromArgb(color.A, color.R, color.G, color.B));

    public static Cursor CreateCircle(int diameter, Color color)
    {
        diameter = Math.Min(diameter, cursorSize);
        int topLeft = (cursorSize - diameter) / 2;

        using Bitmap bitmap = new(cursorSize, cursorSize, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            using SolidBrush brush = new(color);
            g.FillEllipse(brush, topLeft, topLeft, diameter, diameter);
        }
        return Create(bitmap);
    }

    public static Cursor Create(Visual visual, byte hotSpotX = cursorSize / 2, byte hotSpotY = cursorSize / 2) =>
        Create(ToDrawingBitmap(visual, cursorSize, cursorSize), hotSpotX, hotSpotY);

    public static Cursor Create(Bitmap bitmap, byte hotSpotX = cursorSize / 2, byte hotSpotY = cursorSize / 2) =>
        Create(Icon.FromHandle(bitmap.GetHicon()), hotSpotX, hotSpotY);

    public static Cursor Create(Icon icon, byte hotSpotX = cursorSize / 2, byte hotSpotY = cursorSize / 2)
    {
        using MemoryStream stream = new();
        icon.Save(stream);

        // Convert saved file into .cur format
        stream.Seek(0, SeekOrigin.Begin);
        stream.WriteByte(0);
        stream.WriteByte(0);
        stream.WriteByte(2);
        stream.WriteByte(0);
        // Set the hotspot
        stream.Seek(10, SeekOrigin.Begin);
        stream.WriteByte(hotSpotX);
        stream.WriteByte(0);
        stream.WriteByte(hotSpotY);
        stream.WriteByte(0);
        stream.Seek(0, SeekOrigin.Begin);

        return new Cursor(stream);
    }

    private static Bitmap ToDrawingBitmap(Visual visual, int canvasWidth, int canvasHeight, int dpi = 96)
    {
        // Render the visual to a bitmap
        RenderTargetBitmap bitmap = new(canvasWidth, canvasHeight, dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        // Convert to pixels
        var pixels = new uint[canvasWidth * canvasHeight];
        bitmap.CopyPixels(pixels, canvasWidth * sizeof(uint), 0);

        // Convert to System.Drawing.Bitmap
        Bitmap resultBitmap = new(canvasWidth, canvasHeight, PixelFormat.Format32bppPArgb);
        for (int y = 0; y < canvasHeight; y++)
        {
            for (int x = 0; x < canvasWidth; x++)
                resultBitmap.SetPixel(x, y, Color.FromArgb((int)pixels[y * canvasWidth + x]));
        }
        return resultBitmap;
    }
}
