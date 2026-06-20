using System.Windows;

namespace BuffBar.Core;

/// <summary>
/// Contrat minimal pour tout module de la barre.
///
/// Les widgets futurs (Météo, Wi-Fi, Bluetooth, Média, OBS, Visualiseur audio,
/// Batterie, Volume, Luminosité) implémentent cette interface afin de pouvoir
/// être insérés dans une région (gauche / centre / droite) sans modifier le
/// cœur de l'application.
///
/// Convention :
///   - <see cref="View"/> est l'élément visuel encadré (généralement un UserControl
///     dont la racine utilise le style "ModuleBorder").
///   - Le widget gère lui-même son cycle de vie (timers, abonnements) via les
///     événements Loaded / Unloaded de son View.
/// </summary>
public interface IBarWidget
{
    /// <summary>Identifiant court et stable du widget (ex. "clock", "weather").</summary>
    string WidgetId { get; }

    /// <summary>Élément visuel à insérer dans une région de la barre.</summary>
    FrameworkElement View { get; }
}
