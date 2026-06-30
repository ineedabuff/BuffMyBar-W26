using System;
using System.Windows;
using System.Windows.Interop;
using BuffBar.Interop;

namespace BuffBar.Services;

/// <summary>
/// Integre la fenetre avec le compositeur Windows 11.
/// Contrairement a l'ancien comportement, ce service ne force plus les ressources WPF
/// en transparent : ThemeService garde le controle des couleurs visibles de la barre.
/// </summary>
public static class BackdropService
{
    public static bool TryApply(Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return false;

        Refresh(window);

        if (!BarConfig.UseAcrylicBackdrop)
            return false;

        try
        {
            int backdrop = NativeMethods.DWMSBT_TRANSIENTWINDOW;
            int hr = NativeMethods.DwmSetWindowAttribute(
                hwnd,
                NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE,
                ref backdrop,
                sizeof(int));

            return hr == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void Refresh(Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        try
        {
            int dark = WindowsThemeService.Current.SystemLight ? 0 : 1;
            NativeMethods.DwmSetWindowAttribute(
                hwnd,
                NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref dark,
                sizeof(int));
        }
        catch
        {
            // Non bloquant : la barre garde ses couleurs WPF.
        }
    }
}
