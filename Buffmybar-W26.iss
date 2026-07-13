; ============================================================
;  Buffmybar-W26 — script d'installateur Inno Setup
;  Génère : installer\Buffmybar-W26.exe
;  Prérequis : Inno Setup 6 (https://jrsoftware.org/isdl.php)
;  À lancer via make-installer.bat (qui publie l'app d'abord).
; ============================================================

#define AppName "Buffmybar-W26"
#define AppExe "BuffBar.exe"
#define AppVersion "0.9.0"
#define Publisher "IneedABUFF"

[Setup]
AppId={{B0FFBA12-2026-4B0F-BA12-A1B2C3D4E5F6}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=installer
OutputBaseFilename=Buffmybar-W26
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
; Mise à jour « par-dessus » : ferme l'app en cours pour libérer les fichiers.
; (L'app se ferme d'elle-même en lançant l'installateur ; ceci couvre les cas limites.)
CloseApplications=force
RestartApplications=no

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; Flags: unchecked
; Fonctionnalité température CPU : nécessite une tâche planifiée ÉLEVÉE
; (LibreHardwareMonitor lit les MSR via un pilote noyau -> admin requis).
; Cochée = une invite UAC pendant l'installation. Décochée = pas de température.
Name: "cputemp"; Description: "Lecture de la température CPU (démarrage élevé, une invite UAC)"

[Files]
; Contenu publié par make-installer.bat (dossier publish\)
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
; Script de configuration de la tâche élevée (placé à la racine, à côté du .iss)
Source: "install-buffbar-temp.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; L'application gère elle-même son démarrage automatique (HKCU\...\Run\BuffBar).
; On déclare la valeur uniquement pour la nettoyer à la désinstallation.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: none; ValueName: "BuffBar"; Flags: dontcreatekey uninsdeletevalue

[Run]
; Option température : le .ps1 s'auto-élève (UNE invite UAC) et enregistre la
; tâche « BuffBar » qui lancera l'exe en admin à chaque login, puis relance la
; barre. N'est exécuté que si la tâche « cputemp » est cochée.
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\install-buffbar-temp.ps1"" -ExePath ""{app}\{#AppExe}"""; \
    StatusMsg: "Activation de la lecture de temperature CPU (elevation requise)..."; \
    Flags: runhidden waituntilterminated; Tasks: cputemp

; Lancement de fin d'installation — UNIQUEMENT si la température n'est PAS activée.
; (Sinon on lancerait l'exe NON élevé, en conflit de mutex avec la tâche élevée
; qui vient déjà de démarrer la barre.)
Filename: "{app}\{#AppExe}"; Description: "Lancer {#AppName}"; \
    Flags: nowait postinstall skipifsilent; Check: not WizardIsTaskSelected('cputemp')

[UninstallRun]
; Retire la tâche élevée (n'élève QUE si elle existe -> pas d'UAC inutile sinon).
; Doit passer AVANT le taskkill : étant élevé, le .ps1 peut arrêter une barre
; élevée que taskkill (non élevé) ne pourrait pas tuer.
Filename: "powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\install-buffbar-temp.ps1"" -Uninstall"; \
    Flags: runhidden waituntilterminated; RunOnceId: "RemoveBuffBarTask"
; Ferme l'application avant la désinstallation (cas non élevé).
Filename: "{cmd}"; Parameters: "/c taskkill /im {#AppExe} /f"; \
    Flags: runhidden; RunOnceId: "KillBuffBar"
