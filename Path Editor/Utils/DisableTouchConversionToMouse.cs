using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NobleTech.Products.PathEditor.Utils;

/// <summary>
/// As long as this object exists all mouse events created from a touch event for legacy support will be disabled.
/// </summary>
partial class DisableTouchConversionToMouse : IDisposable
{
    private static IntPtr hookId = IntPtr.Zero;

    public DisableTouchConversionToMouse()
    {
        hookId = SetWindowsHookEx(WH_MOUSE_LL, HookCallback, GetModuleHandle(null), 0);
        if (hookId == IntPtr.Zero)
            throw new Win32Exception();
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            MSLLHOOKSTRUCT info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var extraInfo = (uint)info.dwExtraInfo.ToInt64();
            if ((extraInfo & MOUSEEVENTF_MASK) == MOUSEEVENTF_FROMTOUCH)
            {
                if ((extraInfo & 0x80) != 0)
                {
                    // Touch Input
                    return new IntPtr(1);
                }
                else
                {
                    // Pen Input
                    return new IntPtr(1);
                }
            }
        }

        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    private bool disposed;
    public void Dispose()
    {
        if (disposed)
            return;

        UnhookWindowsHookEx(hookId);
        disposed = true;
        GC.SuppressFinalize(this);
    }

    ~DisableTouchConversionToMouse()
    {
        Dispose();
    }

    #region Interop

    // ReSharper disable InconsistentNaming

    private const uint MOUSEEVENTF_MASK = 0xFFFFFF00;
    /// <summary>
    /// The mouse event is from a touch device.
    /// </summary>
    private const uint MOUSEEVENTF_FROMTOUCH = 0xFF515700;
    private const int WH_MOUSE_LL = 14;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Contains the x- and y-coordinates of a point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    /// <summary>
    /// Contains information about a low-level mouse input event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    private static partial IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string? lpModuleName);

    // ReSharper restore InconsistentNaming

    #endregion
}
