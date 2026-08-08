using System;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;

namespace LumenX.Editors;

public class EngineViewport : OpenGlControlBase
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetProcAddressDelegate(IntPtr procNamePtr);

    private GetProcAddressDelegate? _getProcAddressDelegate;
    private GlInterface? _gl;

    protected override void OnOpenGlInit(GlInterface gl)
    {
        _gl = gl;
        _getProcAddressDelegate = ProcAddressBridge;

        IntPtr fnPtr = Marshal.GetFunctionPointerForDelegate(_getProcAddressDelegate);
        EngineInterop.Engine_Init(fnPtr);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        int width = Math.Max(1, (int)Bounds.Width);
        int height = Math.Max(1, (int)Bounds.Height);
        EngineInterop.Engine_RenderFrame(fb, width, height);
    }

    private IntPtr ProcAddressBridge(IntPtr procNamePtr)
    {
        string? procName = Marshal.PtrToStringAnsi(procNamePtr);
        return procName != null ? _gl!.GetProcAddress(procName) : IntPtr.Zero;
    }
}