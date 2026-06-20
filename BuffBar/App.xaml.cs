using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using BuffBar.Services;

namespace BuffBar;

public partial class App : Application
{
    private static Mutex? _instanceMutex;
    private readonly List<MainWindow> _bars = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Instance unique : si BuffBar tourne déjà, on quitte silencieusement.
        _instanceMutex = new Mutex(initiallyOwned: true, "BuffBar_SingleInstance_Mutex", out bool isNew);
        if (!isNew)
        {
            Shutdown();
            return;
        }

        // Résout la police Nerd Font installée AVANT le chargement des fenêtres.
        Logger.Log($"App: démarrage. Journal : {Logger.LogPath}");
        FontService.Apply();

        base.OnStartup(e);

        // Démarrage automatique avec Windows (idempotent).
        if (BarConfig.EnableAutoStart)
            AutoStartService.Enable();

        CreateBars();
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
            var bar = new MainWindow(m.Handle);
            _bars.Add(bar);
            bar.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
