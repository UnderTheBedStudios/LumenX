using System;
using System.Runtime.InteropServices;

namespace LumenX
{
    internal static class EngineInterop
    {
        [DllImport("Engine")]
        internal static extern void Engine_Init(IntPtr getProcAddress);

        [DllImport("Engine")]
        internal static extern void Engine_RenderFrame(int fb, int width, int height);
    }
}