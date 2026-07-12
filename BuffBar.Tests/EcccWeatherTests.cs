using BuffBar.Services;
using Xunit;

namespace BuffBar.Tests;

/// <summary>
/// Mapping des « iconCode » d'Environnement Canada vers nos conditions normalisées.
/// Logique pure : un filet contre les régressions de correspondance.
/// </summary>
public class EcccWeatherTests
{
    [Theory]
    [InlineData(0, WeatherCondition.Sunny)]
    [InlineData(1, WeatherCondition.Sunny)]
    [InlineData(30, WeatherCondition.Sunny)]   // nuit
    [InlineData(31, WeatherCondition.Sunny)]   // nuit
    [InlineData(2, WeatherCondition.PartlyCloudy)]
    [InlineData(3, WeatherCondition.PartlyCloudy)]
    [InlineData(10, WeatherCondition.Overcast)]
    [InlineData(6, WeatherCondition.Showers)]
    [InlineData(12, WeatherCondition.Rain)]
    [InlineData(28, WeatherCondition.Drizzle)]
    [InlineData(15, WeatherCondition.Sleet)]
    [InlineData(16, WeatherCondition.Snow)]
    [InlineData(9, WeatherCondition.Thunder)]
    [InlineData(24, WeatherCondition.Fog)]
    [InlineData(43, WeatherCondition.Wind)]
    [InlineData(99, WeatherCondition.Cloudy)]   // inconnu -> défaut
    public void Map_KnownCodes(int code, WeatherCondition expected)
        => Assert.Equal(expected, EcccWeather.Map(code));

    [Theory]
    [InlineData(0, false)]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(39, true)]
    [InlineData(40, false)]
    public void IsNight_Range(int code, bool night)
        => Assert.Equal(night, EcccWeather.IsNight(code));
}
