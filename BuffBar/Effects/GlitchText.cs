using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace BuffBar.Effects;

/// <summary>
/// Effet « glitchy text reveal » (décryptage) inspiré du pen de jh3y
/// (https://codepen.io/jh3y/pen/mdyymOR), réécrit en WPF natif — aucune
/// dépendance. Chaque caractère défile à travers des symboles aléatoires avant
/// de se figer sur sa vraie valeur, avec une révélation en cascade de gauche à
/// droite (les premiers caractères se stabilisent avant les suivants).
///
/// Usage (XAML), sans toucher au code-behind des widgets :
///   xmlns:fx="clr-namespace:BuffBar.Effects"
///   &lt;TextBlock fx:GlitchText.RevealOnLoad="True" .../&gt;        // décrypte à l'apparition
///   &lt;TextBlock fx:GlitchText.RevealOnChange="True" .../&gt;      // décrypte à chaque nouvelle valeur
///   &lt;TextBlock fx:GlitchText.AccentWhileGlitching="{DynamicResource Accent}" .../&gt;
///
/// Le contrôle reste piloté normalement (le code-behind continue d'écrire
/// TextBlock.Text) : on intercerte les changements de Text et on les rejoue en
/// glitch. Les espaces sont préservés pour ne pas faire « sauter » la largeur
/// du module dans la barre.
/// </summary>
public static class GlitchText
{
    // Jeu de symboles « glitch ». ASCII + blocs de remplissage très « terminal ».
    private const string Pool =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789" +
        "!<>-_\\/[]{}=+*^?#@%&$" +
        "░▒▓█▚▞▙▟▜▛";

    // Réglages du rendu (frame ≈ 28 ms -> ~35 fps).
    private const int TickMs = 28;
    private const int BaseFrames = 2;     // délai avant que le 1er caractère se fige
    private const int SpreadFrames = 2;   // décalage de figement entre caractères (cascade)
    private const int JitterFrames = 2;   // aléa ajouté par caractère (naturel)

    private static readonly Random Rng = new();

    // État interne attaché à chaque TextBlock, sans fuite mémoire.
    private sealed class State
    {
        public DispatcherTimer? Timer;
        public DependencyPropertyDescriptor? TextDescriptor;
        public EventHandler? TextHandler;
        public bool Suppress;         // ignore nos propres écritures de Text
        public bool Revealed;         // RevealOnLoad : déjà joué une fois ?
        public Brush? OriginalBrush;  // couleur à restaurer après le glitch
        public bool ForegroundWasLocal; // la couleur d'origine venait-elle d'une valeur locale ?
    }

    private static readonly ConditionalWeakTable<TextBlock, State> States = new();
    private static State GetState(TextBlock tb) => States.GetValue(tb, _ => new State());

    // ---------------------------------------------------------------- RevealOnLoad

    public static readonly DependencyProperty RevealOnLoadProperty =
        DependencyProperty.RegisterAttached(
            "RevealOnLoad", typeof(bool), typeof(GlitchText),
            new PropertyMetadata(false, OnRevealOnLoadChanged));

    public static bool GetRevealOnLoad(DependencyObject o) => (bool)o.GetValue(RevealOnLoadProperty);
    public static void SetRevealOnLoad(DependencyObject o, bool v) => o.SetValue(RevealOnLoadProperty, v);

    // ---------------------------------------------------------------- RevealOnChange

    public static readonly DependencyProperty RevealOnChangeProperty =
        DependencyProperty.RegisterAttached(
            "RevealOnChange", typeof(bool), typeof(GlitchText),
            new PropertyMetadata(false, OnRevealOnChangeChanged));

    public static bool GetRevealOnChange(DependencyObject o) => (bool)o.GetValue(RevealOnChangeProperty);
    public static void SetRevealOnChange(DependencyObject o, bool v) => o.SetValue(RevealOnChangeProperty, v);

    // ---------------------------------------------------------------- AccentWhileGlitching

    public static readonly DependencyProperty AccentWhileGlitchingProperty =
        DependencyProperty.RegisterAttached(
            "AccentWhileGlitching", typeof(Brush), typeof(GlitchText),
            new PropertyMetadata(null));

    public static Brush? GetAccentWhileGlitching(DependencyObject o) => (Brush?)o.GetValue(AccentWhileGlitchingProperty);
    public static void SetAccentWhileGlitching(DependencyObject o, Brush? v) => o.SetValue(AccentWhileGlitchingProperty, v);

    // ---------------------------------------------------------------- Onde (WaveMember)

    // Participants à l'onde de glitch (ex. déclenchée au changement de chanson).
    // ConditionalWeakTable-like via HashSet : on ajoute au Loaded, on retire au Unloaded,
    // donc aucune référence ne survit à la fenêtre.
    private static readonly HashSet<TextBlock> WaveMembers = new();

    public static readonly DependencyProperty WaveMemberProperty =
        DependencyProperty.RegisterAttached(
            "WaveMember", typeof(bool), typeof(GlitchText),
            new PropertyMetadata(false, OnWaveMemberChanged));

    public static bool GetWaveMember(DependencyObject o) => (bool)o.GetValue(WaveMemberProperty);
    public static void SetWaveMember(DependencyObject o, bool v) => o.SetValue(WaveMemberProperty, v);

    private static void OnWaveMemberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb) return;

        if (e.NewValue is true)
        {
            void Add(object? _, RoutedEventArgs __) => WaveMembers.Add(tb);
            if (tb.IsLoaded) WaveMembers.Add(tb);
            else tb.Loaded += Add;
            tb.Unloaded += (_, _) => WaveMembers.Remove(tb);
        }
        else
        {
            WaveMembers.Remove(tb);
        }
    }

    /// <summary>
    /// Déclenche une onde de décryptage qui balaie la barre de GAUCHE à DROITE.
    /// Chaque participant est joué avec un retard proportionnel à sa position X
    /// réelle à l'écran, normalisé pour que l'onde dure toujours
    /// <paramref name="totalMs"/> quelle que soit la largeur de la barre.
    /// L'onde reste cantonnée à la fenêtre du déclencheur (une barre par moniteur).
    /// </summary>
    public static void TriggerWave(DependencyObject source, double totalMs = 700)
    {
        Window? win = Window.GetWindow(source);
        if (win is null) return;

        // Participants de CETTE barre, avec leur X absolu dans la fenêtre.
        var items = new List<(TextBlock Tb, double X)>();
        foreach (TextBlock tb in WaveMembers)
        {
            if (!tb.IsLoaded || Window.GetWindow(tb) != win) continue;
            double x;
            try { x = tb.TransformToAncestor(win).Transform(new Point(0, 0)).X; }
            catch { x = 0; }
            items.Add((tb, x));
        }
        if (items.Count == 0) return;

        double min = items.Min(i => i.X);
        double max = items.Max(i => i.X);
        double span = Math.Max(1, max - min);

        foreach (var (tb, x) in items)
        {
            double delay = (x - min) / span * totalMs;
            if (delay <= 1)
            {
                Play(tb, tb.Text);
            }
            else
            {
                var t = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(delay)
                };
                t.Tick += (_, _) =>
                {
                    t.Stop();
                    Play(tb, tb.Text);
                };
                t.Start();
            }
        }
    }

    // ---------------------------------------------------------------- Câblage

    private static void OnRevealOnLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb || e.NewValue is not true) return;

        void Hook(object? _, RoutedEventArgs __)
        {
            // Si une valeur est déjà présente, on la révèle ; sinon on attend la
            // première valeur réelle écrite par le widget.
            if (!string.IsNullOrEmpty(tb.Text))
                RevealOnce(tb);
            else
                WatchFirstValue(tb, once: true);
        }

        if (tb.IsLoaded) Hook(null, default!);
        else tb.Loaded += Hook;
    }

    private static void OnRevealOnChangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb || e.NewValue is not true) return;
        void Hook(object? _, RoutedEventArgs __) => WatchFirstValue(tb, once: false);
        if (tb.IsLoaded) Hook(null, default!);
        else tb.Loaded += Hook;
    }

    private static void RevealOnce(TextBlock tb)
    {
        var st = GetState(tb);
        if (st.Revealed) return;
        st.Revealed = true;
        Play(tb, tb.Text);
    }

    // Observe les écritures externes sur Text pour les rejouer en glitch.
    private static void WatchFirstValue(TextBlock tb, bool once)
    {
        var st = GetState(tb);
        if (st.TextDescriptor is not null) return; // déjà observé

        st.TextDescriptor = DependencyPropertyDescriptor.FromProperty(
            TextBlock.TextProperty, typeof(TextBlock));

        st.TextHandler = (_, _) =>
        {
            if (st.Suppress) return;                 // c'est nous qui écrivons
            string target = tb.Text;
            if (string.IsNullOrEmpty(target)) return;

            if (once)
            {
                // Reveal unique : on se détache après le premier coup.
                st.TextDescriptor?.RemoveValueChanged(tb, st.TextHandler!);
                st.TextHandler = null;
                st.TextDescriptor = null;
            }
            Play(tb, target);
        };

        st.TextDescriptor.AddValueChanged(tb, st.TextHandler);

        // Si une valeur initiale existe déjà, on la traite tout de suite.
        if (!string.IsNullOrEmpty(tb.Text))
            st.TextHandler(tb, EventArgs.Empty);

        tb.Unloaded += (_, _) => StopAndDetach(tb);
    }

    private static void StopAndDetach(TextBlock tb)
    {
        if (!States.TryGetValue(tb, out var st)) return;
        st.Timer?.Stop();
        st.Timer = null;
        if (st.TextDescriptor is not null && st.TextHandler is not null)
            st.TextDescriptor.RemoveValueChanged(tb, st.TextHandler);
        st.TextDescriptor = null;
        st.TextHandler = null;
    }

    // ---------------------------------------------------------------- Animation

    /// <summary>
    /// Joue le décryptage du TextBlock vers <paramref name="finalText"/>.
    /// Public pour un déclenchement manuel (survol, clic, etc.).
    /// </summary>
    public static void Play(TextBlock tb, string finalText)
    {
        if (tb is null) return;
        finalText ??= string.Empty;

        var st = GetState(tb);
        st.Timer?.Stop();

        // Couleur accent éventuelle pendant le glitch.
        Brush? accent = GetAccentWhileGlitching(tb);
        if (accent is not null)
        {
            if (st.OriginalBrush is null)
            {
                // On mémorise la SOURCE de la couleur d'origine : locale (posée en
                // code-behind : ping, volume, OBS…) ou héritée du style/thème
                // (DynamicResource). C'est ce qui permet de rendre la main au thème
                // à la fin du glitch au lieu de figer une couleur périmée.
                var source = DependencyPropertyHelper.GetValueSource(tb, TextBlock.ForegroundProperty);
                st.ForegroundWasLocal = source.BaseValueSource == BaseValueSource.Local;
                st.OriginalBrush = tb.Foreground;
            }
            tb.Foreground = accent;
        }

        int n = finalText.Length;
        if (n == 0)
        {
            WriteSuppressed(tb, st, string.Empty);
            RestoreBrush(tb, st);
            return;
        }

        // Frame de figement par caractère (cascade + aléa). Les espaces se figent
        // immédiatement pour préserver la largeur et la structure.
        var settle = new int[n];
        int last = 0;
        for (int i = 0; i < n; i++)
        {
            settle[i] = char.IsWhiteSpace(finalText[i])
                ? 0
                : BaseFrames + i * SpreadFrames + Rng.Next(JitterFrames + 1);
            if (settle[i] > last) last = settle[i];
        }

        int frame = 0;
        var buf = new StringBuilder(n);

        var timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(TickMs)
        };
        st.Timer = timer;

        timer.Tick += (_, _) =>
        {
            buf.Clear();
            for (int i = 0; i < n; i++)
            {
                char target = finalText[i];
                if (frame >= settle[i] || char.IsWhiteSpace(target))
                    buf.Append(target);
                else
                    buf.Append(Pool[Rng.Next(Pool.Length)]);
            }
            WriteSuppressed(tb, st, buf.ToString());

            if (frame++ >= last)
            {
                timer.Stop();
                st.Timer = null;
                WriteSuppressed(tb, st, finalText); // garantit la valeur exacte
                RestoreBrush(tb, st);
            }
        };

        timer.Start();
    }

    // Écrit Text sans redéclencher notre propre interception.
    private static void WriteSuppressed(TextBlock tb, State st, string value)
    {
        st.Suppress = true;
        tb.Text = value;
        st.Suppress = false;
    }

    private static void RestoreBrush(TextBlock tb, State st)
    {
        if (st.OriginalBrush is null) return;

        if (st.ForegroundWasLocal)
        {
            // La couleur d'origine était posée localement (ex. tiers de volume,
            // rouge d'enregistrement OBS) : on la remet telle quelle.
            tb.Foreground = st.OriginalBrush;
        }
        else
        {
            // La couleur venait du style/thème : on efface la valeur locale posée
            // par le glitch pour que le {DynamicResource PrimaryText} du thème
            // reprenne la main. Sans ça, le texte reste figé sur l'ancienne couleur
            // et devient illisible après un passage au thème Windows.
            tb.ClearValue(TextBlock.ForegroundProperty);
        }

        st.OriginalBrush = null;
        st.ForegroundWasLocal = false;
    }
}
