@echo off
setlocal

set "APP_DIR=%~dp0"
set "APP_DATA=%APPDATA%\FFGUITool"
for %%F in ("%APP_DIR%unins*.exe") do (
    start "" /wait "%%~fF"
    if exist "%APP_DATA%" rmdir /s /q "%APP_DATA%"
    reg delete "HKCU\Software\FFGUITool" /f >nul 2>nul
    exit /b 0
)

echo FFGUITool uninstaller was not found in:
echo %APP_DIR%
pause
