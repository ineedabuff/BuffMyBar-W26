# Engineering Notes

## Performance / idle cost

Objectifs (README) : CPU idle < 0.5 %, RAM < 80 Mo, démarrage < 1 s.

### Visualiseur audio

Le poste le plus coûteux au repos. Deux garde-fous :

- **Veille au silence** — quand le spectre est au repos (toutes les barres ≈ 0),
  le rendu passe de ~30 FPS à ~5 FPS et cesse tout `InvalidateVisual`. Réveil
  immédiat au retour du son (la latence ≤ 200 ms est masquée par la rampe
  d'attaque). Voir `Widgets/Visualizer/VisualizerWidget.xaml.cs`.
- **Pause plein écran** — le rendu s'arrête complètement tant qu'une application
  plein écran (jeu) est au premier plan, via la notification shell
  `ABN_FULLSCREENAPP` exposée par `Services/FullscreenState.cs`.

> ⚠️ La pause plein écran ne s'applique **qu'aux widgets décoratifs** (visualiseur).
> `ABN_FULLSCREENAPP` est une notification **globale** (tous moniteurs) : impossible
> de savoir quel écran est couvert. Les widgets **informatifs** (indicateurs
> système, réseau) ne sont donc PAS mis en pause — un joueur surveille souvent
> CPU/temp/latence sur un second écran pendant qu'il joue.

### Journalisation

`Logger.Verbose(...)` est coupé par défaut : la capture audio n'écrit plus une
ligne sur disque à chaque seconde. Activer via `BUFFBAR_VERBOSE=1` (ou build
Debug) pour un rapport de bug. Voir `Services/Logger.cs`.

### Ordonnanceur de widgets

`Services/WidgetScheduler.cs` : un seul `DispatcherTimer` (cadence de base 500 ms)
remplace la dizaine de timers de scrutation. Chaque widget s'abonne avec sa
période (`Subscribe(interval, callback)`) et libère le jeton dans `Unloaded`.
Tous les réveils sont regroupés sur le même battement.

Migrés : horloge, média, batterie, uptime, météo, indicateurs système (métriques
2 s), réseau (cycle/refresh/ping), Bluetooth (cycle/refresh), `ObsProcessWatcher`.

Restent **natifs** (pas de la scrutation régulière, ou réactivité requise) :
rendu du visualiseur (~30 FPS), animations glitch/marquee, clignotement d'alerte
(650 ms), volume (400 ms), gardien de l'AppBar (800 ms), anti-rebond d'affichage.

### Mesurer

`Docs/Engineering/measure-idle.ps1` échantillonne le CPU/RAM du processus pour
comparer avant/après. Mesurer trois cas : silence, musique, jeu plein écran.
