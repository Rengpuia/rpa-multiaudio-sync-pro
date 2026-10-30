@echo off
title Sign RPA MultiAudio Binaries - K Vanlalrengpuia
echo =========================================================================
echo   RPA MultiAudio Sync Pro - Digital Signature Signer
echo   Developer: K Vanlalrengpuia (rengakhiangte@gmail.com)
echo =========================================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "SIGN_PS=%SCRIPT_DIR%sign_binaries.ps1"
set "BASE_DIR=%SCRIPT_DIR%.."

echo [*] Searching for binaries to digitally sign...
echo.

:: 1. Sign primary executable if compiled
if exist "%BASE_DIR%\bin\Release\net8.0-windows\RPAMultiAudioSyncPro.exe" (
    echo [+] Signing Application Executable: RPAMultiAudioSyncPro.exe
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SIGN_PS%" -FilePath "%BASE_DIR%\bin\Release\net8.0-windows\RPAMultiAudioSyncPro.exe"
    echo.
) else (
    echo [-] Application binary not built yet (bin\Release\net8.0-windows\RPAMultiAudioSyncPro.exe).
)

:: 2. Sign Inno Setup Installer output if present
for %%F in ("%BASE_DIR%\installer_output\*.exe" "%BASE_DIR%\..\installer_output\*.exe") do (
    if exist "%%F" (
        echo [+] Signing Windows Installer: %%~nxF
        powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SIGN_PS%" -FilePath "%%F"
        echo.
    )
)

:: 3. Custom file passed via drag-and-drop or parameter
if "%~1" neq "" (
    echo [+] Signing specified file: %~nx1
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SIGN_PS%" -FilePath "%~1"
    echo.
)

echo.
echo =========================================================================
echo [DONE] Digital signing process completed.
echo =========================================================================
echo.
pause
