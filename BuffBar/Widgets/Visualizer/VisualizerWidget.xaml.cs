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
/// </summary>
public partial class VisualizerWidget : UserControl, IBarWidget
{
    private const float Attack = 0.42f;
    private const float Decay = 0.075f;

    private AudioCapture? _capture;
    private readonly float[] _target = new float[AudioCapture.Bands];
    private readonly float[] _display = new float[AudioCapture.Bands];
    private readonly DispatcherTimer _renderTimer;

    public string WidgetId => "visualizer";
    public FrameworkElement View => this;

    public VisualizerWidget()
    {
        InitializeComponent();

        _renderTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _renderTimer.Tick += OnFrame;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _capture ??= SharedAudioCapture.Acquire();
        if (!_renderTimer.IsEnabled)
            _renderTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _renderTimer.Stop();

        if (_capture is not null)
        {
            _capture = null;
            SharedAudioCapture.Release();
        }
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (_capture is null)
            return;

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