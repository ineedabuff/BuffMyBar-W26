using System;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BuffBar.Services;

/// <summary>
/// Client obs-websocket v5 minimal et natif (ClientWebSocket).
/// Suit l'état d'enregistrement d'OBS : handshake Hello/Identify (avec
/// authentification SHA256 si activée), requête initiale GetRecordStatus,
/// puis écoute de l'évènement RecordStateChanged.
/// Reconnexion automatique. Aucune dépendance externe.
/// </summary>
public sealed class ObsService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _password;

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private ClientWebSocket? _ws;
    private readonly byte[] _rxBuf = new byte[16384];

    private volatile bool _recording;

    /// <summary>Notifié à chaque changement d'état d'enregistrement (thread d'arrière-plan).</summary>
    public event Action<bool>? RecordingChanged;

    public bool Recording => _recording;

    public ObsService(string host, int port, string password)
    {
        _host = host;
        _port = port;
        _password = password ?? string.Empty;
    }

    public void Start()
    {
        if (_loop is not null) return;
        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _ws?.Abort(); } catch { }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await SessionAsync(ct);
            }
            catch
            {
                // OBS fermé / connexion perdue / erreur protocole.
            }

            SetRecording(false);  // déconnecté => considéré inactif

            try { await Task.Delay(3000, ct); }
            catch { /* annulé */ }
        }
    }

    private async Task SessionAsync(CancellationToken ct)
    {
        using var ws = new ClientWebSocket();
        _ws = ws;
        await ws.ConnectAsync(new Uri($"ws://{_host}:{_port}"), ct);

        // 1) Hello
        string? auth;
        using (var hello = await ReceiveJsonAsync(ws, ct))
        {
            var d = hello.RootElement.GetProperty("d");
            auth = null;
            if (d.TryGetProperty("authentication", out var a))
            {
                string challenge = a.GetProperty("challenge").GetString() ?? string.Empty;
                string salt = a.GetProperty("salt").GetString() ?? string.Empty;
                auth = ComputeAuth(_password, salt, challenge);
            }
        }

        // 2) Identify
        object identify = auth is null
            ? new { op = 1, d = new { rpcVersion = 1 } }
            : new { op = 1, d = new { rpcVersion = 1, authentication = auth } };
        await SendAsync(ws, identify, ct);

        // 3) Boucle de réception
        while (!ct.IsCancellationRequested)
        {
            using var msg = await ReceiveJsonAsync(ws, ct);
            var root = msg.RootElement;
            int op = root.GetProperty("op").GetInt32();
            var data = root.GetProperty("d");

            switch (op)
            {
                case 2:  // Identified -> on demande l'état courant
                    await SendAsync(ws, new
                    {
                        op = 6,
                        d = new { requestType = "GetRecordStatus", requestId = "rec-init" }
                    }, ct);
                    break;

                case 7:  // RequestResponse
                    if (data.GetProperty("requestType").GetString() == "GetRecordStatus"
                        && data.TryGetProperty("responseData", out var rd)
                        && rd.TryGetProperty("outputActive", out var oa))
                        SetRecording(oa.GetBoolean());
                    break;

                case 5:  // Event
                    if (data.GetProperty("eventType").GetString() == "RecordStateChanged"
                        && data.TryGetProperty("eventData", out var ed)
                        && ed.TryGetProperty("outputActive", out var ea))
                        SetRecording(ea.GetBoolean());
                    break;
            }
        }
    }

    private async Task<JsonDocument> ReceiveJsonAsync(ClientWebSocket ws, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(_rxBuf), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new InvalidOperationException("WebSocket fermé par OBS.");
            ms.Write(_rxBuf, 0, result.Count);
        }
        while (!result.EndOfMessage);

        ms.Position = 0;
        return JsonDocument.Parse(ms);
    }

    private static async Task SendAsync(ClientWebSocket ws, object payload, CancellationToken ct)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private static string ComputeAuth(string password, string salt, string challenge)
    {
        string secret = Sha256Base64(password + salt);
        return Sha256Base64(secret + challenge);
    }

    private static string Sha256Base64(string input)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    private void SetRecording(bool value)
    {
        if (_recording == value) return;
        _recording = value;
        RecordingChanged?.Invoke(value);
    }
}
