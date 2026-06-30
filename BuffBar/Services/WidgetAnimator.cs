using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BuffBar.Services;

/// <summary>
/// Micro-animations BuffMyBar.
/// Le glitch est volontairement court, visible et sans changement de layout important.
/// Il se dÃ©clenche seulement quand une valeur change.
/// </summary>
public static class WidgetAnimator
{
    private sealed class TextState
    {
        public int Version;
    }

    private static readonly ConditionalWeakTable<TextBlock, TextState> States = new();
    private static readonly Random Random = new();
    private static readonly char[] Blocks = new[] { '\u2588', '\u2593', '\u2592' };

    public static void SetText(TextBlock? target, string? value)
        => SetTextWithGlitch(target, value);

    public static void SetTextWithGlitch(TextBlock? target, string? value)
    {
        if (target is null) return;

        value ??= string.Empty;

        if (!target.Dispatcher.CheckAccess())
        {
            target.Dispatcher.Invoke(() => SetTextWithGlitch(target, value));
            return;
        }

        if (target.Text == value) return;

        // Premier affichage: direct, pour eviter un demarrage trop nerveux.
        if (string.IsNullOrEmpty(target.Text) || !target.IsLoaded)
        {
            target.Text = value;
            target.Opacity = 1.0;
            return;
        }

        var state = States.GetOrCreateValue(target);
        state.Version++;
        int version = state.Version;

        _ = RunGlitchAsync(target, value, state, version);
    }

    public static async Task GlitchAsync(TextBlock? target, string? value)
    {
        SetTextWithGlitch(target, value);
        await Task.CompletedTask;
    }

    public static void FadeText(TextBlock? target, string? value)
        => SetTextWithGlitch(target, value);

    // Alias historique utilisé par certains widgets (ex. Battery). Aligné sur les
    // autres raccourcis : délègue au glitch maison.
    public static void SetTextWithFade(TextBlock? target, string? value)
        => SetTextWithGlitch(target, value);

    private static async Task RunGlitchAsync(TextBlock target, string value, TextState state, int version)
    {
        string finalText = value ?? string.Empty;

        try
        {
            const int frames = 5;
            const int delayMs = 42; // ~210 ms total

            for (int i = 0; i < frames; i++)
            {
                if (state.Version != version) return;

                target.Opacity = i % 2 == 0 ? 0.82 : 1.0;
                target.Text = MakeGlitchFrame(finalText, i);
                await Task.Delay(delayMs).ConfigureAwait(true);
            }
        }
        finally
        {
            if (state.Version == version)
            {
                target.Text = finalText;
                target.Opacity = 1.0;
            }
        }
    }

    private static string MakeGlitchFrame(string text, int frame)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var chars = text.ToCharArray();
        int changed = 0;
        int wanted = Math.Max(1, Math.Min(4, chars.Length / 3 + 1));
        double chance = frame % 2 == 0 ? 0.48 : 0.32;

        lock (Random)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsWhiteSpace(c)) continue;

                // Ne pas remplacer tous les symboles Nerd Font: on garde surtout le texte/nombre lisible.
                bool replaceable = char.IsLetterOrDigit(c) || c == '.' || c == '%' || c == ':' || c == '-' || c == '_';
                if (!replaceable) continue;

                if (Random.NextDouble() <= chance)
                {
                    chars[i] = Blocks[Random.Next(Blocks.Length)];
                    changed++;
                    if (changed >= wanted) break;
                }
            }
        }

        if (changed == 0)
        {
            for (int i = chars.Length - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(chars[i]))
                {
                    chars[i] = Blocks[0];
                    break;
                }
            }
        }

        return new string(chars);
    }
}