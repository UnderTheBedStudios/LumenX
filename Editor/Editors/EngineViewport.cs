using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Input;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using System.Numerics;
using Avalonia.Controls;

namespace LumenX.Editors;

public class EngineViewport : OpenGlControlBase, ICustomHitTest
{
    private bool _isFlying;
    private Point _lastPointerPos;
    private readonly HashSet<Key> _keysDown = new();
    private readonly Camera _camera = new();
    private readonly Stopwatch _frameTimer = Stopwatch.StartNew();

    private bool _ignoreNextMove;


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetProcAddressDelegate(IntPtr procNamePtr);

    private GetProcAddressDelegate? _getProcAddressDelegate;
    private GlInterface? _gl;

    private IntPtr GetX11WindowHandle()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return topLevel?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
    }

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
        float aspect = (float)width / height;
        float dt = (float)_frameTimer.Elapsed.TotalSeconds;
        _frameTimer.Restart();

        if (_isFlying) _camera.Update(_keysDown, dt);

        var view = _camera.GetViewMatrix();
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, aspect, 0.1f, 1000f);
        var vp = view * proj;

        EngineInterop.Engine_RenderFrame(fb, width, height, ref vp);

        if (_isFlying) RequestNextFrameRendering();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        Console.WriteLine("[Cam] PointerEntered viewport");
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Console.WriteLine($"[Cam] PointerPressed, right={e.GetCurrentPoint(this).Properties.IsRightButtonPressed}");
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            _isFlying = true;
            _lastPointerPos = e.GetPosition(this);
            e.Pointer.Capture(this);           // keep receiving move events even if pointer leaves bounds
            Cursor = new Cursor(StandardCursorType.None);
            Focus();                            // so KeyDown/KeyUp actually route here
            RequestNextFrameRendering();        // kick the render loop (see #4)
        }
    }

    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isFlying)
        {
            e.Pointer.Capture(null);
            Cursor = Cursor.Default;
            _isFlying = false;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        Console.WriteLine($"[Cam] KeyDown {e.Key}");
        _keysDown.Add(e.Key);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isFlying) return;

        var pos = e.GetPosition(this);
        if (_ignoreNextMove)
        {
            _ignoreNextMove = false;
            _lastPointerPos = pos;
            return;
        }

        var delta = pos - _lastPointerPos;
        _camera.ApplyMouseDelta(delta.X, delta.Y);
        _lastPointerPos = pos;

        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var screenCenter = this.PointToScreen(center);
        X11Interop.WarpPointer(GetX11WindowHandle(), screenCenter.X, screenCenter.Y);
        _ignoreNextMove = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _keysDown.Remove(e.Key);
    }

    private IntPtr ProcAddressBridge(IntPtr procNamePtr)
    {
        string? procName = Marshal.PtrToStringAnsi(procNamePtr);
        return procName != null ? _gl!.GetProcAddress(procName) : IntPtr.Zero;
    }
}