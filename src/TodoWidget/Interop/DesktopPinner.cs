namespace TodoWidget.Interop;

using System;
using System.Diagnostics;

public static class DesktopPinner
{
    public static void ApplyDesktopStyles(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;

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
        if (widgetHwnd == IntPtr.Zero) return false;

        try
        {
            NativeMethods.SetParent(widgetHwnd, IntPtr.Zero);
            ApplyDesktopStyles(widgetHwnd);

            NativeMethods.SetWindowPos(
                widgetHwnd,
                NativeMethods.HWND_BOTTOM,
                0, 0, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_SHOWWINDOW);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DesktopPinner] PinToDesktop error: {ex.Message}");
            return false;
        }
    }

    public static bool UnpinFromDesktop(IntPtr widgetHwnd)
    {
        if (widgetHwnd == IntPtr.Zero) return false;

        try
        {
            NativeMethods.SetParent(widgetHwnd, IntPtr.Zero);
            NativeMethods.SetWindowPos(
                widgetHwnd,
                NativeMethods.HWND_NOTOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_SHOWWINDOW);

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
        if (hWnd == IntPtr.Zero) return false;

        IntPtr insertAfter = topMost ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST;
        return NativeMethods.SetWindowPos(
            hWnd,
            insertAfter,
            0, 0, 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_SHOWWINDOW);
    }
}
