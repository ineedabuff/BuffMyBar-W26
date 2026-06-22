using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BuffBar.Services;

/// <summary>
/// Lit un ou plusieurs flux RSS/Atom et en extrait les titres (manchettes).
/// 100 % natif : HttpClient + System.Xml.Linq, aucune dépendance externe.
///
/// Tolérant aux espaces de noms (RSS 2.0, RSS 1.0/RDF, Atom) : on repère les
/// éléments « item »/« entry » puis leur enfant « title », quel que soit le
/// préfixe. Les titres sont nettoyés (entités HTML décodées, espaces réduits).
/// </summary>
public sealed class NewsService
{
    private static readonly HttpClient _http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // User-Agent de navigateur : certains serveurs (dont Radio-Canada) rejettent
        // les requêtes au User-Agent inhabituel.
        c.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/xml, text/xml, */*");
        return c;
    }

    /// <summary>
    /// Récupère les manchettes de tous les flux fournis, fusionnées et dédoublonnées,
    /// dans l'ordre des flux. Un flux en échec est simplement ignoré.
    /// </summary>
    public async Task<List<string>> FetchAllAsync(IEnumerable<string> feeds, int maxPerFeed)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (string url in feeds)
        {
            List<string> titles;
            try
            {
                titles = await FetchFeedAsync(url, maxPerFeed);
            }
            catch
            {
                continue; // flux indisponible : on passe au suivant
            }

            foreach (string t in titles)
                if (seen.Add(t))
                    result.Add(t);
        }

        return result;
    }

    private static async Task<List<string>> FetchFeedAsync(string url, int maxPerFeed)
    {
        string xml = await _http.GetStringAsync(url);
        XDocument doc = XDocument.Parse(xml);

        return doc.Descendants()
            .Where(e => e.Name.LocalName is "item" or "entry")
            .Select(GetTitle)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(Clean)
            .Take(maxPerFeed)
            .ToList()!;
    }

    private static string? GetTitle(XElement entry)
        => entry.Elements()
                .FirstOrDefault(c => c.Name.LocalName == "title")?
                .Value;

    private static string Clean(string? raw)
    {
        string s = WebUtility.HtmlDecode(raw ?? string.Empty).Trim();
        // Réduit les espaces/retours multiples en une seule espace.
        return string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
