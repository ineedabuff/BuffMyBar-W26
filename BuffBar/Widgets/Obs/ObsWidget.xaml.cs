using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Obs;

/// <summary>
/// OBS module.
/// Sprint-003: visible only while OBS is running, with a light fade.
/// </summary>
public partial class ObsWidget : UserControl, IBarWidget
{
    private static readonly Brush RecBrush = CreateRecBrush();

    private readonly ObsService _obs = new(BarConfig.ObsHost, BarConfig.ObsPort, BarConfig.ObsPassword);
    private readonly ObsProcessWatcher _obsProcessWatcher = new();
    private bool _recording;
    private bool _obsRunning;

    public string WidgetId => "obs";
    public FrameworkElement View => this;

    public ObsWidget()
    {
        InitializeComponent();

        Opacity = 0.0;
        Visibility = Visibility.Collapsed;

        _obsProcessWatcher.IsRunningChanged += OnObsRunningChanged;
        _obs.StatusChanged += OnStatusChanged;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _obsProcessWatcher.Start();
        ApplyStatus(_obs.Status);
        _obs.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _obs.Stop();
        _obsProcessWatcher.Stop();
    }

    private void OnObsRunningChanged(object? sender, bool isRunning)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _obsRunning = isRunning;
            FadeVisibility(isRunning);
        }));
    }

    private void OnStatusChanged(ObsStatus status)
        => Dispatcher.BeginInvoke(new Action(() => ApplyStatus(status)));

    private void FadeVisibility(bool show)
    {
        BeginAnimation(OpacityProperty, null);

        if (show)
        {
            Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(Opacity, 1.0, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
            return;
        }

        var fadeOut = new DoubleAnimation(Opacity, 0.0, TimeSpan.FromMilliseconds(160))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            if (!_obsRunning)
                Visibility = Visibility.Collapsed;
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private void ApplyStatus(ObsStatus status)
    {
        if (status.Recording)
        {
            if (!_recording)
            {
                _recording = true;
                Dot.Fill = RecBrush;
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
            Dot.Fill = white;
            Rec.Foreground = white;
            Time.Visibility = Visibility.Collapsed;
            Time.Text = string.Empty;
        }
    }

    private void StartBlink()
    {
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
        var brush = new SolidColorBrush(Color.FromRgb(0xFF, 0x31, 0x31));
        brush.Freeze();
        return brush;
    }
}