using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BuffBar.Core;
using BuffBar.Services;

namespace BuffBar.Widgets.Volume;

/// <summary>
/// Module volume : glyphe Nerd Font à trois paliers (muet / bas / haut) + pourcentage.
/// Molette pour ajuster (pas de 2 %), clic gauche pour couper le son.
/// Couleur par niveau : > 15 % en #FF3131, > 10 % en #ddff24, sinon blanc.
/// </summary>
public partial class VolumeWidget : UserControl, IBarWidget
{
    // Glyphes Font Awesome 4 (présents dans toutes les Nerd Fonts).
    private const string Muted = "\uF026";  // volume-off (haut-parleur sans ondes)
    private const string Low = "\uF027";  // volume-down
    private const string High = "\uF028";  // volume-up

    // Paliers de couleur du volume
    private static readonly Brush Accent = Frozen(0xDD, 0xFF, 0x24);
    private static readonly Brush Alert = Frozen(0xFF, 0x31, 0x31);

    private const int Step = 2;  // pas de la molette en %

    private readonly VolumeController _controller = new();
    private readonly DispatcherTimer _timer;

    public string WidgetId => "volume";
    public FrameworkElement View => this;

    public VolumeWidget()
    {
        InitializeComponent();

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        _timer.Tick += (_, _) => Refresh();

        Loaded += (_, _) => { Refresh(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Refresh()
    {
        VolumeInfo? info = _controller.Read();

        if (info is not { } v)
        {
            Root.Visibility = Visibility.Collapsed;
            return;
        }

        Root.Visibility = Visibility.Visible;

        string icon = v.Muted ? Muted : (v.Percent <= 50 ? Low : High);
        Label.Text = $"{icon} {v.Percent}%";

        // Couleur : muet -> gris ; > 15 % -> rouge ; > 10 % -> accent ; sinon blanc.
        Label.Foreground = v.Muted ? (Brush)FindResource("SubtleText")
            : v.Percent > 15 ? Alert
            : v.Percent > 10 ? Accent
            : (Brush)FindResource("PrimaryText");
    }

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _controller.Step(e.Delta > 0 ? Step : -Step);
        Refresh();
        e.Handled = true;
    }

    private void OnLeftClick(object sender, MouseButtonEventArgs e)
    {
        _controller.ToggleMute();
        Refresh();
        e.Handled = true;
    }
}
