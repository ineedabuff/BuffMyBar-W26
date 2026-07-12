using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuffBar.Services;

/// <summary>
/// Vérifie s'il existe une version plus récente sur les Releases GitHub.
///
/// 100 % natif (HttpClient + System.Text.Json). Best-effort et non bloquant :
/// une panne réseau ou une réponse illisible laisse simplement l'application en
/// l'état, sans erreur. Ne télécharge ni n'installe rien — expose seulement l'état
/// « mise à jour disponible », que l'interface propose via le menu contextuel.
/// </summary>
public static class UpdateService
{
    private const string LatestReleaseApi =
        "https://api.github.com/repos/ineedabuff/BuffMyBar-W26/releases/latest";

    /// <summary>Page des Releases (ouverte quand l'utilisateur clique sur « Mettre à jour »).</summary>
    public const string ReleasesUrl = "https://github.com/ineedabuff/BuffMyBar-W26/releases";

    private static readonly HttpClient Http = CreateClient();

    /// <summary>Vrai si une version strictement plus récente est publiée.</summary>
    public static bool UpdateAvailable { get; private set; }

    /// <summary>Étiquette de la dernière version publiée (ex. « v0.9.0 »), si connue.</summary>
    public static string? LatestTag { get; private set; }

    /// <summary>Émis quand l'état de mise à jour change (sur le thread appelant).</summary>
    public static event Action? Changed;

    /// <summary>Version courante de l'application (assembly BuffBar).</summary>
    public static Version CurrentVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // L'API GitHub exige un User-Agent.
        c.DefaultRequestHeaders.UserAgent.ParseAdd("BuffBar-UpdateCheck");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return c;
    }

    /// <summary>
    /// Interroge GitHub et met à jour l'état. Best-effort : n'émet jamais d'exception.
    /// </summary>
    public static async Task CheckAsync()
    {
        try
        {
            string json = await Http.GetStringAsync(LatestReleaseApi);
            using JsonDocument doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("tag_name", out JsonElement tagEl))
                return;

            string? tag = tagEl.GetString();
            if (string.IsNullOrWhiteSpace(tag))
                return;

            if (IsNewer(CurrentVersion, tag))
            {
                LatestTag = tag;
                UpdateAvailable = true;
                Changed?.Invoke();
            }
        }
        catch
        {
            // Hors ligne, dépôt sans release, réponse illisible : on ignore.
        }
    }

    /// <summary>Vrai si <paramref name="latestTag"/> désigne une version &gt; <paramref name="current"/>.</summary>
    public static bool IsNewer(Version current, string latestTag)
    {
        Version? latest = ParseVersion(latestTag);
        return latest is not null && latest > current;
    }

    /// <summary>
    /// Convertit une étiquette de version (« v0.9.0 », « 1.2.3 ») en <see cref="Version"/>.
    /// Retourne null si l'étiquette n'est pas une version reconnaissable.
    /// </summary>
    internal static Version? ParseVersion(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        string s = tag.Trim();
        if (s.Length > 0 && (s[0] == 'v' || s[0] == 'V'))
            s = s[1..];

        // Garde uniquement la partie numérique pointée en tête (ignore « -beta », etc.).
        int end = 0;
        while (end < s.Length && (char.IsDigit(s[end]) || s[end] == '.'))
            end++;
        s = s[..end];

        return Version.TryParse(s, out Version? v) ? v : null;
    }
}
