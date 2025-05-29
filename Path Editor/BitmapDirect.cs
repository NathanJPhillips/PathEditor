using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NobleTech.Products.PathEditor;

internal class BitmapDirect
{
    private readonly WriteableBitmap bmp;
    private readonly unsafe byte* pBackBuffer;
    private readonly int stride;

    public BitmapDirect(int width, int height)
    {
        bmp = new(width, height, 96, 96, PixelFormats.Pbgra32, null);
        stride = bmp.BackBufferStride;
        bmp.Lock();
        bmp.AddDirtyRect(new Int32Rect(0, 0, width, height));
        unsafe
        {
            pBackBuffer = (byte*)bmp.BackBuffer;
        }
    }

    public unsafe Color this[int x, int y]
    {
        set
        {
            int index = y * stride + x * 4;
            pBackBuffer[index + 0] = value.B;
            pBackBuffer[index + 1] = value.G;
            pBackBuffer[index + 2] = value.R;
            pBackBuffer[index + 3] = value.A;
        }
    }

    public WriteableBitmap UnlockedBitmap
    {
        get
        {
            bmp.Unlock();
            return bmp;
        }
    }
}
