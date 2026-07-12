using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;
using BuffBar.Widgets.Common;

namespace BuffBar.Widgets.SystemIndicators;

/// <summary>
/// Compact CPU / RAM / GPU indicators.
/// Values that cannot be detected are hidden automatically.
/// Visibility per monitor is decided by the caller (settings scope:
/// external / primary / all).
/// </summary>
public partial class SystemIndicatorsWidget : UserControl, IBarWidget
{
    private static readonly Brush AlertBrush = Frozen(0xFF, 0x31, 0x31);

    private readonly SystemMetricsService _metrics = new();
    private IDisposable? _tick;
    private readonly DispatcherTimer _blinkTimer;
    private readonly bool _showOnThisMonitor;
    private readonly List<TextBlock> _criticalLabels = new();

    private bool _blinkVisible = true;

    public string WidgetId => "system-indicators";
    public FrameworkElement View => this;

    public SystemIndicatorsWidget()
        : this(false)
    {
    }

    public SystemIndicatorsWidget(bool showOnThisMonitor)
    {
        _showOnThisMonitor = showOnThisMonitor;

        InitializeComponent();

        _blinkTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(650)
        };
        _blinkTimer.Tick += (_, _) => ToggleCriticalBlink();

        // Applet détaillée au survol (seulement là où le module est affiché).
        HoverPopup.Attach(
            Root, Flyout, Monitor,
            onOpening: () => Monitor.Update(_metrics.Read()),
            canOpen: () => _showOnThisMonitor);

        Loaded += (_, _) =>
        {
            if (!_showOnThisMonitor)
            {
                Root.Visibility = Visibility.Collapsed;
                return;
            }

            Refresh();
            _tick?.Dispose();
            _tick = WidgetScheduler.Subscribe(TimeSpan.FromSeconds(2), Refresh);
        };

        Unloaded += (_, _) =>
        {
            _tick?.Dispose();
            _tick = null;
            _blinkTimer.Stop();
            _metrics.Dispose();
        };
    }

    private void Refresh()
    {
        if (!_showOnThisMonitor)
        {
            Root.Visibility = Visibility.Collapsed;
            return;
        }

        SystemMetricsSnapshot snapshot = _metrics.Read();

        _criticalLabels.Clear();

        bool cpuVisible = ApplyMetric(
            CpuLabel,
            snapshot.CpuPercent,
            snapshot.CpuTemperatureCelsius is { } temp
                ? $"CPU {snapshot.CpuPercent}% @ {temp}°C"
                : snapshot.CpuPercent is { } cpu
                    ? $"CPU {cpu}%"
                    : null);

        bool ramVisible = ApplyMetric(
            RamLabel,
            snapshot.RamPercent,
            snapshot.RamPercent is { } ram ? $"RAM {ram}%" : null);

        bool gpuVisible = ApplyMetric(
            GpuLabel,
            snapshot.GpuPercent,
            snapshot.GpuPercent is { } gpu ? $"GPU {gpu}%" : null);

        Root.Visibility = (cpuVisible || ramVisible || gpuVisible)
            ? Visibility.Visible
            : Visibility.Collapsed;

        UpdateBlinkState();

        // Met à jour l'applet en direct tant qu'elle est ouverte.
        if (Flyout.IsOpen)
            Monitor.Update(snapshot);
    }

    private bool ApplyMetric(TextBlock label, int? percent, string? text)
    {
        if (percent is not { } value || string.IsNullOrWhiteSpace(text))
        {
            label.Visibility = Visibility.Collapsed;
            label.Opacity = 1.0;
            return false;
        }

        label.Visibility = Visibility.Visible;
        WidgetAnimator.SetText(label, text);

        if (value >= 80)
        {
            label.Foreground = AlertBrush;

            if (value > 90)
                _criticalLabels.Add(label);
        }
        else
        {
            label.Foreground = (Brush)FindResource("PrimaryText");
            label.Opacity = 1.0;
        }

        return true;
    }

    private void UpdateBlinkState()
    {
        if (_criticalLabels.Count == 0)
        {
            _blinkTimer.Stop();
            _blinkVisible = true;
            CpuLabel.Opacity = 1.0;
            RamLabel.Opacity = 1.0;
            GpuLabel.Opacity = 1.0;
            return;
        }

        if (!_blinkTimer.IsEnabled)
        {
            _blinkVisible = true;
            _blinkTimer.Start();
        }

        ApplyBlinkOpacity();
    }

    private void ToggleCriticalBlink()
    {
        _blinkVisible = !_blinkVisible;
        ApplyBlinkOpacity();
    }

    private void ApplyBlinkOpacity()
    {
        CpuLabel.Opacity = 1.0;
        RamLabel.Opacity = 1.0;
        GpuLabel.Opacity = 1.0;

        double opacity = _blinkVisible ? 1.0 : 0.35;

        foreach (TextBlock label in _criticalLabels)
            label.Opacity = opacity;
    }

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
