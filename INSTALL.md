# Installation — BuffMyBar-W26

Barre supérieure native pour Windows 11 (C# / .NET 8 / WPF). Deux façons de
l'installer. Les deux **publient l'app** d'abord (le **SDK .NET 8** est donc requis
sur la machine qui construit), en mode **self-contained** (le PC cible n'a pas
besoin du runtime .NET).

## Prérequis

- **.NET 8 SDK** sur la machine de build (`dotnet --version` ≥ 8.0) —
  https://dotnet.microsoft.com/download
- **JetBrainsMono Nerd Font** installée pour les icônes (sinon repli automatique
  sur Consolas, sans icônes) — https://www.nerdfonts.com
- *(optionnel)* **OBS** + **obs-websocket v5** pour le module d'enregistrement.
- *(optionnel)* Identifiants **OAuth Google** pour les événements d'agenda
  (voir le README, section « Google Agenda »).

## Option A — Vrai installateur .exe (Inno Setup)

Produit un fichier unique `installer\Buffmybar-W26.exe` à distribuer.

1. Installer **Inno Setup 6 ou 7** (gratuit) : https://jrsoftware.org/isdl.php
2. Double-cliquer **`make-installer.bat`**.
3. Récupérer **`installer\Buffmybar-W26.exe`**.

L'installateur installe dans `%LOCALAPPDATA%\Programs\Buffmybar-W26` (sans admin),
crée les raccourcis menu Démarrer (+ Bureau optionnel), propose de lancer l'app et
fournit un désinstalleur propre (qui retire aussi l'entrée de démarrage).

## Option B — Installation directe, sans aucun outil (PowerShell)

Pas d'installateur à distribuer ; installe directement sur cette machine.

1. Double-cliquer **`Install-Buffmybar-W26.bat`**.

Publie, copie dans `%LOCALAPPDATA%\Programs\Buffmybar-W26`, crée le raccourci menu
Démarrer et lance l'app. Relançable à volonté (idempotent).

## Lancer depuis les sources (sans installer)

```powershell
dotnet run --project BuffBar\BuffBar.csproj -c Release
```
ou double-clic sur `run.bat`. **Quitter** : clic droit sur la barre → *Quitter*.

## Configuration & données

Au premier lancement, l'app crée **`%AppData%\BuffMyBar-W26\`** :

```
%AppData%\BuffMyBar-W26\
├── settings.json        réglages (thème, hauteur, widgets, OBS, Google…)
├── google_token.json    jetons Google Agenda (si connecté)
├── themes\              buff.json / windows.json / cyber.json
└── logs\                buffbar.log
```

- Tout se règle par la **fenêtre Paramètres** (clic droit → *Paramètres…*) ou en
  éditant `settings.json` à la main. Voir le README pour le détail des clés.
- Le **démarrage automatique** avec Windows est géré par l'app (clé `HKCU\...\Run`),
  inutile de le configurer dans l'installateur.
- La **désinstallation** ne supprime pas ce dossier de config : le retirer à la main
  pour repartir de zéro.

## Notes

- Mode self-contained = exécutable autonome (plus volumineux). Pour un paquet plus
  léger nécessitant le **.NET 8 Desktop Runtime** sur la cible, retirer
  `--self-contained true` et les options `PublishSingleFile` des scripts.
- Désinstallation : Option A via « Applications installées » de Windows ;
  Option B en supprimant le dossier d'installation et le raccourci.
