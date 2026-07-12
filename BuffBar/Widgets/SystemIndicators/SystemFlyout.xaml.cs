using System;
using System.Windows;
using System.Windows.Controls;
using BuffBar.Services;

namespace BuffBar.Widgets.SystemIndicators;

/// <summary>
/// Applet « Système » (au survol des indicateurs) : CPU / RAM / GPU / température
/// avec une jauge par métrique. Alimentée par <see cref="Update"/>. Les métriques
/// indisponibles (ex. pas de GPU/température exposés) masquent leur ligne.
/// </summary>
public partial class SystemFlyout : UserControl
{
    // La jauge de température est calée sur une échelle 0–100 °C.
    private const int TempScaleMax = 100;

    public SystemFlyout()
    {
        InitializeComponent();
    }

    public void Update(SystemMetricsSnapshot s)
    {
        bool cpu = SetMeter(CpuRow, CpuValue, CpuFill, CpuTrack, s.CpuPercent, v => $"{v} %");
        bool ram = SetMeter(RamRow, RamValue, RamFill, RamTrack, s.RamPercent, v => $"{v} %");
        bool gpu = SetMeter(GpuRow, GpuValue, GpuFill, GpuTrack, s.GpuPercent, v => $"{v} %");
        bool temp = SetMeter(TempRow, TempValue, TempFill, TempTrack, s.CpuTemperatureCelsius,
                             v => $"{v} °C", TempScaleMax);

        EmptyNote.Visibility = (cpu || ram || gpu || temp)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private static bool SetMeter(
        FrameworkElement row,
        TextBlock value,
        ColumnDefinition fill,
        ColumnDefinition track,
        int? metric,
        Func<int, string> format,
        int scaleMax = 100)
    {
        if (metric is not { } m)
        {
            row.Visibility = Visibility.Collapsed;
            return false;
        }

        row.Visibility = Visibility.Visible;
        value.Text = format(m);

        double pct = Math.Clamp(m * 100.0 / scaleMax, 0, 100);
        fill.Width = new GridLength(pct, GridUnitType.Star);
        track.Width = new GridLength(100 - pct, GridUnitType.Star);
        return true;
    }
}
