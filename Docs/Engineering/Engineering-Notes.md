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

### Mesurer

`Docs/Engineering/measure-idle.ps1` échantillonne le CPU/RAM du processus pour
comparer avant/après. Mesurer trois cas : silence, musique, jeu plein écran.
