using System;
using System.Runtime.InteropServices;

namespace LumenX
{
    internal static class EngineInterop
    {
        [DllImport("Engine", CharSet = CharSet.Ansi)]
        internal static extern void Engine_Init(IntPtr getProcAddress, string assetRoot);

        [DllImport("Engine")]
        internal static extern void Engine_RenderFrame(int fb, int width, int height, ref System.Numerics.Matrix4x4 viewProj);

        internal static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !dir.GetFiles("LumenX.code-workspace").Any())
                dir = dir.Parent;
            
            if (dir == null)
                throw new DirectoryNotFoundException("Could not locate repo root (LumenX.code-workspace not found).");
            return dir.FullName;
        }
    }
}