using System.Windows;
using System.Windows.Controls;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Weather;

/// <summary>
/// Applet météo détaillée : conditions actuelles (icône, température, ressenti,
/// humidité, vent) et prévisions sur trois jours. Alimentée par <see cref="Update"/>.
/// </summary>
public partial class WeatherFlyout : UserControl
{
    public WeatherFlyout()
    {
        InitializeComponent();
        Location.Text = BarConfig.WeatherLocation;
    }

    public void Update(WeatherInfo w)
    {
        if (!w.Ok) return;

        BigIcon.Text = WeatherIcons.Glyph(w.Code);
        BigTemp.Text = $"{w.TempC}\u00B0C";
        Desc.Text = w.Description;

        string wind = w.WindKmph > 0
            ? $"vent {w.WindKmph} km/h {w.WindDir}".TrimEnd()
            : "vent —";
        Details.Text = $"Ressenti {w.FeelsLikeC}\u00B0C   ·   humidité {w.Humidity}%   ·   {wind}";

        ForecastGrid.Children.Clear();
        foreach (ForecastDay d in w.Forecast)
            ForecastGrid.Children.Add(BuildDayCard(d));
    }

    private FrameworkElement BuildDayCard(ForecastDay d)
    {
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

        panel.Children.Add(new TextBlock
        {
            Text = d.Label,
            Style = (Style)FindResource("FlyoutSubtle"),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = WeatherIcons.Glyph(d.Code),
            Style = (Style)FindResource("ModuleText"),
            Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush"),
            FontSize = 18,
            Margin = new Thickness(0, 4, 0, 4),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"{d.MaxC}\u00B0 / {d.MinC}\u00B0",
            Style = (Style)FindResource("FlyoutText"),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        return panel;
    }
}
