using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Clock;

/// <summary>
/// Mini-calendrier mensuel (français, semaine débutant le lundi). Navigation
/// mois précédent/suivant ; jour courant en accent #ddff24. Si Google Agenda est
/// activé et connecté, les jours porteurs d'événements reçoivent une pastille et
/// la liste des événements du jour sélectionné s'affiche dessous.
/// </summary>
public partial class CalendarFlyout : UserControl
{
    private static readonly CultureInfo Culture = new("fr-CA");
    private static readonly string[] DayHeaders = { "lu", "ma", "me", "je", "ve", "sa", "di" };

    private DateTime _shown;       // 1er du mois affiché
    private DateTime _selected;    // jour sélectionné
    private int _generation;       // anti-concurrence des chargements asynchrones

    private readonly Dictionary<int, List<CalEvent>> _eventsByDay = new();

    public CalendarFlyout()
    {
        InitializeComponent();

        PrevButton.Click += (_, _) => { _shown = _shown.AddMonths(-1); Build(); };
        NextButton.Click += (_, _) => { _shown = _shown.AddMonths(1); Build(); };

        Loaded += (_, _) => ResetToToday();
    }

    /// <summary>Recentre sur le mois courant (appelé à chaque ouverture).</summary>
    public void ResetToToday()
    {
        DateTime today = DateTime.Today;
        _shown = new DateTime(today.Year, today.Month, 1);
        _selected = today;
        Build();
    }

    private void Build()
    {
        BuildWeekHeader();

        // Sélection par défaut : aujourd'hui s'il est dans le mois affiché, sinon le 1er.
        DateTime today = DateTime.Today;
        if (_selected.Year != _shown.Year || _selected.Month != _shown.Month)
            _selected = (today.Year == _shown.Year && today.Month == _shown.Month) ? today : _shown;

        _eventsByDay.Clear();
        BuildDays();
        MonthLabel.Text = Capitalize(_shown.ToString("MMMM yyyy", Culture));
        RefreshEventList();

        _ = LoadEventsAsync(++_generation);
    }

    private void BuildWeekHeader()
    {
        if (WeekHeader.Children.Count > 0) return;
        foreach (string d in DayHeaders)
        {
            WeekHeader.Children.Add(new TextBlock
            {
                Text = d,
                Style = (Style)FindResource("FlyoutSubtle"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Width = 34
            });
        }
    }

    private void BuildDays()
    {
        DayGrid.Children.Clear();

        var first = new DateTime(_shown.Year, _shown.Month, 1);
        int offset = ((int)first.DayOfWeek + 6) % 7;   // lundi = 0
        int daysInMonth = DateTime.DaysInMonth(_shown.Year, _shown.Month);

        for (int i = 0; i < offset; i++)
            DayGrid.Children.Add(new Border { Width = 34, Height = 30 });

        DateTime today = DateTime.Today;
        var accent = (Brush)FindResource("AccentBrush");

        for (int day = 1; day <= daysInMonth; day++)
        {
            int d = day;
            bool isToday = _shown.Year == today.Year && _shown.Month == today.Month && d == today.Day;

            var number = new TextBlock
            {
                Text = d.ToString(CultureInfo.InvariantCulture),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var dot = new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = isToday ? Brushes.Black : accent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 2),
                Visibility = Visibility.Collapsed
            };

            var cell = new Grid { Width = 30, Height = 28 };
            cell.Children.Add(number);
            cell.Children.Add(dot);

            var btn = new Button
            {
                Content = cell,
                Style = (Style)FindResource("CalDayButton"),
                Tag = dot   // pour activer la pastille après chargement
            };

            if (isToday)
            {
                btn.Background = accent;
                btn.Foreground = Brushes.Black;
                btn.FontWeight = FontWeights.SemiBold;
            }

            btn.Click += (_, _) => { _selected = new DateTime(_shown.Year, _shown.Month, d); RefreshEventList(); };

            DayGrid.Children.Add(btn);
        }
    }

    private async Task LoadEventsAsync(int generation)
    {
        GoogleCalendarConfig g = ConfigService.Current.GoogleCalendar;

        if (!g.Enabled)
        {
            EventHint.Text = string.Empty;
            return;
        }
        if (!GoogleCalendarService.IsConnected)
        {
            EventHint.Text = "Google Agenda : non connecté (Paramètres).";
            return;
        }

        EventHint.Text = "Chargement…";

        var from = new DateTime(_shown.Year, _shown.Month, 1);
        DateTime to = from.AddMonths(1);

        List<CalEvent> events = await GoogleCalendarService.GetEventsAsync(
            g.ClientId, g.ClientSecret, from, to, g.MaxEvents);

        if (generation != _generation) return;   // mois changé entre-temps

        _eventsByDay.Clear();
        foreach (CalEvent e in events)
        {
            int day = e.Start.Day;
            if (e.Start.Year != _shown.Year || e.Start.Month != _shown.Month) continue;
            if (!_eventsByDay.TryGetValue(day, out var list))
                _eventsByDay[day] = list = new List<CalEvent>();
            list.Add(e);
        }

        MarkDots();
        RefreshEventList();
        EventHint.Text = events.Count == 0 ? "Aucun événement ce mois-ci." : string.Empty;
    }

    private void MarkDots()
    {
        foreach (object child in DayGrid.Children)
            if (child is Button { Tag: Ellipse dot } btn && btn.Content is Grid g
                && g.Children[0] is TextBlock t
                && int.TryParse(t.Text, out int day))
                dot.Visibility = _eventsByDay.ContainsKey(day) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshEventList()
    {
        SelectedDateLabel.Text = Capitalize(_selected.ToString("dddd d MMMM", Culture));
        EventList.Children.Clear();

        if (!_eventsByDay.TryGetValue(_selected.Day, out var list) || list.Count == 0)
        {
            if (ConfigService.Current.GoogleCalendar.Enabled && GoogleCalendarService.IsConnected)
                EventList.Children.Add(new TextBlock
                {
                    Text = "Aucun événement.",
                    Style = (Style)FindResource("FlyoutSubtle")
                });
            return;
        }

        foreach (CalEvent e in list.OrderBy(x => x.AllDay ? DateTime.MinValue : x.Start))
            EventList.Children.Add(BuildEventRow(e));
    }

    private FrameworkElement BuildEventRow(CalEvent e)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

        row.Children.Add(new TextBlock
        {
            Text = e.AllDay ? "journée" : e.Start.ToString("HH:mm", Culture),
            Style = (Style)FindResource("FlyoutSubtle"),
            Width = 56
        });

        row.Children.Add(new TextBlock
        {
            Text = e.Title,
            Style = (Style)FindResource("FlyoutText"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 200
        });

        return row;
    }

    private static string Capitalize(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], Culture) + s[1..];
}
