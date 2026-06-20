@echo off
REM ============================================================
REM  BuffBar - compilation (Release)
REM ============================================================
cd /d "%~dp0"
dotnet build -c Release
echo.
echo Termine. EXE : BuffBar\bin\Release\net8.0-windows10.0.19041.0\BuffBar.exe
pause
