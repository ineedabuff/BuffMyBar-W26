using System;
using System.Runtime.InteropServices;

namespace BuffBar.Interop;

/// <summary>
/// Déclarations P/Invoke Win32 pour l'API AppBar (shell32) et la gestion fenêtre/écran (user32).
/// Tout est interne au projet : aucune dépendance externe.
/// </summary>
internal static class NativeMethods
{
    // ---------- Structures ----------

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;   // bornes physiques complètes de l'écran
        public RECT rcWork;      // zone de travail (hors barres réservées)
        public uint dwFlags;
    }

    // ---------- Messages AppBar (ABM_*) ----------

    public const int ABM_NEW = 0x00000000;
    public const int ABM_REMOVE = 0x00000001;
    public const int ABM_QUERYPOS = 0x00000002;
    public const int ABM_SETPOS = 0x00000003;
    public const int ABM_GETTASKBARPOS = 0x00000005;
    public const int ABM_WINDOWPOSCHANGED = 0x00000009;

    // ---------- Notifications AppBar (ABN_*) ----------

    public const int ABN_STATECHANGE = 0x00000000;
    public const int ABN_POSCHANGED = 0x00000001;
    public const int ABN_FULLSCREENAPP = 0x00000002;
    public const int ABN_WINDOWARRANGE = 0x00000003;

    // ---------- Bords (ABE_*) ----------

    public const int ABE_LEFT = 0;
    public const int ABE_TOP = 1;
    public const int ABE_RIGHT = 2;
    public const int ABE_BOTTOM = 3;

    // ---------- Styles fenêtre étendus ----------

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOOLWINDOW = 0x00000080;  // pas dans Alt+Tab / barre des tâches
    public const int WS_EX_NOACTIVATE = 0x08000000;  // ne vole pas le focus

    // ---------- SetWindowPos ----------

    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;

    // ---------- MonitorFromWindow ----------

    public const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;
    public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    public const int MONITORINFOF_PRIMARY = 0x00000001;

    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    // Per-monitor DPI (PerMonitorV2)
    public const int MDT_EFFECTIVE_DPI = 0;

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // ---------- Imports ----------

    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    // ---- DWM : fond translucide système (acrylique / mica), Windows 11 ----

    [DllImport("dwmapi.dll", SetLastError = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public const int DWMSBT_AUTO = 0;
    public const int DWMSBT_NONE = 1;
    public const int DWMSBT_MAINWINDOW = 2;       // Mica
    public const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylique (rendu de la barre des tâches)
    public const int DWMSBT_TABBEDWINDOW = 4;     // Mica Alt

    // ---- Capture d'écran ----

    public const uint WDA_NONE = 0x00000000;
    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
}
