using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using BuffBar.Services;
using static BuffBar.Interop.NativeMethods;

namespace BuffBar.Interop;

/// <summary>
/// Transforme une fenêtre WPF en AppBar Windows native.
///
/// Responsabilités :
///   - Inscrire la fenêtre (ABM_NEW) et réserver l'espace écran (ABM_QUERYPOS / ABM_SETPOS)
///     pour que les fenêtres maximisées ne recouvrent jamais la barre.
///   - Gérer le DPI (PerMonitorV2) : conversion DIP -> pixels physiques.
///   - Réagir aux notifications du shell (repositionnement, app plein écran).
///   - Se désinscrire proprement (ABM_REMOVE) à la fermeture.
///
/// Multi-écrans : cette instance se cale sur l'écran cible (par défaut l'écran
/// principal). Pour une barre par écran, instanciez une fenêtre + un AppBarManager
/// par moniteur (voir README).
/// </summary>
public sealed class AppBarManager
{
    private readonly Window _window;
    private IntPtr _hwnd;
    private HwndSource? _source;
    private uint _callbackId;
    private bool _registered;
    private DispatcherTimer? _guard;

    /// <summary>Hauteur logique (DIP) de la barre. Convertie en pixels physiques selon le DPI.</summary>
    public double BarHeightLogical { get; set; } = 48.0;

    /// <summary>Moniteur cible (HMONITOR). IntPtr.Zero = écran principal.</summary>
    public IntPtr TargetMonitor { get; set; } = IntPtr.Zero;

    public AppBarManager(Window window) => _window = window;

    /// <summary>À appeler depuis Window.OnSourceInitialized (le HWND existe alors).</summary>
    public void Initialize()
    {
        _hwnd = new WindowInteropHelper(_window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);

        MakeToolWindow();
        ApplyScreenCaptureMode();
        Register();
        UpdatePosition();
        StartGuard();
    }

    private void StartGuard()
    {
        if (!BarConfig.KeepBarOnTop && !BarConfig.ReclaimFullscreenWindows)
            return;

        _guard = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };
        _guard.Tick += (_, _) => Guard();
        _guard.Start();
    }

    /// <summary>Désinscription de l'AppBar : libère l'espace réservé.</summary>
    public void Remove()
    {
        _guard?.Stop();
        _guard = null;

        if (!_registered) return;

        var abd = NewData();
        SHAppBarMessage(ABM_REMOVE, ref abd);
        _registered = false;

        _source?.RemoveHook(WndProc);
    }

    // ---------------------------------------------------------------

    private APPBARDATA NewData() => new()
    {
        cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
        hWnd = _hwnd
    };

    private void MakeToolWindow()
    {
        int ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
        ex |= WS_EX_TOOLWINDOW;   // hors Alt+Tab et barre des tâches
        SetWindowLong(_hwnd, GWL_EXSTYLE, ex);
    }

    private void ApplyScreenCaptureMode()
    {
        uint affinity = ConfigService.Current.IncludeInScreenshots
            ? WDA_NONE
            : WDA_EXCLUDEFROMCAPTURE;

        // Contrôle si BuffBar apparaît dans l'outil Capture, Teams, OBS, etc.
        // Sur les versions de Windows qui ne supportent pas ce mode, l'appel échoue sans effet.
        SetWindowDisplayAffinity(_hwnd, affinity);
    }

    private void Register()
    {
        // Message de rappel privé : le shell nous notifie via ce message.
        _callbackId = (uint)RegisterWindowMessage("BuffBar_AppBarCallback");

        var abd = NewData();
        abd.uCallbackMessage = _callbackId;
        SHAppBarMessage(ABM_NEW, ref abd);
        _registered = true;
    }

    private void UpdatePosition()
    {
        if (!_registered) return;

        // Moniteur cible explicite, sinon écran principal.
        IntPtr hMon = TargetMonitor != IntPtr.Zero
            ? TargetMonitor
            : MonitorFromWindow(_hwnd, MONITOR_DEFAULTTOPRIMARY);

        var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
        if (!GetMonitorInfo(hMon, ref mi))
            return;

        double scaleY = GetMonitorScaleY(hMon);
        int heightPx = (int)Math.Round(BarHeightLogical * scaleY);

        var abd = NewData();
        abd.uEdge = ABE_TOP;
        abd.rc.left = mi.rcMonitor.left;
        abd.rc.right = mi.rcMonitor.right;
        abd.rc.top = mi.rcMonitor.top;
        abd.rc.bottom = mi.rcMonitor.top + heightPx;

        // Le shell peut proposer un ajustement.
        SHAppBarMessage(ABM_QUERYPOS, ref abd);

        // Bord supérieur : on verrouille la hauteur après l'ajustement.
        abd.rc.bottom = abd.rc.top + heightPx;
        SHAppBarMessage(ABM_SETPOS, ref abd);

        // On place réellement la fenêtre dans le rectangle accordé (pixels physiques).
        SetWindowPos(_hwnd, HWND_TOPMOST,
            abd.rc.left, abd.rc.top,
            abd.rc.right - abd.rc.left,
            abd.rc.bottom - abd.rc.top,
            SWP_NOACTIVATE);
    }

    private static double GetMonitorScaleY(IntPtr hMon)
    {
        try
        {
            if (GetDpiForMonitor(hMon, MDT_EFFECTIVE_DPI, out _, out uint dpiY) == 0 && dpiY > 0)
                return dpiY / 96.0;
        }
        catch { /* shcore indisponible -> 100 % */ }
        return 1.0;
    }

    // ---- Gardien : persistance face au plein écran ----

    private IntPtr ResolveMonitor()
        => TargetMonitor != IntPtr.Zero
            ? TargetMonitor
            : MonitorFromWindow(_hwnd, MONITOR_DEFAULTTOPRIMARY);

    private void Guard()
    {
        IntPtr fg = GetForegroundWindow();
        bool captureActive = IsScreenCaptureHost(fg);

        if (BarConfig.KeepBarOnTop && !captureActive)
            SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

        if (BarConfig.ReclaimFullscreenWindows && !captureActive)
            ReclaimForeground(fg);
    }

    private void ReclaimForeground(IntPtr fg)
    {
        if (fg == IntPtr.Zero || fg == _hwnd)
            return;

        IntPtr hMon = ResolveMonitor();

        // Ne traiter que les fenêtres situées sur NOTRE moniteur.
        if (MonitorFromWindow(fg, MONITOR_DEFAULTTONEAREST) != hMon)
            return;

        var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
        if (!GetMonitorInfo(hMon, ref mi) || !GetWindowRect(fg, out RECT wr))
            return;

        RECT mon = mi.rcMonitor;
        RECT work = mi.rcWork;

        // La fenêtre couvre-t-elle tout le moniteur (borderless / fenêtré plein écran) ?
        bool coversMonitor =
            wr.left <= mon.left + 1 && wr.top <= mon.top + 1 &&
            wr.right >= mon.right - 1 && wr.bottom >= mon.bottom - 1;

        // On agit uniquement si elle empiète sur la zone réservée de la barre.
        if (coversMonitor && wr.top < work.top)
        {
            SetWindowPos(fg, IntPtr.Zero,
                work.left, work.top,
                work.right - work.left,
                work.bottom - work.top,
                SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }

    private static bool IsScreenCaptureHost(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0)
            return false;

        try
        {
            string name = Process.GetProcessById((int)pid).ProcessName;
            return name.Equals("ScreenClippingHost", StringComparison.OrdinalIgnoreCase)
                || name.Equals("SnippingTool", StringComparison.OrdinalIgnoreCase)
                || name.Equals("SnipAndSketch", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == (int)_callbackId)
        {
            switch (wParam.ToInt32())
            {
                case ABN_POSCHANGED:
                    // Résolution changée, autre barre déplacée, etc. -> on se recale.
                    UpdatePosition();
                    handled = true;
                    break;

                case ABN_FULLSCREENAPP:
                    // Par défaut on RESTE au-dessus (barre inviolable). Comportement
                    // "barre des tâches" (céder le dessus) seulement si KeepBarOnTop=false.
                    if (!BarConfig.KeepBarOnTop)
                        _window.Topmost = lParam == IntPtr.Zero;
                    handled = true;
                    break;
            }
        }
        return IntPtr.Zero;
    }
}
