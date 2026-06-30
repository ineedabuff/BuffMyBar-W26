using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Battery;

/// <summary>
/// Module batterie : glyphe Nerd Font selon le niveau + pourcentage.
/// Éclair en charge. Se masque automatiquement sur un poste sans batterie.
/// </summary>
public partial class BatteryWidget : UserControl, IBarWidget
{
    // Glyphes Font Awesome (Nerd Font) — secteur PUA.
    private const string Bolt = "\uF0E7";  // fa-bolt (en charge)
    private const string Full = "\uF240";  // fa-battery-full
    private const string ThreeQ = "\uF241";  // fa-battery-three-quarters
    private const string Half = "\uF242";  // fa-battery-half
    private const string Quarter = "\uF243";  // fa-battery-quarter
    private const string Empty = "\uF244";  // fa-battery-empty

    private readonly DispatcherTimer _timer;

    public string WidgetId => "battery";
    public FrameworkElement View => this;

    public BatteryWidget()
    {
        InitializeComponent();

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _timer.Tick += (_, _) => Refresh();

        Loaded += (_, _) => { Refresh(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Refresh()
    {
        BatteryInfo info = BatteryService.Read();

        // Poste fixe / pas de batterie : on masque le module.
        Root.Visibility = info.Present ? Visibility.Visible : Visibility.Collapsed;
        if (!info.Present) return;

        string icon = LevelGlyph(info.Percent);
        string prefix = info.Charging ? Bolt + " " : string.Empty;
        WidgetAnimator.SetTextWithFade(Label, $"{prefix}{icon} {info.Percent}%");
    }

    private static string LevelGlyph(int percent) => percent switch
    {
        >= 90 => Full,
        >= 60 => ThreeQ,
        >= 40 => Half,
        >= 10 => Quarter,
        _ => Empty
    };
}
