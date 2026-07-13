using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuffBar.Services;

/// <summary>
/// Vérifie s'il existe une version plus récente sur les Releases GitHub, et — si
/// l'utilisateur le demande — télécharge l'installateur de cette release pour le
/// lancer (mise à jour complète).
///
/// 100 % natif (HttpClient + System.Text.Json). Le contrôle est best-effort et non
/// bloquant : une panne réseau ou une réponse illisible laisse l'application en
/// l'état. Le téléchargement se fait uniquement depuis l'asset officiel de la
/// release (HTTPS, dépôt du projet) et n'est déclenché qu'à la demande.
/// </summary>
public static class UpdateService
{
    private const string LatestReleaseApi =
        "https://api.github.com/repos/ineedabuff/BuffMyBar-W26/releases/latest";

    /// <summary>Page des Releases (repli si aucun installateur n'est attaché).</summary>
    public const string ReleasesUrl = "https://github.com/ineedabuff/BuffMyBar-W26/releases";

    private static readonly HttpClient Http = CreateClient(TimeSpan.FromSeconds(10));
    private static readonly HttpClient DownloadHttp = CreateClient(TimeSpan.FromMinutes(5));

    /// <summary>Vrai si une version strictement plus récente est publiée.</summary>
    public static bool UpdateAvailable { get; private set; }

    /// <summary>Étiquette de la dernière version publiée (ex. « v0.9.0 »), si connue.</summary>
    public static string? LatestTag { get; private set; }

    /// <summary>URL de téléchargement de l'installateur (.exe) de la release, si présent.</summary>
    public static string? InstallerUrl { get; private set; }

    /// <summary>Émis quand l'état de mise à jour change (sur le thread appelant).</summary>
    public static event Action? Changed;

    /// <summary>Version courante de l'application (assembly BuffBar).</summary>
    public static Version CurrentVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);

    private static HttpClient CreateClient(TimeSpan timeout)
    {
        var c = new HttpClient { Timeout = timeout };
        // L'API et le CDN GitHub exigent un User-Agent.
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
            (string? tag, string? installer) = ParseRelease(json);

            if (tag is not null && IsNewer(CurrentVersion, tag))
            {
                LatestTag = tag;
                InstallerUrl = installer;
                UpdateAvailable = true;
                Changed?.Invoke();
            }
        }
        catch
        {
            // Hors ligne, dépôt sans release, réponse illisible : on ignore.
        }
    }

    /// <summary>
    /// Télécharge l'installateur de la release dans un dossier temporaire.
    /// Retourne le chemin du fichier, ou null en cas d'échec / absence d'installateur.
    /// </summary>
    public static async Task<string?> DownloadInstallerAsync()
    {
        if (string.IsNullOrEmpty(InstallerUrl))
            return null;

        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "BuffMyBar-update");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, "Buffmybar-W26-setup.exe");

            using HttpResponseMessage resp = await DownloadHttp.GetAsync(
                InstallerUrl, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            await using (FileStream fs = File.Create(dest))
                await resp.Content.CopyToAsync(fs);

            Logger.Log($"UpdateService: installateur téléchargé -> {dest}");
            return dest;
        }
        catch (Exception ex)
        {
            Logger.Log($"UpdateService: échec du téléchargement : {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extrait l'étiquette de version et l'URL de l'installateur (.exe) d'une réponse
    /// « latest release » de l'API GitHub. Logique pure et testable.
    /// </summary>
    internal static (string? Tag, string? InstallerUrl) ParseRelease(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            string? tag = root.TryGetProperty("tag_name", out JsonElement t) ? t.GetString() : null;
            if (string.IsNullOrWhiteSpace(tag))
                tag = null;

            string? installer = null;
            if (root.TryGetProperty("assets", out JsonElement assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement a in assets.EnumerateArray())
                {
                    string? name = a.TryGetProperty("name", out JsonElement n) ? n.GetString() : null;
                    if (name is not null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        installer = a.TryGetProperty("browser_download_url", out JsonElement u) ? u.GetString() : null;
                        break;
                    }
                }
            }

            return (tag, installer);
        }
        catch
        {
            return (null, null);
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
