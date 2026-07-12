using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    private IDisposable? _tick;
    private bool _busy;
    private bool _hasData;
    private WeatherInfo _last;

    public string WidgetId => "weather";
    public FrameworkElement View => this;

    public WeatherWidget()
    {
        InitializeComponent();
        Root.Visibility = Visibility.Collapsed;

        Loaded += async (_, _) =>
        {
            await Refresh();
            _tick?.Dispose();
            _tick = WidgetScheduler.Subscribe(TimeSpan.FromMinutes(15), () => _ = Refresh());
        };
        Unloaded += (_, _) => { _tick?.Dispose(); _tick = null; };

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
            Icon.Set(w.Condition, w.IsNight);
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
