@echo off
setlocal

echo ========================================
echo  WindowTabs Release Build
echo  Creating ZIP Distribution
echo ========================================
echo.

:: ----------------------------------------
:: Clean previous outputs
:: ----------------------------------------
echo Cleaning previous outputs...
if exist exe\zip\WindowTabs.zip del exe\zip\WindowTabs.zip
if exist exe\zip\WindowTabs rmdir /s /q exe\zip\WindowTabs
echo Done.
echo.

:: ----------------------------------------
:: Check MSBuild
:: ----------------------------------------
set MSBUILD="C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
if not exist %MSBUILD% (
    echo ERROR: MSBuild not found at %MSBUILD%
    echo Please install Visual Studio 2026
    exit /b 1
)

:: ----------------------------------------
:: Restore and build application dependencies
:: ----------------------------------------
set APP_PROJECT=WindowTabs.CSharp\WindowTabs.CSharp.csproj
set APP_OUTPUT=WindowTabs.CSharp\bin\Any CPU\Release\net8.0-windows

echo Restoring dependencies...
%MSBUILD% %APP_PROJECT% /restore /p:Configuration=Release /p:Platform="Any CPU" /v:minimal
if errorlevel 1 (
    echo ERROR: WindowTabs restore failed
    exit /b 1
)
echo Restore completed successfully.
echo.

:: ----------------------------------------
:: Clean OneDrive sync conflict files
:: ----------------------------------------
echo Cleaning OneDrive sync conflict files...
for %%f in ("%APP_OUTPUT%\*-LAPTOP-*.dll" "%APP_OUTPUT%\*-LAPTOP-*.exe" "%APP_OUTPUT%\*-LAPTOP-*.pdb") do (
    if exist "%%f" (
        echo   Removing: %%f
        del "%%f"
    )
)
echo.

:: ----------------------------------------
:: Build WindowTabs
:: ----------------------------------------
echo [1/3] Building WindowTabs...
%MSBUILD% %APP_PROJECT% /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" /v:minimal
if errorlevel 1 (
    echo ERROR: WindowTabs build failed
    exit /b 1
)
echo WindowTabs build completed successfully.
echo.

:: ----------------------------------------
:: Verify runtime files
:: ----------------------------------------
echo Verifying runtime files...
for %%f in (
    "%APP_OUTPUT%\WindowTabs.exe"
    "%APP_OUTPUT%\WindowTabs.dll"
    "%APP_OUTPUT%\WindowTabs.runtimeconfig.json"
    "%APP_OUTPUT%\WindowTabs.deps.json"
    "%APP_OUTPUT%\Microsoft.Extensions.DependencyInjection.dll"
    "%APP_OUTPUT%\Microsoft.Extensions.DependencyInjection.Abstractions.dll"
    "%APP_OUTPUT%\Newtonsoft.Json.dll"
    "%APP_OUTPUT%\Language\FileList.json"
    "%APP_OUTPUT%\Settings\Window_Margin.json"
) do (
    if not exist "%%~f" (
        echo ERROR: Missing runtime file %%~f
        exit /b 1
    )
)
for %%A in ("%APP_OUTPUT%\WindowTabs.exe") do set EXE_SIZE=%%~zA
echo   WindowTabs.exe size: %EXE_SIZE% bytes
if %EXE_SIZE% LEQ 0 (
    echo ERROR: WindowTabs.exe size check failed.
    exit /b 1
)
echo   Runtime files verified successfully.
echo.

:: ----------------------------------------
:: Create ZIP
:: ----------------------------------------
echo [2/3] Creating ZIP...

set OUTPUT_DIR=exe\zip\WindowTabs
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

:: Copy files for ZIP
copy /Y "%APP_OUTPUT%\WindowTabs.exe" "%OUTPUT_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy WindowTabs.exe
    exit /b 1
)
copy /Y "%APP_OUTPUT%\WindowTabs.dll" "%OUTPUT_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy WindowTabs.dll
    exit /b 1
)
copy /Y "%APP_OUTPUT%\WindowTabs.runtimeconfig.json" "%OUTPUT_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy WindowTabs.runtimeconfig.json
    exit /b 1
)
copy /Y "%APP_OUTPUT%\WindowTabs.deps.json" "%OUTPUT_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy WindowTabs.deps.json
    exit /b 1
)
copy /Y "%APP_OUTPUT%\Microsoft.Extensions.DependencyInjection.dll" "%OUTPUT_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy Microsoft.Extensions.DependencyInjection.dll
    exit /b 1
)
copy /Y "%APP_OUTPUT%\Microsoft.Extensions.DependencyInjection.Abstractions.dll" "%OUTPUT_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy Microsoft.Extensions.DependencyInjection.Abstractions.dll
    exit /b 1
)
copy /Y "%APP_OUTPUT%\Newtonsoft.Json.dll" "%OUTPUT_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy Newtonsoft.Json.dll
    exit /b 1
)
copy /Y "version.md" "%OUTPUT_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy version.md
    exit /b 1
)
copy /Y "dist\README.txt" "%OUTPUT_DIR%\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy README.txt
    exit /b 1
)

:: Copy Language folder
mkdir "%OUTPUT_DIR%\Language"
xcopy /Y /E "%APP_OUTPUT%\Language\*" "%OUTPUT_DIR%\Language\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy Language folder
    exit /b 1
)

:: Copy Settings folder
mkdir "%OUTPUT_DIR%\Settings"
xcopy /Y /E "%APP_OUTPUT%\Settings\*" "%OUTPUT_DIR%\Settings\" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy Settings folder
    exit /b 1
)

:: Compress to ZIP
set ZIP_FILE=exe\zip\WindowTabs.zip
if exist "%ZIP_FILE%" del "%ZIP_FILE%"

pushd exe\zip\WindowTabs
tar.exe -a -c -f "..\WindowTabs.zip" *
set COMPRESS_ERROR=%errorlevel%
popd

if %COMPRESS_ERROR% neq 0 (
    echo ERROR: Failed to create ZIP file
    exit /b 1
)
if not exist "%ZIP_FILE%" (
    echo ERROR: ZIP file not created
    exit /b 1
)

:: Remove temporary directory
set CLEANUP_ATTEMPTS=0
:cleanup_zip_dir
rmdir /s /q "%OUTPUT_DIR%" 2>nul
if not exist "%OUTPUT_DIR%" goto cleanup_zip_done
set /a CLEANUP_ATTEMPTS+=1
if %CLEANUP_ATTEMPTS% GEQ 5 goto cleanup_zip_failed
timeout /t 1 /nobreak >nul
goto cleanup_zip_dir
:cleanup_zip_failed
echo WARNING: Failed to remove temporary ZIP directory %OUTPUT_DIR%
:cleanup_zip_done
echo ZIP created successfully.
echo.

:: ----------------------------------------
:: Summary
:: ----------------------------------------
echo [3/3] Done!
echo.
echo ========================================
echo  Release Build Completed!
echo ========================================
echo.
echo Output files:
echo   ZIP: %ZIP_FILE%
echo.
dir exe\zip\WindowTabs.zip 2>nul

endlocal
