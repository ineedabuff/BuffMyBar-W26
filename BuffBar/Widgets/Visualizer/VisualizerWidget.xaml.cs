using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Visualizer;

/// <summary>
/// Module visualiseur audio (style Cava) : barres alimentées par la capture
/// loopback WASAPI. Lissage attaque/chute asymétrique pour un rendu naturel
/// (montée rapide, descente douce).
/// </summary>
public partial class VisualizerWidget : UserControl, IBarWidget
{
    private const float Attack = 0.55f;  // montée (plus haut = plus vif)
    private const float Decay = 0.10f;  // descente (plus bas = retombée plus douce)

    private readonly AudioCapture _capture = new();
    private readonly float[] _target = new float[AudioCapture.Bands];
    private readonly float[] _display = new float[AudioCapture.Bands];

    public string WidgetId => "visualizer";
    public FrameworkElement View => this;

    public VisualizerWidget()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _capture.Start();
        CompositionTarget.Rendering += OnFrame;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= OnFrame;
        _capture.Stop();
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        _capture.GetBands(_target);

        for (int i = 0; i < _display.Length; i++)
        {
            float t = _target[i];
            float d = _display[i];
            float k = t > d ? Attack : Decay;
            _display[i] = d + (t - d) * k;
        }

        Spectrum.SetLevels(_display);
    }
}
