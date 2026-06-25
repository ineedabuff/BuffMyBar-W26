using System;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BuffBar.Services;

/// <summary>État d'enregistrement OBS : actif + durée écoulée.</summary>
public readonly record struct ObsStatus(bool Recording, TimeSpan Duration);

/// <summary>
/// Client obs-websocket v5 minimal et natif (ClientWebSocket).
/// Suit l'état d'enregistrement d'OBS : handshake Hello/Identify (avec
/// authentification SHA256 si activée), puis <b>sondage GetRecordStatus chaque
/// seconde</b> (robuste même si l'évènement RecordStateChanged n'est pas poussé) —
/// ce qui fournit aussi la durée d'enregistrement. L'évènement RecordStateChanged
/// est également pris en compte pour une réaction immédiate.
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
    private TimeSpan _duration;

    /// <summary>Notifié à chaque changement d'état/durée (thread d'arrière-plan).</summary>
    public event Action<ObsStatus>? StatusChanged;

    public ObsStatus Status => new(_recording, _duration);

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

            SetStatus(false, TimeSpan.Zero);  // déconnecté => considéré inactif

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

        // 3) Boucle de réception (+ sondage d'état une fois identifié)
        using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task? poll = null;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                using var msg = await ReceiveJsonAsync(ws, ct);
                var root = msg.RootElement;
                int op = root.GetProperty("op").GetInt32();
                var data = root.GetProperty("d");

                switch (op)
                {
                    case 2:  // Identified -> démarre le sondage périodique
                        poll ??= Task.Run(() => PollLoopAsync(ws, sessionCts.Token));
                        break;

                    case 7:  // RequestResponse (GetRecordStatus)
                        if (data.GetProperty("requestType").GetString() == "GetRecordStatus"
                            && data.TryGetProperty("responseData", out var rd))
                        {
                            bool active = rd.TryGetProperty("outputActive", out var oa) && oa.GetBoolean();
                            TimeSpan dur = TimeSpan.Zero;
                            if (rd.TryGetProperty("outputDuration", out var od) && od.TryGetDouble(out double ms))
                                dur = TimeSpan.FromMilliseconds(ms);
                            SetStatus(active, active ? dur : TimeSpan.Zero);
                        }
                        break;

                    case 5:  // Event (réaction immédiate)
                        if (data.GetProperty("eventType").GetString() == "RecordStateChanged"
                            && data.TryGetProperty("eventData", out var ed)
                            && ed.TryGetProperty("outputActive", out var ea))
                        {
                            bool active = ea.GetBoolean();
                            SetStatus(active, active ? _duration : TimeSpan.Zero);
                        }
                        break;
                }
            }
        }
        finally
        {
            try { sessionCts.Cancel(); } catch { }
            if (poll is not null) { try { await poll; } catch { } }
        }
    }

    /// <summary>Demande GetRecordStatus chaque seconde (unique émetteur de la session).</summary>
    private async Task PollLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            try
            {
                await SendAsync(ws, new
                {
                    op = 6,
                    d = new { requestType = "GetRecordStatus", requestId = "rec-poll" }
                }, ct);
            }
            catch { break; }

            try { await Task.Delay(1000, ct); }
            catch { break; }
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

    private void SetStatus(bool recording, TimeSpan duration)
    {
        bool changed = _recording != recording || _duration != duration;
        _recording = recording;
        _duration = duration;
        if (changed) StatusChanged?.Invoke(new ObsStatus(recording, duration));
    }
}
