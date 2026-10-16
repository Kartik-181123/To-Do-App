namespace TodoWidget.Interop;

using System;
using System.Diagnostics;
public static class DesktopPinner
{
    public static void ApplyDesktopStyles(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        try
        {
            nint exStyle = NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE);
            exStyle |= NativeMethods.WS_EX_TOOLWINDOW;
            exStyle &= ~NativeMethods.WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE, exStyle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DesktopPinner] Failed to apply desktop extended styles: {ex.Message}");
        }
    }
    public static bool PinToDesktop(IntPtr widgetHwnd)
    {
        if (widgetHwnd == IntPtr.Zero)
        {
            return false;
        }
        ApplyDesktopStyles(widgetHwnd);
        IntPtr progman = NativeMethods.FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            Debug.WriteLine("[DesktopPinner] Progman window not found. Falling back to HWND_BOTTOM.");
            return FallbackToBottom(widgetHwnd);
        }
        UIntPtr result;
        NativeMethods.SendMessageTimeout(
            progman,
            NativeMethods.WM_SPAWN_WORKER,
            UIntPtr.Zero,
            IntPtr.Zero,
            NativeMethods.SMTO_NORMAL,
            1000,
            out result);
        IntPtr workerW = IntPtr.Zero;
        NativeMethods.EnumWindows((tophandle, _) =>
        {
            IntPtr shellView = NativeMethods.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                workerW = NativeMethods.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
            }
            return true;
        }, IntPtr.Zero);
        if (workerW != IntPtr.Zero)
        {
            IntPtr previousParent = NativeMethods.SetParent(widgetHwnd, workerW);
            if (previousParent != IntPtr.Zero || workerW != IntPtr.Zero)
            {
                NativeMethods.SetWindowPos(
                    widgetHwnd,
                    NativeMethods.HWND_BOTTOM,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

                Debug.WriteLine($"[DesktopPinner] Successfully pinned widget to WorkerW handle: 0x{workerW.ToInt64():X}");
                return true;
            }
        }
        Debug.WriteLine("[DesktopPinner] WorkerW not found or SetParent failed; using Progman or HWND_BOTTOM fallback.");
        return FallbackToBottom(widgetHwnd, progman);
    }
    public static bool UnpinFromDesktop(IntPtr widgetHwnd)
    {
        if (widgetHwnd == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            NativeMethods.SetParent(widgetHwnd, IntPtr.Zero);
            NativeMethods.SetWindowPos(
                widgetHwnd,
                NativeMethods.HWND_NOTOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DesktopPinner] Error unpinning window: {ex.Message}");
            return false;
        }
    }
    public static bool SetAlwaysOnTop(IntPtr hWnd, bool topMost)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        IntPtr insertAfter = topMost ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST;
        return NativeMethods.SetWindowPos(
            hWnd,
            insertAfter,
            0, 0, 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }
    private static bool FallbackToBottom(IntPtr widgetHwnd, IntPtr progman = default)
    {
        try
        {
            if (progman != IntPtr.Zero)
            {
                NativeMethods.SetParent(widgetHwnd, progman);
            }

            return NativeMethods.SetWindowPos(
                widgetHwnd,
                NativeMethods.HWND_BOTTOM,
                0, 0, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DesktopPinner] FallbackToBottom error: {ex.Message}");
            return false;
        }
    }
}
