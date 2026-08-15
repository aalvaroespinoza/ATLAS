using System.Runtime.InteropServices;
using ATLAS.UI.Interop;

namespace ATLAS.UI.Services;

/// <summary>
/// Service that registers a system-wide global hotkey using Win32 RegisterHotKey and SetWindowSubclass.
/// </summary>
public sealed class HotKeyService : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly int _hotKeyId;
    private readonly NativeMethods.SUBCLASSPROC _subclassProc;
    private bool _isRegistered;
    private bool _disposed;

    public event Action? HotKeyPressed;

    public HotKeyService(IntPtr hwnd, int hotKeyId = 1001)
    {
        _hwnd = hwnd;
        _hotKeyId = hotKeyId;
        _subclassProc = WindowSubclassProc;
    }

    /// <summary>
    /// Registers the Ctrl+Space global hotkey and subclasses the window procedure.
    /// </summary>
    public bool Register()
    {
        if (_isRegistered || _hwnd == IntPtr.Zero)
        {
            return _isRegistered;
        }

        // Subclass the window to intercept WM_HOTKEY
        var subclassed = NativeMethods.SetWindowSubclass(
            _hwnd,
            _subclassProc,
            new UIntPtr((uint)_hotKeyId),
            IntPtr.Zero);

        if (!subclassed)
        {
            return false;
        }

        // Register Ctrl+Space with MOD_NOREPEAT
        var registered = NativeMethods.RegisterHotKey(
            _hwnd,
            _hotKeyId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_NOREPEAT,
            NativeMethods.VK_SPACE);

        if (!registered)
        {
            NativeMethods.RemoveWindowSubclass(_hwnd, _subclassProc, new UIntPtr((uint)_hotKeyId));
            return false;
        }

        _isRegistered = true;
        return true;
    }

    /// <summary>
    /// Unregisters the hotkey and removes the window subclass.
    /// </summary>
    public void Unregister()
    {
        if (!_isRegistered || _hwnd == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_hwnd, _hotKeyId);
        NativeMethods.RemoveWindowSubclass(_hwnd, _subclassProc, new UIntPtr((uint)_hotKeyId));
        _isRegistered = false;
    }

    private IntPtr WindowSubclassProc(
        IntPtr hWnd,
        uint uMsg,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr uIdSubclass,
        IntPtr dwRefData)
    {
        if (uMsg == NativeMethods.WM_HOTKEY && (int)wParam == _hotKeyId)
        {
            HotKeyPressed?.Invoke();
            return IntPtr.Zero;
        }

        return NativeMethods.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Unregister();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
