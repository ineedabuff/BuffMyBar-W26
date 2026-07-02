using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace BuffBar.Services;

/// <summary>
/// Gère le démarrage automatique de BuffBar avec Windows.
///
/// Deux mécanismes possibles :
///   - clé HKCU\...\Run : portée utilisateur, non élevée (par défaut) ;
///   - tâche planifiée « BuffBar » avec privilèges les plus élevés : lancement
///     ADMINISTRATEUR sans invite UAC (installée par install-buffbar-temp.ps1,
///     nécessaire pour lire la température CPU via LibreHardwareMonitor).
///
/// Si la tâche planifiée existe, c'est elle qui fait foi et l'entrée Run est
/// retirée : sinon deux instances se disputeraient le mutex au démarrage et la
/// version non élevée pourrait gagner (donc pas de température).
/// Opérations idempotentes.
/// </summary>
public static class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "BuffBar";

    /// <summary>Nom de la tâche planifiée « démarrage élevé » (voir install-buffbar-temp.ps1).</summary>
    private const string ScheduledTaskName = "BuffBar";

    public static void Enable()
    {
        // La tâche élevée a priorité : elle lance la barre en administrateur.
        // On enlève alors l'entrée Run (non élevée) pour éviter la course au mutex.
        if (ScheduledTaskExists())
        {
            Disable();
            return;
        }

        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
            return;

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                                 ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key?.SetValue(ValueName, $"\"{exePath}\"");
    }

    public static void Disable()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key?.GetValue(ValueName) is not null)
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public static bool IsEnabled()
    {
        if (ScheduledTaskExists())
            return true;

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is not null;
    }

    /// <summary>
    /// Vrai si la tâche planifiée élevée « BuffBar » est enregistrée. Interroge
    /// schtasks.exe (natif, aucune dépendance). En cas de doute, renvoie false
    /// pour conserver le comportement historique (clé Run).
    /// </summary>
    private static bool ScheduledTaskExists()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/query /tn \"{ScheduledTaskName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process? p = Process.Start(psi);
            if (p is null)
                return false;

            if (!p.WaitForExit(1500))
            {
                try { p.Kill(); } catch { /* meilleur effort */ }
                return false;
            }

            // schtasks /query renvoie 0 si la tâche existe, non-zéro sinon.
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
