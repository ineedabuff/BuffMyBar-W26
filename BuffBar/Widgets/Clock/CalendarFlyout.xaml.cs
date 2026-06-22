using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BuffBar.Widgets.Clock;

/// <summary>
/// Mini-calendrier mensuel (français, semaine débutant le lundi) affiché dans le
/// flyout de l'horloge. Navigation mois précédent / suivant. Le jour courant est
/// mis en évidence avec l'accent #ddff24.
/// </summary>
public partial class CalendarFlyout : UserControl
{
    private static readonly CultureInfo Culture = new("fr-CA");
    private static readonly string[] DayHeaders = { "lu", "ma", "me", "je", "ve", "sa", "di" };

    private DateTime _shown;

    public CalendarFlyout()
    {
        InitializeComponent();

        PrevButton.Click += (_, _) => { _shown = _shown.AddMonths(-1); Build(); };
        NextButton.Click += (_, _) => { _shown = _shown.AddMonths(1); Build(); };

        Loaded += (_, _) => ResetToToday();
    }

    /// <summary>Recentre l'affichage sur le mois courant (appelé à chaque ouverture).</summary>
    public void ResetToToday()
    {
        DateTime today = DateTime.Today;
        _shown = new DateTime(today.Year, today.Month, 1);
        Build();
    }

    private void Build()
    {
        BuildWeekHeader();
        BuildDays();

        string title = _shown.ToString("MMMM yyyy", Culture);
        MonthLabel.Text = Capitalize(title);

        string full = DateTime.Now.ToString("dddd d MMMM yyyy", Culture);
        TodayLabel.Text = Capitalize(full);
    }

    private void BuildWeekHeader()
    {
        if (WeekHeader.Children.Count > 0) return; // statique : une seule fois

        foreach (string d in DayHeaders)
        {
            WeekHeader.Children.Add(new TextBlock
            {
                Text = d,
                Style = (Style)FindResource("FlyoutSubtle"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Width = 30
            });
        }
    }

    private void BuildDays()
    {
        DayGrid.Children.Clear();

        var first = new DateTime(_shown.Year, _shown.Month, 1);
        // Décalage pour que la semaine commence le lundi (lundi = 0 ... dimanche = 6).
        int offset = ((int)first.DayOfWeek + 6) % 7;
        int daysInMonth = DateTime.DaysInMonth(_shown.Year, _shown.Month);

        for (int i = 0; i < offset; i++)
            DayGrid.Children.Add(new Border { Width = 30, Height = 28 });

        DateTime today = DateTime.Today;
        var accent = (Brush)FindResource("AccentBrush");

        for (int day = 1; day <= daysInMonth; day++)
        {
            var btn = new Button
            {
                Content = day.ToString(CultureInfo.InvariantCulture),
                Style = (Style)FindResource("CalDayButton")
            };

            bool isToday = _shown.Year == today.Year
                        && _shown.Month == today.Month
                        && day == today.Day;

            if (isToday)
            {
                btn.Background = accent;
                btn.Foreground = Brushes.Black;
                btn.FontWeight = FontWeights.SemiBold;
            }

            DayGrid.Children.Add(btn);
        }
    }

    private static string Capitalize(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], Culture) + s[1..];
}
