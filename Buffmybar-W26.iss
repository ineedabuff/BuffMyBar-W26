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

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; Flags: unchecked

[Files]
; Contenu publié par make-installer.bat (dossier publish\)
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; L'application gère elle-même son démarrage automatique (HKCU\...\Run\BuffBar).
; On déclare la valeur uniquement pour la nettoyer à la désinstallation.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: none; ValueName: "BuffBar"; Flags: dontcreatekey uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "Lancer {#AppName}"; \
    Flags: nowait postinstall skipifsilent

[UninstallRun]
; Ferme l'application avant la désinstallation.
Filename: "{cmd}"; Parameters: "/c taskkill /im {#AppExe} /f"; \
    Flags: runhidden; RunOnceId: "KillBuffBar"
