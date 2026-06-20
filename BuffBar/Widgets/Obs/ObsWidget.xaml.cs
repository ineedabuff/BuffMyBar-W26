using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Obs;

/// <summary>
/// Module OBS : "● REC".
///  - Inactif  : blanc fixe (comme les autres modules).
///  - Enregistrement : #FF3131 et clignotement.
/// État obtenu en direct via obs-websocket (ObsService).
/// </summary>
public partial class ObsWidget : UserControl, IBarWidget
{
    private static readonly Brush RecBrush = CreateRecBrush();

    private readonly ObsService _obs = new(BarConfig.ObsHost, BarConfig.ObsPort, BarConfig.ObsPassword);

    public string WidgetId => "obs";
    public FrameworkElement View => this;

    public ObsWidget()
    {
        InitializeComponent();
        _obs.RecordingChanged += OnRecordingChanged;

        Loaded += (_, _) => { ApplyState(_obs.Recording); _obs.Start(); };
        Unloaded += (_, _) => _obs.Stop();
    }

    private void OnRecordingChanged(bool recording)
        => Dispatcher.Invoke(() => ApplyState(recording));

    private void ApplyState(bool recording)
    {
        if (recording)
        {
            Dot.Foreground = RecBrush;
            Rec.Foreground = RecBrush;
            StartBlink();
        }
        else
        {
            StopBlink();
            var white = (Brush)FindResource("PrimaryText");
            Dot.Foreground = white;
            Rec.Foreground = white;
        }
    }

    private void StartBlink()
    {
        // Clignotement net (allumé/éteint) à 1 Hz.
        var blink = new DoubleAnimationUsingKeyFrames
        {
            RepeatBehavior = RepeatBehavior.Forever,
            Duration = TimeSpan.FromSeconds(1.0)
        };
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
        Glyphs.BeginAnimation(OpacityProperty, blink);
    }

    private void StopBlink()
    {
        Glyphs.BeginAnimation(OpacityProperty, null);
        Glyphs.Opacity = 1.0;
    }

    private static Brush CreateRecBrush()
    {
        var b = new SolidColorBrush(Color.FromRgb(0xFF, 0x31, 0x31));
        b.Freeze();
        return b;
    }
}
