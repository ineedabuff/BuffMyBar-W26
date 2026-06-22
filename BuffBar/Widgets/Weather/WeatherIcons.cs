using System.Collections.Generic;

namespace BuffBar.Widgets.Weather;

/// <summary>
/// Correspondance code météo WWO (wttr.in) -> glyphe Nerd Font (Font Awesome).
/// Partagée par le module et son flyout.
/// </summary>
public static class WeatherIcons
{
    public const string Sun = "\uF185";    // sun
    public const string Cloud = "\uF0C2";  // cloud
    public const string Rain = "\uF043";   // tint (goutte)
    public const string Flake = "\uF2DC";  // snowflake
    public const string Bolt = "\uF0E7";   // bolt (orage)

    private static readonly HashSet<int> Thunder = new() { 200, 386, 389, 392, 395 };
    private static readonly HashSet<int> Snow = new()
        { 179, 182, 227, 230, 323, 326, 329, 332, 335, 338, 350, 362, 365, 368, 371, 374, 377 };
    private static readonly HashSet<int> Clouds = new() { 116, 119, 122, 143, 248, 260 };

    public static string Glyph(int code)
    {
        if (code == 113) return Sun;
        if (Thunder.Contains(code)) return Bolt;
        if (Snow.Contains(code)) return Flake;
        if (Clouds.Contains(code)) return Cloud;
        if (code >= 176) return Rain;
        return Cloud;
    }
}
