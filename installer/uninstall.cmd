@echo off
setlocal

set "APP_DIR=%~dp0"
for %%F in ("%APP_DIR%unins*.exe") do (
    start "" "%%~fF"
    exit /b 0
)

echo FFGUITool uninstaller was not found in:
echo %APP_DIR%
pause
