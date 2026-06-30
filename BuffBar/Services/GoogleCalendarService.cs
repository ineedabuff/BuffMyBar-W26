using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuffBar.Services;

/// <summary>Événement d'agenda simplifié pour le flyout.</summary>
public sealed record CalEvent(DateTime Start, DateTime End, string Title, bool AllDay);

/// <summary>
/// Accès à Google Agenda en lecture seule, 100 % natif (HttpClient +
/// System.Text.Json + écouteur TCP loopback pour le retour OAuth) — aucun paquet.
///
/// Flux OAuth « application de bureau » (PKCE + redirection loopback). Le jeton de
/// rafraîchissement est conservé dans %AppData%\BuffMyBar-W26\google_token.json
/// (hors settings.json). Identifiants OAuth à créer dans Google Cloud Console.
/// </summary>
public static class GoogleCalendarService
{
    private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string Scope = "https://www.googleapis.com/auth/calendar.readonly";
    private const string ApiBase = "https://www.googleapis.com/calendar/v3";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly JsonSerializerOptions J = new() { PropertyNameCaseInsensitive = true };

    private static string TokenPath => Path.Combine(ConfigService.RootDir, "google_token.json");

    private sealed class TokenData
    {
        public string? RefreshToken { get; set; }
        public string? AccessToken { get; set; }
        public DateTime Expiry { get; set; }
    }

    /// <summary>Vrai si un jeton de rafraîchissement est déjà enregistré.</summary>
    public static bool IsConnected => LoadToken()?.RefreshToken is { Length: > 0 };

    /// <summary>Supprime les jetons enregistrés (déconnexion).</summary>
    public static void Disconnect()
    {
        try { if (File.Exists(TokenPath)) File.Delete(TokenPath); } catch { /* ignore */ }
    }

    // ---------------------------------------------------------------- OAuth

    /// <summary>Lance le consentement dans le navigateur et enregistre les jetons.</summary>
    public static async Task<bool> ConnectAsync(string clientId, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientId)) return false;

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        string redirect = $"http://127.0.0.1:{port}";

        string verifier = Base64Url(RandomBytes(32));
        string challenge = Base64Url(Sha256(Encoding.ASCII.GetBytes(verifier)));

        string url =
            $"{AuthEndpoint}?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirect)}" +
            "&response_type=code" +
            $"&scope={Uri.EscapeDataString(Scope)}" +
            $"&code_challenge={challenge}&code_challenge_method=S256" +
            "&access_type=offline&prompt=consent";

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

            string? code = await ReceiveCodeAsync(listener);
            if (string.IsNullOrEmpty(code)) return false;

            var form = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code!,
                ["code_verifier"] = verifier,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirect
            };

            using var resp = await Http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form));
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return false;

            using var doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;

            var token = new TokenData
            {
                AccessToken = Str(root, "access_token"),
                RefreshToken = Str(root, "refresh_token"),
                Expiry = DateTime.UtcNow.AddSeconds(Int(root, "expires_in", 3000) - 60)
            };
            if (string.IsNullOrEmpty(token.RefreshToken)) return false;
            SaveToken(token);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { listener.Stop(); } catch { /* ignore */ }
        }
    }

    private static async Task<string?> ReceiveCodeAsync(TcpListener listener)
    {
        using TcpClient client = await listener.AcceptTcpClientAsync();
        using NetworkStream stream = client.GetStream();

        var buffer = new byte[8192];
        int n = await stream.ReadAsync(buffer);
        string request = Encoding.ASCII.GetString(buffer, 0, n);

        string? code = QueryParam(request, "code");

        const string html =
            "<!doctype html><html><head><meta charset='utf-8'></head>" +
            "<body style=\"font-family:Segoe UI,sans-serif;background:#0c0c0c;color:#ddff24;" +
            "display:flex;align-items:center;justify-content:center;height:100vh;margin:0\">" +
            "<div style='text-align:center'><h2>BuffMyBar — Google Agenda connecté</h2>" +
            "<p style='color:#fff'>Vous pouvez fermer cette fenêtre.</p></div></body></html>";

        byte[] payload = Encoding.UTF8.GetBytes(html);
        string head = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n" +
                      $"Content-Length: {payload.Length}\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(head));
        await stream.WriteAsync(payload);
        await stream.FlushAsync();

        return code;
    }

    private static async Task<string?> GetAccessTokenAsync(string clientId, string clientSecret)
    {
        TokenData? t = LoadToken();
        if (t?.RefreshToken is not { Length: > 0 }) return null;

        if (!string.IsNullOrEmpty(t.AccessToken) && t.Expiry > DateTime.UtcNow.AddSeconds(30))
            return t.AccessToken;

        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = t.RefreshToken!,
            ["grant_type"] = "refresh_token"
        };

        try
        {
            using var resp = await Http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form));
            if (!resp.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            JsonElement root = doc.RootElement;

            t.AccessToken = Str(root, "access_token");
            t.Expiry = DateTime.UtcNow.AddSeconds(Int(root, "expires_in", 3000) - 60);
            SaveToken(t);
            return t.AccessToken;
        }
        catch
        {
            return null;
        }
    }

    // ---------------------------------------------------------------- Events

    /// <summary>Événements de TOUS les agendas visibles, entre deux dates locales.</summary>
    public static async Task<List<CalEvent>> GetEventsAsync(
        string clientId, string clientSecret, DateTime fromLocal, DateTime toLocal, int maxEvents)
    {
        var events = new List<CalEvent>();

        string? access = await GetAccessTokenAsync(clientId, clientSecret);
        if (access == null) return events;

        string timeMin = new DateTimeOffset(fromLocal).ToString("yyyy-MM-ddTHH:mm:sszzz");
        string timeMax = new DateTimeOffset(toLocal).ToString("yyyy-MM-ddTHH:mm:sszzz");
        int pageSize = Math.Clamp(maxEvents <= 0 ? 250 : maxEvents, 50, 250);

        foreach (string calId in await GetCalendarIdsAsync(access))
        {
            string? pageToken = null;
            do
            {
                string url = $"{ApiBase}/calendars/{Uri.EscapeDataString(calId)}/events" +
                             $"?singleEvents=true&orderBy=startTime&maxResults={pageSize}" +
                             $"&timeMin={Uri.EscapeDataString(timeMin)}&timeMax={Uri.EscapeDataString(timeMax)}";
                if (pageToken != null) url += "&pageToken=" + Uri.EscapeDataString(pageToken);

                JsonDocument? doc = await GetJsonAsync(url, access);
                if (doc == null) break;

                using (doc)
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("items", out var items))
                    {
                        foreach (JsonElement it in items.EnumerateArray())
                        {
                            // Ignore les invitations refusées.
                            if (IsDeclined(it)) continue;
                            string title = Str(it, "summary") ?? "(sans titre)";
                            (DateTime start, bool allDay) = ParseWhen(it, "start");
                            (DateTime end, _) = ParseWhen(it, "end");
                            events.Add(new CalEvent(start, end, title, allDay));
                        }
                    }
                    pageToken = root.TryGetProperty("nextPageToken", out var nt) ? nt.GetString() : null;
                }
            }
            while (!string.IsNullOrEmpty(pageToken));
        }

        return events;
    }

    /// <summary>Identifiants de tous les agendas non masqués (repli : primary).</summary>
    private static async Task<List<string>> GetCalendarIdsAsync(string access)
    {
        var ids = new List<string>();
        try
        {
            JsonDocument? doc = await GetJsonAsync($"{ApiBase}/users/me/calendarList", access);
            if (doc != null)
                using (doc)
                {
                    if (doc.RootElement.TryGetProperty("items", out var items))
                        foreach (JsonElement it in items.EnumerateArray())
                        {
                            if (it.TryGetProperty("hidden", out var h) && h.ValueKind == JsonValueKind.True)
                                continue;
                            string? id = Str(it, "id");
                            if (!string.IsNullOrEmpty(id)) ids.Add(id!);
                        }
                }
        }
        catch { /* repli ci-dessous */ }

        if (ids.Count == 0) ids.Add("primary");
        return ids;
    }

    private static async Task<JsonDocument?> GetJsonAsync(string url, string access)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("Authorization", "Bearer " + access);
            using var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            return JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        }
        catch
        {
            return null;
        }
    }

    private static bool IsDeclined(JsonElement item)
    {
        if (!item.TryGetProperty("attendees", out var att) || att.ValueKind != JsonValueKind.Array)
            return false;
        foreach (JsonElement a in att.EnumerateArray())
            if (a.TryGetProperty("self", out var self) && self.ValueKind == JsonValueKind.True)
                return Str(a, "responseStatus") == "declined";
        return false;
    }

    private static (DateTime, bool) ParseWhen(JsonElement item, string node)
    {
        if (item.TryGetProperty(node, out var w))
        {
            if (w.TryGetProperty("dateTime", out var dt)
                && DateTime.TryParse(dt.GetString(), out DateTime timed))
                return (timed.ToLocalTime(), false);

            if (w.TryGetProperty("date", out var d)
                && DateTime.TryParse(d.GetString(), out DateTime allDay))
                return (allDay, true);
        }
        return (DateTime.MinValue, false);
    }

    // ---------------------------------------------------------------- Helpers

    private static TokenData? LoadToken()
    {
        try
        {
            if (!File.Exists(TokenPath)) return null;

            byte[] raw = File.ReadAllBytes(TokenPath);

            // Token chiffré DPAPI (format actuel).
            byte[]? plain = Interop.Dpapi.Unprotect(raw);
            if (plain is not null)
                return JsonSerializer.Deserialize<TokenData>(plain, J);

            // Migration douce : ancien token en clair (JSON brut). On le lit une
            // fois, puis on le ré-enregistre chiffré au prochain SaveToken.
            var migrated = JsonSerializer.Deserialize<TokenData>(raw, J);
            if (migrated is not null) SaveToken(migrated);
            return migrated;
        }
        catch { /* ignore */ }
        return null;
    }

    private static void SaveToken(TokenData t)
    {
        try
        {
            byte[] plain = JsonSerializer.SerializeToUtf8Bytes(t, J);
            byte[]? enc = Interop.Dpapi.Protect(plain);
            // Repli en clair uniquement si DPAPI échoue (ne devrait pas arriver).
            File.WriteAllBytes(TokenPath, enc ?? plain);
        }
        catch { /* ignore */ }
    }

    private static byte[] RandomBytes(int n)
    {
        var b = new byte[n];
        RandomNumberGenerator.Fill(b);
        return b;
    }

    private static byte[] Sha256(byte[] data) => SHA256.HashData(data);

    private static string Base64Url(byte[] b) =>
        Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string? Str(JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int Int(JsonElement e, string name, int fallback)
        => e.TryGetProperty(name, out var p) && p.TryGetInt32(out int v) ? v : fallback;

    private static string? QueryParam(string httpRequest, string key)
    {
        // Première ligne : "GET /?code=XXX&scope=... HTTP/1.1"
        int sp1 = httpRequest.IndexOf(' ');
        if (sp1 < 0) return null;
        int sp2 = httpRequest.IndexOf(' ', sp1 + 1);
        if (sp2 < 0) return null;

        string path = httpRequest.Substring(sp1 + 1, sp2 - sp1 - 1);
        int q = path.IndexOf('?');
        if (q < 0) return null;

        foreach (string pair in path[(q + 1)..].Split('&'))
        {
            int eq = pair.IndexOf('=');
            if (eq > 0 && pair[..eq] == key)
                return Uri.UnescapeDataString(pair[(eq + 1)..]);
        }
        return null;
    }
}
