using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Network;

/// <summary>
/// Network module: alternates local/public IP with a subtle fade.
/// </summary>
public partial class NetworkWidget : UserControl, IBarWidget
{
    private const string Local = "\uF015";
    private const string Public = "\uF0AC";
    private const string Gauge = "\uF0E4";

    private static readonly Brush PingGood = Frozen(0xDD, 0xFF, 0x24);
    private static readonly Brush PingMid = Frozen(0xFF, 0xFF, 0xFF);
    private static readonly Brush PingBad = Frozen(0xFF, 0x31, 0x31);

    private readonly NetworkService _service = new();
    private IDisposable? _cycleTick;
    private IDisposable? _refreshTick;
    private IDisposable? _pingTick;

    private bool _showPublic;
    private string? _publicIp;
    private string? _lastIcon;
    private string? _lastIp;

    public string WidgetId => "network";
    public FrameworkElement View => this;

    public NetworkWidget()
    {
        InitializeComponent();

        PingIcon.Text = Gauge;

        Loaded += async (_, _) =>
        {
            UpdateDisplay(animated: false);
            await RefreshPublic();
            _cycleTick?.Dispose();
            _refreshTick?.Dispose();
            _cycleTick = WidgetScheduler.Subscribe(TimeSpan.FromSeconds(3), CycleDisplay);
            _refreshTick = WidgetScheduler.Subscribe(TimeSpan.FromMinutes(5), () => _ = RefreshPublic());

            if (BarConfig.GamingMode)
            {
                PingIcon.Visibility = Visibility.Visible;
                Ping.Visibility = Visibility.Visible;
                await RefreshPing();
                _pingTick?.Dispose();
                _pingTick = WidgetScheduler.Subscribe(TimeSpan.FromSeconds(2), () => _ = RefreshPing());
            }
        };
        Unloaded += (_, _) =>
        {
            _cycleTick?.Dispose(); _cycleTick = null;
            _refreshTick?.Dispose(); _refreshTick = null;
            _pingTick?.Dispose(); _pingTick = null;
        };
    }

    private void CycleDisplay()
    {
        _showPublic = !_showPublic;
        UpdateDisplay(animated: true);
    }

    private async Task RefreshPublic()
    {
        _publicIp = await _service.FetchPublicIpAsync();
        UpdateDisplay(animated: true);
    }

    private async Task RefreshPing()
    {
        long ms = await NetworkService.PingAsync();
        if (ms < 0)
        {
            Ping.Text = "-";
            Ping.Foreground = PingBad;
            return;
        }

        WidgetAnimator.SetTextWithGlitch(Ping, ms.ToString(CultureInfo.InvariantCulture) + " ms");
        Ping.Foreground = ms < 60 ? PingGood : ms < 120 ? PingMid : PingBad;
    }

    private void UpdateDisplay(bool animated)
    {
        string? local = NetworkService.LocalIp();
        bool canPublic = _publicIp is not null;
        bool canLocal = local is not null;

        if (!canPublic && !canLocal)
        {
            Root.Visibility = Visibility.Collapsed;
            return;
        }

        Root.Visibility = Visibility.Visible;

        bool showPub = canPublic && (_showPublic || !canLocal);
        string nextIcon = showPub ? Public : Local;
        string? nextIp = showPub ? _publicIp : local;

        bool changed = _lastIcon != nextIcon || _lastIp != nextIp;
        _lastIcon = nextIcon;
        _lastIp = nextIp;

        if (animated && changed)
        {
            FadeTextChange(Icon, nextIcon);
            FadeTextChange(Ip, nextIp ?? "-");
        }
        else
        {
            Icon.Text = nextIcon;
            Ip.Text = nextIp ?? "-";
        }

        Root.ToolTip = $"Locale : {local ?? "-"}    Publique : {_publicIp ?? "-"}";
    }

    private static void FadeTextChange(TextBlock textBlock, string text)
    {
        var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(90))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            textBlock.Text = text;
            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(130))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            textBlock.BeginAnimation(OpacityProperty, fadeIn);
        };
        textBlock.BeginAnimation(OpacityProperty, fadeOut);
    }

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}