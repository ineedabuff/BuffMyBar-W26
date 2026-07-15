using System;
using System.Windows;
using System.Windows.Controls;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Battery;

/// <summary>
/// Module batterie : icône native (niveau proportionnel, éclair en charge) +
/// pourcentage. Se masque automatiquement sur un poste sans batterie.
/// </summary>
public partial class BatteryWidget : UserControl, IBarWidget
{
    private IDisposable? _tick;

    public string WidgetId => "battery";
    public FrameworkElement View => this;

    public BatteryWidget()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            Refresh();
            _tick?.Dispose();
            _tick = WidgetScheduler.Subscribe(TimeSpan.FromSeconds(5), Refresh);
        };
        Unloaded += (_, _) => { _tick?.Dispose(); _tick = null; };
    }

    private void Refresh()
    {
        BatteryInfo info = BatteryService.Read();

        // Poste fixe / pas de batterie : on masque le module.
        Root.Visibility = info.Present ? Visibility.Visible : Visibility.Collapsed;
        if (!info.Present) return;

        Icon.Set(info.Percent, info.Charging);
        WidgetAnimator.SetTextWithFade(Percent, $"{info.Percent}%");
    }
}
