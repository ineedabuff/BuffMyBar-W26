using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BuffBar.Widgets.Bluetooth;

/// <summary>
/// Rune Bluetooth dessinée nativement (un seul tracé) — suit le thème (PrimaryText).
/// </summary>
public sealed class BluetoothIcon : Grid
{
    public BluetoothIcon()
    {
        var stage = new Canvas { Width = 100, Height = 100, Background = Brushes.Transparent };

        var rune = new Polyline
        {
            Points = new PointCollection
            {
                new(30, 35), new(70, 65), new(50, 80), new(50, 20), new(70, 35), new(30, 65)
            },
            StrokeThickness = 9,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        rune.SetResourceReference(Shape.StrokeProperty, "PrimaryText");

        stage.Children.Add(rune);
        Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = stage });
    }
}
