@echo off
REM Launcher for Release.ps1 — double-click or run: Release.bat [version]
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Release.ps1" %*
echo.
pause
