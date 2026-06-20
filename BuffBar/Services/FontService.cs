using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace BuffBar.Services;

/// <summary>
/// Résout au démarrage le nom EXACT de la police Nerd Font installée et le
/// pousse dans la ressource "AppFont". Évite l'écueil classique : selon la
/// variante installée, WPF attend "JetBrainsMono Nerd Font",
/// "JetBrainsMono Nerd Font Mono", "JetBrainsMono NF", etc. Un mauvais nom
/// fait retomber sur Consolas — qui n'a aucun glyphe Nerd Font.
/// </summary>
public static class FontService
{
    public static void Apply()
    {
        try
        {
            string? family = Resolve();
            Logger.Log($"FontService: police retenue = {family ?? "(aucune Nerd Font trouvée -> repli Consolas)"}");
            if (family is not null)
                Application.Current.Resources["AppFont"] =
                    new FontFamily($"{family}, Consolas, Segoe UI");
        }
        catch
        {
            // En cas d'échec on conserve le repli défini dans le thème.
        }
    }

    private static string? Resolve()
    {
        List<string> names = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        // Ordre de préférence : JetBrains Mono Nerd Font (Mono pour l'alignement),
        // puis toute JetBrains Mono, puis une police de symboles Nerd Font.
        string? pick =
            Exact(names, "JetBrainsMono Nerd Font Mono") ??
            Exact(names, "JetBrains Mono Nerd Font Mono") ??
            Exact(names, "JetBrainsMono Nerd Font") ??
            Exact(names, "JetBrains Mono Nerd Font") ??
            Exact(names, "JetBrainsMono NF") ??
            Exact(names, "JetBrains Mono NF") ??
            names.FirstOrDefault(n => Has(n, "jetbrains") && (Has(n, "nerd") || Has(n, "nf"))) ??
            names.FirstOrDefault(n => Has(n, "jetbrains mono")) ??
            Exact(names, "Symbols Nerd Font") ??
            names.FirstOrDefault(n => Has(n, "nerd font"));

        return pick;
    }

    private static string? Exact(List<string> names, string target)
        => names.FirstOrDefault(n => string.Equals(n, target, StringComparison.OrdinalIgnoreCase));

    private static bool Has(string source, string needle)
        => source.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
