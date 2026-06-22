using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BuffBar.Core;

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
    private readonly DispatcherTimer _timer;

    public string WidgetId => "clock";
    public FrameworkElement View => this;

    public ClockWidget()
    {
        InitializeComponent();

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => Refresh();

        Loaded += (_, _) => { Refresh(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();

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
