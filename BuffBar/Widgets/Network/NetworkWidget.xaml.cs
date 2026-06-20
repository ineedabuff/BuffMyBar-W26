using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Network;

/// <summary>
/// Module réseau : une seule IP affichée à la fois, en alternance toutes les ~3 s.
///  - IP locale  : icône maison.
///  - IP publique : icône globe.
/// L'IP publique est récupérée en arrière-plan et rafraîchie périodiquement.
/// </summary>
public partial class NetworkWidget : UserControl, IBarWidget
{
    private const string Local = "\uF015";   // home
    private const string Public = "\uF0AC";  // globe

    private readonly NetworkService _service = new();
    private readonly DispatcherTimer _cycle;    // alternance ~3 s
    private readonly DispatcherTimer _refresh;  // IP publique

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

        Loaded += async (_, _) =>
        {
            UpdateDisplay();
            await RefreshPublic();
            _cycle.Start();
            _refresh.Start();
        };
        Unloaded += (_, _) => { _cycle.Stop(); _refresh.Stop(); };
    }

    private async Task RefreshPublic()
    {
        _publicIp = await _service.FetchPublicIpAsync();
        UpdateDisplay();
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
}
