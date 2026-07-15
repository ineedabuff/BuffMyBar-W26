using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BuffBar.Widgets.Uptime;

/// <summary>
/// Petite horloge dessinée nativement (cercle + aiguilles) — suit le thème (PrimaryText).
/// </summary>
public sealed class UptimeIcon : Grid
{
    public UptimeIcon()
    {
        var stage = new Canvas { Width = 100, Height = 100, Background = Brushes.Transparent };

        var dial = new Ellipse { Width = 74, Height = 74, StrokeThickness = 9, Fill = Brushes.Transparent };
        Canvas.SetLeft(dial, 13);
        Canvas.SetTop(dial, 13);
        dial.SetResourceReference(Shape.StrokeProperty, "PrimaryText");

        var hour = Hand(50, 32);   // vers le haut
        var minute = Hand(67, 57); // vers le bas-droite

        stage.Children.Add(dial);
        stage.Children.Add(hour);
        stage.Children.Add(minute);
        Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = stage });
    }

    private static Line Hand(double x2, double y2)
    {
        var line = new Line
        {
            X1 = 50, Y1 = 50, X2 = x2, Y2 = y2,
            StrokeThickness = 8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        line.SetResourceReference(Shape.StrokeProperty, "PrimaryText");
        return line;
    }
}
