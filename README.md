# BuffMyBar-W26

Barre supérieure native pour Windows 11, en C# / .NET 8 / WPF.
Complète la barre des tâches (ne la remplace pas), à la manière de **Waybar** sous
Linux ou des panneaux **KDE Plasma**.

Esthétique : fond noir, texte blanc, modules encadrés gris, survol gris,
JetBrainsMono Nerd Font.

---

## v1 (cette version)

- [x] Barre **AppBar native** (`SHAppBarMessage`) — réservation réelle de l'espace
      écran : les fenêtres maximisées ne la recouvrent jamais.
- [x] Positionnée en haut, hauteur 48 px (= barre des tâches Win 11), **DPI-aware** (PerMonitorV2).
- [x] **Heure + date** centrées (français, locale `fr-CA`), mise à jour à la seconde.
- [x] Démarrage automatique avec Windows (clé `Run`, idempotent).
- [x] Fenêtre outil (hors Alt+Tab / barre des tâches), instance unique, légère.
- [x] Passe sous les applications plein écran puis se restaure (comme la barre des tâches).
- [x] Architecture modulaire prête pour les widgets.

Disposition complète. Modules optionnels possibles plus tard.

## v0.9

- [x] **Persistance plein écran** — la barre reste toujours au-dessus (`KeepBarOnTop`)
      et un « gardien » réduit activement les fenêtres borderless/fenêtré plein écran à
      la zone de travail, sous la barre (`ReclaimFullscreenWindows`). Les deux options
      dans `BarConfig`. Limite : le plein écran **exclusif** ne peut pas être recouvert
      (limite Windows) — jouer en mode borderless.

## v0.8

- [x] **Multi-écrans** — une barre persistante (AppBar) **par moniteur** : énumération
      via `EnumDisplayMonitors`, réservation d'espace et DPI calculés par écran
      (`GetDpiForMonitor`). Chaque écran a sa copie complète des modules.
- [x] **Uptime** (entre météo et réseau) — temps depuis le démarrage (j / h / m).
- [x] Visualiseur repassé en **blanc**.
- [x] Volume coloré par niveau : > 15 % en `#FF3131`, > 10 % en `#ddff24`, sinon blanc.
- [x] Réordonné à droite : Visualiseur · Volume · Bluetooth · Batterie.

## v0.7

- [x] **Réseau** (gauche) — une seule IP à la fois, alternance locale (icône maison) /
      publique (icône globe) toutes les ~3 s. IP locale via socket, IP publique via
      service HTTP (rafraîchie périodiquement). Détail dans l'infobulle.
- [x] **Bluetooth** (entre batterie et volume) — nom + batterie du dispositif connecté
      (WinRT, classique + BLE). Plusieurs appareils : alternance toutes les ~3 s.
      Se masque si aucun. Le % de batterie dépend du support du périphérique/pilote.

## v0.6

- [x] **OBS** (au centre, à droite de l'heure) — état d'enregistrement en direct via
      **obs-websocket v5** (client `ClientWebSocket` natif, handshake + auth SHA256).
      Inactif : `● REC` blanc fixe. Enregistrement : `● REC` en `#FF3131` clignotant.
      Reconnexion automatique. Configurer hôte/port/mot de passe dans `BarConfig`.

## v0.5

- [x] **Météo** — wttr.in (JSON `j1`, descriptions en français) pour l'emplacement
      défini dans `BarConfig.WeatherLocation` (défaut : Montréal). Icône Font Awesome
      selon les conditions + température ; description et ressenti dans l'infobulle.
      Rafraîchit toutes les 15 min, conserve la dernière valeur en cas d'échec réseau.

## v0.4

- [x] **Média** — session active du système via WinRT
      (`GlobalSystemMediaTransportControlsSessionManager`, équivalent MPRIS/playerctl) :
      icône lecture/pause + titre — artiste. **Clic** = lecture/pause, **molette** =
      piste suivante/précédente. Se masque quand aucun lecteur n'est actif.
      (Le TFM passe à `net8.0-windows10.0.19041.0` pour exposer la projection WinRT,
      toujours sans paquet externe.)

## v0.3

- [x] **Visualiseur audio** (style Cava) — capture **loopback WASAPI** en COM pur,
      FFT radix-2 maison, 20 bandes logarithmiques (40 Hz → 16 kHz), lissage
      attaque/chute. Barres en accent `#ddff24`. Thread d'arrière-plan dédié,
      ré-initialisation automatique au changement de périphérique. Aucune dépendance.

## v0.2

- [x] **Batterie** — `GetSystemPowerStatus`, glyphe Nerd Font par niveau + éclair en
      charge ; se masque sur un poste fixe sans batterie.
- [x] **Volume** — Core Audio (COM, sans bibliothèque tierce) : glyphe à trois paliers
      + pourcentage. **Molette** = ±2 %, **clic gauche** = muet (grisé). Robuste au
      changement de périphérique (casque branché à chaud).

---

## Prérequis

- **.NET 8 SDK** (`dotnet --version` ≥ 8.0) — https://dotnet.microsoft.com/download
- **JetBrainsMono Nerd Font** installée (sinon repli automatique sur Consolas).

---

## Compiler / Lancer

Double-clic sur `build.bat` puis `run.bat`, ou bien :

```powershell
dotnet build -c Release
dotnet run --project BuffBar\BuffBar.csproj -c Release
```

Ouverture dans Visual Studio : `BuffBar.sln`.

L'EXE final : `BuffBar\bin\Release\net8.0-windows\BuffBar.exe`.

**Quitter** : clic droit sur la barre → *Quitter BuffBar*.

---

## Architecture

```
BuffBar/
├── App.xaml(.cs)            Instance unique + activation du démarrage auto
├── MainWindow.xaml(.cs)     Coquille de la barre : régions Gauche/Centre/Droite + cycle AppBar
├── app.manifest             Conscience DPI PerMonitorV2 (positionnement pixel correct)
├── Core/
│   ├── BarConfig.cs         Hauteur, démarrage auto (config centrale)
│   └── IBarWidget.cs        Contrat des modules
├── Interop/
│   ├── NativeMethods.cs     P/Invoke Win32 (shell32 / user32) — zéro dépendance
│   └── AppBarManager.cs     Cœur AppBar : ABM_NEW/QUERYPOS/SETPOS/REMOVE, DPI, notifications
├── Services/
│   └── AutoStartService.cs  Démarrage Windows (HKCU\...\Run)
├── Themes/
│   └── BuffTheme.xaml       Palette, police, style "ModuleBorder" + survol
└── Widgets/
    └── Clock/
        ├── ClockWidget.xaml(.cs)   Heure + date centrées
```

### Ajouter un widget

1. Créer `Widgets/MonModule/MonModuleWidget.xaml(.cs)`, racine = `Border` avec
   `Style="{StaticResource ModuleBorder}"`, implémentant `IBarWidget`.
2. L'insérer dans une région depuis `MainWindow.ComposeWidgets()` :

```csharp
LeftRegion.Children.Add(new WeatherWidget());
RightRegion.Children.Add(new BatteryWidget());
```

Aucune modification du cœur AppBar n'est nécessaire.

---

## Multi-écrans

La v1 cale la barre sur l'**écran principal**. Pour une barre par moniteur,
instanciez une `MainWindow` + un `AppBarManager` par écran (énumération via
`MonitorFromWindow` / `GetMonitorInfo`, déjà importés dans `NativeMethods`).
C'est l'évolution prévue dès qu'un premier widget tournera proprement.

---

## Notes techniques

- **DPI** : la hauteur (48 DIP) est convertie en pixels physiques via le facteur
  d'échelle de l'écran hôte, donc correcte à 100/125/150/175 %.
- **Plein écran exclusif** : comme la barre des tâches Windows, BuffBar passe
  derrière une application plein écran (jeu, vidéo) puis se restaure — comportement
  normal et voulu.
- **Zéro dépendance** : aucun Electron / Python / processus externe. Uniquement
  WPF + P/Invoke Win32 du framework.
