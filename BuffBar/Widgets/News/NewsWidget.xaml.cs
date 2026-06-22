using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.News;

/// <summary>
/// Bandeau d'actualités défilant : agrège les manchettes des flux RSS configurés
/// (faits divers de la région de Montréal) et les fait défiler en continu.
///
/// Rafraîchissement périodique en arrière-plan ; en cas d'échec réseau, le
/// dernier contenu valide est conservé.
/// </summary>
public partial class NewsWidget : UserControl, IBarWidget
{
    private const string Newspaper = "\uF1EA";          // FA newspaper
    private const string Separator = "      \u2022      "; // " • " espacé entre manchettes

    private readonly NewsService _service = new();
    private readonly DispatcherTimer _refresh;

    public string WidgetId => "news";
    public FrameworkElement View => this;

    public NewsWidget()
    {
        InitializeComponent();

        Icon.Text = Newspaper;
        Label.Text = "Actualités…";

        _refresh = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMinutes(Math.Max(1, BarConfig.NewsRefreshMinutes))
        };
        _refresh.Tick += async (_, _) => await Refresh();

        Loaded += async (_, _) =>
        {
            await Refresh();
            _refresh.Start();
        };
        Unloaded += (_, _) => _refresh.Stop();
    }

    private async Task Refresh()
    {
        List<string> heads = await _service.FetchAllAsync(BarConfig.NewsFeeds, BarConfig.NewsMaxPerFeed);

        if (heads.Count == 0)
            return; // réseau indisponible : on garde l'ancien contenu

        Label.Text = string.Join(Separator, heads);
    }
}
