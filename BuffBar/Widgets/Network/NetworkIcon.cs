using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BuffBar.Widgets.Network;

/// <summary>
/// Icône réseau native : maison (IP locale) ou globe (IP publique), au choix.
/// Suit le thème (PrimaryText).
/// </summary>
public sealed class NetworkIcon : Grid
{
    private readonly Canvas _house = new();
    private readonly Canvas _globe = new() { Visibility = Visibility.Collapsed };

    public NetworkIcon()
    {
        var stage = new Canvas { Width = 100, Height = 100, Background = Brushes.Transparent };

        var house = new Polygon
        {
            Points = new PointCollection
            {
                new(50, 18), new(84, 50), new(74, 50), new(74, 82), new(58, 82),
                new(58, 62), new(42, 62), new(42, 82), new(26, 82), new(26, 50), new(16, 50)
            }
        };
        house.SetResourceReference(Shape.FillProperty, "PrimaryText");
        _house.Children.Add(house);

        var circle = Outline(new Ellipse { Width = 68, Height = 68, StrokeThickness = 8 }, 16, 16);
        var meridian = Outline(new Ellipse { Width = 30, Height = 68, StrokeThickness = 6 }, 35, 16);
        var equator = new Line { X1 = 16, Y1 = 50, X2 = 84, Y2 = 50, StrokeThickness = 6 };
        equator.SetResourceReference(Shape.StrokeProperty, "PrimaryText");
        _globe.Children.Add(circle);
        _globe.Children.Add(meridian);
        _globe.Children.Add(equator);

        stage.Children.Add(_house);
        stage.Children.Add(_globe);
        Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = stage });
    }

    public void Set(bool showPublic)
    {
        _house.Visibility = showPublic ? Visibility.Collapsed : Visibility.Visible;
        _globe.Visibility = showPublic ? Visibility.Visible : Visibility.Collapsed;
    }

    private static Ellipse Outline(Ellipse e, double left, double top)
    {
        e.Fill = Brushes.Transparent;
        Canvas.SetLeft(e, left);
        Canvas.SetTop(e, top);
        e.SetResourceReference(Shape.StrokeProperty, "PrimaryText");
        return e;
    }
}
