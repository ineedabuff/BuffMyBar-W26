namespace BuffBar.Core;

/// <summary>Activation/désactivation de chaque widget.</summary>
public sealed class WidgetToggles
{
    public bool Weather { get; set; } = true;
    public bool Uptime { get; set; } = true;
    public bool Network { get; set; } = true;
    public bool Media { get; set; } = true;
    public bool Obs { get; set; } = true;
    public bool Visualizer { get; set; } = true;
    public bool Volume { get; set; } = true;
    public bool Bluetooth { get; set; } = true;
    public bool Battery { get; set; } = true;
}

/// <summary>Paramètres de connexion à obs-websocket.</summary>
public sealed class ObsConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 4455;
    public string Password { get; set; } = "";
}

/// <summary>
/// Intégration Google Agenda (lecture seule). Les identifiants OAuth « Desktop »
/// se créent dans Google Cloud Console ; les jetons sont stockés à part
/// (google_token.json), jamais dans settings.json.
/// </summary>
public sealed class GoogleCalendarConfig
{
    public bool Enabled { get; set; } = false;
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public int MaxEvents { get; set; } = 50;
}

/// <summary>
/// Configuration de BuffMyBar, sérialisée en JSON dans
/// %AppData%\BuffMyBar-W26\settings.json (façon Waybar, mais éditable aussi par
/// la fenêtre Paramètres).
/// </summary>
public sealed class Config
{
    /// <summary>Hauteur de la barre en DIP.</summary>
    public double Height { get; set; } = 36;

    /// <summary>Ville pour la météo (nom de ville Environnement Canada, ex. « Mascouche »).</summary>
    public string WeatherCity { get; set; } = "Terrebonne";

    /// <summary>Mode jeu : ajoute la latence (ping) au widget réseau.</summary>
    public bool GamingMode { get; set; } = false;

    /// <summary>Fond acrylique translucide (façon barre des tâches).</summary>
    public bool Acrylic { get; set; } = true;

    /// <summary>Inclut BuffBar dans les captures d'écran et le partage d'écran.</summary>
    public bool IncludeInScreenshots { get; set; } = false;

    /// <summary>Où afficher les indicateurs système : external | primary | all.</summary>
    public string SystemIndicatorsScope { get; set; } = "external";

    public WidgetToggles Widgets { get; set; } = new();
    public ObsConfig Obs { get; set; } = new();
    public GoogleCalendarConfig GoogleCalendar { get; set; } = new();
}
