using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// The display remains activated for the lifetime of a Unity player. Hide its native
// window when the operator leaves the experiment, and show it again on demand.
public static class SecondDisplayWindow
{
    private const int GwlStyle = -16;
    private const int GwlWndProc = -4;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const long SecondaryChrome = WsCaption | WsThickFrame | WsSysMenu | WsMinimizeBox | WsMaximizeBox;
    private const long ChromeMask = 0x80CF0000L;
    private const uint WmClose = 0x0010;
    private const int SwHide = 0;
    private const int SwShow = 5;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;

    private static IntPtr window;
    private static bool activated;
    private static IntPtr primaryWindow;
    private static long primaryChrome;
    private static bool primaryStyleSaved;
    private static int chromeRepairFrames;
    private static IntPtr originalSecondaryWndProc;
    private static readonly WindowProcedure secondaryWndProc = SecondaryWindowProcedure;
    private static bool closeRequested;

    public static bool WasActivated => activated;

    public static bool Available => Display.displays.Length > 1 && Application.platform == RuntimePlatform.WindowsPlayer;

    public static bool Show(int displayIndex, int x, int y, int width, int height)
    {
        if (!Available || displayIndex < 1 || displayIndex >= Display.displays.Length)
            return false;

        if (!activated)
        {
            CapturePrimaryWindow();
            Display.displays[displayIndex].Activate(width, height, 60);
            activated = true;
        }

        Display.displays[displayIndex].SetParams(width, height, x, y);
        window = FindSecondaryWindow();
        if (window == IntPtr.Zero)
        {
            EnsurePrimaryWindowChrome();
            return false;
        }

        EnsureSecondaryWindowChrome();
        if (originalSecondaryWndProc == IntPtr.Zero)
            originalSecondaryWndProc = SetWindowLongPtr(window, GwlWndProc,
                Marshal.GetFunctionPointerForDelegate(secondaryWndProc));
        SetWindowPos(window, IntPtr.Zero, x, y, width, height, SwpFrameChanged | SwpNoZOrder);
        ShowWindow(window, SwShow);
        chromeRepairFrames = 15;
        EnsurePrimaryWindowChrome();
        return true;
    }

    public static void Hide()
    {
        if (window == IntPtr.Zero)
            window = FindSecondaryWindow();
        if (window != IntPtr.Zero)
            ShowWindow(window, SwHide);
        chromeRepairFrames = 15;
        EnsurePrimaryWindowChrome();
    }

    public static bool ConsumeCloseRequest()
    {
        if (!closeRequested)
            return false;
        closeRequested = false;
        return true;
    }

    public static void EnsureSecondaryWindowChrome()
    {
        if (window == IntPtr.Zero || !IsWindow(window))
            return;
        long style = GetWindowLongPtr(window, GwlStyle).ToInt64();
        if ((style & SecondaryChrome) == SecondaryChrome)
            return;
        SetWindowLongPtr(window, GwlStyle, new IntPtr(style | SecondaryChrome));
        SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0,
            SwpFrameChanged | SwpNoMove | SwpNoSize | SwpNoZOrder);
    }

    private static IntPtr SecondaryWindowProcedure(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (handle == window && message == WmClose)
        {
            closeRequested = true;
            return IntPtr.Zero;
        }
        return CallWindowProc(originalSecondaryWndProc, handle, message, wParam, lParam);
    }

    public static RectInt GetBounds()
    {
        if (window == IntPtr.Zero)
            window = FindSecondaryWindow();
        if (window != IntPtr.Zero && GetWindowRect(window, out NativeRect rect))
            return new RectInt(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        return default;
    }

    public static RectInt[] GetMonitors()
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer)
            return Array.Empty<RectInt>();
        var monitors = new System.Collections.Generic.List<RectInt>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr handle, IntPtr dc, ref NativeRect rect, IntPtr data) =>
        {
            monitors.Add(new RectInt(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top));
            return true;
        }, IntPtr.Zero);
        monitors.Sort((a, b) => a.x == 0 && a.y == 0 ? -1 : b.x == 0 && b.y == 0 ? 1 : a.x.CompareTo(b.x));
        return monitors.ToArray();
    }

    private static IntPtr FindSecondaryWindow()
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer)
            return IntPtr.Zero;

        uint processId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        IntPtr found = IntPtr.Zero;
        EnumWindows((handle, _) =>
        {
            GetWindowThreadProcessId(handle, out uint owner);
            var title = new StringBuilder(128);
            GetWindowText(handle, title, title.Capacity);
            if (owner == processId && title.ToString() == "Unity Secondary Display" && GetWindowRect(handle, out NativeRect rect)
                && rect.right > rect.left && rect.bottom > rect.top)
            {
                found = handle;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static void CapturePrimaryWindow()
    {
        uint processId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        EnumWindows((handle, _) =>
        {
            GetWindowThreadProcessId(handle, out uint owner);
            var title = new StringBuilder(128);
            GetWindowText(handle, title, title.Capacity);
            if (owner == processId && title.ToString() == Application.productName)
            {
                primaryWindow = handle;
                primaryChrome = GetWindowLongPtr(handle, GwlStyle).ToInt64() & ChromeMask;
                primaryStyleSaved = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        if (!primaryStyleSaved)
        {
            primaryWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (primaryWindow != IntPtr.Zero)
            {
                primaryChrome = GetWindowLongPtr(primaryWindow, GwlStyle).ToInt64() & ChromeMask;
                primaryStyleSaved = true;
            }
        }
    }

    public static void EnsurePrimaryWindowChrome()
    {
        if (!primaryStyleSaved || primaryWindow == IntPtr.Zero)
            return;
        long current = GetWindowLongPtr(primaryWindow, GwlStyle).ToInt64();
        long restored = (current & ~ChromeMask) | primaryChrome;
        if (current != restored || chromeRepairFrames > 0)
        {
            SetWindowLongPtr(primaryWindow, GwlStyle, new IntPtr(restored));
            SetWindowPos(primaryWindow, IntPtr.Zero, 0, 0, 0, 0,
                SwpFrameChanged | SwpNoMove | SwpNoSize | SwpNoZOrder);
        }
        if (chromeRepairFrames > 0)
            chromeRepairFrames--;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int left, top, right, bottom; }

    private delegate bool EnumWindowsCallback(IntPtr handle, IntPtr parameter);
    private delegate bool EnumMonitorsCallback(IntPtr handle, IntPtr dc, ref NativeRect rect, IntPtr data);
    private delegate IntPtr WindowProcedure(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, EnumMonitorsCallback callback, IntPtr data);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr handle, StringBuilder text, int capacity);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr handle, int command);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr handle);
    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")] private static extern IntPtr CallWindowProc(IntPtr previous, IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr handle, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr handle, IntPtr after, int x, int y, int width, int height, uint flags);
}
