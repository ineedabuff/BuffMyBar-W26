using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuffBar.Services;

/// <summary>Instantané météo.</summary>
public readonly record struct WeatherInfo(bool Ok, int TempC, int FeelsLikeC, string Description, int Code)
{
    public static WeatherInfo Failed => new(false, 0, 0, string.Empty, 0);
}

/// <summary>
/// Récupère la météo depuis wttr.in au format JSON (j1), en français.
/// HttpClient + System.Text.Json natifs : aucune dépendance externe.
/// </summary>
public sealed class WeatherService
{
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
            JsonElement cur = doc.RootElement.GetProperty("current_condition")[0];

            int temp = ParseInt(cur, "temp_C");
            int feels = ParseInt(cur, "FeelsLikeC");
            int code = ParseInt(cur, "weatherCode");
            string desc = ReadDescription(cur);

            return new WeatherInfo(true, temp, feels, desc, code);
        }
        catch
        {
            return WeatherInfo.Failed;
        }
    }

    private static int ParseInt(JsonElement element, string name)
        => element.TryGetProperty(name, out var p) && int.TryParse(p.GetString(), out int v) ? v : 0;

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
