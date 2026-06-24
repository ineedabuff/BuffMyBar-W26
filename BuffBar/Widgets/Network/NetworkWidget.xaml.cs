using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Network;

/// <summary>
/// Module réseau : une seule IP affichée à la fois, en alternance toutes les ~3 s.
///  - IP locale  : icône maison.
///  - IP publique : icône globe.
/// En mode jeu (BarConfig.GamingMode), affiche aussi la latence (ping) en tête,
/// colorée selon le niveau.
/// </summary>
public partial class NetworkWidget : UserControl, IBarWidget
{
    private const string Local = "\uF015";    // home
    private const string Public = "\uF0AC";   // globe
    private const string Gauge = "\uF0E4";    // tachometer (ping)

    private static readonly Brush PingGood = Frozen(0xDD, 0xFF, 0x24);
    private static readonly Brush PingMid = Frozen(0xFF, 0xFF, 0xFF);
    private static readonly Brush PingBad = Frozen(0xFF, 0x31, 0x31);

    private readonly NetworkService _service = new();
    private readonly DispatcherTimer _cycle;    // alternance ~3 s
    private readonly DispatcherTimer _refresh;  // IP publique
    private readonly DispatcherTimer _ping;     // latence (mode jeu)

    private bool _showPublic;
    private string? _publicIp;

    public string WidgetId => "network";
    public FrameworkElement View => this;

    public NetworkWidget()
    {
        InitializeComponent();

        _cycle = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _cycle.Tick += (_, _) => { _showPublic = !_showPublic; UpdateDisplay(); };

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
            UpdateDisplay();
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
        Unloaded += (_, _) => { _cycle.Stop(); _refresh.Stop(); _ping.Stop(); };
    }

    private async Task RefreshPublic()
    {
        _publicIp = await _service.FetchPublicIpAsync();
        UpdateDisplay();
    }

    private async Task RefreshPing()
    {
        long ms = await NetworkService.PingAsync();
        if (ms < 0)
        {
            Ping.Text = "—";
            Ping.Foreground = PingBad;
            return;
        }
        Ping.Text = ms.ToString(CultureInfo.InvariantCulture) + " ms";
        Ping.Foreground = ms < 60 ? PingGood : ms < 120 ? PingMid : PingBad;
    }

    private void UpdateDisplay()
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
        if (showPub)
        {
            Icon.Text = Public;
            Ip.Text = _publicIp;
        }
        else
        {
            Icon.Text = Local;
            Ip.Text = local;
        }

        Root.ToolTip = $"Locale : {local ?? "—"}    Publique : {_publicIp ?? "—"}";
    }

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var br = new SolidColorBrush(Color.FromRgb(r, g, b));
        br.Freeze();
        return br;
    }
}
