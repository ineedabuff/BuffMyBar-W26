using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace BuffBar.Services;

/// <summary>
/// Ordonnanceur de widgets : un seul <see cref="DispatcherTimer"/> partagé (cadence
/// de base) remplace la dizaine de timers de scrutation individuels. Chaque widget
/// s'abonne avec sa période ; tous les réveils sont regroupés sur le même battement,
/// ce qui réduit les réveils du thread UI (meilleure autonomie).
///
/// Affinité thread UI : <see cref="Subscribe"/>, la libération du jeton et les
/// rappels s'exécutent sur le thread de l'interface, exactement comme un
/// DispatcherTimer classique.
///
/// Restent volontairement natifs (ce ne sont pas de la scrutation régulière) :
/// le rendu du visualiseur (~30 FPS), les animations (glitch, marquee), et
/// quelques timers sous la seconde qui exigent de la réactivité (volume,
/// clignotement d'alerte, gardien de l'AppBar).
/// </summary>
public static class WidgetScheduler
{
    /// <summary>Cadence de base : les périodes plus courtes sont ramenées à ce pas.</summary>
    private static readonly TimeSpan BaseTick = TimeSpan.FromMilliseconds(500);

    private static readonly List<Entry> Entries = new();
    private static DispatcherTimer? _timer;

    private sealed class Entry
    {
        public required TimeSpan Interval;
        public required Action Callback;
        public double AccumulatedMs;
    }

    /// <summary>
    /// Abonne un rappel périodique. Retourne un jeton à libérer (Dispose) pour se
    /// désabonner — typiquement dans l'événement Unloaded du widget.
    /// </summary>
    public static IDisposable Subscribe(TimeSpan interval, Action callback)
    {
        var entry = new Entry
        {
            Interval = interval < BaseTick ? BaseTick : interval,
            Callback = callback
        };
        Entries.Add(entry);
        EnsureRunning();
        return new Token(entry);
    }

    private static void EnsureRunning()
    {
        if (_timer != null)
            return;

        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = BaseTick };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private static void OnTick(object? sender, EventArgs e)
    {
        // Copie : un rappel peut s'abonner ou se désabonner pendant l'itération.
        Entry[] snapshot = Entries.ToArray();
        double tickMs = BaseTick.TotalMilliseconds;

        foreach (Entry entry in snapshot)
        {
            entry.AccumulatedMs += tickMs;
            if (entry.AccumulatedMs + 0.5 < entry.Interval.TotalMilliseconds)
                continue;

            entry.AccumulatedMs = 0;
            try { entry.Callback(); }
            catch { /* un widget en échec ne doit pas casser les autres */ }
        }
    }

    private sealed class Token : IDisposable
    {
        private Entry? _entry;

        public Token(Entry entry) => _entry = entry;

        public void Dispose()
        {
            if (_entry is null)
                return;

            Entries.Remove(_entry);
            _entry = null;

            if (Entries.Count == 0 && _timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTick;
                _timer = null;
            }
        }
    }
}
