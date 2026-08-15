using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using ATLAS.UI.Interop;

namespace ATLAS.UI.Services;

/// <summary>
/// Service that registers and manages system-wide global hotkeys using Win32 RegisterHotKey and SetWindowSubclass.
/// </summary>
public sealed class HotKeyService : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly NativeMethods.SUBCLASSPROC _subclassProc;
    private readonly ConcurrentDictionary<int, Action> _handlers = new();
    private bool _subclassed;
    private bool _disposed;

    public HotKeyService(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _subclassProc = WindowSubclassProc;
    }

    /// <summary>
    /// Registers a global hotkey with modifiers, key, and a callback action.
    /// </summary>
    public bool Register(int id, uint modifiers, uint vk, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (_hwnd == IntPtr.Zero || _disposed)
        {
            return false;
        }

        if (!_subclassed)
        {
            _subclassed = NativeMethods.SetWindowSubclass(
                _hwnd,
                _subclassProc,
                new UIntPtr(9999),
                IntPtr.Zero);

            if (!_subclassed)
            {
                return false;
            }
        }

        var registered = NativeMethods.RegisterHotKey(
            _hwnd,
            id,
            modifiers | NativeMethods.MOD_NOREPEAT,
            vk);

        if (registered)
        {
            _handlers[id] = callback;
        }

        return registered;
    }

    /// <summary>
    /// Unregisters a specific hotkey by its ID.
    /// </summary>
    public void Unregister(int id)
    {
        if (_hwnd == IntPtr.Zero) return;

        NativeMethods.UnregisterHotKey(_hwnd, id);
        _handlers.TryRemove(id, out _);
    }

    /// <summary>
    /// Unregisters all registered hotkeys and removes the window subclass.
    /// </summary>
    public void UnregisterAll()
    {
        if (_hwnd == IntPtr.Zero) return;

        foreach (var id in _handlers.Keys)
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
        }
        _handlers.Clear();

        if (_subclassed)
        {
            NativeMethods.RemoveWindowSubclass(_hwnd, _subclassProc, new UIntPtr(9999));
            _subclassed = false;
        }
    }

    private IntPtr WindowSubclassProc(
        IntPtr hWnd,
        uint uMsg,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr uIdSubclass,
        IntPtr dwRefData)
    {
        if (uMsg == NativeMethods.WM_HOTKEY)
        {
            var id = (int)wParam;
            if (_handlers.TryGetValue(id, out var action))
            {
                action.Invoke();
                return IntPtr.Zero;
            }
        }

        return NativeMethods.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        UnregisterAll();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
