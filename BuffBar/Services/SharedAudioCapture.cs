using System.Threading;

namespace BuffBar.Services;

/// <summary>
/// Enveloppe à comptage de références autour d'<see cref="AudioCapture"/>.
///
/// Problème résolu : avec une barre par moniteur, chaque VisualizerWidget créait sa
/// PROPRE capture loopback WASAPI + FFT. Sur deux écrans = deux threads MTA, deux
/// captures et deux FFT calculant exactement le même spectre. Pur gaspillage CPU.
///
/// Ici, une seule capture est démarrée au premier <see cref="Acquire"/> et arrêtée
/// au dernier <see cref="Release"/>. <see cref="AudioCapture.GetBands"/> étant déjà
/// protégé par verrou, plusieurs widgets peuvent lire la même instance sans risque.
/// </summary>
public static class SharedAudioCapture
{
    private static readonly object Gate = new();
    private static AudioCapture? _instance;
    private static int _refCount;

    /// <summary>Obtient la capture partagée, en la démarrant si c'est le premier client.</summary>
    public static AudioCapture Acquire()
    {
        lock (Gate)
        {
            if (_instance is null)
            {
                _instance = new AudioCapture();
                _instance.Start();
                Logger.Log("SharedAudioCapture: capture démarrée (1er client).");
            }
            _refCount++;
            return _instance;
        }
    }

    /// <summary>Libère une référence ; arrête la capture quand plus personne ne l'utilise.</summary>
    public static void Release()
    {
        lock (Gate)
        {
            if (_refCount == 0) return;
            _refCount--;
            if (_refCount == 0 && _instance is not null)
            {
                _instance.Stop();
                _instance = null;
                Logger.Log("SharedAudioCapture: capture arrêtée (dernier client).");
            }
        }
    }
}
