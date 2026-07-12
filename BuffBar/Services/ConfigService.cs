using System;
using System.IO;
using System.Text.Json;
using BuffBar.Core;

namespace BuffBar.Services;

/// <summary>
/// Gère l'arborescence de configuration sous %AppData%\BuffMyBar-W26\ :
///
///   BuffMyBar-W26\
///   ├── settings.json
///   └── logs\
///
/// 100 % natif (System.Text.Json). Crée les valeurs/fichiers par défaut au besoin.
/// </summary>
public static class ConfigService
{
    public const string AppFolderName = "BuffMyBar-W26";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static Config Current { get; private set; } = new();

    /// <summary>Émis après un enregistrement (pour réappliquer la config en direct).</summary>
    public static event Action? Changed;

    public static string RootDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName);

    public static string LogsDir => Path.Combine(RootDir, "logs");
    public static string SettingsPath => Path.Combine(RootDir, "settings.json");

    /// <summary>À appeler une fois au démarrage, avant tout le reste.</summary>
    public static void Initialize()
    {
        try
        {
            Directory.CreateDirectory(RootDir);
            Directory.CreateDirectory(LogsDir);
            Current = Load();
        }
        catch
        {
            Current = new Config();   // repli : config par défaut en mémoire
        }
    }

    private static Config Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                Config? c = JsonSerializer.Deserialize<Config>(json, JsonOpts);
                if (c != null) return c;
            }
        }
        catch { /* fichier illisible : on régénère */ }

        var fresh = new Config();
        SaveToDisk(fresh);
        return fresh;
    }

    /// <summary>Enregistre la config, la rend courante et notifie les abonnés.</summary>
    public static void Save(Config config)
    {
        Current = config;
        SaveToDisk(config);
        Changed?.Invoke();
    }

    private static void SaveToDisk(Config config)
    {
        try
        {
            Directory.CreateDirectory(RootDir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(config, JsonOpts));
        }
        catch { /* non bloquant */ }
    }
}
