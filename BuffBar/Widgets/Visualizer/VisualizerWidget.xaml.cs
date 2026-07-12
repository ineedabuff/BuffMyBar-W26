using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Visualizer;

/// <summary>
/// Audio visualizer widget.
/// Sprint-003: stable ~30 FPS render cadence with smoother attack/decay.
///
/// Optimisation énergie/CPU :
///   - Silence : bascule à ~5 FPS et cesse tout redraw tant que le spectre est
///     au repos (aucun <see cref="SpectrumView.SetLevels"/>, donc aucun
///     <c>InvalidateVisual</c>). Retour au plein régime dès qu'un son revient.
///   - Plein écran : arrêt complet du rendu pendant qu'une application plein
///     écran (jeu) est au premier plan, via <see cref="FullscreenState"/>.
/// </summary>
public partial class VisualizerWidget : UserControl, IBarWidget
{
    private const float Attack = 0.42f;
    private const float Decay = 0.075f;

    // Cadences de rendu : plein régime avec du son, régime réduit au repos.
    private const double FullRateMs = 33;    // ~30 FPS
    private const double IdleRateMs = 200;   // ~5 FPS (sondage léger, pas de redraw)
    private const float RestEpsilon = 0.001f; // seuil « barre à zéro »

    private AudioCapture? _capture;
    private readonly float[] _target = new float[AudioCapture.Bands];
    private readonly float[] _display = new float[AudioCapture.Bands];
    private readonly DispatcherTimer _renderTimer;

    private bool _loaded;
    private bool _fullscreen;
    private bool _idle;   // vrai quand on tourne en cadence réduite (spectre au repos)

    public string WidgetId => "visualizer";
    public FrameworkElement View => this;

    public VisualizerWidget()
    {
        InitializeComponent();

        _renderTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(FullRateMs)
        };
        _renderTimer.Tick += OnFrame;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _capture ??= SharedAudioCapture.Acquire();

        _loaded = true;
        _fullscreen = FullscreenState.IsActive;
        FullscreenState.Changed += OnFullscreenChanged;

        UpdateRenderTimer();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _loaded = false;
        FullscreenState.Changed -= OnFullscreenChanged;
        _renderTimer.Stop();

        if (_capture is not null)
        {
            _capture = null;
            SharedAudioCapture.Release();
        }
    }

    private void OnFullscreenChanged(bool active)
    {
        // La notification shell arrive sur le thread UI, mais on se protège au cas
        // où l'état serait diffusé depuis un autre contexte.
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => OnFullscreenChanged(active)));
            return;
        }

        _fullscreen = active;
        UpdateRenderTimer();
    }

    /// <summary>Démarre ou arrête le rendu selon l'état (chargé, plein écran).</summary>
    private void UpdateRenderTimer()
    {
        bool shouldRun = _loaded && !_fullscreen;

        if (shouldRun)
        {
            if (!_renderTimer.IsEnabled)
            {
                _idle = false;
                _renderTimer.Interval = TimeSpan.FromMilliseconds(FullRateMs);
                _renderTimer.Start();
            }
        }
        else
        {
            _renderTimer.Stop();
        }
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (_capture is null)
            return;

        _capture.GetBands(_target);

        bool active = false;
        for (int i = 0; i < _display.Length; i++)
        {
            float t = _target[i];
            float d = _display[i];
            float k = t > d ? Attack : Decay;
            float nd = d + (t - d) * k;
            _display[i] = nd;

            if (t > RestEpsilon || nd > RestEpsilon)
                active = true;
        }

        if (active)
        {
            // Son présent : plein régime + rendu de la trame.
            if (_idle)
            {
                _idle = false;
                _renderTimer.Interval = TimeSpan.FromMilliseconds(FullRateMs);
            }

            Spectrum.SetLevels(_display);
        }
        else if (!_idle)
        {
            // Bascule au repos : on pousse une dernière trame (barres à zéro),
            // puis on passe en cadence réduite.
            _idle = true;
            Spectrum.SetLevels(_display);
            _renderTimer.Interval = TimeSpan.FromMilliseconds(IdleRateMs);
        }
        // Repos établi : spectre déjà vide -> aucun redraw jusqu'au retour du son.
    }
}
