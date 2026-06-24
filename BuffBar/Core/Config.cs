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
/// Configuration de BuffMyBar, sérialisée en JSON dans
/// %AppData%\BuffMyBar-W26\settings.json (façon Waybar, mais éditable aussi par
/// la fenêtre Paramètres).
/// </summary>
public sealed class Config
{
    /// <summary>Nom du thème : buff | windows | cyber (fichier themes\&lt;nom&gt;.json).</summary>
    public string Theme { get; set; } = "buff";

    /// <summary>Hauteur de la barre en DIP.</summary>
    public double Height { get; set; } = 36;

    /// <summary>Ville pour la météo (wttr.in).</summary>
    public string WeatherCity { get; set; } = "Terrebonne";

    /// <summary>Mode jeu : ajoute la latence (ping) au widget réseau.</summary>
    public bool GamingMode { get; set; } = false;

    /// <summary>Accent inversé (#ddff24) sur le moniteur externe.</summary>
    public bool ExternalAccent { get; set; } = false;

    /// <summary>Fond acrylique translucide (façon barre des tâches).</summary>
    public bool Acrylic { get; set; } = true;

    public WidgetToggles Widgets { get; set; } = new();
    public ObsConfig Obs { get; set; } = new();
}
