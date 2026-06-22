using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BuffBar.Widgets.Common;

/// <summary>
/// Texte défilant réutilisable (style Waybar) :
///  - s'adapte à la longueur du texte jusqu'à <see cref="MaxWidth"/> ;
///  - au-delà, défile en boucle continue et sans couture (deux copies + écart).
///
/// Couleur et police héritées du thème (TextBlock enfants non stylés).
/// </summary>
public sealed class MarqueeText : Grid
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(MarqueeText),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Vitesse de défilement en pixels par seconde.</summary>
    public double Speed { get; set; } = 40.0;

    /// <summary>Écart entre la fin du texte et le début de la copie suivante.</summary>
    public double Gap { get; set; } = 36.0;

    public double TextSize
    {
        get => _t1.FontSize;
        set { _t1.FontSize = _t2.FontSize = value; UpdateMarquee(); }
    }

    private readonly TextBlock _t1 = new();
    private readonly TextBlock _t2 = new();
    private readonly StackPanel _track = new() { Orientation = Orientation.Horizontal };
    private readonly TranslateTransform _tx = new();

    public MarqueeText()
    {
        ClipToBounds = true;

        _t1.VerticalAlignment = VerticalAlignment.Center;
        _t2.VerticalAlignment = VerticalAlignment.Center;
        _t1.FontSize = _t2.FontSize = 12;
        _t1.TextWrapping = _t2.TextWrapping = TextWrapping.NoWrap;
        _t2.Visibility = Visibility.Collapsed;

        _track.RenderTransform = _tx;
        _track.Children.Add(_t1);
        _track.Children.Add(_t2);
        Children.Add(_track);

        Loaded += (_, _) => UpdateMarquee();
        SizeChanged += (_, _) => UpdateMarquee();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var m = (MarqueeText)d;
        string text = (string)e.NewValue ?? string.Empty;
        m._t1.Text = text;
        m._t2.Text = text;
        m.UpdateMarquee();
    }

    private void UpdateMarquee()
    {
        // Toujours repartir d'un état propre.
        _tx.BeginAnimation(TranslateTransform.XProperty, null);
        _tx.X = 0;

        string text = _t1.Text ?? string.Empty;
        if (text.Length == 0)
        {
            _t2.Visibility = Visibility.Collapsed;
            return;
        }

        // Largeur de référence (le « hublot ») :
        //  - MaxWidth défini (mode plafonné, ex. Bluetooth) -> on s'y réfère ;
        //  - sinon (mode étiré, ex. Média) -> on prend la largeur réellement allouée.
        bool capped = !double.IsNaN(MaxWidth) && !double.IsInfinity(MaxWidth);
        double viewport = capped ? MaxWidth : ActualWidth;

        if (viewport <= 0)
        {
            // Pas encore mis en page (mode étiré) : on attend le prochain SizeChanged.
            _t2.Visibility = Visibility.Collapsed;
            return;
        }
        // Mesure via la MISE EN PAGE (DesiredSize) — la même source que celle qui
        // positionne la 2e copie dans le StackPanel. Indispensable pour que la
        // distance d'animation corresponde EXACTEMENT à l'écart réel : sinon
        // l'animation dépasse et laisse un blanc (temps mort) avant le rebouclage.
        _t1.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double textWidth = _t1.DesiredSize.Width;

        if (textWidth <= viewport)
        {
            // Tient dans le hublot : statique, une seule copie.
            _t2.Visibility = Visibility.Collapsed;
            return;
        }

        // Écart entre répétitions, plafonné pour ne jamais dépasser 3 s de "vide".
        double gap = Math.Min(Gap, Speed * 3.0);

        // Débordement : défilement continu et sans couture.
        _t2.Visibility = Visibility.Visible;
        _t2.Margin = new Thickness(gap, 0, 0, 0);

        double distance = textWidth + gap;
        var anim = new DoubleAnimation
        {
            From = 0,
            To = -distance,
            Duration = TimeSpan.FromSeconds(distance / Math.Max(1.0, Speed)),
            RepeatBehavior = RepeatBehavior.Forever
        };
        _tx.BeginAnimation(TranslateTransform.XProperty, anim);
    }
}
