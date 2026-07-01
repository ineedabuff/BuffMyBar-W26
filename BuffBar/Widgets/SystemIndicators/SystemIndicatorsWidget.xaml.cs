using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.SystemIndicators;

/// <summary>
/// Compact CPU / RAM / GPU indicators.
/// Values that cannot be detected are hidden automatically.
/// </summary>
public partial class SystemIndicatorsWidget : UserControl, IBarWidget
{
    private readonly SystemMetricsService _metrics = new();
    private readonly DispatcherTimer _timer;

    public string WidgetId => "system-indicators";
    public FrameworkElement View => this;

    public SystemIndicatorsWidget()
    {
        InitializeComponent();

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += (_, _) => Refresh();

        Loaded += (_, _) =>
        {
            Refresh();
            _timer.Start();
        };

        Unloaded += (_, _) => _timer.Stop();
    }

    private void Refresh()
    {
        SystemMetricsSnapshot snapshot = _metrics.Read();

        bool cpuVisible = ApplyMetric(CpuLabel, "CPU", snapshot.CpuPercent);
        bool ramVisible = ApplyMetric(RamLabel, "RAM", snapshot.RamPercent);
        bool gpuVisible = ApplyMetric(GpuLabel, "GPU", snapshot.GpuPercent);

        Root.Visibility = (cpuVisible || ramVisible || gpuVisible)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static bool ApplyMetric(TextBlock label, string name, int? value)
    {
        if (value is not { } percent)
        {
            label.Visibility = Visibility.Collapsed;
            return false;
        }

        label.Visibility = Visibility.Visible;
        WidgetAnimator.SetText(label, $"{name} {percent}%");
        return true;
    }
}
