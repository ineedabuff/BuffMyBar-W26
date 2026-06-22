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

    /// <summary>Emplacement pour la météo (wttr.in). Code postal J7L 2M2.</summary>
    public const string WeatherLocation = "Mascouche, Québec";

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

    /// <summary>
    /// Adapte les couleurs de la barre au thème Windows 11 (clair/sombre + couleur
    /// d'accentuation sur la barre des tâches), avec mise à jour en direct.
    /// Mettre à false pour garder le thème noir fixe de BuffBar.
    /// </summary>
    public static readonly bool FollowWindowsTheme = true;

    /// <summary>
    /// Fond translucide « acrylique » natif (DWM), comme la barre des tâches Windows 11.
    /// Repli sûr : si le système ne le supporte pas, la barre reste opaque.
    /// Mettre à false pour un fond plein (noir buff ou couleur du thème).
    /// </summary>
    public static readonly bool UseAcrylicBackdrop = true;

    /// <summary>
    /// Conserve l'accent signature #ddff24 même quand <see cref="FollowWindowsTheme"/>
    /// est actif (au lieu d'adopter la couleur d'accentuation de Windows).
    /// Mettre à false pour coller à 100 % aux couleurs de Windows.
    /// </summary>
    public static readonly bool KeepBuffAccent = true;
}
