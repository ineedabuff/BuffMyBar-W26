using System;
using BuffBar.Interop;

namespace BuffBar.Services;

/// <summary>État volume à un instant donné.</summary>
public readonly record struct VolumeInfo(int Percent, bool Muted);

/// <summary>
/// Contrôle du volume maître du périphérique de lecture par défaut via Core Audio.
/// Robuste au changement de périphérique : ré-acquisition automatique en cas d'échec
/// (ex. branchement d'un casque).
/// </summary>
public sealed class VolumeController
{
    private IAudioEndpointVolume? _endpoint;
    private Guid _eventCtx = Guid.Empty;

    public VolumeController() => TryAcquire();

    private void TryAcquire()
    {
        try
        {
            Type? t = Type.GetTypeFromCLSID(CoreAudio.CLSID_MMDeviceEnumerator);
            if (t is null) return;

            var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(t)!;
            if (enumerator.GetDefaultAudioEndpoint(CoreAudio.eRender, CoreAudio.eConsole, out IMMDevice dev) != 0
                || dev is null)
                return;

            Guid iid = CoreAudio.IID_IAudioEndpointVolume;
            if (dev.Activate(ref iid, CoreAudio.CLSCTX_ALL, IntPtr.Zero, out object o) != 0)
                return;

            _endpoint = o as IAudioEndpointVolume;
        }
        catch
        {
            _endpoint = null;
        }
    }

    public VolumeInfo? Read()
    {
        if (_endpoint is null) TryAcquire();
        if (_endpoint is null) return null;

        try
        {
            _endpoint.GetMasterVolumeLevelScalar(out float level);
            _endpoint.GetMute(out bool muted);
            return new VolumeInfo((int)Math.Round(level * 100f), muted);
        }
        catch
        {
            _endpoint = null;  // périphérique probablement disparu -> ré-acquisition au prochain appel
            return null;
        }
    }

    public void SetPercent(int percent)
    {
        if (_endpoint is null) return;
        float level = Math.Clamp(percent, 0, 100) / 100f;
        try { _endpoint.SetMasterVolumeLevelScalar(level, ref _eventCtx); }
        catch { _endpoint = null; }
    }

    public void Step(int delta)
    {
        if (Read() is { } info)
            SetPercent(info.Percent + delta);
    }

    public void ToggleMute()
    {
        if (Read() is not { } info || _endpoint is null) return;
        try { _endpoint.SetMute(!info.Muted, ref _eventCtx); }
        catch { _endpoint = null; }
    }
}
