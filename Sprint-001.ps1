#requires -Version 5.1
<#
  BuffMyBar-W26 - Sprint-001
  Objectif: masquer automatiquement le widget OBS quand OBS n'est pas ouvert,
            l'afficher automatiquement quand OBS démarre.

  Utilisation, depuis la racine du repo:
    powershell -ExecutionPolicy Bypass -File .\Sprint-001.ps1

  Script idempotent: peut être relancé sans dupliquer les ajouts.
#>

$ErrorActionPreference = 'Stop'

Write-Host "=== BuffMyBar-W26 Sprint-001 : OBS Auto-Hide ===" -ForegroundColor Cyan

$Root = (Get-Location).Path
$BackupDir = Join-Path $Root ".sprint-backups\Sprint-001-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

function Copy-Backup {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (Test-Path $Path) {
        $rel = Resolve-Path $Path | ForEach-Object { $_.Path.Substring($Root.Length).TrimStart('\') }
        $dst = Join-Path $BackupDir $rel
        New-Item -ItemType Directory -Force -Path (Split-Path $dst -Parent) | Out-Null
        Copy-Item $Path $dst -Force
    }
}

function Write-FileUtf8Bom {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Content
    )
    $enc = New-Object System.Text.UTF8Encoding($true)
    [System.IO.File]::WriteAllText($Path, $Content, $enc)
}

function Patch-FileOnce {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Marker,
        [Parameter(Mandatory=$true)][scriptblock]$Patch
    )
    $text = Get-Content -LiteralPath $Path -Raw
    if ($text.Contains($Marker)) {
        Write-Host "Déjà patché: $Path" -ForegroundColor DarkGray
        return $false
    }
    Copy-Backup $Path
    $newText = & $Patch $text
    if ($newText -eq $text) {
        throw "Impossible de patcher automatiquement: $Path"
    }
    Write-FileUtf8Bom -Path $Path -Content $newText
    Write-Host "Patché: $Path" -ForegroundColor Green
    return $true
}

# 1) Trouver projet C# principal
$csprojs = Get-ChildItem -Path $Root -Recurse -Filter *.csproj | Where-Object {
    $_.FullName -notmatch '\\bin\\|\\obj\\|\\.sprint-backups\\'
}

if (-not $csprojs) { throw "Aucun .csproj trouvé. Lance ce script depuis la racine du repo BuffMyBar-W26." }

$project = $csprojs | Where-Object { $_.Name -match 'Buff|Bar|BuffMyBar' } | Select-Object -First 1
if (-not $project) { $project = $csprojs | Select-Object -First 1 }
$ProjectDir = $project.Directory.FullName
Write-Host "Projet détecté: $($project.FullName)" -ForegroundColor Yellow

# 2) Ajouter service ObsProcessWatcher
$ServicesDir = Join-Path $ProjectDir "Services"
New-Item -ItemType Directory -Force -Path $ServicesDir | Out-Null
$WatcherPath = Join-Path $ServicesDir "ObsProcessWatcher.cs"

$watcherCode = @'
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;

namespace BuffMyBar.Services;

public sealed class ObsProcessWatcher : IDisposable
{
    private readonly DispatcherTimer _timer;
    private bool _isRunning;

    public event EventHandler<bool>? IsRunningChanged;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning == value) return;
            _isRunning = value;
            IsRunningChanged?.Invoke(this, value);
        }
    }

    public ObsProcessWatcher(TimeSpan? interval = null)
    {
        _timer = new DispatcherTimer
        {
            Interval = interval ?? TimeSpan.FromSeconds(2)
        };
        _timer.Tick += (_, _) => Refresh();
    }

    public void Start()
    {
        Refresh();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    public void Refresh()
    {
        try
        {
            IsRunning = Process.GetProcesses()
                .Any(p =>
                {
                    try
                    {
                        var name = p.ProcessName;
                        return name.Equals("obs64", StringComparison.OrdinalIgnoreCase)
                            || name.Equals("obs32", StringComparison.OrdinalIgnoreCase)
                            || name.Equals("obs", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });
        }
        catch
        {
            IsRunning = false;
        }
    }

    public void Dispose()
    {
        Stop();
        _timer.Tick -= (_, _) => Refresh();
    }
}
'@

if (Test-Path $WatcherPath) { Copy-Backup $WatcherPath }
Write-FileUtf8Bom -Path $WatcherPath -Content $watcherCode
Write-Host "Service ajouté/mis à jour: $WatcherPath" -ForegroundColor Green

# 3) Trouver fichiers qui semblent contenir le widget OBS
$csFiles = Get-ChildItem -Path $ProjectDir -Recurse -Filter *.cs | Where-Object {
    $_.FullName -notmatch '\\bin\\|\\obj\\|\\.sprint-backups\\' -and $_.Name -ne 'ObsProcessWatcher.cs'
}

$obsFiles = foreach ($f in $csFiles) {
    $t = Get-Content -LiteralPath $f.FullName -Raw
    if ($t -match 'OBS|Obs|obs64|obs32|WebSocket|Recording|Streaming') { $f }
}

if (-not $obsFiles) {
    throw "Aucun fichier C# lié à OBS trouvé. Cherche manuellement le widget OBS, puis colle-moi le nom du fichier."
}

Write-Host "Fichiers OBS candidats:" -ForegroundColor Yellow
$obsFiles | ForEach-Object { Write-Host " - $($_.FullName)" }

# 4) Stratégie A: patcher un contrôle/classe OBS avec Visibility direct.
$patched = $false
foreach ($file in $obsFiles) {
    $path = $file.FullName
    $text = Get-Content -LiteralPath $path -Raw

    # On cible surtout les classes dont le nom de fichier contient OBS/Obs.
    if ($file.BaseName -notmatch 'OBS|Obs') { continue }
    if ($text -notmatch 'class\s+\w+') { continue }
    if ($text -notmatch 'UserControl|Window|Grid|StackPanel|TextBlock|Button|Border|Visibility') { continue }

    $marker = 'BUFFMYBAR_SPRINT001_OBS_AUTOHIDE'
    try {
        Patch-FileOnce -Path $path -Marker $marker -Patch {
            param($src)
            $out = $src

            # using
            if ($out -notmatch 'using\s+BuffMyBar\.Services\s*;') {
                $out = $out -replace '(using\s+System[^;]*;\s*)', "`$1`r`nusing BuffMyBar.Services;`r`n"
            }
            if ($out -notmatch 'using\s+System\.Windows\s*;') {
                $out = $out -replace '(using\s+System[^;]*;\s*)', "`$1`r`nusing System.Windows;`r`n"
            }

            # Ajouter champ dans la classe, après première accolade de class
            $out = [regex]::Replace($out, '(class\s+\w+[^\{]*\{)', "`$1`r`n    // BUFFMYBAR_SPRINT001_OBS_AUTOHIDE`r`n    private readonly ObsProcessWatcher _obsProcessWatcher = new();`r`n", 1)

            # Injecter dans constructeur après InitializeComponent(); si présent
            if ($out -match 'InitializeComponent\s*\(\s*\)\s*;') {
                $inject = @'
InitializeComponent();
        // BUFFMYBAR_SPRINT001_OBS_AUTOHIDE
        Visibility = Visibility.Collapsed;
        _obsProcessWatcher.IsRunningChanged += (_, isRunning) =>
        {
            Dispatcher.Invoke(() => Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed);
        };
        _obsProcessWatcher.Start();
'@
                $out = [regex]::Replace($out, 'InitializeComponent\s*\(\s*\)\s*;', [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $inject }, 1)
            }
            else {
                # Injecter dans premier constructeur de la classe après accolade ouvrante.
                $className = [regex]::Match($out, 'class\s+(\w+)').Groups[1].Value
                if ($className) {
                    $pattern = "(public\s+$className\s*\([^\)]*\)\s*\{)"
                    $inject2 = @'
$1
        // BUFFMYBAR_SPRINT001_OBS_AUTOHIDE
        Visibility = Visibility.Collapsed;
        _obsProcessWatcher.IsRunningChanged += (_, isRunning) =>
        {
            Dispatcher.Invoke(() => Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed);
        };
        _obsProcessWatcher.Start();
'@
                    $out = [regex]::Replace($out, $pattern, $inject2, 1)
                }
            }
            return $out
        } | Out-Null
        $patched = $true
        break
    }
    catch {
        Write-Host "Patch direct impossible pour $path : $($_.Exception.Message)" -ForegroundColor DarkYellow
    }
}

# 5) Stratégie B: si direct impossible, créer helper statique et patcher MainWindow/AppShell pour masquer un élément nommé OBS.
if (-not $patched) {
    $HelperPath = Join-Path $ServicesDir "ObsVisibilityHelper.cs"
    $helperCode = @'
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BuffMyBar.Services;

public static class ObsVisibilityHelper
{
    private static readonly ObsProcessWatcher Watcher = new(TimeSpan.FromSeconds(2));
    private static FrameworkElement? _target;
    private static bool _started;

    public static void Attach(DependencyObject root)
    {
        _target = FindObsElement(root);
        if (_target is null) return;

        _target.Visibility = Visibility.Collapsed;
        Watcher.IsRunningChanged += (_, isRunning) =>
        {
            _target.Dispatcher.Invoke(() =>
                _target.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed);
        };

        if (!_started)
        {
            _started = true;
            Watcher.Start();
        }
    }

    private static FrameworkElement? FindObsElement(DependencyObject root)
    {
        if (root is FrameworkElement fe)
        {
            var name = fe.Name ?? string.Empty;
            var tag = fe.Tag?.ToString() ?? string.Empty;

            if (name.Contains("obs", StringComparison.OrdinalIgnoreCase) ||
                tag.Contains("obs", StringComparison.OrdinalIgnoreCase))
            {
                return fe;
            }

            if (fe is ContentControl cc && cc.Content?.ToString()?.Contains("OBS", StringComparison.OrdinalIgnoreCase) == true)
            {
                return fe;
            }
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var found = FindObsElement(child);
            if (found is not null) return found;
        }

        return null;
    }
}
'@
    if (Test-Path $HelperPath) { Copy-Backup $HelperPath }
    Write-FileUtf8Bom -Path $HelperPath -Content $helperCode
    Write-Host "Helper ajouté/mis à jour: $HelperPath" -ForegroundColor Green

    $mainCandidates = $csFiles | Where-Object {
        $_.BaseName -match 'Main|Shell|Bar|Window|App' -and
        (Get-Content -LiteralPath $_.FullName -Raw) -match 'InitializeComponent\s*\(\s*\)\s*;'
    }

    foreach ($file in $mainCandidates) {
        try {
            Patch-FileOnce -Path $file.FullName -Marker 'BUFFMYBAR_SPRINT001_OBS_HELPER_ATTACH' -Patch {
                param($src)
                $out = $src
                if ($out -notmatch 'using\s+BuffMyBar\.Services\s*;') {
                    $out = $out -replace '(using\s+System[^;]*;\s*)', "`$1`r`nusing BuffMyBar.Services;`r`n"
                }
                $out = [regex]::Replace($out, 'InitializeComponent\s*\(\s*\)\s*;', "InitializeComponent();`r`n        // BUFFMYBAR_SPRINT001_OBS_HELPER_ATTACH`r`n        Loaded += (_, _) => ObsVisibilityHelper.Attach(this);", 1)
                return $out
            } | Out-Null
            $patched = $true
            break
        }
        catch {
            Write-Host "Patch helper impossible pour $($file.FullName): $($_.Exception.Message)" -ForegroundColor DarkYellow
        }
    }
}

if (-not $patched) {
    throw "Sprint incomplet: impossible de raccorder automatiquement le widget OBS. Backup: $BackupDir"
}

# 6) Compiler
Write-Host "Compilation..." -ForegroundColor Cyan
Push-Location $ProjectDir
try {
    dotnet build $project.FullName -c Release
}
finally {
    Pop-Location
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "Compilation échouée. Les sauvegardes sont dans: $BackupDir" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Sprint-001 terminé avec succès." -ForegroundColor Green
Write-Host "Test: lance BuffMyBar, ferme OBS => widget caché; ouvre OBS => widget visible." -ForegroundColor Green
Write-Host "Backup: $BackupDir" -ForegroundColor DarkGray

# 7) Lancer si possible
$runProject = $project.FullName
Write-Host "Lancement..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @('-NoExit','-ExecutionPolicy','Bypass','-Command',"cd '$ProjectDir'; dotnet run --project '$runProject' -c Release") | Out-Null
