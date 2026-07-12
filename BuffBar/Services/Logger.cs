using System;
using System.IO;

namespace BuffBar.Services;

/// <summary>
/// Journal de diagnostic simple, écrit dans
/// %AppData%\BuffMyBar-W26\logs\buffbar.log (réinitialisé à chaque lancement).
/// Thread-safe, silencieux en cas d'échec d'écriture.
/// </summary>
public static class Logger
{
    private static readonly object Gate = new();

    /// <summary>Chemin complet du fichier journal.</summary>
    public static string LogPath { get; } = ResolvePath();

    /// <summary>
    /// Active les traces de diagnostic à haute fréquence (<see cref="Verbose"/>).
    /// Désactivé par défaut : en usage normal, la capture audio n'écrit plus sur
    /// disque à chaque seconde. À activer pour un rapport de bug (voir App).
    /// </summary>
    public static bool VerboseEnabled { get; set; }

    private static string ResolvePath()
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BuffMyBar-W26", "logs");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "buffbar.log");
            File.WriteAllText(path,
                $"=== BuffMyBar — {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
            return path;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Trace à haute fréquence (boucles de capture, etc.). Ignorée tant que
    /// <see cref="VerboseEnabled"/> est faux, pour éviter d'écrire sur disque en
    /// continu pendant le fonctionnement normal.
    /// </summary>
    public static void Verbose(string message)
    {
        if (!VerboseEnabled) return;
        Log(message);
    }

    public static void Log(string message)
    {
        if (string.IsNullOrEmpty(LogPath)) return;
        try
        {
            lock (Gate)
                File.AppendAllText(LogPath,
                    $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }
}
