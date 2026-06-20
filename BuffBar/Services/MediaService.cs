using System;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace BuffBar.Services;

/// <summary>Instantané de la session média active.</summary>
public readonly record struct MediaInfo(bool HasSession, string Title, string Artist, bool Playing)
{
    public static MediaInfo None => new(false, string.Empty, string.Empty, false);
}

/// <summary>
/// Accès au lecteur média actif du système via WinRT
/// (GlobalSystemMediaTransportControlsSessionManager) — équivalent MPRIS/playerctl.
/// Lecture du titre/artiste/état et contrôle lecture-pause / piste suivante-précédente.
/// Robuste : ré-acquisition de la session courante à chaque appel.
/// </summary>
public sealed class MediaService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;

    private async Task<GlobalSystemMediaTransportControlsSession?> CurrentAsync()
    {
        _manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        return _manager.GetCurrentSession();
    }

    public async Task<MediaInfo> ReadAsync()
    {
        try
        {
            var session = await CurrentAsync();
            if (session is null) return MediaInfo.None;

            var playback = session.GetPlaybackInfo();
            bool playing = playback.PlaybackStatus
                == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            var props = await session.TryGetMediaPropertiesAsync();
            string title = props?.Title ?? string.Empty;
            string artist = props?.Artist ?? string.Empty;

            return new MediaInfo(true, title, artist, playing);
        }
        catch
        {
            return MediaInfo.None;
        }
    }

    public async Task TogglePlayPauseAsync()
    {
        try
        {
            var s = await CurrentAsync();
            if (s is not null) await s.TryTogglePlayPauseAsync();
        }
        catch { /* ignore */ }
    }

    public async Task NextAsync()
    {
        try
        {
            var s = await CurrentAsync();
            if (s is not null) await s.TrySkipNextAsync();
        }
        catch { /* ignore */ }
    }

    public async Task PreviousAsync()
    {
        try
        {
            var s = await CurrentAsync();
            if (s is not null) await s.TrySkipPreviousAsync();
        }
        catch { /* ignore */ }
    }
}
