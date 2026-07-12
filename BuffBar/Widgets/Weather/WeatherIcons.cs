using BuffBar.Services;

namespace BuffBar.Widgets.Weather;

/// <summary>
/// Correspondance <see cref="WeatherCondition"/> -> glyphe Nerd Font (Font Awesome).
/// Les points de code sont construits par cast pour éviter toute ambiguïté d'échappement.
/// Étape provisoire : les icônes animées WPF natives remplaceront ces glyphes.
/// </summary>
public static class WeatherIcons
{
    public static readonly string Sun = char.ToString((char)0xF185);    // sun
    public static readonly string Moon = char.ToString((char)0xF186);   // moon
    public static readonly string Cloud = char.ToString((char)0xF0C2);  // cloud
    public static readonly string Rain = char.ToString((char)0xF043);   // tint (goutte)
    public static readonly string Flake = char.ToString((char)0xF2DC);  // snowflake
    public static readonly string Bolt = char.ToString((char)0xF0E7);   // bolt (orage)

    public static string Glyph(WeatherCondition condition, bool night = false) => condition switch
    {
        WeatherCondition.Sunny => night ? Moon : Sun,
        WeatherCondition.Rain or WeatherCondition.Showers or WeatherCondition.Drizzle => Rain,
        WeatherCondition.Snow or WeatherCondition.Sleet => Flake,
        WeatherCondition.Thunder => Bolt,
        _ => Cloud
    };
}
