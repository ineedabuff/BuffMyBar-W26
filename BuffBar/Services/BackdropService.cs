using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using BuffBar.Interop;

namespace BuffBar.Services;

/// <summary>
/// Applique un fond translucide « acrylique » natif (DWM, Windows 11) afin que
/// BuffBar adopte le même rendu translucide que la barre des tâches.
///
/// Zéro dépendance : un seul appel à DwmSetWindowAttribute. Repli sûr — si le
/// système ne supporte pas l'attribut (Windows 10, anciennes builds 11), la
/// fenêtre reste opaque avec le fond du thème.
/// </summary>
public static class BackdropService
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>
    /// Tente d'activer l'acrylique sur la fenêtre. Retourne true si réussi.
    /// À appeler après que le HWND existe (OnSourceInitialized).
    /// </summary>
    public static bool TryApply(Window window)
    {
        if (!BarConfig.UseAcrylicBackdrop)
            return false;

        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return false;

        try
        {
            // Acrylique translucide (comme la barre des tâches).
            int backdrop = NativeMethods.DWMSBT_TRANSIENTWINDOW;
            int hr = NativeMethods.DwmSetWindowAttribute(
                hwnd, NativeMethods.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));

            // S_OK = 0. Tout autre code = attribut non supporté -> repli opaque.
            if (hr != 0)
                return false;

            // Indice de mode sombre pour le compositeur DWM.
            int dark = ThemeIsDark() ? 1 : 0;
            NativeMethods.DwmSetWindowAttribute(
                hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

            // Rendre la cible de rendu WPF transparente pour laisser passer l'acrylique.
            HwndSource? src = HwndSource.FromHwnd(hwnd);
            if (src?.CompositionTarget != null)
                src.CompositionTarget.BackgroundColor = Colors.Transparent;

            // Le thème passe alors les fonds Bar/Module en transparent
            // et le survol en translucide.
            ThemeService.EnableAcrylic();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ThemeIsDark()
    {
        try
        {
            using RegistryKey? k = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return (k?.GetValue("SystemUsesLightTheme") as int? ?? 0) == 0;
        }
        catch
        {
            return true;
        }
    }
}
