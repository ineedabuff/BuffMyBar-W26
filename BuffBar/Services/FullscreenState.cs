using System;

namespace BuffBar.Services;

/// <summary>
/// État global « une application plein écran est au premier plan » (jeu,
/// vidéo plein écran, présentation…).
///
/// Alimenté par les notifications shell <c>ABN_FULLSCREENAPP</c> reçues par
/// chaque AppBar (voir <see cref="BuffBar.Interop.AppBarManager"/>). Les widgets
/// décoratifs coûteux — au premier chef le visualiseur audio à 30 FPS — s'y
/// abonnent pour se mettre en veille et rendre tout le CPU/GPU à l'application
/// de premier plan.
///
/// La notification shell est globale (tous moniteurs). Mettre en veille les
/// visualiseurs de toutes les barres pendant un jeu plein écran est le
/// comportement souhaité : on maximise le budget de la fenêtre active.
/// </summary>
public static class FullscreenState
{
    private static bool _active;

    /// <summary>Vrai si une application plein écran est actuellement au premier plan.</summary>
    public static bool IsActive => _active;

    /// <summary>Émis uniquement lors d'une transition réelle de l'état.</summary>
    public static event Action<bool>? Changed;

    /// <summary>
    /// Signale l'état courant. Idempotent : plusieurs barres reçoivent la même
    /// notification shell, mais l'événement n'est déclenché que sur transition.
    /// </summary>
    public static void Set(bool active)
    {
        if (_active == active)
            return;

        _active = active;
        Changed?.Invoke(active);
    }
}
