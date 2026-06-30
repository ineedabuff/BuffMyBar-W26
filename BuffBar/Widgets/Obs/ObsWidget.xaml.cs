using System;

using BuffMyBar.Services;
using System.Windows;

using BuffMyBar.Services;
using System.Windows.Controls;

using BuffMyBar.Services;
using System.Windows.Media;

using BuffMyBar.Services;
using System.Windows.Media.Animation;

using BuffMyBar.Services;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Obs;

/// <summary>
/// Module OBS : "â— REC hh:mm:ss".
///  - Inactif : blanc fixe (comme les autres modules), minuteur masquÃ©.
///  - Enregistrement : #FF3131, pastille clignotante (1 Hz) et durÃ©e Ã©coulÃ©e.
/// Ã‰tat + durÃ©e obtenus en direct via obs-websocket (ObsService, sondage 1 s).
/// </summary>
public partial class ObsWidget : UserControl, IBarWidget
{
    // BUFFMYBAR_SPRINT001_OBS_AUTOHIDE
    private readonly ObsProcessWatcher _obsProcessWatcher = new();

    private static readonly Brush RecBrush = CreateRecBrush();

    private readonly ObsService _obs = new(BarConfig.ObsHost, BarConfig.ObsPort, BarConfig.ObsPassword);
    private bool _recording;

    public string WidgetId => "obs";
    public FrameworkElement View => this;

    public ObsWidget()
    {
        InitializeComponent();
        // BUFFMYBAR_SPRINT001_OBS_AUTOHIDE
        Visibility = Visibility.Collapsed;
        _obsProcessWatcher.IsRunningChanged += (_, isRunning) =>
        {
            Dispatcher.Invoke(() => Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed);
        };
        _obsProcessWatcher.Start();
        _obs.StatusChanged += OnStatusChanged;

        Loaded += (_, _) => { ApplyStatus(_obs.Status); _obs.Start(); };
        Unloaded += (_, _) => _obs.Stop();
    }

    private void OnStatusChanged(ObsStatus status)
        => Dispatcher.BeginInvoke(new Action(() => ApplyStatus(status)));

    private void ApplyStatus(ObsStatus status)
    {
        if (status.Recording)
        {
            if (!_recording)
            {
                _recording = true;
                Dot.Foreground = RecBrush;
                Rec.Foreground = RecBrush;
                Time.Foreground = RecBrush;
                Time.Visibility = Visibility.Visible;
                StartBlink();
            }
            Time.Text = Format(status.Duration);
        }
        else if (_recording || Time.Visibility == Visibility.Visible)
        {
            _recording = false;
            StopBlink();
            var white = (Brush)FindResource("PrimaryText");
            Dot.Foreground = white;
            Rec.Foreground = white;
            Time.Visibility = Visibility.Collapsed;
            Time.Text = string.Empty;
        }
    }

    private void StartBlink()
    {
        // Clignotement net (allumÃ©/Ã©teint) Ã  1 Hz, sur la pastille seulement
        // pour garder le minuteur lisible.
        var blink = new DoubleAnimationUsingKeyFrames
        {
            RepeatBehavior = RepeatBehavior.Forever,
            Duration = TimeSpan.FromSeconds(1.0)
        };
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
        Dot.BeginAnimation(OpacityProperty, blink);
    }

    private void StopBlink()
    {
        Dot.BeginAnimation(OpacityProperty, null);
        Dot.Opacity = 1.0;
    }

    private static string Format(TimeSpan t)
        => t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");

    private static Brush CreateRecBrush()
    {
        var b = new SolidColorBrush(Color.FromRgb(0xFF, 0x31, 0x31));
        b.Freeze();
        return b;
    }
}
