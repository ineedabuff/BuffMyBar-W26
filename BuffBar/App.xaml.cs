using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using BuffBar.Services;

namespace BuffBar;

public partial class App : Application
{
    private static Mutex? _instanceMutex;
    private readonly List<MainWindow> _bars = new();
    private bool _restarting;
    private DispatcherTimer? _displayDebounce;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Instance unique : si BuffBar tourne déjà, on quitte silencieusement.
        _instanceMutex = new Mutex(initiallyOwned: true, "BuffBar_SingleInstance_Mutex", out bool isNew);
        if (!isNew)
        {
            Shutdown();
            return;
        }

        // Charge la configuration (settings.json) AVANT tout le reste.
        ConfigService.Initialize();

        // Journalisation détaillée : coupée par défaut (pas d'écriture disque en
        // continu). Activable pour un rapport de bug via BUFFBAR_VERBOSE=1, et
        // toujours active dans les builds Debug.
        Logger.VerboseEnabled =
            Environment.GetEnvironmentVariable("BUFFBAR_VERBOSE") == "1";
#if DEBUG
        Logger.VerboseEnabled = true;
#endif

        // Résout la police Nerd Font installée AVANT le chargement des fenêtres.
        Logger.Log($"App: démarrage. Journal : {Logger.LogPath}");
        FontService.Apply();
        ThemeService.Start();
        ThemeService.Applied += OnThemeApplied;

        base.OnStartup(e);

        // Démarrage automatique avec Windows (idempotent).
        if (BarConfig.EnableAutoStart)
            AutoStartService.Enable();

        CreateBars();

        // Auto-récupération : reconstruire les barres après un changement d'affichage
        // (réveil de veille, moniteur éteint/rallumé, changement de résolution).
        // Anti-rebond pour absorber les rafales d'événements.
        _displayDebounce = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(1200)
        };
        _displayDebounce.Tick += (_, _) => { _displayDebounce!.Stop(); RestartBars(); };

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        // Vérifie les mises à jour en arrière-plan (best-effort, non bloquant).
        _ = UpdateService.CheckAsync();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) => ScheduleRestart();

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) ScheduleRestart();
    }

    // Marshalle vers le thread UI puis (ré)arme l'anti-rebond.
    private void ScheduleRestart()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _displayDebounce?.Stop();
            _displayDebounce?.Start();
        }));
    }

    /// <summary>
    /// Ferme toutes les barres et les recrée pour les moniteurs actuellement
    /// connectés. Corrige les bugs d'AppBar après veille / extinction d'un écran.
    /// </summary>
    public void RestartBars()
    {
        if (_restarting) return;
        _restarting = true;
        try
        {
            Logger.Log("App: redémarrage des barres.");
            foreach (MainWindow bar in _bars)
            {
                try { bar.Close(); } catch { /* on poursuit malgré tout */ }
            }
            _bars.Clear();
            CreateBars();
        }
        catch (Exception ex)
        {
            Logger.Log($"App: échec du redémarrage des barres : {ex.Message}");
        }
        finally
        {
            _restarting = false;
        }
    }

    // Une barre persistante (AppBar) par moniteur connecté.
    private void CreateBars()
    {
        List<MonitorEntry> monitors = MonitorService.Enumerate();
        if (monitors.Count == 0)
            monitors.Add(new MonitorEntry(IntPtr.Zero, true));

        Logger.Log($"App: {monitors.Count} moniteur(s) détecté(s).");

        foreach (MonitorEntry m in monitors)
        {
            // « Externe » = moniteur non principal (sur un portable, l'écran intégré
            // est le principal). C'est là que s'applique le mode accent externe.
            var bar = new MainWindow(m.Handle, isExternal: !m.IsPrimary);
            _bars.Add(bar);
            bar.Show();
        }
    }


    private void OnThemeApplied()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            foreach (MainWindow bar in _bars)
                bar.RefreshThemeSurface();
        }));
    }

    /// <summary>Réapplique le thème puis reconstruit les barres (après changement de config).</summary>
    public void ApplyConfigAndRestart()
    {
        ThemeService.Apply();
        RestartBars();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        ThemeService.Applied -= OnThemeApplied;
        ThemeService.Stop();
        _displayDebounce?.Stop();

        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
