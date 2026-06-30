using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
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
    private readonly DispatcherTimer _cycle;
    private readonly DispatcherTimer _refresh;
    private readonly DispatcherTimer _ping;

    private bool _showPublic;
    private string? _publicIp;
    private string? _lastIcon;
    private string? _lastIp;

    public string WidgetId => "network";
    public FrameworkElement View => this;

    public NetworkWidget()
    {
        InitializeComponent();

        _cycle = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _cycle.Tick += (_, _) =>
        {
            _showPublic = !_showPublic;
            UpdateDisplay(animated: true);
        };

        _refresh = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _refresh.Tick += async (_, _) => await RefreshPublic();

        _ping = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _ping.Tick += async (_, _) => await RefreshPing();

        PingIcon.Text = Gauge;

        Loaded += async (_, _) =>
        {
            UpdateDisplay(animated: false);
            await RefreshPublic();
            _cycle.Start();
            _refresh.Start();

            if (BarConfig.GamingMode)
            {
                PingIcon.Visibility = Visibility.Visible;
                Ping.Visibility = Visibility.Visible;
                await RefreshPing();
                _ping.Start();
            }
        };
        Unloaded += (_, _) =>
        {
            _cycle.Stop();
            _refresh.Stop();
            _ping.Stop();
        };
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

        Ping.Text = ms.ToString(CultureInfo.InvariantCulture) + " ms";
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