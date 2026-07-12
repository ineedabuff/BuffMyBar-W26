namespace BuffBar.Services;

/// <summary>
/// Catégorie météo normalisée, indépendante de la source. Sert à choisir l'icône
/// (glyphe aujourd'hui, animation WPF native ensuite) sans dépendre des codes
/// propres à un fournisseur.
/// </summary>
public enum WeatherCondition
{
    Unknown,
    Sunny,
    PartlyCloudy,
    Cloudy,
    Overcast,
    Fog,
    Drizzle,
    Rain,
    Showers,
    Snow,
    Sleet,
    Thunder,
    Wind
}

/// <summary>
/// Correspondance des « iconCode » d'Environnement Canada (citypage weather) vers
/// nos <see cref="WeatherCondition"/>. Les codes 30–39 sont les variantes de nuit.
/// Logique pure et testable.
/// </summary>
public static class EcccWeather
{
    /// <summary>Vrai pour les codes de nuit (30–39).</summary>
    public static bool IsNight(int iconCode) => iconCode is >= 30 and <= 39;

    public static WeatherCondition Map(int iconCode) => iconCode switch
    {
        0 or 1 or 30 or 31 => WeatherCondition.Sunny,
        2 or 3 or 4 or 5 or 22 or 32 or 33 or 34 or 35 => WeatherCondition.PartlyCloudy,
        10 => WeatherCondition.Overcast,
        6 or 36 => WeatherCondition.Showers,
        11 or 12 or 13 => WeatherCondition.Rain,
        27 or 28 => WeatherCondition.Drizzle,
        7 or 14 or 15 => WeatherCondition.Sleet,
        8 or 16 or 17 or 18 or 25 or 26 or 38 or 40 => WeatherCondition.Snow,
        9 or 19 or 39 or 46 or 47 => WeatherCondition.Thunder,
        23 or 24 or 44 => WeatherCondition.Fog,
        41 or 43 or 48 => WeatherCondition.Wind,
        _ => WeatherCondition.Cloudy
    };
}
