using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BuffBar.Widgets.Volume;

/// <summary>
/// Icône de volume dessinée nativement : haut-parleur + ondes (1 ou 2 selon le
/// niveau), barre diagonale si muet. Suit le thème (PrimaryText), rouge à 16 %+.
/// </summary>
public sealed class VolumeIcon : Grid
{
    private static readonly Brush Alert = Frozen(0xFF, 0x31, 0x31);

    private readonly Canvas _stage = new() { Width = 100, Height = 100, Background = Brushes.Transparent };

    private readonly Polygon _speaker = new()
    {
        Points = new PointCollection
        {
            new(12, 40), new(30, 40), new(50, 22), new(50, 78), new(30, 60), new(12, 60)
        }
    };

    private readonly Path _wave1;
    private readonly Path _wave2;
    private readonly Line _slash = new()
    {
        X1 = 20, Y1 = 22, X2 = 80, Y2 = 82,
        StrokeThickness = 8, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
        Visibility = Visibility.Collapsed
    };

    public VolumeIcon()
    {
        _wave1 = Arc(60, 36, 60, 64, 20);
        _wave2 = Arc(70, 26, 70, 74, 30);

        _stage.Children.Add(_speaker);
        _stage.Children.Add(_wave1);
        _stage.Children.Add(_wave2);
        _stage.Children.Add(_slash);

        Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = _stage });
    }

    public void Set(int percent, bool muted)
    {
        bool alert = !muted && percent >= 16;
        Brush color = alert ? Alert : (Brush)Application.Current.Resources["PrimaryText"];

        _speaker.Fill = color;
        _wave1.Stroke = color;
        _wave2.Stroke = color;
        _slash.Stroke = color;

        _wave1.Visibility = !muted && percent > 0 ? Visibility.Visible : Visibility.Collapsed;
        _wave2.Visibility = !muted && percent > 50 ? Visibility.Visible : Visibility.Collapsed;
        _slash.Visibility = muted ? Visibility.Visible : Visibility.Collapsed;
    }

    private static Path Arc(double x1, double y1, double x2, double y2, double r)
    {
        var fig = new PathFigure { StartPoint = new Point(x1, y1) };
        fig.Segments.Add(new ArcSegment(new Point(x2, y2), new Size(r, r), 0, false, SweepDirection.Clockwise, true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        return new Path
        {
            Data = geo,
            StrokeThickness = 8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
    }

    private static SolidColorBrush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
