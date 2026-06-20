namespace BuffBar;

/// <summary>
/// Configuration centrale de la barre.
/// Point unique pour ajuster hauteur, démarrage auto, etc.
/// (Sera remplacé par un fichier JSON/TOML chargé au runtime dans une version ultérieure.)
/// </summary>
public static class BarConfig
{
    /// <summary>Hauteur logique de la barre en DIP. 48 = hauteur visuelle de la barre des tâches Windows 11.</summary>
    public const double BarHeight = 48.0;

    /// <summary>Inscrit BuffBar au démarrage de Windows (HKCU\...\Run).</summary>
    public const bool EnableAutoStart = true;

    /// <summary>Emplacement pour la météo (wttr.in).</summary>
    public const string WeatherLocation = "Montreal";

    // --- obs-websocket (OBS : Outils > Paramètres du serveur WebSocket) ---
    public const string ObsHost = "127.0.0.1";
    public const int ObsPort = 4455;
    public const string ObsPassword = "";  // laisse vide si l'authentification est désactivée

    // --- Persistance face au plein écran ---
    /// <summary>Maintient la barre au-dessus des applications plein écran (borderless).</summary>
    public static readonly bool KeepBarOnTop = true;

    /// <summary>
    /// Réduit activement les fenêtres qui couvrent tout l'écran (borderless / fenêtré
    /// plein écran) à la zone de travail, sous la barre. NB : le plein écran EXCLUSIF
    /// ne peut pas être géré (limite Windows) — utiliser le mode borderless dans les jeux.
    /// Mettre à false si un jeu se redimensionne en boucle.
    /// </summary>
    public static readonly bool ReclaimFullscreenWindows = true;
}
