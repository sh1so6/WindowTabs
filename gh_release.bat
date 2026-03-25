@echo off
setlocal enabledelayedexpansion

REM Extract latest version from version.md using PowerShell
for /f "usebackq delims=" %%i in (`powershell -Command "(Select-String -Path 'version.md' -Pattern '^## version ').Line.Split(' ')[2]"`) do (
    set VERSION=%%i
    goto :version_found
)

:version_found
if "%VERSION%"=="" (
    echo Error: Could not extract version from version.md
    pause
    exit /b 1
)

set TAG=%VERSION%
set TITLE=WindowTabs version %VERSION%
set NOTES=For details, see [version.md](https://github.com/standard-software/WindowTabs/blob/master/version.md)

echo Extracted version: %VERSION%
echo.

echo Creating GitHub Release: %TAG%
echo.

gh release create %TAG% "exe\zip\WindowTabs.zip" --title "%TITLE%" --notes "%NOTES%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Release failed! Please check authentication with: gh auth login
) else (
    echo.
    echo Release created successfully!
)
pause
