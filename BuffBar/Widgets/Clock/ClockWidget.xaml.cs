using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Clock;

/// <summary>
/// Module horloge : heure (HH:mm:ss) sur la première ligne,
/// date complète en français sur la seconde, les deux centrées.
/// Exemple :
///     15:47:32
///     Vendredi 19 juin 2026
/// </summary>
public partial class ClockWidget : UserControl, IBarWidget
{
    private static readonly CultureInfo Culture = new("fr-CA");
    private IDisposable? _tick;

    public string WidgetId => "clock";
    public FrameworkElement View => this;

    public ClockWidget()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            Refresh();
            _tick?.Dispose();
            _tick = WidgetScheduler.Subscribe(TimeSpan.FromSeconds(1), Refresh);
        };
        Unloaded += (_, _) => { _tick?.Dispose(); _tick = null; };

        // Ouverture du calendrier au survol (recentré sur le mois courant).
        Widgets.Common.HoverPopup.Attach(Root, Flyout, Calendar, onOpening: Calendar.ResetToToday);
    }

    private void Refresh()
    {
        DateTime now = DateTime.Now;

        TimeText.Text = now.ToString("HH:mm:ss", Culture);

        // "vendredi 19 juin 2026" -> capitalise uniquement la première lettre.
        string date = now.ToString("dddd d MMMM yyyy", Culture);
        if (date.Length > 0)
            date = char.ToUpper(date[0], Culture) + date[1..];

        DateText.Text = date;
    }
}
