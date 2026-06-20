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

set "ISCC="
for %%P in (
  "%ProgramFiles%\Inno Setup 7\ISCC.exe"
  "%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
  "%ProgramFiles%\Inno Setup 6\ISCC.exe"
  "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
) do if not defined ISCC if exist "%%~P" set "ISCC=%%~P"

if not defined ISCC for /f "delims=" %%I in ('where ISCC.exe 2^>nul') do if not defined ISCC set "ISCC=%%I"

if not defined ISCC (
  echo.
  echo Inno Setup introuvable.
  echo - Installe-le gratuitement : https://jrsoftware.org/isdl.php
  echo - Ou ouvre Buffmybar-W26.iss dans l'editeur Inno Setup puis Build ^> Compile.
  pause & exit /b 1
)

echo Compilateur : "%ISCC%"
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
