using System;
using System.IO;

namespace BuffBar.Services;

/// <summary>
/// Journal de diagnostic simple, écrit dans
/// %LOCALAPPDATA%\BuffBar\buffbar.log (réinitialisé à chaque lancement).
/// Thread-safe, silencieux en cas d'échec d'écriture.
/// </summary>
public static class Logger
{
    private static readonly object Gate = new();

    /// <summary>Chemin complet du fichier journal.</summary>
    public static string LogPath { get; } = ResolvePath();

    private static string ResolvePath()
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BuffBar");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "buffbar.log");
            File.WriteAllText(path,
                $"=== BuffBar — {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
            return path;
        }
        catch
        {
            return string.Empty;
        }
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
