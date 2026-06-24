using Microsoft.Win32;

namespace BuffBar.Services;

/// <summary>Mode de couleurs de la barre.</summary>
public enum ThemeMode
{
    /// <summary>Suit le thème de Windows (clair/sombre + accent), comme la barre des tâches.</summary>
    FollowWindows,
    /// <summary>Force la palette buff : fond #000000, accent #ddff24, texte blanc.</summary>
    Buff
}

/// <summary>
/// Réglages persistants de BuffBar, stockés sous HKCU\Software\BuffBar.
/// Natif (registre), aucune dépendance. Permet de retenir, d'un lancement à
/// l'autre, le choix fait par l'utilisateur (ex. le mode de couleurs).
/// </summary>
public static class SettingsService
{
    private const string KeyPath = @"Software\BuffBar";
    private const string ThemeModeValue = "ThemeMode";
    private const string ExternalAccentValue = "ExternalAccent";

    public static ThemeMode GetThemeMode(bool defaultFollow)
    {
        try
        {
            using RegistryKey? k = Registry.CurrentUser.OpenSubKey(KeyPath);
            string? v = k?.GetValue(ThemeModeValue) as string;
            if (string.Equals(v, "Buff", System.StringComparison.OrdinalIgnoreCase))
                return ThemeMode.Buff;
            if (string.Equals(v, "Windows", System.StringComparison.OrdinalIgnoreCase))
                return ThemeMode.FollowWindows;
        }
        catch { /* valeur absente ou illisible : on retombe sur le défaut */ }

        return defaultFollow ? ThemeMode.FollowWindows : ThemeMode.Buff;
    }

    public static void SetThemeMode(ThemeMode mode)
    {
        try
        {
            using RegistryKey k = Registry.CurrentUser.CreateSubKey(KeyPath);
            k.SetValue(ThemeModeValue, mode == ThemeMode.Buff ? "Buff" : "Windows",
                       RegistryValueKind.String);
        }
        catch { /* non bloquant : le réglage ne sera simplement pas mémorisé */ }
    }

    /// <summary>
    /// Mode « accent inversé » sur le moniteur externe : fond de barre #ddff24,
    /// fonds de widgets noirs, icônes et police en #ddff24.
    /// </summary>
    public static bool GetExternalAccent()
    {
        try
        {
            using RegistryKey? k = Registry.CurrentUser.OpenSubKey(KeyPath);
            return (k?.GetValue(ExternalAccentValue) as string) == "1";
        }
        catch { return false; }
    }

    public static void SetExternalAccent(bool on)
    {
        try
        {
            using RegistryKey k = Registry.CurrentUser.CreateSubKey(KeyPath);
            k.SetValue(ExternalAccentValue, on ? "1" : "0", RegistryValueKind.String);
        }
        catch { /* non bloquant */ }
    }
}
