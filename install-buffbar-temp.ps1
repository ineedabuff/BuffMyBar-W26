#Requires -Version 5.1
<#
.SYNOPSIS
    Fait démarrer BuffBar en ADMINISTRATEUR au login, sans invite UAC, afin que
    LibreHardwareMonitor puisse charger son pilote noyau et lire la température CPU.

.DESCRIPTION
    Crée une tâche planifiée « BuffBar » :
      - déclencheur : à l'ouverture de session de l'utilisateur courant ;
      - privilèges : les plus élevés (RunLevel Highest) -> élévation silencieuse ;
      - session interactive (nécessaire pour afficher la fenêtre WPF) ;
      - démarre même sur batterie et ne s'arrête pas au passage sur batterie ;
      - pas de limite de durée d'exécution (la barre tourne en continu).

    Retire aussi l'entrée HKCU\...\Run « BuffBar » : sinon deux instances se
    disputeraient le mutex au démarrage et la version non élevée pourrait gagner
    (donc pas de température). AutoStartService détecte la tâche et ne réécrit
    plus la clé Run tant qu'elle existe.

    S'auto-élève au besoin (une seule invite UAC). Conçu pour un appel manuel OU
    depuis l'installeur Inno Setup (qui, lui, tourne en utilisateur standard).

.PARAMETER ExePath
    Chemin complet de BuffBar.exe. Auto-détecté si omis.

.PARAMETER Uninstall
    Supprime la tâche planifiée et arrête la barre. N'élève QUE si la tâche
    existe (pas d'UAC inutile si la fonctionnalité n'a jamais été activée).

.EXAMPLE
    .\install-buffbar-temp.ps1 -ExePath "C:\...\BuffBar.exe"

.EXAMPLE
    .\install-buffbar-temp.ps1 -Uninstall
#>

[CmdletBinding()]
param(
    [string]$ExePath,
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'

# ----------------------------------------------------------------- Esthétique buff
$e      = [char]27
$Accent = "$e[38;2;221;255;36m"   # #ddff24
$Red    = "$e[38;2;255;49;49m"    # #FF3131
$Dim    = "$e[38;2;150;150;150m"
$Reset  = "$e[0m"

function Write-Banner {
    Write-Host ""
    Write-Host "$Accent[ BuffBar ]$Reset température CPU — tâche de démarrage élevée"
    Write-Host "$Dim------------------------------------------------------------$Reset"
}
function Write-Step([string]$m) { Write-Host "$Accent[ .. ]$Reset $m" }
function Write-Ok  ([string]$m) { Write-Host "$Accent[ ok ]$Reset $m" }
function Write-Err2([string]$m) { Write-Host "$Red[ !! ]$Reset $m" }

$TaskName = 'BuffBar'
$RunKey   = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$RunValue = 'BuffBar'

# ----------------------------------------------------------------- Utilitaires
function Test-Admin {
    $id = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $pr = New-Object System.Security.Principal.WindowsPrincipal($id)
    return $pr.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-TaskExists {
    return $null -ne (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue)
}

# ----------------------------------------------------------------- Élévation ciblée
# Lire une tâche ne demande pas l'admin ; seules la création et la suppression
# l'exigent. On n'élève donc à la désinstallation QUE si la tâche existe.
$needElevation = $true
if ($Uninstall -and -not (Test-TaskExists)) {
    $needElevation = $false
}

if ($needElevation -and -not (Test-Admin)) {
    Write-Banner
    Write-Step "Élévation requise…"
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    if ($Uninstall) { $argList += '-Uninstall' }
    if ($ExePath)   { $argList += @('-ExePath', "`"$ExePath`"") }
    try {
        # -Wait : l'installeur (waituntilterminated) attend la fin réelle du travail.
        Start-Process -FilePath 'powershell.exe' -ArgumentList $argList -Verb RunAs -Wait
        exit 0
    }
    catch {
        Write-Err2 "Élévation refusée. La température CPU restera indisponible."
        exit 1
    }
}

Write-Banner

# ----------------------------------------------------------------- Désinstallation
if ($Uninstall) {
    if (Test-TaskExists) {
        # On est élevé ici : on peut arrêter une barre élevée puis retirer la tâche.
        Get-Process -Name 'BuffBar' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
        Write-Ok "Tâche « $TaskName » supprimée, barre arrêtée."
    }
    else {
        Write-Ok "Aucune tâche « $TaskName » : rien à faire."
    }
    Write-Host "$Dim  La clé Run se recréera au prochain lancement normal de la barre.$Reset"
    exit 0
}

# ----------------------------------------------------------------- Résolution de l'exe
if (-not $ExePath) {
    $candidates = @(
        (Join-Path $PSScriptRoot 'BuffBar.exe'),
        (Join-Path $PSScriptRoot 'publish\BuffBar.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Buffmybar-W26\BuffBar.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\BuffBar\BuffBar.exe')
    )
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) { $ExePath = $c; break }
    }
}

if (-not $ExePath) {
    Write-Err2 "BuffBar.exe introuvable. Précise le chemin avec -ExePath."
    exit 1
}

$ExePath = [System.IO.Path]::GetFullPath($ExePath)
if (-not (Test-Path -LiteralPath $ExePath)) {
    Write-Err2 "Chemin invalide : $ExePath"
    exit 1
}
Write-Ok "Exécutable : $ExePath"

# ----------------------------------------------------------------- Création de la tâche
$userId = "$env:USERDOMAIN\$env:USERNAME"

try {
    $action    = New-ScheduledTaskAction -Execute $ExePath
    $trigger   = New-ScheduledTaskTrigger -AtLogOn -User $userId
    $principal = New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Highest
    $settings  = New-ScheduledTaskSettingsSet `
                    -AllowStartIfOnBatteries `
                    -DontStopIfGoingOnBatteries `
                    -StartWhenAvailable `
                    -MultipleInstances IgnoreNew `
                    -ExecutionTimeLimit ([TimeSpan]::Zero)

    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger `
        -Principal $principal -Settings $settings -Force | Out-Null

    Write-Ok "Tâche « $TaskName » enregistrée (au login, élevée, sans UAC)."
}
catch {
    Write-Err2 "Échec de la création de la tâche : $($_.Exception.Message)"
    exit 1
}

# ----------------------------------------------------------------- Nettoyage clé Run
$existingRun = Get-ItemProperty -Path $RunKey -Name $RunValue -ErrorAction SilentlyContinue
if ($null -ne $existingRun) {
    Remove-ItemProperty -Path $RunKey -Name $RunValue -ErrorAction SilentlyContinue
    Write-Ok "Entrée HKCU\...\Run « $RunValue » retirée (évite le doublon non élevé)."
}

# ----------------------------------------------------------------- Relance immédiate
Write-Step "Relance de la barre en mode élevé…"
Get-Process -Name 'BuffBar' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 400
try {
    Start-ScheduledTask -TaskName $TaskName
    Write-Ok "Barre relancée par la tâche."
}
catch {
    Write-Err2 "Démarrage différé au prochain login : $($_.Exception.Message)"
}

Write-Host ""
Write-Ok "Terminé. Vérifie « CPU x% @ y°C », puis le rapport :"
Write-Host "$Dim  %AppData%\BuffMyBar-W26\logs\sensors.log  (Process admin doit être True)$Reset"
Write-Host ""
