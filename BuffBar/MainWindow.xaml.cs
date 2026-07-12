using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using BuffBar.Interop;
using BuffBar.Services;
using BuffBar.Core;
using BuffBar.Widgets.Battery;
using BuffBar.Widgets.Bluetooth;
using BuffBar.Widgets.Clock;
using BuffBar.Widgets.Media;
using BuffBar.Widgets.Network;
using BuffBar.Widgets.Obs;
using BuffBar.Widgets.SystemIndicators;
using BuffBar.Widgets.Uptime;
using BuffBar.Widgets.Visualizer;
using BuffBar.Widgets.Volume;
using BuffBar.Widgets.Weather;
using static BuffBar.Interop.NativeMethods;

namespace BuffBar;

public partial class MainWindow : Window
{
    private AppBarManager? _appBar;
    private readonly IntPtr _targetMonitor;
    private readonly bool _isExternal;

    public MainWindow() : this(IntPtr.Zero, false) { }

    public MainWindow(IntPtr targetMonitor) : this(targetMonitor, false) { }

    public MainWindow(IntPtr targetMonitor, bool isExternal)
    {
        _targetMonitor = targetMonitor;
        _isExternal = isExternal;
        InitializeComponent();

        // Dimensions provisoires : l'AppBarManager les écrasera en pixels physiques.
        Width = 800;
        Height = BarConfig.BarHeight;
        PositionHint();

        ComposeWidgets();
    }

    // Place approximativement la fenêtre sur le moniteur cible avant l'affichage
    // (l'AppBarManager finalisera la position exacte en pixels physiques).
    private void PositionHint()
    {
        Left = 0;
        Top = 0;
        if (_targetMonitor == IntPtr.Zero) return;

        var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
        if (GetMonitorInfo(_targetMonitor, ref mi))
        {
            Left = mi.rcMonitor.left;
            Top = mi.rcMonitor.top;
        }
    }

    private void ComposeWidgets()
    {
        WidgetToggles w = ConfigService.Current.Widgets;

        // Centre — heure (toujours), puis OBS si activé (le couple reste centré)
        var center = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        center.Children.Add(new ClockWidget());
        if (w.Obs) center.Children.Add(new ObsWidget());
        CenterRegion.Content = center;

        // Gauche — modules à largeur naturelle ; le Média (si activé) remplit le reste.
        if (w.Weather) AddDockedLeft(new WeatherWidget());
        if (w.Uptime) AddDockedLeft(new UptimeWidget());
        if (w.Network) AddDockedLeft(new NetworkWidget());

        LeftRegion.LastChildFill = w.Media;
        if (w.Media) LeftRegion.Children.Add(new MediaWidget());

        // Droite — alignée à droite (ajout = gauche -> droite)
        RightRegion.Children.Add(new SystemIndicatorsWidget(_isExternal));
        if (w.Visualizer) RightRegion.Children.Add(new VisualizerWidget());
        if (w.Volume) RightRegion.Children.Add(new VolumeWidget());
        if (w.Bluetooth) RightRegion.Children.Add(new BluetoothWidget());
        if (w.Battery) RightRegion.Children.Add(new BatteryWidget());
    }

    private void AddDockedLeft(FrameworkElement widget)
    {
        DockPanel.SetDock(widget, Dock.Left);
        LeftRegion.Children.Add(widget);
    }

    private void OpenSettings()
    {
        var win = new SettingsWindow { Owner = this };
        win.ShowDialog();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _appBar = new AppBarManager(this)
        {
            BarHeightLogical = BarConfig.BarHeight,
            TargetMonitor = _targetMonitor
        };
        _appBar.Initialize();

        // Fond translucide « acrylique » comme la barre des tâches (repli sûr si non supporté).
        BackdropService.TryApply(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        _appBar?.Remove();
        base.OnClosed(e);
    }

    private void OnReload(object sender, RoutedEventArgs e)
    {
        // Force un recalcul de la position (utile après changement d'écran/résolution).
        _appBar?.Remove();
        _appBar = new AppBarManager(this)
        {
            BarHeightLogical = BarConfig.BarHeight,
            TargetMonitor = _targetMonitor
        };
        _appBar.Initialize();
    }

    /// <summary>Réapplique les éléments dépendants du thème sur cette fenêtre.</summary>
    public void RefreshThemeSurface()
    {
        BackdropService.Refresh(this);
    }

    private void OnRestart(object sender, RoutedEventArgs e)
    {
        // Différé : laisse l'événement du menu se terminer avant de fermer/recréer
        // les fenêtres (celle-ci comprise).
        Dispatcher.BeginInvoke(new Action(() => (Application.Current as App)?.RestartBars()));
    }

    private void OnQuit(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    // ---- Menu contextuel ----

    private void OnSettings(object sender, RoutedEventArgs e) => OpenSettings();
}
