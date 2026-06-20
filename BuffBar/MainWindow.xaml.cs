using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using BuffBar.Interop;
using BuffBar.Widgets.Battery;
using BuffBar.Widgets.Bluetooth;
using BuffBar.Widgets.Clock;
using BuffBar.Widgets.Media;
using BuffBar.Widgets.Network;
using BuffBar.Widgets.Obs;
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

    public MainWindow() : this(IntPtr.Zero) { }

    public MainWindow(IntPtr targetMonitor)
    {
        _targetMonitor = targetMonitor;
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
        // Centre — heure, puis OBS juste à droite (le couple reste centré)
        var center = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        center.Children.Add(new ClockWidget());
        center.Children.Add(new ObsWidget());
        CenterRegion.Content = center;

        // Gauche — [Météo] [Uptime] [Réseau] [Média]
        LeftRegion.Children.Add(new WeatherWidget());
        LeftRegion.Children.Add(new UptimeWidget());
        LeftRegion.Children.Add(new NetworkWidget());
        LeftRegion.Children.Add(new MediaWidget());

        // Droite — [Visualiseur] [Volume] [Bluetooth] [Batterie]
        // (StackPanel horizontal aligné à droite : ajout = gauche -> droite)
        RightRegion.Children.Add(new VisualizerWidget());
        RightRegion.Children.Add(new VolumeWidget());
        RightRegion.Children.Add(new BluetoothWidget());
        RightRegion.Children.Add(new BatteryWidget());
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

    private void OnQuit(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
