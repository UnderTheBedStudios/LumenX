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
using Avalonia.Platform;
using SkiaSharp;

namespace LumenX.Editors;

public class EngineViewport : OpenGlControlBase, ICustomHitTest
{
    private bool _isFlying;
    private bool _isPivoting;
    private Point _lastPointerPos;
    private readonly HashSet<Key> _keysDown = new();
    private readonly Camera _camera = new();
    private readonly Stopwatch _frameTimer = Stopwatch.StartNew();

    private bool _ignoreNextMove;


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetProcAddressDelegate(IntPtr procNamePtr);

    private GetProcAddressDelegate? _getProcAddressDelegate;
    private GlInterface? _gl;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (TopLevel.GetTopLevel(this)?.TryGetPlatformHandle() is { } handle)
        {
            Console.WriteLine($"[Wayland] Handle=0x{handle.Handle:X} Descriptor={handle.HandleDescriptor}");
        }
        else
        {
            Console.WriteLine("[Wayland] No platform handle available yet");
        }
    }

    private IntPtr GetX11WindowHandle()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return topLevel?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        _gl = gl;
        _getProcAddressDelegate = ProcAddressBridge;

        if (TopLevel.GetTopLevel(this)?.TryGetPlatformHandle() is { } handle)
            Console.WriteLine($"[Wayland] Handle=0x{handle.Handle:X} Descriptor={handle.HandleDescriptor}");
        else
            Console.WriteLine("[Wayland] Still no handle at GL init");

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

        if (_isFlying || _isPivoting) _camera.Update(_keysDown, dt);

        var view = _camera.GetViewMatrix();
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, aspect, 0.1f, 1000f);
        var vp = view * proj;

        EngineInterop.Engine_RenderFrame(fb, width, height, ref vp);

        if (_isFlying || _isPivoting) RequestNextFrameRendering();
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
            _isPivoting = false;
            _lastPointerPos = e.GetPosition(this);
            e.Pointer.Capture(this);           // keep receiving move events even if pointer leaves bounds
            Cursor = new Cursor(StandardCursorType.None);
            Focus();                            // so KeyDown/KeyUp actually route here
            RequestNextFrameRendering();        // kick the render loop (see #4)
        }
        else if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            _isPivoting = true;
            _isFlying = false;
            e.Pointer.Capture(this);
            Cursor = new Cursor(StandardCursorType.None);
            Focus();
            RequestNextFrameRendering();
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
        else if (_isPivoting)
        {
            e.Pointer.Capture(null);
            Cursor = Cursor.Default;
            _isPivoting = false;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        Console.WriteLine($"[Cam] KeyDown {e.Key}");
        _keysDown.Add(e.Key);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_isFlying)
        {
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
            X11Interop.WarpPointer(screenCenter.X, screenCenter.Y);
            _ignoreNextMove = true;
            Console.WriteLine($"[Warp] target=({screenCenter.X},{screenCenter.Y}) scaling={TopLevel.GetTopLevel(this)?.RenderScaling}");
        }
        else if (_isPivoting)
        {
            float dt = (float)_frameTimer.Elapsed.TotalSeconds;
            var pos = e.GetPosition(this);
            if (_ignoreNextMove)
            {
                _ignoreNextMove = false;
                _lastPointerPos = pos;
                return;
            }

            var delta = pos - _lastPointerPos;
            _camera.ApplyPivot(delta.X, delta.Y, dt);
            _lastPointerPos = pos;

            var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
            var screenCenter = this.PointToScreen(center);
            X11Interop.WarpPointer(screenCenter.X, screenCenter.Y);
            _ignoreNextMove = true;
            Console.WriteLine($"[Warp] target=({screenCenter.X},{screenCenter.Y}) scaling={TopLevel.GetTopLevel(this)?.RenderScaling}");
        }
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