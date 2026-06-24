# BuffMyBar-W26

**Langue / Language : [Français](#français) · [English](#english)**

Une barre supérieure native pour Windows 11 (façon Waybar), en C# / .NET 8 / WPF —
configurable par JSON **et** par interface graphique. Zéro dépendance externe.

A native top bar for Windows 11 (Waybar-style), in C# / .NET 8 / WPF — configurable
by JSON **and** by a GUI. Zero external dependencies.

---

# Français

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

## Disposition

```
[Météo] [Uptime] [Réseau] [Média ········· ]     [Heure] [● REC]     [ ········· Visualiseur] [Volume] [Bluetooth] [Batterie]
└─────────── gauche (Média extensible) ──────┘   └ centre (centré) ┘   └──────────── droite (alignée à droite) ───────────┘
```

- **Gauche** — Météo · Uptime · Réseau · Média (le média occupe tout l'espace restant).
- **Centre** — Horloge (+ OBS à sa droite), réellement centrée à l'écran.
- **Droite** — Visualiseur · Volume · Bluetooth · Batterie.

Au **survol** : pointer l'**heure** ouvre le calendrier, pointer la **météo** ouvre les prévisions.

## Prérequis

- **.NET 8 SDK** (`dotnet --version` ≥ 8.0) — https://dotnet.microsoft.com/download
- **JetBrainsMono Nerd Font** installée (sinon repli automatique sur Consolas).
- *(optionnel)* **OBS** + **obs-websocket v5** pour le module REC.
- *(optionnel)* identifiants **OAuth Google** pour les événements d'agenda.

## Compiler / Lancer

Double-clic sur `build.bat` puis `run.bat`, ou bien :

```powershell
dotnet build -c Release
dotnet run --project BuffBar\BuffBar.csproj -c Release
```

Ouverture dans Visual Studio : `BuffBar.sln`.
L'EXE final : `BuffBar\bin\Release\net8.0-windows10.0.19041.0\BuffBar.exe`.
Installation : voir `INSTALL.md`.

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

Tous ces choix sont **enregistrés dans `settings.json`**.

## Configuration

Toute la configuration vit sous `%AppData%\BuffMyBar-W26\`, créée au premier lancement :

```
%AppData%\BuffMyBar-W26\
├── settings.json
├── google_token.json   (jetons Google Agenda, si connecté)
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
  "obs": { "host": "127.0.0.1", "port": 4455, "password": "" },
  "googleCalendar": { "enabled": false, "clientId": "", "clientSecret": "", "maxEvents": 50 }
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
| `googleCalendar` | Événements Google Agenda dans le flyout de l'horloge.               |

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

### Google Agenda (optionnel)

Le flyout de l'horloge peut afficher tes événements (lecture seule, agenda
principal). Mise en place, une seule fois :

1. **Google Cloud Console** : crée un projet et active l'**API Google Calendar**.
2. Crée des identifiants OAuth de type **Application de bureau** → tu obtiens un
   **Client ID** et un **Client secret**.
3. Dans BuffMyBar : clic droit → *Paramètres* → **Google Agenda** : colle les
   identifiants, coche *Afficher mes événements*, puis **Connecter…**. Le navigateur
   s'ouvre pour le consentement ; au retour, l'état passe à « ✔ Connecté ».

Les jetons sont conservés dans `google_token.json` (jamais dans `settings.json`).
*Déconnecter* supprime ce fichier. Tout reste natif (HttpClient + écouteur loopback,
aucun paquet ajouté).

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
│   ├── ConfigService.cs          settings.json + thèmes (JSON natif)
│   ├── GoogleCalendarService.cs  OAuth (PKCE/loopback) + lecture Google Agenda
│   ├── AutoStartService.cs       Démarrage Windows (HKCU\...\Run)
│   ├── BackdropService.cs        Fond acrylique DWM
│   ├── ThemeService.cs           Applique le thème (buff/cyber) ou suit Windows
│   ├── FontService.cs            Détection de la Nerd Font installée
│   ├── MonitorService.cs         Énumération des écrans
│   ├── WeatherService.cs         wttr.in (conditions + prévisions 3 jours)
│   ├── ObsService.cs             obs-websocket v5
│   ├── MediaService.cs           Session média WinRT
│   ├── NetworkService.cs         IP locale / publique + ping
│   ├── BluetoothService.cs       Appareils Bluetooth (WinRT)
│   ├── VolumeController.cs        Volume Core Audio
│   ├── AudioCapture.cs / Fft.cs  Capture WASAPI + FFT
│   └── Logger.cs                 Journal (%AppData%\BuffMyBar-W26\logs\buffbar.log)
├── Themes/
│   └── BuffTheme.xaml       Palette, police, styles de module et de flyout
└── Widgets/
    ├── Common/              MarqueeText (défilement), HoverPopup (flyout au survol)
    ├── Clock/               Horloge + CalendarFlyout (événements Google Agenda)
    ├── Weather/             Météo + WeatherFlyout + WeatherIcons
    ├── Obs/  Media/  Network/  Uptime/
    ├── Visualizer/  Volume/  Bluetooth/  Battery/
```

### Ajouter un widget

1. Créer `Widgets/MonModule/MonModuleWidget.xaml(.cs)`, racine = `Border` avec
   `Style="{StaticResource ModuleBorder}"`, implémentant `IBarWidget`.
2. L'insérer dans une région depuis `MainWindow.ComposeWidgets()`.

### Multi-écrans

Une barre (AppBar) **par moniteur** : `App` énumère les écrans (`MonitorService` →
`EnumDisplayMonitors`) et crée une `MainWindow` + un `AppBarManager` pour chacun,
avec réservation d'espace et facteur DPI calculés par écran (`GetDpiForMonitor`).

### Notes techniques

- **DPI** : la hauteur est convertie en pixels physiques selon l'échelle de l'écran
  hôte — correcte à 100 / 125 / 150 / 175 %.
- **Acrylique** : le fond de la fenêtre devient transparent (le compositeur DWM rend
  l'acrylique) ; les flyouts gardent un fond opaque pour rester lisibles.
- **Plein écran exclusif** : comme la barre des tâches, BuffMyBar passe derrière une
  application plein écran exclusif (jeu, vidéo) — comportement normal.
- **Zéro dépendance** : WPF + P/Invoke Win32 + WinRT du framework, rien d'autre.

## Historique des versions

### v2.1 — Google Agenda dans le calendrier *(version actuelle)*
- Le flyout de l'horloge affiche les **événements Google Agenda** : pastille accent
  sur les jours concernés, liste des événements du jour sélectionné.
- **`GoogleCalendarService`** natif : OAuth 2.0 « application de bureau » (PKCE +
  redirection loopback), rafraîchissement du jeton, API Calendar REST. Lecture seule.
- Jeton stocké à part dans `google_token.json`. Connexion via la fenêtre Paramètres.

### v2.0 — Configuration JSON + fenêtre Paramètres
- **`settings.json`** (façon Waybar) + **thèmes en fichiers** (`buff`, `windows`, `cyber`).
- **Fenêtre Paramètres** (clic droit → *Paramètres…*) : thème, hauteur, ville, mode
  jeu, acrylique, accent externe, activation par widget, OBS. Application en direct.
- **Mode jeu** : latence (ping) dans le widget réseau. Le registre est remplacé par le JSON.

### v1.6 — Accent inversé sur le moniteur externe
- Fond de barre `#ddff24`, widgets noirs, texte/icônes en accent — écran externe seul.
  Pinceaux surchargeables par fenêtre ; couleurs sémantiques inchangées.

### v1.5 — Redémarrage + auto-récupération
- « Redémarrer la barre » + reconstruction automatique après veille / écran éteint
  (`DisplaySettingsChanged`, `PowerModeChanged`, anti-rebond). `OnExplicitShutdown`.

### v1.4 — Sélecteur de couleurs
- Choix Suivre Windows / Buff au clic droit, en direct et persistant.

### v1.3 — Flyouts interactifs
- Au survol : **horloge → calendrier**, **météo → applet** (prévisions 3 jours).
  Utilitaire `HoverPopup` (ouverture au survol, fermeture différée).

### v1.2 — Disposition dynamique + météo locale
- Réseau à largeur fixe ; Média extensible (grille 3 colonnes) ; visualiseur 64 barres.

### v1.1 — Fond acrylique
- Acrylique translucide natif (DWM), repli opaque sûr ; accent buff conservable.

### v1.0 — Thème Windows 11
- Suit le clair/sombre + l'accent du système, en direct. Couleurs sémantiques fixes.

### v0.9 — Persistance plein écran
- Barre toujours au-dessus + réduction des fenêtres plein écran (non exclusif).

### v0.8 — Multi-écrans + Uptime
- Une AppBar par moniteur (DPI par écran) ; widget Uptime.

### v0.7 — Réseau + Bluetooth
- IP locale/publique alternées ; nom + batterie du périphérique Bluetooth.

### v0.6 — OBS
- État d'enregistrement via obs-websocket v5 (client natif, auth SHA256).

### v0.5 — Météo
- wttr.in (français) : icône + température, prévisions, repli hors-ligne.

### v0.4 — Média
- Session système via WinRT (équivalent MPRIS) : titre défilant, clic/molette.

### v0.3 — Visualiseur audio
- Capture loopback WASAPI (COM pur) + FFT maison, style Cava.

### v0.2 — Batterie + Volume
- Batterie (auto-masquée sur poste fixe) ; volume Core Audio (molette/clic).

### Socle — AppBar native
- AppBar `SHAppBarMessage` (réserve l'espace écran), DPI-aware, horloge + date,
  démarrage auto, instance unique, architecture modulaire `IBarWidget`.

---

# English

A native top bar for Windows 11, in **C# / .NET 8 / WPF**.
It **complements** the taskbar (does not replace it), in the spirit of **Waybar**
on Linux or **KDE Plasma** panels.

The "buff" look: black background `#000000`, white text, yellow-green accent
`#ddff24`, framed modules, **JetBrainsMono Nerd Font**. The bar can also
**follow Windows 11 colors** (light/dark + accent) and show a **translucent acrylic
background** like the taskbar.

Everything is **configurable via JSON** (`settings.json`, Waybar-style) **and** via
a **Settings window** (right-click → *Settings…*): theme, height, weather city,
gaming mode, OBS, and per-widget toggles.

> **Zero dependencies**: no Electron / Python / external process. Only WPF +
> Win32 P/Invoke + WinRT projection, all from the framework.

## Layout

```
[Weather] [Uptime] [Network] [Media ········· ]   [Clock] [● REC]   [ ········· Visualizer] [Volume] [Bluetooth] [Battery]
└────────── left (Media stretches) ──────────┘   └ center (centered) ┘   └──────────── right (right-aligned) ────────────┘
```

- **Left** — Weather · Uptime · Network · Media (Media fills the remaining space).
- **Center** — Clock (+ OBS to its right), truly screen-centered.
- **Right** — Visualizer · Volume · Bluetooth · Battery.

On **hover**: pointing at the **clock** opens the calendar, pointing at the
**weather** opens the forecast.

## Requirements

- **.NET 8 SDK** (`dotnet --version` ≥ 8.0) — https://dotnet.microsoft.com/download
- **JetBrainsMono Nerd Font** installed (otherwise it falls back to Consolas).
- *(optional)* **OBS** + **obs-websocket v5** for the REC module.
- *(optional)* **Google OAuth** credentials for calendar events.

## Build / Run

Double-click `build.bat` then `run.bat`, or:

```powershell
dotnet build -c Release
dotnet run --project BuffBar\BuffBar.csproj -c Release
```

Open in Visual Studio: `BuffBar.sln`.
Final EXE: `BuffBar\bin\Release\net8.0-windows10.0.19041.0\BuffBar.exe`.
Installation: see `INSTALL.md`.

## Context menu (right-click the bar)

- **Settings…** — opens the configuration window.
- **Bar colors**
  - *Buff — black #000000 / accent #ddff24* — signature palette.
  - *Windows (taskbar)* — light/dark + system accent.
  - *Cyber* — dark/cyan palette.
  - *External monitor: #ddff24 background* — inverted accent on the external screen
    (accent background, black widgets, accent text/icons).
- **Reload position** — recomputes the current bar's position.
- **Restart bar** — rebuilds all bars (after sleep / a monitor turning off).
- **Quit** — closes the app (the only exit; no taskbar entry).

All of these are **saved in `settings.json`**.

## Configuration

All configuration lives under `%AppData%\BuffMyBar-W26\`, created on first run:

```
%AppData%\BuffMyBar-W26\
├── settings.json
├── google_token.json   (Google Calendar tokens, if connected)
├── themes\
│   ├── buff.json
│   ├── windows.json
│   └── cyber.json
└── logs\
    └── buffbar.log
```

Edit it by hand **or** through the *Settings* window (right-click → *Settings…*).

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
  "obs": { "host": "127.0.0.1", "port": 4455, "password": "" },
  "googleCalendar": { "enabled": false, "clientId": "", "clientSecret": "", "maxEvents": 50 }
}
```

| Key              | Purpose                                                              |
| ---------------- | ------------------------------------------------------------------- |
| `theme`          | `buff` \| `windows` \| `cyber` (= `themes\<name>.json`).            |
| `height`         | Bar height in DIP.                                                   |
| `weatherCity`    | City for weather (wttr.in).                                          |
| `gamingMode`     | Adds latency (ping) to the network widget.                          |
| `externalAccent` | Inverted `#ddff24` accent on the external monitor.                  |
| `acrylic`        | Translucent acrylic background (opaque fallback if unsupported).     |
| `widgets`        | Per-module enable/disable.                                           |
| `obs`            | obs-websocket connection (host / port / password).                  |
| `googleCalendar` | Google Calendar events in the clock flyout.                         |

### Themes — `themes\<name>.json`

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

`"followWindows": true` ignores the colors and follows the Windows theme
(`windows.json`). Create a new file in `themes\` and point `theme` at it in
`settings.json` for a custom theme.

### Google Calendar (optional)

The clock flyout can show your events (read-only, primary calendar). One-time setup:

1. **Google Cloud Console**: create a project and enable the **Google Calendar API**.
2. Create **Desktop app** OAuth credentials → you get a **Client ID** and a
   **Client secret**.
3. In BuffMyBar: right-click → *Settings* → **Google Calendar**: paste the
   credentials, check *Show my events*, then **Connect…**. The browser opens for
   consent; on return, the status becomes "✔ Connected".

Tokens are kept in `google_token.json` (never in `settings.json`). *Disconnect*
deletes that file. Everything stays native (HttpClient + loopback listener, no
added package).

## Architecture

```
BuffBar/
├── App.xaml(.cs)            Single instance, config, theme, one bar per monitor
├── MainWindow.xaml(.cs)     Bar shell: L/C/R regions + AppBar + acrylic + context menu
├── SettingsWindow.xaml(.cs) Settings window (edits settings.json, applies live)
├── app.manifest             PerMonitorV2 DPI awareness
├── Core/
│   ├── Config.cs            settings.json model (theme, height, widgets, OBS…)
│   ├── ThemePalette.cs      Theme model (themes\<name>.json)
│   ├── BarConfig.cs         Central access (delegates to ConfigService.Current)
│   └── IBarWidget.cs        Module contract
├── Interop/
│   ├── NativeMethods.cs     Win32 P/Invoke (shell32 / user32 / dwmapi / shcore)
│   ├── AppBarManager.cs     AppBar core: ABM_*, per-screen DPI, fullscreen guard
│   ├── CoreAudio.cs         COM Core Audio (volume + loopback capture)
│   └── PowerNative.cs       Battery state
├── Services/
│   ├── ConfigService.cs          settings.json + themes (native JSON)
│   ├── GoogleCalendarService.cs  OAuth (PKCE/loopback) + Google Calendar read
│   ├── AutoStartService.cs       Windows startup (HKCU\...\Run)
│   ├── BackdropService.cs        DWM acrylic backdrop
│   ├── ThemeService.cs           Applies theme (buff/cyber) or follows Windows
│   ├── FontService.cs            Detects the installed Nerd Font
│   ├── MonitorService.cs         Monitor enumeration
│   ├── WeatherService.cs         wttr.in (current + 3-day forecast)
│   ├── ObsService.cs             obs-websocket v5
│   ├── MediaService.cs           WinRT media session
│   ├── NetworkService.cs         Local / public IP + ping
│   ├── BluetoothService.cs       Bluetooth devices (WinRT)
│   ├── VolumeController.cs        Core Audio volume
│   ├── AudioCapture.cs / Fft.cs  WASAPI capture + FFT
│   └── Logger.cs                 Log (%AppData%\BuffMyBar-W26\logs\buffbar.log)
├── Themes/
│   └── BuffTheme.xaml       Palette, font, module and flyout styles
└── Widgets/
    ├── Common/              MarqueeText (scrolling), HoverPopup (hover flyout)
    ├── Clock/               Clock + CalendarFlyout (Google Calendar events)
    ├── Weather/             Weather + WeatherFlyout + WeatherIcons
    ├── Obs/  Media/  Network/  Uptime/
    ├── Visualizer/  Volume/  Bluetooth/  Battery/
```

### Add a widget

1. Create `Widgets/MyModule/MyModuleWidget.xaml(.cs)`, root = `Border` with
   `Style="{StaticResource ModuleBorder}"`, implementing `IBarWidget`.
2. Insert it into a region from `MainWindow.ComposeWidgets()`.

### Multi-monitor

One AppBar **per monitor**: `App` enumerates screens (`MonitorService` →
`EnumDisplayMonitors`) and creates a `MainWindow` + an `AppBarManager` for each,
with space reservation and DPI factor computed per screen (`GetDpiForMonitor`).

### Technical notes

- **DPI**: the height is converted to physical pixels using the host screen's scale —
  correct at 100 / 125 / 150 / 175 %.
- **Acrylic**: the window background becomes transparent (the DWM compositor renders
  the acrylic); flyouts keep an opaque background to stay readable.
- **Exclusive fullscreen**: like the taskbar, BuffMyBar goes behind an exclusive
  fullscreen app (game, video) — expected behavior.
- **Zero dependencies**: WPF + Win32 P/Invoke + WinRT from the framework, nothing else.

## Changelog

### v2.1 — Google Calendar in the calendar *(current)*
- The clock flyout shows **Google Calendar events**: an accent dot on days with
  events, and the selected day's event list.
- Native **`GoogleCalendarService`**: OAuth 2.0 "Desktop app" (PKCE + loopback
  redirect), token refresh, Calendar REST API. Read-only.
- Token stored separately in `google_token.json`. Connection from the Settings window.

### v2.0 — JSON configuration + Settings window
- **`settings.json`** (Waybar-style) + **file-based themes** (`buff`, `windows`, `cyber`).
- **Settings window** (right-click → *Settings…*): theme, height, city, gaming mode,
  acrylic, external accent, per-widget toggles, OBS. Applied live.
- **Gaming mode**: latency (ping) in the network widget. Registry replaced by JSON.

### v1.6 — Inverted accent on the external monitor
- Accent `#ddff24` bar background, black widgets, accent text/icons — external screen
  only. Per-window overridable brushes; semantic colors unchanged.

### v1.5 — Restart + auto-recovery
- "Restart bar" + automatic rebuild after sleep / monitor off
  (`DisplaySettingsChanged`, `PowerModeChanged`, debounced). `OnExplicitShutdown`.

### v1.4 — Color selector
- Follow Windows / Buff choice from the right-click menu, live and persistent.

### v1.3 — Interactive flyouts
- On hover: **clock → calendar**, **weather → applet** (3-day forecast).
  `HoverPopup` helper (open on hover, delayed close).

### v1.2 — Dynamic layout + local weather
- Fixed-width Network; stretchable Media (3-column grid); 64-bar visualizer.

### v1.1 — Acrylic background
- Native translucent acrylic (DWM), safe opaque fallback; buff accent can be kept.

### v1.0 — Windows 11 theme
- Follows system light/dark + accent, live. Semantic colors stay fixed.

### v0.9 — Fullscreen persistence
- Bar stays on top + shrinks (non-exclusive) fullscreen windows.

### v0.8 — Multi-monitor + Uptime
- One AppBar per monitor (per-screen DPI); Uptime widget.

### v0.7 — Network + Bluetooth
- Alternating local/public IP; connected Bluetooth device name + battery.

### v0.6 — OBS
- Recording state via obs-websocket v5 (native client, SHA256 auth).

### v0.5 — Weather
- wttr.in (French): icon + temperature, forecast, offline fallback.

### v0.4 — Media
- System session via WinRT (MPRIS-equivalent): scrolling title, click/wheel.

### v0.3 — Audio visualizer
- WASAPI loopback capture (pure COM) + in-house FFT, Cava-style.

### v0.2 — Battery + Volume
- Battery (auto-hidden on desktops); Core Audio volume (wheel/click).

### Foundation — Native AppBar
- `SHAppBarMessage` AppBar (reserves screen space), DPI-aware, clock + date,
  auto-start, single instance, modular `IBarWidget` architecture.
