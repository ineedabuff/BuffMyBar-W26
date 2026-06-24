# BuffBar

Barre supérieure native pour Windows 11, en **C# / .NET 8 / WPF**.
Complète la barre des tâches (ne la remplace pas), à la manière de **Waybar** sous
Linux ou des panneaux **KDE Plasma**.

Esthétique « buff » : fond noir `#000000`, texte blanc, accent vert-jaune
`#ddff24`, modules encadrés, **JetBrainsMono Nerd Font**. La barre peut aussi
**suivre les couleurs de Windows 11** (clair/sombre + accentuation) et afficher un
**fond acrylique translucide** comme la barre des tâches.

Tout est **configurable par JSON** (`settings.json`, façon Waybar) **et** par une
**fenêtre Paramètres** (clic droit → *Paramètres…*) : thème, hauteur, ville météo,
mode jeu, OBS, et activation de chaque widget.

> **Zéro dépendance** : aucun Electron / Python / processus externe. Uniquement
> WPF + P/Invoke Win32 + projection WinRT, tout du framework.

---

## Disposition

```
[Météo] [Uptime] [Réseau] [Média ········· ]     [Heure] [● REC]     [ ········· Visualiseur] [Volume] [Bluetooth] [Batterie]
└─────────── gauche (Média extensible) ──────┘   └ centre (centré) ┘   └──────────── droite (alignée à droite) ───────────┘
```

- **Gauche** — Météo · Uptime · Réseau · Média (le média occupe tout l'espace restant).
- **Centre** — Horloge (+ OBS à sa droite), réellement centrée à l'écran.
- **Droite** — Visualiseur · Volume · Bluetooth · Batterie.

Au **survol** : pointer l'**heure** ouvre le calendrier, pointer la **météo** ouvre les prévisions.

---

## Historique des versions

### v2.0 — Configuration JSON + fenêtre Paramètres *(version actuelle)*

- [x] **`settings.json`** (façon Waybar) sous `%AppData%\BuffMyBar-W26\` : thème,
      hauteur, ville météo, mode jeu, acrylique, accent externe, OBS et activation
      par widget. Créé automatiquement au premier lancement.
- [x] **Thèmes en fichiers** `themes\*.json` (`buff`, `windows`, `cyber`) — palette
      éditable ; `windows.json` suit le thème du système. Ajoute ton propre `.json`
      pour un thème maison.
- [x] **Fenêtre Paramètres** (clic droit → *Paramètres…*) : thème, hauteur, ville,
      mode jeu, acrylique, accent externe, **cases d'activation par widget** et
      réglages **OBS** (hôte/port/mot de passe). Application en direct à l'enregistrement.
- [x] **Mode jeu** : ajoute la **latence (ping)** en tête du widget réseau, colorée
      selon le niveau.
- [x] **Widgets activables/désactivables** ; **hauteur**, **météo** et **OBS**
      entièrement configurables.
- [x] Le système de réglages par registre est remplacé par le JSON (source unique).

### v1.6 — Accent inversé sur le moniteur externe

- [x] **Option « Moniteur externe : fond accent #ddff24 »** (clic droit →
      *Couleurs de la barre*). Sur l'écran **externe** uniquement : fond de barre
      `#ddff24`, fonds des widgets **noirs**, police et icônes en `#ddff24`.
      L'écran principal n'est pas affecté.
- [x] Mécanique : les styles de module passent en `DynamicResource`, ce qui rend
      les pinceaux **surchargeables par fenêtre** ; la barre externe pose sa propre
      surcharge locale (`Window.Resources`), réversible et persistante (registre).
- [x] Les couleurs **sémantiques** (REC d'OBS, paliers de volume) restent inchangées.

### v1.5 — Redémarrage + auto-récupération

- [x] **Clic droit → « Redémarrer la barre »** : ferme et recrée toutes les barres
      pour les moniteurs actuellement connectés (réinitialise la réservation d'espace
      AppBar et l'acrylique).
- [x] **Auto-récupération** après veille / extinction d'écran : l'app écoute
      `DisplaySettingsChanged` et `PowerModeChanged` (réveil) et reconstruit les
      barres automatiquement, avec **anti-rebond** (~1,2 s) pour absorber les rafales
      d'événements. Corrige les bugs d'AppBar sur le 2ᵉ moniteur.
- [x] `ShutdownMode = OnExplicitShutdown` : la reconstruction des barres ne ferme
      plus l'application par mégarde (sortie uniquement via *Quitter*).

### v1.4 — Sélecteur de couleurs

- [x] **Clic droit → « Couleurs de la barre »** : choix entre **Suivre Windows**
      (clair/sombre + accent, comme la barre des tâches) et **Buff** (fond `#000000`,
      accent `#ddff24`, texte blanc). Bascule **en direct** sur tous les écrans.
- [x] Choix **persistant** entre deux lancements via `SettingsService`
      (registre `HKCU\Software\BuffBar`). `BarConfig.FollowWindowsTheme` ne sert plus
      que de valeur par défaut au premier démarrage.

### v1.3 — Flyouts interactifs

- [x] **Survol = applet** (façon YASB). Utilitaire `HoverPopup` : ouverture au
      survol du module, fermeture différée (~280 ms) tant que le pointeur est sur
      le module **ou** sur le popup (pour pouvoir y entrer sans qu'il se referme).
- [x] **Horloge → calendrier** : mini-calendrier mensuel français (semaine
      débutant le lundi), navigation mois ‹ ›, jour courant en accent `#ddff24`,
      recentré sur le mois courant à chaque ouverture.
- [x] **Météo → applet détaillée** : grande icône + température, ressenti /
      humidité / vent, et **prévisions sur 3 jours** (icône, max/min). Les données
      sont déjà en cache → aucune requête réseau au survol.
- [x] Styles de flyout dans `BuffTheme.xaml` : fond volontairement opaque (lisible
      même quand la barre est en acrylique), liseré accent, coins arrondis, ombre.

### v1.2 — Disposition dynamique + météo locale

- [x] **Réseau à largeur fixe** — le module ne change plus de taille lors de
      l'alternance IP locale / publique.
- [x] **Média extensible** — grille 3 colonnes (`*` / `Auto` / `*`) + `DockPanel` :
      le module Média remplit tout l'espace restant jusqu'à l'horloge, qui demeure
      parfaitement centrée.
- [x] **Visualiseur élargi** — largeur doublée et **64 barres** (FFT portée à 2048
      pour une meilleure résolution dans les basses fréquences).
- [x] **Météo** recentrée sur **Mascouche, Québec** (`BarConfig.WeatherLocation`).

### v1.1 — Fond acrylique

- [x] **Acrylique translucide natif** (DWM `DWMWA_SYSTEMBACKDROP_TYPE`), comme la
      barre des tâches Windows 11. Repli sûr en opaque si le système ne le supporte
      pas. Option `BarConfig.UseAcrylicBackdrop`.
- [x] **Accent buff conservable** — option `BarConfig.KeepBuffAccent` pour garder
      `#ddff24` même lorsque la barre suit le thème de Windows (sinon elle adopte
      la couleur d'accentuation du système).

### v1.0 — Thème Windows 11

- [x] La barre suit le **mode clair/sombre** et la **couleur d'accentuation** de la
      barre des tâches (`SystemUsesLightTheme`, `ColorPrevalence`, accent système),
      avec mise à jour en direct. Désactivable via `BarConfig.FollowWindowsTheme`.
      Les couleurs sémantiques (REC, paliers de volume) restent fixes.

### v0.9 — Persistance plein écran

- [x] La barre reste toujours au-dessus (`KeepBarOnTop`) et un « gardien » réduit
      activement les fenêtres borderless / fenêtré plein écran à la zone de travail,
      sous la barre (`ReclaimFullscreenWindows`). Limite : le plein écran
      **exclusif** ne peut pas être recouvert (limite Windows) — jouer en borderless.

### v0.8 — Multi-écrans + Uptime

- [x] **Multi-écrans** — une barre persistante (AppBar) **par moniteur** :
      énumération via `EnumDisplayMonitors`, réservation d'espace et DPI calculés
      par écran (`GetDpiForMonitor`). Chaque écran a sa copie complète des modules.
- [x] **Uptime** (entre météo et réseau) — temps depuis le démarrage (j / h / m).
- [x] Visualiseur repassé en **blanc** ; volume coloré par niveau (> 15 % `#FF3131`,
      > 10 % `#ddff24`, sinon blanc) ; ordre à droite : Visualiseur · Volume ·
      Bluetooth · Batterie.

### v0.7 — Réseau + Bluetooth

- [x] **Réseau** (gauche) — une IP à la fois, alternance locale (maison) / publique
      (globe) toutes les ~3 s. IP locale via socket, publique via HTTP. Détail en
      infobulle.
- [x] **Bluetooth** — nom + batterie du dispositif connecté (WinRT, classique + BLE),
      alternance si plusieurs, masqué si aucun. Le % dépend du support du pilote.

### v0.6 — OBS

- [x] État d'enregistrement en direct via **obs-websocket v5** (client
      `ClientWebSocket` natif, handshake + auth SHA256). Inactif : `● REC` blanc fixe.
      Enregistrement : `● REC` `#FF3131` clignotant. Reconnexion automatique.
      Hôte / port / mot de passe dans `BarConfig`.

### v0.5 — Météo

- [x] **wttr.in** (JSON `j1`, descriptions en français) selon
      `BarConfig.WeatherLocation`. Icône Font Awesome + température, détail en
      infobulle. Rafraîchit toutes les 15 min, conserve la dernière valeur en cas
      d'échec réseau.

### v0.4 — Média

- [x] Session active du système via WinRT
      (`GlobalSystemMediaTransportControlsSessionManager`, équivalent MPRIS) : icône
      lecture/pause + titre défilant. **Clic** = lecture/pause, **molette** = piste
      suivante/précédente. Masqué quand aucun lecteur n'est actif. (Le TFM passe à
      `net8.0-windows10.0.19041.0` pour exposer WinRT, toujours sans paquet externe.)

### v0.3 — Visualiseur audio

- [x] Style Cava : capture **loopback WASAPI** en COM pur, FFT radix-2 maison,
      bandes logarithmiques, lissage attaque/chute. Thread d'arrière-plan dédié,
      ré-initialisation au changement de périphérique. Aucune dépendance.

### v0.2 — Batterie + Volume

- [x] **Batterie** — `GetSystemPowerStatus`, glyphe par niveau + éclair en charge,
      masquée sur poste fixe sans batterie.
- [x] **Volume** — Core Audio (COM) : glyphe à trois paliers + pourcentage.
      **Molette** = ±2 %, **clic** = muet. Robuste au changement de périphérique.

### Socle — AppBar native

- [x] Barre **AppBar native** (`SHAppBarMessage`) — réservation réelle de l'espace
      écran : les fenêtres maximisées ne la recouvrent jamais.
- [x] Positionnée en haut, hauteur 48 px, **DPI-aware** (PerMonitorV2).
- [x] **Heure + date** centrées (locale `fr-CA`), mise à jour à la seconde.
- [x] Démarrage automatique avec Windows (clé `Run`, idempotent).
- [x] Fenêtre outil (hors Alt+Tab / barre des tâches), instance unique, légère.
- [x] Architecture modulaire (`IBarWidget`) prête pour les widgets.

---

## Prérequis

- **.NET 8 SDK** (`dotnet --version` ≥ 8.0) — https://dotnet.microsoft.com/download
- **JetBrainsMono Nerd Font** installée (sinon repli automatique sur Consolas).
- *(optionnel)* **OBS** + **obs-websocket v5** pour le module REC.

---

## Compiler / Lancer

Double-clic sur `build.bat` puis `run.bat`, ou bien :

```powershell
dotnet build -c Release
dotnet run --project BuffBar\BuffBar.csproj -c Release
```

Ouverture dans Visual Studio : `BuffBar.sln`.
L'EXE final : `BuffBar\bin\Release\net8.0-windows10.0.19041.0\BuffBar.exe`.

---

## Menu contextuel (clic droit sur la barre)

- **Paramètres…** — ouvre la fenêtre de configuration.
- **Couleurs de la barre**
  - *Buff — noir #000000 / accent #ddff24* — palette signature.
  - *Windows (barre des tâches)* — clair/sombre + accent du système.
  - *Cyber* — palette cyan/sombre.
  - *Moniteur externe : fond accent #ddff24* — accent inversé sur l'écran externe
    (fond accent, widgets noirs, texte/icônes en accent).
- **Recharger la position** — recalcule la position de la barre courante.
- **Redémarrer la barre** — reconstruit toutes les barres (après veille / écran éteint).
- **Quitter** — ferme l'application (seule sortie ; pas d'entrée dans la barre des tâches).

Tous ces choix sont **enregistrés dans `settings.json`** (voir ci-dessous).

---

## Installateur

- `make-installer.bat` → publie en **self-contained fichier unique** puis génère
  `installer\Buffmybar-W26.exe` via **Inno Setup** (installation par-utilisateur,
  sans droits admin, clé de démarrage auto nettoyée à la désinstallation).
- Sans aucun outil : `Install-Buffmybar-W26.ps1` (ou son `.bat`), installateur en
  PowerShell pur.
- Détails : `INSTALL.md`.

---

## Configuration

Toute la configuration vit sous `%AppData%\BuffMyBar-W26\`, créée au premier lancement :

```
%AppData%\BuffMyBar-W26\
├── settings.json
├── themes\
│   ├── buff.json
│   ├── windows.json
│   └── cyber.json
└── logs\
    └── buffbar.log
```

Éditable à la main **ou** par la fenêtre *Paramètres* (clic droit → *Paramètres…*).

### `settings.json`

```json
{
  "theme": "buff",
  "height": 36,
  "weatherCity": "Terrebonne",
  "gamingMode": false,
  "externalAccent": false,
  "acrylic": true,
  "widgets": {
    "weather": true,
    "uptime": true,
    "network": true,
    "media": true,
    "obs": true,
    "visualizer": true,
    "volume": true,
    "bluetooth": true,
    "battery": true
  },
  "obs": { "host": "127.0.0.1", "port": 4455, "password": "" }
}
```

| Clé              | Rôle                                                                 |
| ---------------- | ------------------------------------------------------------------- |
| `theme`          | `buff` \| `windows` \| `cyber` (= `themes\<nom>.json`).             |
| `height`         | Hauteur de la barre en DIP.                                          |
| `weatherCity`    | Ville pour la météo (wttr.in).                                       |
| `gamingMode`     | Ajoute la latence (ping) au widget réseau.                          |
| `externalAccent` | Accent inversé `#ddff24` sur le moniteur externe.                   |
| `acrylic`        | Fond acrylique translucide (repli opaque si non supporté).          |
| `widgets`        | Activation de chaque module.                                         |
| `obs`            | Connexion obs-websocket (hôte / port / mot de passe).               |

### Thèmes — `themes\<nom>.json`

```json
{
  "followWindows": false,
  "barBackground":   "#000000",
  "moduleBackground":"#000000",
  "moduleBorder":    "#3A3A3A",
  "hoverBackground": "#1E1E1E",
  "hoverBorder":     "#555555",
  "primaryText":     "#FFFFFF",
  "subtleText":      "#C8C8C8",
  "accent":          "#DDFF24"
}
```

`"followWindows": true` ignore les couleurs et suit le thème de Windows
(`windows.json`). Crée un nouveau fichier dans `themes\` puis pointe `theme`
dessus dans `settings.json` pour un thème maison.

---

## Architecture

```
BuffBar/
├── App.xaml(.cs)            Instance unique, config, thème, une barre par moniteur
├── MainWindow.xaml(.cs)     Coquille : régions G/C/D + AppBar + acrylique + menu contextuel
├── SettingsWindow.xaml(.cs) Fenêtre Paramètres (édite settings.json, applique en direct)
├── app.manifest             Conscience DPI PerMonitorV2
├── Core/
│   ├── Config.cs            Modèle settings.json (thème, hauteur, widgets, OBS…)
│   ├── ThemePalette.cs      Modèle d'un thème (themes\<nom>.json)
│   ├── BarConfig.cs         Accès centralisé (délègue à ConfigService.Current)
│   └── IBarWidget.cs        Contrat des modules
├── Interop/
│   ├── NativeMethods.cs     P/Invoke Win32 (shell32 / user32 / dwmapi / shcore)
│   ├── AppBarManager.cs     Cœur AppBar : ABM_*, DPI par écran, gardien plein écran
│   ├── CoreAudio.cs         COM Core Audio (volume + capture loopback)
│   └── PowerNative.cs       État de la batterie
├── Services/
│   ├── ConfigService.cs     Charge/enregistre settings.json + thèmes (JSON natif)
│   ├── AutoStartService.cs  Démarrage Windows (HKCU\...\Run)
│   ├── BackdropService.cs   Fond acrylique DWM
│   ├── ThemeService.cs      Applique le thème (buff/cyber) ou suit Windows
│   ├── FontService.cs       Détection de la Nerd Font installée
│   ├── MonitorService.cs    Énumération des écrans
│   ├── WeatherService.cs    wttr.in (conditions + prévisions 3 jours)
│   ├── ObsService.cs        obs-websocket v5
│   ├── MediaService.cs      Session média WinRT
│   ├── NetworkService.cs    IP locale / publique
│   ├── BluetoothService.cs  Appareils Bluetooth (WinRT)
│   ├── VolumeController.cs   Volume Core Audio
│   ├── AudioCapture.cs / Fft.cs   Capture WASAPI + FFT
│   └── Logger.cs            Journal (%AppData%\BuffMyBar-W26\logs\buffbar.log)
├── Themes/
│   └── BuffTheme.xaml       Palette, police, styles de module et de flyout
└── Widgets/
    ├── Common/              MarqueeText (défilement), HoverPopup (flyout au survol)
    ├── Clock/               Horloge + CalendarFlyout
    ├── Weather/             Météo + WeatherFlyout + WeatherIcons
    ├── Obs/  Media/  Network/  Uptime/
    ├── Visualizer/  Volume/  Bluetooth/  Battery/
```

### Ajouter un widget

1. Créer `Widgets/MonModule/MonModuleWidget.xaml(.cs)`, racine = `Border` avec
   `Style="{StaticResource ModuleBorder}"`, implémentant `IBarWidget`.
2. L'insérer dans une région depuis `MainWindow.ComposeWidgets()` :

```csharp
LeftRegion.Children.Add(new WeatherWidget());
RightRegion.Children.Add(new BatteryWidget());
```

### Ajouter un flyout au survol

Dans le XAML du module : un `Border x:Name="Root"` + un `Popup x:Name="Flyout"`
(`StaysOpen="True"`, `AllowsTransparency="True"`) contenant le contenu de l'applet.
Puis, dans le constructeur :

```csharp
Widgets.Common.HoverPopup.Attach(Root, Flyout, MonContenu,
    onOpening: () => { /* rafraîchir le contenu */ },
    canOpen:   () => /* condition d'ouverture */ true);
```

---

## Multi-écrans

Une barre (AppBar) **par moniteur** : `App` énumère les écrans
(`MonitorService` → `EnumDisplayMonitors`) et crée une `MainWindow` + un
`AppBarManager` pour chacun, avec réservation d'espace et facteur DPI calculés par
écran (`GetDpiForMonitor`). Chaque barre reçoit la disposition complète des modules.

---

## Notes techniques

- **DPI** : la hauteur (48 DIP) est convertie en pixels physiques selon l'échelle de
  l'écran hôte — correcte à 100 / 125 / 150 / 175 %.
- **Acrylique** : le fond de la fenêtre devient transparent (le compositeur DWM rend
  l'acrylique) ; les flyouts gardent un fond opaque pour rester lisibles.
- **Plein écran exclusif** : comme la barre des tâches, BuffBar passe derrière une
  application plein écran exclusif (jeu, vidéo) — comportement normal.
- **Zéro dépendance** : WPF + P/Invoke Win32 + WinRT du framework, rien d'autre.
