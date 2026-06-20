@echo off
REM ============================================================
REM  Buffmybar-W26 — génération de l'installateur .exe (Inno Setup)
REM  Résultat : installer\Buffmybar-W26.exe
REM ============================================================
setlocal
cd /d "%~dp0"

echo [1/2] Publication de l'application (self-contained, fichier unique)...
dotnet publish "BuffBar\BuffBar.csproj" -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -o "publish"
if errorlevel 1 (
  echo.
  echo Echec de la publication.
  pause & exit /b 1
)

echo.
echo [2/2] Compilation de l'installateur avec Inno Setup...
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
  echo.
  echo Inno Setup introuvable.
  echo Installe-le gratuitement depuis : https://jrsoftware.org/isdl.php
  echo puis relance ce script.
  pause & exit /b 1
)

"%ISCC%" "Buffmybar-W26.iss"
if errorlevel 1 (
  echo.
  echo Echec de la compilation de l'installateur.
  pause & exit /b 1
)

echo.
echo ============================================================
echo  Installateur cree : installer\Buffmybar-W26.exe
echo ============================================================
pause
