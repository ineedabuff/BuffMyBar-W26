using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuffBar.Services;

/// <summary>Prévision d'une journée (pour le flyout météo).</summary>
public readonly record struct ForecastDay(string Label, int MinC, int MaxC, int Code);

/// <summary>Instantané météo + prévisions.</summary>
public readonly record struct WeatherInfo(
    bool Ok,
    int TempC,
    int FeelsLikeC,
    string Description,
    int Code,
    int Humidity,
    int WindKmph,
    string WindDir,
    IReadOnlyList<ForecastDay> Forecast)
{
    public static WeatherInfo Failed =>
        new(false, 0, 0, string.Empty, 0, 0, 0, string.Empty, Array.Empty<ForecastDay>());
}

/// <summary>
/// Récupère la météo depuis wttr.in au format JSON (j1), en français.
/// HttpClient + System.Text.Json natifs : aucune dépendance externe.
/// </summary>
public sealed class WeatherService
{
    private static readonly CultureInfo Culture = new("fr-CA");
    private static readonly HttpClient Http = CreateClient();
    private readonly string _location;

    public WeatherService(string location) => _location = location;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("BuffBar/1.0");
        return client;
    }

    public async Task<WeatherInfo> FetchAsync()
    {
        try
        {
            string url = $"https://wttr.in/{Uri.EscapeDataString(_location)}?format=j1&lang=fr";
            string json = await Http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            JsonElement cur = root.GetProperty("current_condition")[0];

            int temp = ParseInt(cur, "temp_C");
            int feels = ParseInt(cur, "FeelsLikeC");
            int code = ParseInt(cur, "weatherCode");
            int humidity = ParseInt(cur, "humidity");
            int wind = ParseInt(cur, "windspeedKmph");
            string windDir = ReadString(cur, "winddir16Point");
            string desc = ReadDescription(cur);

            List<ForecastDay> forecast = ReadForecast(root);

            return new WeatherInfo(true, temp, feels, desc, code, humidity, wind, windDir, forecast);
        }
        catch
        {
            return WeatherInfo.Failed;
        }
    }

    private static List<ForecastDay> ReadForecast(JsonElement root)
    {
        var days = new List<ForecastDay>();
        if (!root.TryGetProperty("weather", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return days;

        int idx = 0;
        foreach (JsonElement d in arr.EnumerateArray())
        {
            int max = ParseInt(d, "maxtempC");
            int min = ParseInt(d, "mintempC");
            int code = MiddayCode(d);
            string label = idx == 0 ? "Auj." : DayLabel(d);
            days.Add(new ForecastDay(label, min, max, code));

            if (++idx >= 3) break;
        }
        return days;
    }

    // Code météo représentatif de la journée : créneau horaire de midi si présent.
    private static int MiddayCode(JsonElement day)
    {
        if (!day.TryGetProperty("hourly", out var hourly) || hourly.ValueKind != JsonValueKind.Array)
            return 0;

        foreach (JsonElement h in hourly.EnumerateArray())
            if (ReadString(h, "time") == "1200")
                return ParseInt(h, "weatherCode");

        // À défaut, le milieu du tableau.
        int len = hourly.GetArrayLength();
        return len > 0 ? ParseInt(hourly[len / 2], "weatherCode") : 0;
    }

    private static string DayLabel(JsonElement day)
    {
        string date = ReadString(day, "date");
        if (DateTime.TryParse(date, Culture, DateTimeStyles.None, out DateTime dt))
        {
            string s = dt.ToString("ddd", Culture).TrimEnd('.');
            return s.Length > 0 ? char.ToUpper(s[0], Culture) + s[1..] : s;
        }
        return date;
    }

    private static int ParseInt(JsonElement element, string name)
        => element.TryGetProperty(name, out var p) && int.TryParse(p.GetString(), out int v) ? v : 0;

    private static string ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var p) ? p.GetString() ?? string.Empty : string.Empty;

    private static string ReadDescription(JsonElement cur)
    {
        if (cur.TryGetProperty("lang_fr", out var fr)
            && fr.ValueKind == JsonValueKind.Array && fr.GetArrayLength() > 0)
            return fr[0].GetProperty("value").GetString() ?? string.Empty;

        if (cur.TryGetProperty("weatherDesc", out var d)
            && d.ValueKind == JsonValueKind.Array && d.GetArrayLength() > 0)
            return d[0].GetProperty("value").GetString() ?? string.Empty;

        return string.Empty;
    }
}
