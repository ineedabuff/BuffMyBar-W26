using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        // Mode « accent inversé » sur le moniteur externe, si l'option est active.
        RefreshExternalAccent();
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

    // ---- Mode « accent inversé » sur le moniteur externe ----

    private static readonly string[] AccentKeys =
    {
        "BarBackground", "ModuleBackground", "ModuleBorderBrush",
        "HoverBackground", "HoverBorderBrush", "PrimaryText", "SubtleText", "AccentBrush"
    };

    /// <summary>
    /// Applique (ou retire) la surcharge de couleurs propre à cette fenêtre selon
    /// l'option « accent externe ». N'a d'effet que sur le moniteur externe.
    /// Surcharge locale (Window.Resources) : les autres barres ne sont pas touchées.
    /// </summary>
    public void RefreshExternalAccent()
    {
        bool on = _isExternal && ConfigService.Current.ExternalAccent;

        if (on)
        {
            // Sprint-006:
            // Moniteur externe en mode accent = barre pleine #ddff24.
            // Les modules se fondent dans la barre: aucun rectangle noir visible.
            // Tout le texte, les icones et les accents deviennent noirs.
            SetLocalBrush("BarBackground", 0xDD, 0xFF, 0x24);
            SetLocalBrush("ModuleBackground", 0xDD, 0xFF, 0x24);
            SetLocalBrush("ModuleBorderBrush", 0xDD, 0xFF, 0x24);
            SetLocalBrush("HoverBackground", 0xDD, 0xFF, 0x24);
            SetLocalBrush("HoverBorderBrush", 0xDD, 0xFF, 0x24);
            SetLocalBrush("PrimaryText", 0x00, 0x00, 0x00);
            SetLocalBrush("SubtleText", 0x00, 0x00, 0x00);
            SetLocalBrush("AccentBrush", 0x00, 0x00, 0x00);
        }
        else
        {
            foreach (string k in AccentKeys)
                Resources.Remove(k);
        }
    }

    private void SetLocalBrush(string key, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        Resources[key] = brush;
    }


    /// <summary>Réapplique les éléments dépendants du thème sur cette fenêtre.</summary>
    public void RefreshThemeSurface()
    {
        RefreshExternalAccent();
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

    // ---- Menu contextuel : Paramètres + sélecteur de couleurs ----

    private void OnContextOpened(object sender, RoutedEventArgs e) => SyncThemeChecks();

    private void OnSettings(object sender, RoutedEventArgs e) => OpenSettings();

    private void OnThemeFollow(object sender, RoutedEventArgs e)
    {
        ThemeService.SetTheme("windows");
        SyncThemeChecks();
    }

    private void OnThemeBuff(object sender, RoutedEventArgs e)
    {
        ThemeService.SetTheme("buff");
        SyncThemeChecks();
    }

    private void OnThemeCyber(object sender, RoutedEventArgs e)
    {
        ThemeService.SetTheme("cyber");
        SyncThemeChecks();
    }

    private void OnExternalAccent(object sender, RoutedEventArgs e)
    {
        Config c = ConfigService.Current;
        c.ExternalAccent = !c.ExternalAccent;
        ConfigService.Save(c);
        (Application.Current as App)?.RefreshExternalAccentAll();
        SyncThemeChecks();
    }

    private void SyncThemeChecks()
    {
        string theme = ConfigService.Current.Theme;
        ThemeFollow.IsChecked = theme == "windows";
        ThemeBuff.IsChecked = theme == "buff";
        ThemeCyber.IsChecked = theme == "cyber";
        ExternalAccent.IsChecked = ConfigService.Current.ExternalAccent;
    }
}
