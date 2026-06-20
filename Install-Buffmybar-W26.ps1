<#
  Buffmybar-W26 — installateur PowerShell (sans aucun outil tiers)
  - Publie l'application (self-contained, fichier unique)
  - Installe dans %LOCALAPPDATA%\Programs\Buffmybar-W26 (sans droits admin)
  - Crée un raccourci dans le menu Démarrer
  - Lance l'application (qui s'enregistre elle-même au démarrage de Windows)
  Idempotent : relançable sans problème.
#>

$ErrorActionPreference = 'Stop'

$AppName    = 'Buffmybar-W26'
$ExeName    = 'BuffBar.exe'
$Root       = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project    = Join-Path $Root 'BuffBar\BuffBar.csproj'
$PublishDir = Join-Path $Root 'publish'
$InstallDir = Join-Path $env:LOCALAPPDATA "Programs\$AppName"

Write-Host "== Installation de $AppName ==" -ForegroundColor Cyan

# 0) Vérifier le SDK .NET
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "Le SDK .NET 8 est introuvable. Installe-le depuis https://dotnet.microsoft.com/download"
}

# 1) Publier
Write-Host "[1/4] Publication..." -ForegroundColor Yellow
dotnet publish $Project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "Échec de la publication." }

# 2) Fermer une instance en cours puis copier
Write-Host "[2/4] Copie vers $InstallDir..." -ForegroundColor Yellow
Get-Process -Name 'BuffBar' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item (Join-Path $PublishDir '*') $InstallDir -Recurse -Force

# 3) Raccourci menu Démarrer
Write-Host "[3/4] Raccourci menu Démarrer..." -ForegroundColor Yellow
$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$shell = New-Object -ComObject WScript.Shell
$lnk = $shell.CreateShortcut((Join-Path $startMenu "$AppName.lnk"))
$lnk.TargetPath       = Join-Path $InstallDir $ExeName
$lnk.WorkingDirectory = $InstallDir
$lnk.Description       = $AppName
$lnk.Save()

# 4) Lancer
Write-Host "[4/4] Lancement..." -ForegroundColor Yellow
Start-Process (Join-Path $InstallDir $ExeName)

Write-Host ""
Write-Host "Termine. $AppName est installe dans :" -ForegroundColor Green
Write-Host "  $InstallDir"
Write-Host "Le demarrage automatique avec Windows est gere par l'application elle-meme."
