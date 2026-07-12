using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BuffBar.Services;

/// <summary>Prévision d'une journée (pour le flyout météo).</summary>
public readonly record struct ForecastDay(string Label, int MinC, int MaxC, WeatherCondition Condition);

/// <summary>Instantané météo + prévisions.</summary>
public readonly record struct WeatherInfo(
    bool Ok,
    int TempC,
    int FeelsLikeC,
    string Description,
    WeatherCondition Condition,
    bool IsNight,
    int Humidity,
    int WindKmph,
    string WindDir,
    IReadOnlyList<ForecastDay> Forecast)
{
    public static WeatherInfo Failed =>
        new(false, 0, 0, string.Empty, WeatherCondition.Unknown, false, 0, 0, string.Empty,
            Array.Empty<ForecastDay>());
}

/// <summary>
/// Météo depuis la source officielle et gratuite d'Environnement Canada
/// (« citypage weather », Datamart MSC). Aucune clé requise.
///
/// Le Datamart est partitionné par date/heure : il n'existe plus d'URL stable par
/// ville. On résout donc la ville en code de site (siteList.xml), puis on parcourt
/// le dossier horaire le plus récent pour trouver le fichier XML de la ville.
/// 100 % natif (HttpClient + System.Xml.Linq).
/// </summary>
public sealed class WeatherService
{
    private const string Root = "https://dd.weather.gc.ca";
    private static readonly CultureInfo Culture = new("fr-CA");
    private static readonly HttpClient Http = CreateClient();

    // Cache partagé nom de ville normalisé -> (province, code de site).
    private static Dictionary<string, (string Prov, string Code)>? _sites;
    private static readonly System.Threading.SemaphoreSlim SitesLock = new(1, 1);

    private readonly string _city;

    public WeatherService(string city) => _city = city;

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
            (string Prov, string Code)? site = await ResolveSiteAsync(_city);
            if (site is null)
                return WeatherInfo.Failed;

            string? url = await FindLatestCityXmlAsync(site.Value.Prov, site.Value.Code);
            if (url is null)
                return WeatherInfo.Failed;

            string xml = await Http.GetStringAsync(url);
            return Parse(xml);
        }
        catch
        {
            return WeatherInfo.Failed;
        }
    }

    // ---- Résolution ville -> site ---------------------------------------------

    private async Task<(string Prov, string Code)?> ResolveSiteAsync(string city)
    {
        Dictionary<string, (string, string)> sites = await EnsureSitesAsync();
        if (sites.Count == 0)
            return null;

        string key = Normalize(city);

        if (sites.TryGetValue(key, out (string, string) exact))
            return exact;

        // Repli : première ville dont le nom contient la requête (ou inversement).
        foreach (KeyValuePair<string, (string, string)> kv in sites)
            if (kv.Key.Contains(key, StringComparison.Ordinal) || key.Contains(kv.Key, StringComparison.Ordinal))
                return kv.Value;

        return null;
    }

    private static async Task<Dictionary<string, (string, string)>> EnsureSitesAsync()
    {
        if (_sites is not null)
            return _sites;

        await SitesLock.WaitAsync();
        try
        {
            if (_sites is not null)
                return _sites;

            var map = new Dictionary<string, (string, string)>(StringComparer.Ordinal);

            foreach (string date in DateCandidates())
            {
                try
                {
                    string xml = await Http.GetStringAsync(
                        $"{Root}/{date}/WXO-DD/citypage_weather/siteList.xml");
                    XDocument doc = XDocument.Parse(xml);

                    foreach (XElement s in doc.Descendants("site"))
                    {
                        string? code = s.Attribute("code")?.Value;
                        string? prov = s.Element("provinceCode")?.Value;
                        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(prov))
                            continue;

                        foreach (string? n in new[] { s.Element("nameFr")?.Value, s.Element("nameEn")?.Value })
                            if (!string.IsNullOrWhiteSpace(n))
                                map[Normalize(n)] = (prov!, code!);
                    }

                    if (map.Count > 0)
                        break;
                }
                catch { /* essaie la date suivante */ }
            }

            _sites = map;
            return _sites;
        }
        finally
        {
            SitesLock.Release();
        }
    }

    // ---- Localisation du fichier XML le plus récent ---------------------------

    private static async Task<string?> FindLatestCityXmlAsync(string prov, string code)
    {
        foreach (string date in DateCandidates())
        {
            string provUrl = $"{Root}/{date}/WXO-DD/citypage_weather/{prov}/";

            string listing;
            try { listing = await Http.GetStringAsync(provUrl); }
            catch { continue; }

            List<string> hours = Regex.Matches(listing, "href=\"(\\d\\d)/\"")
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .OrderByDescending(h => h)
                .ToList();

            // On sonde au plus les 4 dossiers horaires les plus récents.
            foreach (string hour in hours.Take(4))
            {
                string hourUrl = $"{provUrl}{hour}/";
                string hourListing;
                try { hourListing = await Http.GetStringAsync(hourUrl); }
                catch { continue; }

                // Le code de site inclut déjà le préfixe « s » (ex. s0000145).
                string? file = Regex.Matches(hourListing, $"href=\"([^\"]*_{code}_fr\\.xml)\"")
                    .Select(m => m.Groups[1].Value)
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .LastOrDefault();

                if (file is not null)
                    return hourUrl + file;
            }
        }

        return null;
    }

    private static IEnumerable<string> DateCandidates()
    {
        DateTime utc = DateTime.UtcNow;
        yield return utc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        yield return utc.AddDays(-1).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    // ---- Parsing du citypage weather ------------------------------------------

    private static WeatherInfo Parse(string xml)
    {
        XDocument doc = XDocument.Parse(xml);
        XElement? cc = doc.Root?.Element("currentConditions");
        if (cc is null)
            return WeatherInfo.Failed;

        XElement? group = doc.Root?.Element("forecastGroup");

        // Certaines stations n'exposent ni icône ni description dans l'observation
        // courante : on retombe alors sur la première période de prévision.
        int icon = ReadIntNullable(cc.Element("iconCode")) ?? FirstForecastIcon(group);
        WeatherCondition condition = EcccWeather.Map(icon);

        // Jour/nuit d'après le lever/coucher réels : le code de prévision de repli
        // peut désigner une période de nuit alors qu'il fait encore jour.
        bool night = ComputeNight(doc.Root?.Element("riseSet")) ?? EcccWeather.IsNight(icon);

        int temp = ReadRounded(cc.Element("temperature"));
        int feels = ReadRoundedNullable(cc.Element("windChill"))
                    ?? ReadRoundedNullable(cc.Element("humidex"))
                    ?? temp;

        string desc = cc.Element("condition")?.Value?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(desc))
            desc = FirstForecastSummary(group);

        int humidity = ReadInt(cc.Element("relativeHumidity"));

        XElement? wind = cc.Element("wind");
        int windKmph = ReadInt(wind?.Element("speed"));
        string windDir = wind?.Element("direction")?.Value?.Trim() ?? string.Empty;

        List<ForecastDay> forecast = ReadForecast(group);

        return new WeatherInfo(true, temp, feels, desc, condition, night, humidity, windKmph, windDir, forecast);
    }

    private static List<ForecastDay> ReadForecast(XElement? group)
    {
        var days = new List<ForecastDay>();
        if (group is null)
            return days;

        DayBuilder? cur = null;

        foreach (XElement f in group.Elements("forecast"))
        {
            XElement? tEl = f.Element("temperatures")?.Element("temperature");
            if (tEl is null)
                continue;

            string cls = tEl.Attribute("class")?.Value ?? "high";
            int value = ReadRounded(tEl);
            WeatherCondition cond = EcccWeather.Map(
                ReadInt(f.Element("abbreviatedForecast")?.Element("iconCode")));
            string label = CleanLabel(f.Element("period")?.Value ?? string.Empty);

            if (cls == "low" && cur is { HasLow: false })
            {
                cur.MinC = value;
                cur.HasLow = true;
                continue;
            }

            if (cur is not null)
                days.Add(cur.ToDay());

            cur = new DayBuilder { Label = label, MaxC = value, MinC = value, Condition = cond };

            if (days.Count >= 3)
            {
                cur = null;
                break;
            }
        }

        if (cur is not null && days.Count < 3)
            days.Add(cur.ToDay());

        return days.Take(3).ToList();
    }

    private sealed class DayBuilder
    {
        public string Label = string.Empty;
        public int MinC;
        public int MaxC;
        public bool HasLow;
        public WeatherCondition Condition;

        public ForecastDay ToDay() => new(Label, Math.Min(MinC, MaxC), Math.Max(MinC, MaxC), Condition);
    }

    // ---- Helpers ---------------------------------------------------------------

    private static string CleanLabel(string period)
    {
        string p = period.Trim();
        if (p.StartsWith("Aujourd", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("Ce soir", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("Cette nuit", StringComparison.OrdinalIgnoreCase))
            return "Auj.";

        // Retire les qualificatifs de fin (« soir », « la nuit », « nuit »).
        foreach (string suffix in new[] { " la nuit", " soir", " nuit" })
            if (p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                p = p[..^suffix.Length];

        string word = p.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? p;
        if (word.Length > 3)
            word = word[..3];

        return word.Length > 0 ? char.ToUpper(word[0], Culture) + word[1..] : word;
    }

    /// <summary>
    /// Vrai s'il fait nuit d'après les heures UTC de lever/coucher du soleil.
    /// Null si <c>riseSet</c> est absent ou illisible (l'appelant retombe alors sur
    /// le code d'icône).
    /// </summary>
    private static bool? ComputeNight(XElement? riseSet)
    {
        if (riseSet is null)
            return null;

        DateTime? sunrise = ReadRiseUtc(riseSet, "sunrise");
        DateTime? sunset = ReadRiseUtc(riseSet, "sunset");
        if (sunrise is null || sunset is null)
            return null;

        DateTime now = DateTime.UtcNow;
        return now < sunrise.Value || now >= sunset.Value;
    }

    private static DateTime? ReadRiseUtc(XElement riseSet, string name)
    {
        XElement? dt = riseSet.Elements("dateTime").FirstOrDefault(e =>
            (string?)e.Attribute("zone") == "UTC" && (string?)e.Attribute("name") == name);

        string? ts = dt?.Element("timeStamp")?.Value;
        return ts != null && DateTime.TryParseExact(ts, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime v)
            ? v
            : null;
    }

    private static int FirstForecastIcon(XElement? group)
    {
        XElement? first = group?.Elements("forecast").FirstOrDefault();
        return ReadInt(first?.Element("abbreviatedForecast")?.Element("iconCode"));
    }

    private static string FirstForecastSummary(XElement? group)
    {
        XElement? first = group?.Elements("forecast").FirstOrDefault();
        return first?.Element("abbreviatedForecast")?.Element("textSummary")?.Value?.Trim() ?? string.Empty;
    }

    private static int ReadInt(XElement? e)
        => e != null && int.TryParse(e.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;

    private static int? ReadIntNullable(XElement? e)
        => e != null && int.TryParse(e.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : null;

    private static int ReadRounded(XElement? e)
        => ReadRoundedNullable(e) ?? 0;

    private static int? ReadRoundedNullable(XElement? e)
    {
        if (e != null && double.TryParse(e.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            return (int)Math.Round(d);
        return null;
    }

    private static string Normalize(string s)
    {
        string lowered = s.Trim().ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);

        foreach (char c in lowered.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
