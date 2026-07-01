using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BuffBar.Services;

/// <summary>
/// Lecture centralisee du theme Windows 11.
/// Suit le mode clair/sombre, la couleur d'accent, la preference de transparence
/// et l'option "Afficher la couleur d'accentuation sur demarrer et la barre des taches".
/// </summary>
public static class WindowsThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string DwmKey = @"Software\Microsoft\Windows\DWM";

    private static DispatcherTimer? _poller;
    private static WindowsThemeSnapshot _current = ReadSnapshot();

    public static event Action<WindowsThemeSnapshot>? Changed;

    public static WindowsThemeSnapshot Current => _current;

    public static void Start()
    {
        Stop();
        _current = ReadSnapshot();

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        // UserPreferenceChanged capte la plupart des bascules clair/sombre en
        // direct ; ce sondage de repli sert surtout aux changements de couleur
        // d'accent. 5 s au lieu de 2 s : moins de réveils CPU sur portable.
        _poller = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _poller.Tick += (_, _) => Refresh();
        _poller.Start();
    }

    public static void Stop()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _poller?.Stop();
        _poller = null;
    }

    public static void Refresh(bool force = false)
    {
        WindowsThemeSnapshot next = ReadSnapshot();
        if (!force && next.Equals(_current))
            return;

        _current = next;
        Application.Current?.Dispatcher.BeginInvoke(new Action(() => Changed?.Invoke(_current)));
    }

    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        => Refresh(force: true);

    private static WindowsThemeSnapshot ReadSnapshot()
    {
        bool systemLight = ReadDword(PersonalizeKey, "SystemUsesLightTheme", 0) == 1;
        bool appsLight = ReadDword(PersonalizeKey, "AppsUseLightTheme", systemLight ? 1 : 0) == 1;
        bool transparency = ReadDword(PersonalizeKey, "EnableTransparency", 1) == 1;

        int prevalencePersonalize = ReadDword(PersonalizeKey, "ColorPrevalence", 0);
        int prevalenceDwm = ReadDword(DwmKey, "ColorPrevalence", prevalencePersonalize);
        bool accentOnTaskbar = prevalencePersonalize == 1 || prevalenceDwm == 1;

        Color accent = ReadAccentColor();

        return new WindowsThemeSnapshot(systemLight, appsLight, transparency, accentOnTaskbar, accent);
    }

    private static int ReadDword(string path, string name, int fallback)
    {
        try
        {
            using RegistryKey? k = Registry.CurrentUser.OpenSubKey(path);
            object? value = k?.GetValue(name);
            return value is int i ? i : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static Color ReadAccentColor()
    {
        try
        {
            int hr = DwmGetColorizationColor(out uint colorization, out _);
            if (hr == 0)
            {
                byte r = (byte)((colorization >> 16) & 0xFF);
                byte g = (byte)((colorization >> 8) & 0xFF);
                byte b = (byte)(colorization & 0xFF);
                return Color.FromRgb(r, g, b);
            }
        }
        catch
        {
            // repli registre ci-dessous
        }

        try
        {
            int raw = ReadDword(DwmKey, "AccentColor", unchecked((int)0xFF0078D4));
            byte b = (byte)((raw >> 16) & 0xFF);
            byte g = (byte)((raw >> 8) & 0xFF);
            byte r = (byte)(raw & 0xFF);
            return Color.FromRgb(r, g, b);
        }
        catch
        {
            return Color.FromRgb(0x00, 0x78, 0xD4);
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);
}

public readonly record struct WindowsThemeSnapshot(
    bool SystemLight,
    bool AppsLight,
    bool TransparencyEnabled,
    bool AccentOnTaskbar,
    Color AccentColor);
