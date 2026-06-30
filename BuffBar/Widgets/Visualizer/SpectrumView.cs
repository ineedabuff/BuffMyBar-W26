using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BuffBar.Widgets.Visualizer;

/// <summary>
/// Lightweight spectrum renderer.
/// Sprint-002/003 rules:
/// - normal black background: white bars;
/// - external accent background #ddff24: black bars;
/// - no border and no private widget background.
/// </summary>
public sealed class SpectrumView : FrameworkElement
{
    private float[] _levels = Array.Empty<float>();
    private const double Gap = 1.0;

    private static readonly SolidColorBrush WhiteBars = CreateFrozenBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush BlackBars = CreateFrozenBrush(Color.FromRgb(0x00, 0x00, 0x00));

    public void SetLevels(float[] levels)
    {
        _levels = levels ?? Array.Empty<float>();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        int n = _levels.Length;

        if (n == 0 || w <= 0 || h <= 0)
            return;

        Brush barBrush = IsOnBuffAccentBackground() ? BlackBars : WhiteBars;

        double barWidth = (w - (n - 1) * Gap) / n;
        if (barWidth < 1)
            barWidth = 1;

        for (int i = 0; i < n; i++)
        {
            double lvl = _levels[i];
            if (lvl <= 0)
                continue;
            if (lvl > 1)
                lvl = 1;

            double barHeight = lvl * h;
            double x = i * (barWidth + Gap);
            dc.DrawRectangle(barBrush, null, new Rect(x, h - barHeight, barWidth, barHeight));
        }
    }

    private bool IsOnBuffAccentBackground()
    {
        DependencyObject? current = this;

        for (int i = 0; i < 16 && current != null; i++)
        {
            Brush? background = TryGetBackground(current);
            if (background is SolidColorBrush solid && IsBuffAccent(solid.Color))
                return true;

            current = GetParent(current);
        }

        return false;
    }

    private static Brush? TryGetBackground(DependencyObject obj)
    {
        if (obj is Border border)
            return border.Background;

        if (obj is Panel panel)
            return panel.Background;

        if (obj is Control control)
            return control.Background;

        return null;
    }

    private static DependencyObject? GetParent(DependencyObject obj)
    {
        try
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(obj);
            if (parent != null)
                return parent;
        }
        catch
        {
            // Some logical-only objects are not visual children.
        }

        return LogicalTreeHelper.GetParent(obj);
    }

    private static bool IsBuffAccent(Color color)
    {
        return Math.Abs(color.R - 0xDD) <= 2
            && Math.Abs(color.G - 0xFF) <= 2
            && Math.Abs(color.B - 0x24) <= 2;
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}