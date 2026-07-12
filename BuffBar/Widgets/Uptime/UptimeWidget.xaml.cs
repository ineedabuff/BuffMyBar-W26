using System;
using System.Windows;
using System.Windows.Controls;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Uptime;

/// <summary>
/// Module uptime : temps écoulé depuis le démarrage du PC
/// (Environment.TickCount64, horloge monotone). Format j / h / m.
/// </summary>
public partial class UptimeWidget : UserControl, IBarWidget
{
    private IDisposable? _tick;

    public string WidgetId => "uptime";
    public FrameworkElement View => this;

    public UptimeWidget()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            Refresh();
            _tick?.Dispose();
            _tick = WidgetScheduler.Subscribe(TimeSpan.FromSeconds(30), Refresh);
        };
        Unloaded += (_, _) => { _tick?.Dispose(); _tick = null; };
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
