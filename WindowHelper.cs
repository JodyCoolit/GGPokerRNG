using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WindowHelper
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public static IEnumerable<IntPtr> GetOpenWindows()
    {
        IList<IntPtr> windows = new List<IntPtr>();

        EnumWindowsProc callback = (hWnd, lParam) =>
        {
            windows.Add(hWnd);
            return true; // Continue enumeration
        };

        EnumWindows(callback, IntPtr.Zero);
        return windows;
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        const int nChars = 256;
        StringBuilder Buff = new StringBuilder(nChars);

        if (GetWindowText(hWnd, Buff, nChars) > 0)
        {
            return Buff.ToString();
        }
        return null;
    }
}
