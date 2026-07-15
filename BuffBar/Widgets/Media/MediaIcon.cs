using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BuffBar.Widgets.Media;

/// <summary>
/// Icône média native : triangle « lecture » ou deux barres « pause ».
/// Suit le thème (PrimaryText).
/// </summary>
public sealed class MediaIcon : Grid
{
    private readonly Polygon _play = new()
    {
        Points = new PointCollection { new(32, 22), new(32, 78), new(78, 50) }
    };

    private readonly Canvas _pause = new() { Visibility = Visibility.Collapsed };

    public MediaIcon()
    {
        var stage = new Canvas { Width = 100, Height = 100, Background = Brushes.Transparent };

        _play.SetResourceReference(Shape.FillProperty, "PrimaryText");
        _pause.Children.Add(Bar(30));
        _pause.Children.Add(Bar(56));

        stage.Children.Add(_play);
        stage.Children.Add(_pause);
        Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = stage });
    }

    /// <summary>Vrai = en lecture → montre l'icône « pause » (pour mettre en pause).</summary>
    public void Set(bool playing)
    {
        _pause.Visibility = playing ? Visibility.Visible : Visibility.Collapsed;
        _play.Visibility = playing ? Visibility.Collapsed : Visibility.Visible;
    }

    private static Rectangle Bar(double x)
    {
        var r = new Rectangle { Width = 14, Height = 56, RadiusX = 3, RadiusY = 3 };
        Canvas.SetLeft(r, x);
        Canvas.SetTop(r, 22);
        r.SetResourceReference(Shape.FillProperty, "PrimaryText");
        return r;
    }
}
