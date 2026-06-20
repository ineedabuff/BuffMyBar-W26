using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Weather;

/// <summary>
/// Module météo : icône Nerd Font (Font Awesome) selon les conditions + température.
/// Description complète et ressenti dans l'infobulle. Rafraîchit toutes les 15 min.
/// Conserve la dernière valeur connue si une requête échoue.
/// </summary>
public partial class WeatherWidget : UserControl, IBarWidget
{
    // Glyphes Font Awesome (cohérents avec les autres modules).
    private const string Sun = "\uF185";    // sun
    private const string Cloud = "\uF0C2";  // cloud
    private const string Rain = "\uF043";   // tint (goutte)
    private const string Flake = "\uF2DC";  // snowflake
    private const string Bolt = "\uF0E7";   // bolt (orage)

    // Codes météo WWO (wttr.in)
    private static readonly HashSet<int> ThunderCodes = new() { 200, 386, 389, 392, 395 };
    private static readonly HashSet<int> SnowCodes = new()
        { 179, 182, 227, 230, 323, 326, 329, 332, 335, 338, 350, 362, 365, 368, 371, 374, 377 };
    private static readonly HashSet<int> CloudCodes = new() { 116, 119, 122, 143, 248, 260 };

    private readonly WeatherService _service = new(BarConfig.WeatherLocation);
    private readonly DispatcherTimer _timer;
    private bool _busy;
    private bool _hasData;

    public string WidgetId => "weather";
    public FrameworkElement View => this;

    public WeatherWidget()
    {
        InitializeComponent();
        Root.Visibility = Visibility.Collapsed;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMinutes(15)
        };
        _timer.Tick += async (_, _) => await Refresh();

        Loaded += async (_, _) => { await Refresh(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    private async Task Refresh()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            WeatherInfo w = await _service.FetchAsync();
            if (!w.Ok)
            {
                if (!_hasData) Root.Visibility = Visibility.Collapsed;
                return;  // on garde la dernière valeur connue
            }

            _hasData = true;
            Root.Visibility = Visibility.Visible;
            Icon.Text = Glyph(w.Code);
            Temp.Text = $"{w.TempC}°C";
            Root.ToolTip = string.IsNullOrWhiteSpace(w.Description)
                ? $"Ressenti {w.FeelsLikeC}°C"
                : $"{w.Description} · ressenti {w.FeelsLikeC}°C";
        }
        finally
        {
            _busy = false;
        }
    }

    private static string Glyph(int code)
    {
        if (code == 113) return Sun;
        if (ThunderCodes.Contains(code)) return Bolt;
        if (SnowCodes.Contains(code)) return Flake;
        if (CloudCodes.Contains(code)) return Cloud;
        if (code >= 176) return Rain;  // autres précipitations (pluie, bruine, averses)
        return Cloud;
    }
}
