using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BuffBar.Widgets.Battery;

/// <summary>
/// Icône de batterie dessinée nativement (formes WPF) — exemple à comparer aux
/// glyphes Nerd Font. Contour + remplissage PROPORTIONNEL au niveau réel (plus
/// parlant que les 5 paliers d'un glyphe), éclair en charge. Suit le thème
/// (PrimaryText), passe au rouge sous 15 %. Nette à toute taille (Viewbox).
/// </summary>
public sealed class BatteryIcon : Grid
{
    private static readonly Brush Low = Frozen(0xFF, 0x31, 0x31);

    private readonly Canvas _stage = new() { Width = 100, Height = 54, Background = Brushes.Transparent };
    private readonly Rectangle _body = new() { Width = 82, Height = 34, RadiusX = 8, RadiusY = 8, StrokeThickness = 5, Fill = Brushes.Transparent };
    private readonly Rectangle _nub = new() { Width = 8, Height = 16, RadiusX = 3, RadiusY = 3 };
    private readonly Rectangle _fill = new() { Height = 20, RadiusX = 3, RadiusY = 3 };

    private readonly Polygon _bolt = new()
    {
        Visibility = Visibility.Collapsed,
        Points = new PointCollection
        {
            new(50, 14), new(38, 32), new(46, 32), new(41, 44),
            new(56, 24), new(48, 24), new(53, 14)
        }
    };

    public BatteryIcon()
    {
        Canvas.SetLeft(_body, 3); Canvas.SetTop(_body, 10);
        Canvas.SetLeft(_nub, 88); Canvas.SetTop(_nub, 19);
        Canvas.SetLeft(_fill, 11); Canvas.SetTop(_fill, 17);

        _body.SetResourceReference(Shape.StrokeProperty, "PrimaryText");
        _nub.SetResourceReference(Shape.FillProperty, "PrimaryText");
        _bolt.SetResourceReference(Shape.FillProperty, "AccentBrush");

        _stage.Children.Add(_body);
        _stage.Children.Add(_nub);
        _stage.Children.Add(_fill);
        _stage.Children.Add(_bolt);

        Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = _stage });
    }

    /// <summary>Met à jour le niveau (0–100) et l'état de charge.</summary>
    public void Set(int percent, bool charging)
    {
        int p = Math.Clamp(percent, 0, 100);

        _fill.Width = 66.0 * p / 100.0;   // largeur interne max ≈ 66

        if (p < 15)
            _fill.Fill = Low;
        else
            _fill.SetResourceReference(Shape.FillProperty, "PrimaryText");

        _bolt.Visibility = charging ? Visibility.Visible : Visibility.Collapsed;
    }

    private static SolidColorBrush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
