<#
.SYNOPSIS
    Mesure la consommation CPU/RAM de BuffBar au repos, pour chiffrer les
    optimisations (objectif : CPU idle < 0.5 %, RAM < 80 Mo).

.DESCRIPTION
    Échantillonne le compteur « % Processor Time » du processus BuffBar sur une
    fenêtre de N secondes, normalise par le nombre de cœurs logiques, et rapporte
    la moyenne / le pic CPU ainsi que le working set et le nombre de threads.

    Protocole de comparaison :
      1. Fermer les fenêtres/flyouts, laisser la barre au repos, ne jouer AUCUN son
         (le visualiseur doit être en veille).
      2. Lancer ce script AVANT la modif -> noter la moyenne CPU.
      3. Relancer APRÈS la modif dans les mêmes conditions -> comparer.

    Pour isoler le visualiseur : mesurer une fois en silence, une fois avec de la
    musique (plein régime), et une fois avec un jeu plein écran au premier plan
    (le rendu doit être totalement en pause).

.EXAMPLE
    pwsh -File Docs/Engineering/measure-idle.ps1 -Seconds 60
#>
param(
    [int]$Seconds = 60,
    [string]$ProcessName = "BuffBar"
)

$proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Error "Processus '$ProcessName' introuvable. Lance BuffBar d'abord."
    exit 1
}

$cores = [Environment]::ProcessorCount
Write-Host "Mesure de '$ProcessName' sur $Seconds s ($cores cœurs logiques)..." -ForegroundColor Cyan

$counterPath = "\Process($ProcessName)\% Processor Time"
$samples = Get-Counter -Counter $counterPath -SampleInterval 1 -MaxSamples $Seconds |
    ForEach-Object { $_.CounterSamples[0].CookedValue / $cores }

$avg = ($samples | Measure-Object -Average).Average
$max = ($samples | Measure-Object -Maximum).Maximum

# Rafraîchit les infos mémoire.
$proc.Refresh()
$ramMb = [math]::Round($proc.WorkingSet64 / 1MB, 1)

Write-Host ""
Write-Host ("CPU moyen : {0:N2} %  (cible < 0.5 %)" -f $avg) -ForegroundColor Green
Write-Host ("CPU pic   : {0:N2} %" -f $max)
Write-Host ("RAM       : {0} Mo  (cible < 80 Mo)" -f $ramMb)
Write-Host ("Threads   : {0}" -f $proc.Threads.Count)
