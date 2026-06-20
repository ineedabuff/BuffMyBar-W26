@echo off
REM ============================================================
REM  BuffBar - lancement
REM ============================================================
cd /d "%~dp0"
dotnet run --project "BuffBar\BuffBar.csproj" -c Release
