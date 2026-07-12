using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using BuffBar.Services;

namespace BuffBar.Widgets.Weather;

/// <summary>
/// Icône météo animée, 100 % WPF natif (formes + storyboards), sans dépendance ni
/// asset externe. Dessinée dans un repère 100×100 mis à l'échelle par un Viewbox,
/// elle suit le thème (AccentBrush pour le soleil/lune, PrimaryText pour le reste).
///
/// Animations volontairement lentes et légères (rotation, dérive, chute) : le coût
/// est porté par le fil de composition, pas par le thread UI.
/// </summary>
public sealed class AnimatedWeatherIcon : Grid
{
    private const double S = 100; // côté du repère

    private readonly Canvas _stage = new() { Width = S, Height = S, Background = Brushes.Transparent };
    private readonly List<(IAnimatable Obj, DependencyProperty Prop)> _anims = new();

    public AnimatedWeatherIcon()
    {
        Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = _stage });
    }

    /// <summary>Reconstruit l'icône pour la condition donnée (jour/nuit).</summary>
    public void Set(WeatherCondition condition, bool night)
    {
        StopAll();
        _stage.Children.Clear();

        switch (condition)
        {
            case WeatherCondition.Sunny:
                if (night) AddMoon(50, 50, 1.0); else AddSun(50, 50, 1.0);
                break;
            case WeatherCondition.PartlyCloudy:
                if (night) AddMoon(38, 40, 0.6); else AddSun(38, 40, 0.62);
                AddCloud(56, 54, 0.95);
                break;
            case WeatherCondition.Cloudy:
            case WeatherCondition.Overcast:
                AddCloud(58, 44, 0.7, "SubtleText");
                AddCloud(46, 54, 1.0);
                break;
            case WeatherCondition.Fog:
                AddCloud(50, 36, 0.85);
                AddFog();
                break;
            case WeatherCondition.Drizzle:
                AddCloud(50, 40, 1.0);
                AddPrecip(rain: true, count: 3);
                break;
            case WeatherCondition.Rain:
            case WeatherCondition.Showers:
                AddCloud(50, 40, 1.0);
                AddPrecip(rain: true, count: 4);
                break;
            case WeatherCondition.Snow:
                AddCloud(50, 40, 1.0);
                AddPrecip(rain: false, count: 4);
                break;
            case WeatherCondition.Sleet:
                AddCloud(50, 40, 1.0);
                AddPrecip(rain: true, count: 2);
                AddPrecip(rain: false, count: 2);
                break;
            case WeatherCondition.Thunder:
                AddCloud(50, 38, 1.0);
                AddBolt();
                break;
            case WeatherCondition.Wind:
                AddWind();
                break;
            default:
                AddCloud(50, 48, 1.1);
                break;
        }
    }

    // ---- Éléments ----------------------------------------------------------

    private void AddSun(double cx, double cy, double scale)
    {
        var group = new Canvas { RenderTransform = new RotateTransform(0, cx, cy) };

        for (int i = 0; i < 8; i++)
        {
            double a = i * Math.PI / 4;
            var ray = new Line
            {
                X1 = cx + Math.Cos(a) * 24 * scale,
                Y1 = cy + Math.Sin(a) * 24 * scale,
                X2 = cx + Math.Cos(a) * 32 * scale,
                Y2 = cy + Math.Sin(a) * 32 * scale,
                StrokeThickness = 4 * scale,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            Themed(ray, "AccentBrush", stroke: true);
            group.Children.Add(ray);
        }

        group.Children.Add(Disc(cx, cy, 17 * scale, "AccentBrush"));
        _stage.Children.Add(group);

        Animate((RotateTransform)group.RenderTransform, RotateTransform.AngleProperty,
            Loop(0, 360, 18));
    }

    private void AddMoon(double cx, double cy, double scale)
    {
        double r = 20 * scale;
        var outer = new EllipseGeometry(new Point(cx, cy), r, r);
        var inner = new EllipseGeometry(new Point(cx + r * 0.5, cy - r * 0.28), r * 0.9, r * 0.9);
        var crescent = new Path { Data = new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner) };
        Themed(crescent, "AccentBrush");
        _stage.Children.Add(crescent);

        // Petite étoile qui scintille.
        Ellipse star = Disc(cx + 26 * scale, cy - 18 * scale, 2.4 * scale, "PrimaryText");
        _stage.Children.Add(star);
        Animate(star, OpacityProperty, Pulse(0.2, 1.0, 1.4));
    }

    /// <summary>Nuage dérivant, dessiné autour de (cx, cy).</summary>
    private void AddCloud(double cx, double cy, double scale, string key = "PrimaryText")
    {
        var cloud = new Canvas { RenderTransform = new TranslateTransform() };

        var body = new Rectangle
        {
            Width = 44 * scale,
            Height = 16 * scale,
            RadiusX = 8 * scale,
            RadiusY = 8 * scale
        };
        Canvas.SetLeft(body, cx - 22 * scale);
        Canvas.SetTop(body, cy + 1 * scale);
        Themed(body, key);
        cloud.Children.Add(body);

        cloud.Children.Add(Disc(cx - 13 * scale, cy + 4 * scale, 9 * scale, key));
        cloud.Children.Add(Disc(cx, cy - 5 * scale, 13 * scale, key));
        cloud.Children.Add(Disc(cx + 14 * scale, cy + 3 * scale, 10 * scale, key));

        _stage.Children.Add(cloud);

        Animate((TranslateTransform)cloud.RenderTransform, TranslateTransform.XProperty,
            Sway(-3, 3, 5));
    }

    private void AddPrecip(bool rain, int count)
    {
        double top = 58;
        double fall = 24;

        for (int i = 0; i < count; i++)
        {
            double x = 34 + i * (32.0 / Math.Max(1, count));
            double begin = i * 0.24;
            var move = new TranslateTransform();

            Shape drop = rain
                ? new Line { X1 = x, Y1 = top, X2 = x, Y2 = top + 7, StrokeThickness = 3, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round }
                : Disc(x, top, 3, "PrimaryText");

            Themed(drop, "PrimaryText", stroke: rain);
            drop.RenderTransform = move;
            _stage.Children.Add(drop);

            double dur = rain ? 0.9 : 1.7;
            Animate(move, TranslateTransform.YProperty, Loop(0, fall, dur, begin));
            Animate(drop, OpacityProperty, Fade(dur, begin));
            if (!rain)
                Animate(move, TranslateTransform.XProperty, Sway(-3, 3, dur * 1.3, begin));
        }
    }

    private void AddBolt()
    {
        var bolt = new Polygon
        {
            Points = new PointCollection { new(52, 54), new(44, 74), new(50, 74), new(46, 88), new(60, 66), new(53, 66), new(58, 54) }
        };
        Themed(bolt, "AccentBrush");
        _stage.Children.Add(bolt);

        var blink = new DoubleAnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.25, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.10))));
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.25, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.22))));
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.34))));
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.25, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.46))));
        blink.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.25, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.4))));
        Animate(bolt, OpacityProperty, blink);
    }

    private void AddFog()
    {
        double[] ys = { 60, 72, 84 };
        for (int i = 0; i < ys.Length; i++)
        {
            var bar = new Rectangle { Width = 54 - i * 6, Height = 5, RadiusX = 2.5, RadiusY = 2.5, RenderTransform = new TranslateTransform() };
            Canvas.SetLeft(bar, 23 + i * 3);
            Canvas.SetTop(bar, ys[i]);
            Themed(bar, "SubtleText");
            _stage.Children.Add(bar);
            Animate((TranslateTransform)bar.RenderTransform, TranslateTransform.XProperty, Sway(-6, 6, 4 + i, i * 0.4));
        }
    }

    private void AddWind()
    {
        double[] ys = { 38, 54, 70 };
        double[] widths = { 46, 58, 40 };
        for (int i = 0; i < ys.Length; i++)
        {
            var bar = new Rectangle { Width = widths[i], Height = 5, RadiusX = 2.5, RadiusY = 2.5, RenderTransform = new TranslateTransform() };
            Canvas.SetLeft(bar, 22);
            Canvas.SetTop(bar, ys[i]);
            Themed(bar, i == 1 ? "AccentBrush" : "PrimaryText");
            _stage.Children.Add(bar);
            Animate((TranslateTransform)bar.RenderTransform, TranslateTransform.XProperty, Sway(-5, 9, 3.2 + i * 0.6, i * 0.3));
        }
    }

    // ---- Helpers -----------------------------------------------------------

    private static Ellipse Disc(double cx, double cy, double r, string key)
    {
        var e = new Ellipse { Width = 2 * r, Height = 2 * r };
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e, cy - r);
        Themed(e, key);
        return e;
    }

    private static void Themed(Shape s, string key, bool stroke = false)
        => s.SetResourceReference(stroke ? Shape.StrokeProperty : Shape.FillProperty, key);

    private static DoubleAnimation Loop(double from, double to, double seconds, double beginSeconds = 0) => new(from, to, new Duration(TimeSpan.FromSeconds(seconds)))
    {
        RepeatBehavior = RepeatBehavior.Forever,
        BeginTime = TimeSpan.FromSeconds(beginSeconds)
    };

    private static DoubleAnimation Sway(double from, double to, double seconds, double beginSeconds = 0) => new(from, to, new Duration(TimeSpan.FromSeconds(seconds)))
    {
        RepeatBehavior = RepeatBehavior.Forever,
        AutoReverse = true,
        BeginTime = TimeSpan.FromSeconds(beginSeconds)
    };

    private static DoubleAnimation Pulse(double from, double to, double seconds) => new(from, to, new Duration(TimeSpan.FromSeconds(seconds)))
    {
        RepeatBehavior = RepeatBehavior.Forever,
        AutoReverse = true
    };

    private static DoubleAnimation Fade(double seconds, double beginSeconds) => new(1.0, 0.0, new Duration(TimeSpan.FromSeconds(seconds)))
    {
        RepeatBehavior = RepeatBehavior.Forever,
        BeginTime = TimeSpan.FromSeconds(beginSeconds)
    };

    private void Animate(IAnimatable obj, DependencyProperty prop, AnimationTimeline anim)
    {
        obj.BeginAnimation(prop, anim);
        _anims.Add((obj, prop));
    }

    private void StopAll()
    {
        foreach ((IAnimatable obj, DependencyProperty prop) in _anims)
            obj.BeginAnimation(prop, null);
        _anims.Clear();
    }
}
