using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BuffBar.Core;

namespace BuffBar.Services;

/// <summary>
/// Applique les couleurs actives de BuffMyBar.
/// En mode "windows", cette classe suit le vrai theme Windows 11 via WindowsThemeService.
/// </summary>
public static class ThemeService
{
    private static bool _followingWindows;

    public static event Action? Applied;

    /// <summary>Point d'entree au demarrage.</summary>
    public static void Start()
    {
        WindowsThemeService.Start();
        WindowsThemeService.Changed += OnWindowsThemeChanged;
        ApplyConfigTheme();
    }

    public static void Stop()
    {
        WindowsThemeService.Changed -= OnWindowsThemeChanged;
        WindowsThemeService.Stop();
    }

    /// <summary>Applique le theme nomme dans settings.json.</summary>
    public static void ApplyConfigTheme()
    {
        ThemePalette p = ConfigService.LoadTheme(ConfigService.Current.Theme);
        _followingWindows = p.FollowWindows;

        if (_followingWindows)
            ApplyWindows(WindowsThemeService.Current);
        else
            ApplyPalette(p);
    }

    /// <summary>Change le theme, sauvegarde et applique en direct.</summary>
    public static void SetTheme(string name)
    {
        Config c = ConfigService.Current;
        if (string.Equals(c.Theme, name, StringComparison.OrdinalIgnoreCase))
        {
            ApplyConfigTheme();
            return;
        }

        c.Theme = name;
        ConfigService.Save(c);
        ApplyConfigTheme();
    }

    private static void OnWindowsThemeChanged(WindowsThemeSnapshot snapshot)
    {
        if (_followingWindows)
            ApplyWindows(snapshot);
    }

    private static void ApplyPalette(ThemePalette p)
    {
        Set("BarBackground", Hex(p.BarBackground));
        Set("ModuleBackground", Hex(p.ModuleBackground));
        Set("ModuleBorderBrush", Hex(p.ModuleBorder));
        Set("HoverBackground", Hex(p.HoverBackground));
        Set("HoverBorderBrush", Hex(p.HoverBorder));
        Set("PrimaryText", Hex(p.PrimaryText));
        Set("SubtleText", Hex(p.SubtleText));
        Set("AccentBrush", Hex(p.Accent));
        Applied?.Invoke();
    }

    private static void ApplyWindows(WindowsThemeSnapshot w)
    {
        Palette p = ComputeWindowsPalette(w);

        Set("BarBackground", p.BarBackground);
        Set("ModuleBackground", p.ModuleBackground);
        Set("ModuleBorderBrush", p.ModuleBorder);
        Set("HoverBackground", p.HoverBackground);
        Set("HoverBorderBrush", p.HoverBorder);
        Set("PrimaryText", p.PrimaryText);
        Set("SubtleText", p.SubtleText);

        if (!BarConfig.KeepBuffAccent)
            Set("AccentBrush", w.AccentColor);

        Applied?.Invoke();
    }

    private readonly struct Palette
    {
        public required Color BarBackground { get; init; }
        public required Color ModuleBackground { get; init; }
        public required Color ModuleBorder { get; init; }
        public required Color HoverBackground { get; init; }
        public required Color HoverBorder { get; init; }
        public required Color PrimaryText { get; init; }
        public required Color SubtleText { get; init; }
    }

    private static Palette ComputeWindowsPalette(WindowsThemeSnapshot w)
    {
        if (w.AccentOnTaskbar)
        {
            Color bg = w.SystemLight ? Lighten(w.AccentColor, 0.05f) : Darken(w.AccentColor, 0.42f);
            bool dark = Luminance(bg) < 0.45;
            Color text = dark ? Rgb(0xFF, 0xFF, 0xFF) : Rgb(0x18, 0x18, 0x18);
            return new Palette
            {
                BarBackground = bg,
                ModuleBackground = bg,
                ModuleBorder = bg,
                HoverBackground = dark ? Lighten(bg, 0.12f) : Darken(bg, 0.07f),
                HoverBorder = dark ? Lighten(bg, 0.18f) : Darken(bg, 0.13f),
                PrimaryText = text,
                SubtleText = Blend(text, bg, 0.34f)
            };
        }

        if (w.SystemLight)
        {
            return new Palette
            {
                BarBackground = Rgb(0xF3, 0xF3, 0xF3),
                ModuleBackground = Rgb(0xF3, 0xF3, 0xF3),
                ModuleBorder = Rgb(0xF3, 0xF3, 0xF3),
                HoverBackground = Rgb(0xE8, 0xE8, 0xE8),
                HoverBorder = Rgb(0xD6, 0xD6, 0xD6),
                PrimaryText = Rgb(0x1B, 0x1B, 0x1B),
                SubtleText = Rgb(0x5F, 0x5F, 0x5F)
            };
        }

        return new Palette
        {
            BarBackground = Rgb(0x1F, 0x1F, 0x1F),
            ModuleBackground = Rgb(0x1F, 0x1F, 0x1F),
            ModuleBorder = Rgb(0x1F, 0x1F, 0x1F),
            HoverBackground = Rgb(0x2B, 0x2B, 0x2B),
            HoverBorder = Rgb(0x3A, 0x3A, 0x3A),
            PrimaryText = Rgb(0xF7, 0xF7, 0xF7),
            SubtleText = Rgb(0xB7, 0xB7, 0xB7)
        };
    }

    private static void Set(string key, Color color)
    {
        if (Application.Current?.Resources[key] is SolidColorBrush brush && !brush.IsFrozen)
            brush.Color = color;
        else if (Application.Current != null)
            Application.Current.Resources[key] = new SolidColorBrush(color);
    }

    private static Color Hex(string s)
    {
        try
        {
            string h = s.Trim().TrimStart('#');
            if (h.Length == 6)
            {
                return Color.FromRgb(
                    byte.Parse(h[..2], NumberStyles.HexNumber),
                    byte.Parse(h.Substring(2, 2), NumberStyles.HexNumber),
                    byte.Parse(h.Substring(4, 2), NumberStyles.HexNumber));
            }
            if (h.Length == 8)
            {
                return Color.FromArgb(
                    byte.Parse(h[..2], NumberStyles.HexNumber),
                    byte.Parse(h.Substring(2, 2), NumberStyles.HexNumber),
                    byte.Parse(h.Substring(4, 2), NumberStyles.HexNumber),
                    byte.Parse(h.Substring(6, 2), NumberStyles.HexNumber));
            }
        }
        catch
        {
            // couleur de diagnostic visible
        }

        return Color.FromRgb(0xFF, 0x00, 0xFF);
    }

    private static Color Rgb(int r, int g, int b) => Color.FromRgb((byte)r, (byte)g, (byte)b);

    private static double Luminance(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;

    private static Color Lighten(Color c, float amount) => Blend(c, Rgb(0xFF, 0xFF, 0xFF), amount);

    private static Color Darken(Color c, float amount) => Blend(c, Rgb(0x00, 0x00, 0x00), amount);

    private static Color Blend(Color a, Color b, float t)
        => Rgb(Clamp(a.R + (b.R - a.R) * t), Clamp(a.G + (b.G - a.G) * t), Clamp(a.B + (b.B - a.B) * t));

    private static int Clamp(double value) => (int)Math.Max(0, Math.Min(255, value));
}
