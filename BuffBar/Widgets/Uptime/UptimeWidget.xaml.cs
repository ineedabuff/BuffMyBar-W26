using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BuffBar.Core;

namespace BuffBar.Widgets.Uptime;

/// <summary>
/// Module uptime : temps écoulé depuis le démarrage du PC
/// (Environment.TickCount64, horloge monotone). Format j / h / m.
/// </summary>
public partial class UptimeWidget : UserControl, IBarWidget
{
    private readonly DispatcherTimer _timer;

    public string WidgetId => "uptime";
    public FrameworkElement View => this;

    public UptimeWidget()
    {
        InitializeComponent();

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _timer.Tick += (_, _) => Refresh();

        Loaded += (_, _) => { Refresh(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Refresh()
    {
        TimeSpan up = TimeSpan.FromMilliseconds(Environment.TickCount64);

        Label.Text = up.TotalDays >= 1
            ? $"{(int)up.TotalDays}j {up.Hours}h {up.Minutes}m"
            : up.TotalHours >= 1
                ? $"{up.Hours}h {up.Minutes}m"
                : $"{up.Minutes}m";
    }
}
