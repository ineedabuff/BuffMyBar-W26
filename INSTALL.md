# Installateur — Buffmybar-W26

Deux façons de générer/installer. Les deux **publient l'app** d'abord (donc le
SDK .NET 8 est requis sur la machine qui construit), en mode **self-contained**
(le PC cible n'a pas besoin du runtime .NET).

## Option A — Vrai installateur .exe (Inno Setup)

Produit un fichier unique `installer\Buffmybar-W26.exe` à distribuer.

1. Installer **Inno Setup 6** (gratuit) : https://jrsoftware.org/isdl.php
2. Double-cliquer **`make-installer.bat`**.
3. Récupérer **`installer\Buffmybar-W26.exe`**.

L'installateur : installe dans `%LOCALAPPDATA%\Programs\Buffmybar-W26` (sans
admin), crée les raccourcis menu Démarrer (+ Bureau optionnel), propose de lancer
l'app, et fournit un désinstalleur propre (qui retire aussi l'entrée de démarrage).

## Option B — Installation directe, sans aucun outil (PowerShell)

Pas d'installateur à distribuer, mais installe directement sur cette machine.

1. Double-cliquer **`Install-Buffmybar-W26.bat`**.

Publie, copie dans `%LOCALAPPDATA%\Programs\Buffmybar-W26`, crée le raccourci
menu Démarrer et lance l'app. Relançable à volonté (idempotent).

## Notes

- Le **démarrage automatique** avec Windows est géré par l'application elle-même
  (clé `HKCU\...\Run`), donc inutile de le configurer dans l'installateur.
- Mode self-contained = exécutable autonome (~ plus volumineux). Pour un paquet
  plus léger nécessitant le **.NET 8 Desktop Runtime** sur la cible, retirer
  `--self-contained true` et les options `PublishSingleFile` des scripts.
- Désinstallation : Option A via « Applications installées » de Windows ;
  Option B en supprimant le dossier d'installation et le raccourci.
