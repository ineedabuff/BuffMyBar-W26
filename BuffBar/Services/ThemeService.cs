using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using BuffBar.Core;

namespace BuffBar.Services;

/// <summary>
/// Adapte la palette de BuffBar au thème Windows 11, comme la barre des tâches :
///  - mode clair / sombre ("Mode Windows" = SystemUsesLightTheme) ;
///  - couleur d'accentuation sur la barre des tâches (ColorPrevalence) ;
/// avec mise à jour en direct (sondage léger toutes les 4 s).
///
/// La mise à jour modifie la COULEUR des pinceaux existants (sans les remplacer),
/// donc tous les modules se rafraîchissent automatiquement. Les couleurs sémantiques
/// figées (REC, paliers de volume) ne sont pas touchées.
/// </summary>
public static class ThemeService
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private static DispatcherTimer? _timer;
    private static string _signature = string.Empty;

    /// <summary>Vrai quand un fond acrylique translucide est actif (voir BackdropService).</summary>
    public static bool AcrylicActive { get; private set; }

    /// <summary>
    /// Active le mode acrylique : les fonds Bar/Module deviennent transparents
    /// (l'acrylique du système transparaît) et le survol passe en translucide.
    /// </summary>
    public static void EnableAcrylic()
    {
        AcrylicActive = true;
        ApplyAcrylicBackgrounds();
    }

    private static void ApplyAcrylicBackgrounds()
    {
        bool light = ReadDword(PersonalizeKey, "SystemUsesLightTheme", 0) == 1;

        SetArgb("BarBackground", 0x00, 0x00, 0x00, 0x00);
        SetArgb("ModuleBackground", 0x00, 0x00, 0x00, 0x00);

        if (light)
            SetArgb("HoverBackground", 0x18, 0x00, 0x00, 0x00);
        else
            SetArgb("HoverBackground", 0x22, 0xFF, 0xFF, 0xFF);
    }

    /// <summary>Point d'entrée au démarrage : applique le thème de la config.</summary>
    public static void Start() => ApplyConfigTheme();

    /// <summary>Applique le thème nommé dans la config (buff / cyber / windows).</summary>
    public static void ApplyConfigTheme()
    {
        ThemePalette p = ConfigService.LoadTheme(ConfigService.Current.Theme);
        if (p.FollowWindows)
        {
            StartPolling();
            Apply(force: true);        // suit Windows en direct
        }
        else
        {
            StopPolling();
            ApplyPalette(p);           // palette explicite (buff, cyber, ...)
        }
    }

    /// <summary>Change le thème, l'enregistre et l'applique en direct.</summary>
    public static void SetTheme(string name)
    {
        Config c = ConfigService.Current;
        if (c.Theme == name) return;
        c.Theme = name;
        ConfigService.Save(c);
        ApplyConfigTheme();
    }

    private static void ApplyPalette(ThemePalette p)
    {
        _signature = string.Empty;   // pour forcer un nouvel Apply si on revient à « windows »

        Set("BarBackground", Hex(p.BarBackground));
        Set("ModuleBackground", Hex(p.ModuleBackground));
        Set("ModuleBorderBrush", Hex(p.ModuleBorder));
        Set("HoverBackground", Hex(p.HoverBackground));
        Set("HoverBorderBrush", Hex(p.HoverBorder));
        Set("PrimaryText", Hex(p.PrimaryText));
        Set("SubtleText", Hex(p.SubtleText));
        Set("AccentBrush", Hex(p.Accent));

        if (AcrylicActive)
            ApplyAcrylicBackgrounds();
    }

    private static void StartPolling()
    {
        if (_timer != null) return;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(4)
        };
        _timer.Tick += (_, _) =>
        {
            if (ConfigService.LoadTheme(ConfigService.Current.Theme).FollowWindows)
                Apply(force: false);
        };
        _timer.Start();
    }

    private static void StopPolling()
    {
        _timer?.Stop();
        _timer = null;
    }

    private static void Apply(bool force)
    {
        bool light = ReadDword(PersonalizeKey, "SystemUsesLightTheme", 0) == 1;
        bool prevalence = ReadDword(PersonalizeKey, "ColorPrevalence", 0) == 1;
        Color accent = ReadAccent();

        string sig = $"{light}|{prevalence}|{accent}";
        if (!force && sig == _signature) return;
        _signature = sig;

        Palette p = Compute(light, prevalence, accent);

        Set("BarBackground", p.Bg);
        Set("ModuleBackground", p.Bg);
        Set("ModuleBorderBrush", p.Border);
        Set("HoverBackground", p.Hover);
        Set("HoverBorderBrush", p.HoverBorder);
        Set("PrimaryText", p.Text);
        Set("SubtleText", p.Subtle);
        if (!BarConfig.KeepBuffAccent)
            Set("AccentBrush", accent);

        // En mode acrylique, les fonds Bar/Module/Survol restent translucides.
        if (AcrylicActive)
            ApplyAcrylicBackgrounds();
    }

    // ---- Calcul de la palette ----

    private struct Palette
    {
        public Color Bg, Border, Hover, HoverBorder, Text, Subtle;
    }

    private static Palette Compute(bool light, bool prevalence, Color accent)
    {
        if (prevalence)
        {
            Color bg = light ? accent : Scale(accent, 0.55f);
            bool darkBg = Lum(bg) < 0.5;
            Color text = darkBg ? Rgb(0xFF, 0xFF, 0xFF) : Rgb(0x1A, 0x1A, 0x1A);
            return new Palette
            {
                Bg = bg,
                Text = text,
                Subtle = Blend(text, bg, 0.30f),
                Border = darkBg ? Lighten(bg, 0.18f) : Darken(bg, 0.18f),
                Hover = darkBg ? Lighten(bg, 0.10f) : Darken(bg, 0.08f),
                HoverBorder = darkBg ? Lighten(bg, 0.30f) : Darken(bg, 0.28f)
            };
        }

        if (light)
        {
            return new Palette
            {
                Bg = Rgb(0xF2, 0xF2, 0xF2),
                Text = Rgb(0x1A, 0x1A, 0x1A),
                Subtle = Rgb(0x5C, 0x5C, 0x5C),
                Border = Rgb(0xD0, 0xD0, 0xD0),
                Hover = Rgb(0xE5, 0xE5, 0xE5),
                HoverBorder = Rgb(0xBF, 0xBF, 0xBF)
            };
        }

        // Sombre (proche de la barre des tâches Win11).
        return new Palette
        {
            Bg = Rgb(0x1C, 0x1C, 0x1C),
            Text = Rgb(0xFF, 0xFF, 0xFF),
            Subtle = Rgb(0xB8, 0xB8, 0xB8),
            Border = Rgb(0x33, 0x33, 0x33),
            Hover = Rgb(0x2D, 0x2D, 0x2D),
            HoverBorder = Rgb(0x45, 0x45, 0x45)
        };
    }

    // ---- Lecture des réglages Windows ----

    private static int ReadDword(string path, string name, int fallback)
    {
        try
        {
            using RegistryKey? k = Registry.CurrentUser.OpenSubKey(path);
            return k?.GetValue(name) is int v ? v : fallback;
        }
        catch { return fallback; }
    }

    private static Color ReadAccent()
    {
        try
        {
            var ui = new Windows.UI.ViewManagement.UISettings();
            var a = ui.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
            return Color.FromRgb(a.R, a.G, a.B);
        }
        catch
        {
            return Rgb(0x00, 0x78, 0xD4);  // bleu Windows par défaut
        }
    }

    // ---- Application aux ressources (mutation en place) ----

    private static void Set(string key, Color c)
    {
        if (Application.Current.Resources[key] is SolidColorBrush b && !b.IsFrozen)
            b.Color = c;
        else
            Application.Current.Resources[key] = new SolidColorBrush(c);
    }

    private static void SetArgb(string key, int a, int r, int g, int b)
        => Set(key, Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b));

    // ---- Utilitaires couleur ----

    private static Color Rgb(int r, int g, int b) => Color.FromRgb((byte)r, (byte)g, (byte)b);

    /// <summary>Convertit "#RRGGBB" ou "#AARRGGBB" en Color (repli : magenta visible).</summary>
    private static Color Hex(string s)
    {
        try
        {
            string h = s.TrimStart('#');
            if (h.Length == 6)
                return Color.FromRgb(
                    byte.Parse(h.Substring(0, 2), NumberStyles.HexNumber),
                    byte.Parse(h.Substring(2, 2), NumberStyles.HexNumber),
                    byte.Parse(h.Substring(4, 2), NumberStyles.HexNumber));
            if (h.Length == 8)
                return Color.FromArgb(
                    byte.Parse(h.Substring(0, 2), NumberStyles.HexNumber),
                    byte.Parse(h.Substring(2, 2), NumberStyles.HexNumber),
                    byte.Parse(h.Substring(4, 2), NumberStyles.HexNumber),
                    byte.Parse(h.Substring(6, 2), NumberStyles.HexNumber));
        }
        catch { /* repli ci-dessous */ }
        return Color.FromRgb(0xFF, 0x00, 0xFF);
    }
    private static double Lum(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
    private static Color Scale(Color c, float f) => Rgb(Cl(c.R * f), Cl(c.G * f), Cl(c.B * f));
    private static Color Lighten(Color c, float t) => Blend(c, Rgb(0xFF, 0xFF, 0xFF), t);
    private static Color Darken(Color c, float t) => Blend(c, Rgb(0, 0, 0), t);

    private static Color Blend(Color a, Color b, float t)
        => Rgb(Cl(a.R + (b.R - a.R) * t), Cl(a.G + (b.G - a.G) * t), Cl(a.B + (b.B - a.B) * t));

    private static int Cl(double v) => (int)Math.Max(0, Math.Min(255, v));
}
