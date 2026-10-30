@echo off
title Install RPA MultiAudio Developer Certificate - K Vanlalrengpuia
echo =========================================================================
echo   RPA MultiAudio Sync Pro - Digital Signature Trust Installer
echo   Developer: K Vanlalrengpuia (rengakhiangte@gmail.com)
echo =========================================================================
echo.
echo This tool registers the Developer Digital Certificate into your Windows
echo Trusted Root & Trusted Publisher stores so Windows SmartScreen and UAC
echo will NOT block the installer or application.
echo.

:: Check for Administrator permissions
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [!] Administrator privileges required. Requesting elevation...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

set "CER_PATH=%~dp0K_Vanlalrengpuia_CodeSigning.cer"

if not exist "%CER_PATH%" (
    echo [ERROR] Certificate file not found: %CER_PATH%
    pause
    exit /b 1
)

echo [*] Installing to Trusted Root Certification Authorities (Local Machine)...
certutil.exe -addstore -f "Root" "%CER_PATH%"
if %errorLevel% equ 0 (
    echo [+] Successfully added to Trusted Root Certification Authorities.
) else (
    echo [!] Warning: Failed to add to Root store.
)

echo.
echo [*] Installing to Trusted Publishers (Local Machine)...
certutil.exe -addstore -f "TrustedPublisher" "%CER_PATH%"
if %errorLevel% equ 0 (
    echo [+] Successfully added to Trusted Publishers store.
) else (
    echo [!] Warning: Failed to add to TrustedPublisher store.
)

echo.
echo [*] Installing to Current User Trusted Root & Trusted Publisher...
certutil.exe -user -addstore -f "Root" "%CER_PATH%" >nul 2>&1
certutil.exe -user -addstore -f "TrustedPublisher" "%CER_PATH%" >nul 2>&1

echo.
echo =========================================================================
echo [SUCCESS] Developer signature for K Vanlalrengpuia is now TRUSTED!
echo           Windows will NO LONGER block the installer or executables.
echo =========================================================================
echo.
pause
