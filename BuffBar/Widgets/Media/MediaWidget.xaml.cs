using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BuffBar.Core;
using BuffBar.Effects;
using BuffBar.Services;

namespace BuffBar.Widgets.Media;

/// <summary>
/// Module média : icône lecture/pause + titre — artiste de la session active.
/// Clic = lecture/pause, molette = piste suivante/précédente.
/// Se masque automatiquement quand aucun lecteur n'est actif.
/// </summary>
public partial class MediaWidget : UserControl, IBarWidget
{
    // Glyphes Font Awesome (Nerd Font) — l'icône reflète l'action du clic.
    private const string Play = "\uF04B";   // lecture (en pause -> clic pour jouer)
    private const string Pause = "\uF04C";   // pause (en lecture -> clic pour mettre en pause)

    private readonly MediaService _media = new();
    private IDisposable? _tick;
    private bool _busy;
    private string? _lastTrack;   // dernière piste vue, pour détecter les changements
    private bool? _lastPlaying;   // dernier état lecture/pause, pour détecter les bascules

    public string WidgetId => "media";
    public FrameworkElement View => this;

    public MediaWidget()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            await Refresh();
            _tick?.Dispose();
            _tick = WidgetScheduler.Subscribe(TimeSpan.FromSeconds(1), () => _ = Refresh());
        };
        Unloaded += (_, _) => { _tick?.Dispose(); _tick = null; };
    }

    private async Task Refresh()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            MediaInfo info = await _media.ReadAsync();

            if (!info.HasSession || string.IsNullOrWhiteSpace(info.Title))
            {
                Root.Visibility = Visibility.Collapsed;
                return;
            }

            Root.Visibility = Visibility.Visible;
            Icon.Set(info.Playing);

            string track = string.IsNullOrWhiteSpace(info.Artist)
                ? info.Title
                : $"{info.Title} — {info.Artist}";
            Label.Text = track;

            // Onde de glitch (gauche -> droite) sur changement de chanson OU bascule
            // lecture/pause. Détecté ici (et pas dans OnClick) pour couvrir aussi les
            // touches média du clavier et les changements venus d'une autre appli.
            // On ignore le tout premier chargement : c'est le reveal de démarrage.
            bool first = _lastTrack is null && _lastPlaying is null;
            bool trackChanged = _lastTrack is not null && !string.Equals(track, _lastTrack, StringComparison.Ordinal);
            bool playChanged = _lastPlaying is not null && info.Playing != _lastPlaying;
            _lastTrack = track;
            _lastPlaying = info.Playing;
            if (!first && (trackChanged || playChanged))
                GlitchText.TriggerWave(this);
        }
        finally
        {
            _busy = false;
        }
    }

    private async void OnClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        await _media.TogglePlayPauseAsync();
        await Refresh();
    }

    private async void OnWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        if (e.Delta > 0) await _media.NextAsync();
        else await _media.PreviousAsync();
        await Refresh();
    }
}
