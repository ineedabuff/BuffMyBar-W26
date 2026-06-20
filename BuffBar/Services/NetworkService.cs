using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BuffBar.Services;

/// <summary>
/// Résolution de l'IP locale (sans trafic, via "connect" UDP) et de l'IP publique
/// (service HTTP en clair). HttpClient natif, aucune dépendance.
/// </summary>
public sealed class NetworkService
{
    private static readonly HttpClient Http = CreateClient();

    private static readonly string[] PublicIpUrls =
    {
        "https://api.ipify.org",
        "https://icanhazip.com",
        "https://ifconfig.me/ip"
    };

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("BuffBar/1.0");
        return c;
    }

    /// <summary>IP locale préférée (celle utilisée pour sortir vers Internet).</summary>
    public static string? LocalIp()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect("8.8.8.8", 65530);  // aucun paquet envoyé, fixe juste la route
            if (s.LocalEndPoint is IPEndPoint ep)
                return ep.Address.ToString();
        }
        catch { /* repli ci-dessous */ }

        try
        {
            foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    return ip.ToString();
        }
        catch { /* ignore */ }

        return null;
    }

    public async Task<string?> FetchPublicIpAsync()
    {
        foreach (string url in PublicIpUrls)
        {
            try
            {
                string ip = (await Http.GetStringAsync(url)).Trim();
                if (IPAddress.TryParse(ip, out _))
                    return ip;
            }
            catch { /* service suivant */ }
        }
        return null;
    }
}
