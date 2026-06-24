using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar;

/// <summary>
/// Accès centralisé à la configuration. Les valeurs réglables par l'utilisateur
/// proviennent de <see cref="ConfigService.Current"/> (settings.json) ; quelques
/// constantes système restent figées.
/// </summary>
public static class BarConfig
{
    private static Config C => ConfigService.Current;

    // --- Réglables (settings.json / fenêtre Paramètres) ---
    public static double BarHeight => C.Height;
    public static string WeatherLocation => C.WeatherCity;
    public static bool GamingMode => C.GamingMode;
    public static bool UseAcrylicBackdrop => C.Acrylic;

    public static string ObsHost => C.Obs.Host;
    public static int ObsPort => C.Obs.Port;
    public static string ObsPassword => C.Obs.Password;

    // --- Constantes système ---
    /// <summary>Inscrit BuffMyBar au démarrage de Windows (HKCU\...\Run).</summary>
    public const bool EnableAutoStart = true;

    /// <summary>Maintient la barre au-dessus des applications plein écran (borderless).</summary>
    public static readonly bool KeepBarOnTop = true;

    /// <summary>Réduit les fenêtres plein écran (non exclusif) à la zone de travail.</summary>
    public static readonly bool ReclaimFullscreenWindows = true;

    /// <summary>
    /// En thème « windows », conserve l'accent #ddff24 au lieu de l'accent système.
    /// </summary>
    public static readonly bool KeepBuffAccent = true;
}
