#if TWOFAST_WINDOWS
using System.Runtime.InteropServices;
using System.Text;
namespace Project2FA.Services.MacOS;

internal sealed record WindowsCaptureSource(string Name, IntPtr Window, int X, int Y, int Width, int Height, int Camera = -1)
{
    public override string ToString() => Name;
}
internal static class WindowsScreenCapture
{
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, Bits; public uint Compression, ImageSize; public int X, Y; public uint Used, Important;
    }
    private delegate bool WindowCallback(IntPtr window, IntPtr state);
    private delegate bool MonitorCallback(IntPtr monitor, IntPtr hdc, ref Rect rect, IntPtr state);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr state);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorCallback callback, IntPtr state);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr target, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] pixels, ref BitmapInfo info, uint usage);
    internal static List<WindowsCaptureSource> Sources()
    {
        var items = new List<WindowsCaptureSource>();
        int index = 0;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr dc, ref Rect r, IntPtr state) =>
        { items.Add(new("Screen " + ++index, IntPtr.Zero, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top)); return true; }, IntPtr.Zero);
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out uint process);
            if (process == Environment.ProcessId || !IsWindowVisible(window) || IsIconic(window)) return true;
            var title = new StringBuilder(512); GetWindowText(window, title, title.Capacity);
            if (title.Length > 0 && GetWindowRect(window, out var r) && r.Right > r.Left && r.Bottom > r.Top)
                items.Add(new("Window: " + title, window, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top));
            return true;
        }, IntPtr.Zero);
        return items;
    }
    internal static (byte[] Pixels, int Width, int Height) Capture(WindowsCaptureSource source)
    {
        int width = source.Width, height = source.Height;
        if (source.Window != IntPtr.Zero)
        {
            if (IsIconic(source.Window) || !GetWindowRect(source.Window, out var r)) throw new IOException("Restore the selected window or choose another source.");
            width = r.Right - r.Left; height = r.Bottom - r.Top;
        }
        if (width <= 0 || height <= 0 || (long)width * height > 33_554_432) throw new IOException("The capture source has unsupported dimensions.");
        IntPtr screen = GetDC(IntPtr.Zero), memory = IntPtr.Zero, bitmap = IntPtr.Zero, previous = IntPtr.Zero;
        try
        {
            memory = CreateCompatibleDC(screen); bitmap = CreateCompatibleBitmap(screen, width, height);
            if (screen == IntPtr.Zero || memory == IntPtr.Zero || bitmap == IntPtr.Zero) throw new IOException("Windows could not allocate a capture frame.");
            previous = SelectObject(memory, bitmap);
            bool success = source.Window == IntPtr.Zero
                ? BitBlt(memory, 0, 0, width, height, screen, source.X, source.Y, 0x40CC0020)
                : PrintWindow(source.Window, memory, 2);
            if (!success) throw new IOException("This window cannot be captured. Select its screen and keep the QR code visible.");
            SelectObject(memory, previous); previous = IntPtr.Zero;
            var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, Bits = 32 };
            byte[] pixels = new byte[checked(width * height * 4)];
            if (GetDIBits(screen, bitmap, 0, (uint)height, pixels, ref info, 0) != height) throw new IOException("Windows could not read the selected frame.");
            return (pixels, width, height);
        }
        finally
        {
            if (previous != IntPtr.Zero) SelectObject(memory, previous);
            if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
            if (memory != IntPtr.Zero) DeleteDC(memory);
            if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
        }
    }
}
#endif
