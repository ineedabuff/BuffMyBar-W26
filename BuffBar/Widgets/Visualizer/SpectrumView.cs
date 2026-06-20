using System;
using System.Windows;
using System.Windows.Media;

namespace BuffBar.Widgets.Visualizer;

/// <summary>
/// Élément de rendu léger : dessine des barres verticales depuis le bas.
/// Couleur = accent identitaire (#ddff24) ; modifiable via la ressource "AccentBrush".
/// </summary>
public sealed class SpectrumView : FrameworkElement
{
    private float[] _levels = Array.Empty<float>();
    private readonly Brush _brush;
    private const double Gap = 2.0;

    public SpectrumView()
    {
        _brush = (Application.Current?.TryFindResource("PrimaryText") as Brush) ?? Brushes.White;
        if (_brush.CanFreeze) _brush.Freeze();
    }

    /// <summary>Met à jour les niveaux (0..1) et redessine.</summary>
    public void SetLevels(float[] levels)
    {
        _levels = levels;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        int n = _levels.Length;
        if (n == 0 || w <= 0 || h <= 0) return;

        double barWidth = (w - (n - 1) * Gap) / n;
        if (barWidth < 1) barWidth = 1;

        for (int i = 0; i < n; i++)
        {
            double lvl = _levels[i];
            if (lvl <= 0) continue;
            if (lvl > 1) lvl = 1;

            double barHeight = lvl * h;
            double x = i * (barWidth + Gap);
            dc.DrawRectangle(_brush, null, new Rect(x, h - barHeight, barWidth, barHeight));
        }
    }
}
