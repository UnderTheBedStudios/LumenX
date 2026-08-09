using System.Runtime.InteropServices;

namespace LumenX
{
    internal static class X11Interop
    {
        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(string? display);

        [DllImport("libX11.so.6")]
        private static extern int XWarpPointer(IntPtr display, IntPtr srcWindow,
            IntPtr destWindow, int srcX, int srcY, uint srcWidth, uint srcHeight,
            int destX, int destY);
        
        [DllImport("libX11.so.6")]
        private static extern int XFlush(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);

        private static IntPtr _display = IntPtr.Zero;

        public static void WarpPointer(int x, int y)
        {
            if (_display == IntPtr.Zero)
            _display = XOpenDisplay(null);

            if (_display == IntPtr.Zero) return;

            var root = XDefaultRootWindow(_display);
            XWarpPointer(_display, IntPtr.Zero, root, 0, 0, 0, 0, x, y);
            XFlush(_display);
        }
    }
}