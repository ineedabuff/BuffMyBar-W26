using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Weather;

/// <summary>
/// Module météo : icône Nerd Font (Font Awesome) selon les conditions + température.
/// Clic = applet détaillée (ressenti, humidité, vent, prévisions 3 jours).
/// Rafraîchit toutes les 15 min ; conserve la dernière valeur connue en cas d'échec.
/// </summary>
public partial class WeatherWidget : UserControl, IBarWidget
{
    private readonly WeatherService _service = new(BarConfig.WeatherLocation);
    private readonly DispatcherTimer _timer;
    private bool _busy;
    private bool _hasData;
    private WeatherInfo _last;

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

        // Ouverture de l'applet au survol, seulement si des données sont disponibles.
        Widgets.Common.HoverPopup.Attach(
            Root, Flyout, Applet,
            onOpening: () => Applet.Update(_last),
            canOpen: () => _hasData);
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

            _last = w;
            _hasData = true;
            Root.Visibility = Visibility.Visible;
            WidgetAnimator.SetTextWithGlitch(Icon, WeatherIcons.Glyph(w.Code));
            WidgetAnimator.SetTextWithGlitch(Temp, $"{w.TempC}\u00B0C");
            Root.ToolTip = string.IsNullOrWhiteSpace(w.Description)
                ? $"Ressenti {w.FeelsLikeC}\u00B0C — cliquer pour les prévisions"
                : $"{w.Description} · ressenti {w.FeelsLikeC}\u00B0C — cliquer pour les prévisions";

            if (Flyout.IsOpen) Applet.Update(w);   // mise à jour en direct si ouvert
        }
        finally
        {
            _busy = false;
        }
    }
}
