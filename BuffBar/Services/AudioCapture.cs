using System;
using System.Runtime.InteropServices;
using System.Threading;
using BuffBar.Interop;

namespace BuffBar.Services;

/// <summary>
/// Capture le flux audio rendu (loopback WASAPI) du périphérique de lecture
/// par défaut, calcule une FFT et produit un spectre en bandes logarithmiques.
///
/// - Thread d'arrière-plan MTA dédié, scrutation par paquets (robuste au silence).
/// - Ré-initialisation automatique en cas d'erreur ou de changement de périphérique.
/// - Contrôle de gain automatique : exploite toute la hauteur quel que soit le volume.
/// - Journalisation de diagnostic via Logger.
/// - Aucune dépendance externe : COM Core Audio + FFT maison.
/// </summary>
public sealed class AudioCapture
{
    public const int Bands = 64;

    private const int FftSize = 2048;          // puissance de deux (meilleure résolution bas du spectre)
    private const int HopSize = FftSize / 2;   // 50 % de recouvrement
    // --- Réglages de sensibilité (monte Gain et/ou baisse Curve pour plus de mouvement) ---
    private const double Gain = 1.65;           // gain visuel global
    private const float Curve = 0.35f;          // exposant perceptuel (plus bas = barres plus hautes)
    private const float AgcDecay = 0.985f;      // chute de l'enveloppe AGC (plus bas = plus réactif)
    private const float MinRef = 1e-6f;         // garde anti division par zéro
    private const float SilenceMax = 3e-5f;     // sous ce pic : silence réel -> barres à 0
    private const float RelGate = 0.04f;        // coupe le bruit résiduel (relatif au pic)
    private const double FreqMin = 40.0;
    private const double FreqMaxCap = 16000.0;

    private readonly object _lock = new();
    private readonly float[] _bands = new float[Bands];

    private readonly float[] _acc = new float[FftSize];
    private int _accFill;
    private float _agc = 1e-4f;  // enveloppe du contrôle automatique de gain

    private readonly float[] _re = new float[FftSize];
    private readonly float[] _im = new float[FftSize];
    private readonly float[] _hann = new float[FftSize];

    private float[] _floatBuf = new float[FftSize * 4];
    private short[] _shortBuf = new short[FftSize * 4];

    // Diagnostic
    private long _diagSamples;
    private float _diagMaxAmp;

    private Thread? _thread;
    private volatile bool _running;

    public AudioCapture()
    {
        for (int i = 0; i < FftSize; i++)
            _hann[i] = (float)(0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (FftSize - 1))));
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _thread = new Thread(Run) { IsBackground = true, Name = "BuffBar.AudioCapture" };
        _thread.SetApartmentState(ApartmentState.MTA);
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(700);
        _thread = null;
    }

    public void GetBands(float[] dest)
    {
        lock (_lock)
        {
            int n = Math.Min(dest.Length, _bands.Length);
            Array.Copy(_bands, dest, n);
        }
    }

    // -----------------------------------------------------------------

    private void Run()
    {
        Logger.Log("AudioCapture: thread démarré.");
        while (_running)
        {
            try
            {
                CaptureSession();
            }
            catch (Exception ex)
            {
                Logger.Log($"AudioCapture: EXCEPTION {ex.GetType().Name}: {ex.Message}");
            }

            if (_running)
            {
                Logger.Log("AudioCapture: session terminée, nouvelle tentative dans 500 ms.");
                Thread.Sleep(500);
            }
        }
        Logger.Log("AudioCapture: thread arrêté.");
    }

    private static string Hr(int hr) => $"0x{(uint)hr:X8}";

    private void CaptureSession()
    {
        IAudioClient? client = null;
        IAudioCaptureClient? capture = null;

        try
        {
            Type? t = Type.GetTypeFromCLSID(CoreAudio.CLSID_MMDeviceEnumerator);
            if (t is null) { Logger.Log("AudioCapture: CLSID MMDeviceEnumerator introuvable."); return; }

            var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(t)!;
            int hr = enumerator.GetDefaultAudioEndpoint(CoreAudio.eRender, CoreAudio.eConsole, out IMMDevice dev);
            Logger.Log($"AudioCapture: GetDefaultAudioEndpoint -> {Hr(hr)}");
            if (hr != 0 || dev is null) return;

            Guid iidClient = CoreAudio.IID_IAudioClient;
            hr = dev.Activate(ref iidClient, CoreAudio.CLSCTX_ALL, IntPtr.Zero, out object oc);
            Logger.Log($"AudioCapture: Activate(IAudioClient) -> {Hr(hr)}");
            if (hr != 0) return;
            client = oc as IAudioClient;
            if (client is null) { Logger.Log("AudioCapture: cast IAudioClient nul."); return; }

            hr = client.GetMixFormat(out IntPtr pFormat);
            if (hr != 0 || pFormat == IntPtr.Zero) { Logger.Log($"AudioCapture: GetMixFormat -> {Hr(hr)}"); return; }

            WAVEFORMATEX fmt = Marshal.PtrToStructure<WAVEFORMATEX>(pFormat);
            int channels = Math.Max(1, (int)fmt.nChannels);
            int sampleRate = fmt.nSamplesPerSec;
            bool isFloat = fmt.wBitsPerSample == 32;
            int bytesPerSample = fmt.wBitsPerSample / 8;
            Logger.Log($"AudioCapture: mix format tag={fmt.wFormatTag} ch={channels} rate={sampleRate} bits={fmt.wBitsPerSample} (isFloat={isFloat})");

            hr = client.Initialize(
                CoreAudio.AUDCLNT_SHAREMODE_SHARED,
                CoreAudio.AUDCLNT_STREAMFLAGS_LOOPBACK,
                CoreAudio.REFTIMES_PER_SEC / 10,
                0, pFormat, IntPtr.Zero);
            Logger.Log($"AudioCapture: Initialize(LOOPBACK) -> {Hr(hr)}");

            Marshal.FreeCoTaskMem(pFormat);
            if (hr != 0) return;

            Guid iidCapture = CoreAudio.IID_IAudioCaptureClient;
            hr = client.GetService(ref iidCapture, out object ocap);
            Logger.Log($"AudioCapture: GetService(IAudioCaptureClient) -> {Hr(hr)}");
            if (hr != 0) return;
            capture = ocap as IAudioCaptureClient;
            if (capture is null) { Logger.Log("AudioCapture: cast IAudioCaptureClient nul."); return; }

            hr = client.Start();
            Logger.Log($"AudioCapture: Start -> {Hr(hr)}");
            if (hr != 0) return;

            _accFill = 0;
            _diagSamples = 0;
            _diagMaxAmp = 0;
            long lastLog = Environment.TickCount64;
            Logger.Log("AudioCapture: boucle de capture active (joue de l'audio pour voir bouger les barres).");

            while (_running)
            {
                long now = Environment.TickCount64;
                if (now - lastLog >= 1000)
                {
                    float bandMax = 0f;
                    lock (_lock)
                        for (int b = 0; b < Bands; b++)
                            if (_bands[b] > bandMax) bandMax = _bands[b];
                    Logger.Log($"AudioCapture: 1s -> samples={_diagSamples} maxAmp={_diagMaxAmp:F4} bandMax={bandMax:F3}");
                    _diagSamples = 0;
                    _diagMaxAmp = 0;
                    lastLog = now;
                }

                if (capture.GetNextPacketSize(out uint packet) != 0)
                    break;

                if (packet == 0)
                {
                    DecayBands();
                    Thread.Sleep(15);
                    continue;
                }

                while (packet != 0)
                {
                    int bhr = capture.GetBuffer(out IntPtr pData, out uint frames,
                        out uint flags, out _, out _);
                    if (bhr != 0) { Logger.Log($"AudioCapture: GetBuffer -> {Hr(bhr)}"); return; }

                    if (frames > 0)
                    {
                        bool silent = (flags & CoreAudio.AUDCLNT_BUFFERFLAGS_SILENT) != 0;
                        if (!silent && pData != IntPtr.Zero)
                            Consume(pData, (int)frames, channels, isFloat, bytesPerSample, sampleRate);
                        else
                            ConsumeSilence((int)frames, sampleRate);
                    }

                    capture.ReleaseBuffer(frames);

                    if (capture.GetNextPacketSize(out packet) != 0)
                        break;
                }
            }

            client.Stop();
        }
        finally
        {
            if (capture is not null) Marshal.ReleaseComObject(capture);
            if (client is not null) Marshal.ReleaseComObject(client);
        }
    }

    private void Consume(IntPtr pData, int frames, int channels, bool isFloat,
        int bytesPerSample, int sampleRate)
    {
        int sampleCount = frames * channels;
        EnsureCapacity(sampleCount);

        if (isFloat)
        {
            Marshal.Copy(pData, _floatBuf, 0, sampleCount);
            for (int f = 0; f < frames; f++)
            {
                float mono = 0f;
                int baseIdx = f * channels;
                for (int c = 0; c < channels; c++)
                    mono += _floatBuf[baseIdx + c];
                Push(mono / channels, sampleRate);
            }
        }
        else if (bytesPerSample == 2)
        {
            Marshal.Copy(pData, _shortBuf, 0, sampleCount);
            for (int f = 0; f < frames; f++)
            {
                float mono = 0f;
                int baseIdx = f * channels;
                for (int c = 0; c < channels; c++)
                    mono += _shortBuf[baseIdx + c] / 32768f;
                Push(mono / channels, sampleRate);
            }
        }
        else
        {
            ConsumeSilence(frames, sampleRate);
        }
    }

    private void ConsumeSilence(int frames, int sampleRate)
    {
        for (int f = 0; f < frames; f++)
            Push(0f, sampleRate);
    }

    private void Push(float sample, int sampleRate)
    {
        _diagSamples++;
        float amp = MathF.Abs(sample);
        if (amp > _diagMaxAmp) _diagMaxAmp = amp;

        _acc[_accFill++] = sample;
        if (_accFill < FftSize) return;

        ComputeSpectrum(sampleRate);

        Array.Copy(_acc, HopSize, _acc, 0, HopSize);
        _accFill = HopSize;
    }

    private void ComputeSpectrum(int sampleRate)
    {
        for (int i = 0; i < FftSize; i++)
        {
            _re[i] = _acc[i] * _hann[i];
            _im[i] = 0f;
        }

        Fft.Transform(_re, _im);

        int half = FftSize / 2;
        double fMax = Math.Min(FreqMaxCap, sampleRate / 2.0);
        double ratio = fMax / FreqMin;
        float invN = 1f / FftSize;

        Span<float> raw = stackalloc float[Bands];
        float frameMax = 0f;

        for (int b = 0; b < Bands; b++)
        {
            double fLo = FreqMin * Math.Pow(ratio, (double)b / Bands);
            double fHi = FreqMin * Math.Pow(ratio, (double)(b + 1) / Bands);

            int binLo = (int)(fLo * FftSize / sampleRate);
            int binHi = (int)(fHi * FftSize / sampleRate);
            binLo = Math.Clamp(binLo, 1, half - 1);
            binHi = Math.Clamp(binHi, binLo, half - 1);

            float peak = 0f;
            for (int bin = binLo; bin <= binHi; bin++)
            {
                float mag = MathF.Sqrt(_re[bin] * _re[bin] + _im[bin] * _im[bin]);
                if (mag > peak) peak = mag;
            }

            peak *= invN;
            raw[b] = peak;
            if (peak > frameMax) frameMax = peak;
        }

        _agc = MathF.Max(frameMax, _agc * AgcDecay);

        // Silence réel -> barres à zéro (on n'amplifie pas le bruit de fond).
        if (frameMax < SilenceMax)
        {
            lock (_lock)
                Array.Clear(_bands, 0, Bands);
            return;
        }

        // Normalisation RELATIVE au pic récent : indépendante du volume système.
        float reference = MathF.Max(_agc, MinRef);

        Span<float> outv = stackalloc float[Bands];
        for (int b = 0; b < Bands; b++)
        {
            float v = raw[b] / reference;            // ~0..1, quel que soit le volume
            v = MathF.Pow(v, Curve) * (float)Gain;   // réponse perceptuelle + gain
            if (v < RelGate) v = 0f;                 // coupe le bruit résiduel (relatif)
            outv[b] = Math.Clamp(v, 0f, 1f);
        }

        lock (_lock)
        {
            for (int b = 0; b < Bands; b++)
                _bands[b] = outv[b];
        }
    }

    private void DecayBands()
    {
        lock (_lock)
        {
            for (int b = 0; b < Bands; b++)
                _bands[b] *= 0.80f;
        }
    }

    private void EnsureCapacity(int sampleCount)
    {
        if (_floatBuf.Length < sampleCount) _floatBuf = new float[sampleCount];
        if (_shortBuf.Length < sampleCount) _shortBuf = new short[sampleCount];
    }
}
