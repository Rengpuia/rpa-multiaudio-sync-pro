@echo off
title Connect & Push to GitHub - RPA MultiAudio Sync Pro
echo =========================================================================
echo   Connect to GitHub & Trigger Cloud Installer Build
echo   Developer: K Vanlalrengpuia (rengakhiangte@gmail.com)
echo =========================================================================
echo.

set "REPO_URL=%~1"

if "%REPO_URL%"=="" (
    echo Please enter your GitHub repository URL:
    echo (Example: https://github.com/your-username/rpa-multiaudio-sync-pro.git)
    echo.
    set /p REPO_URL="GitHub Repo URL: "
)

if "%REPO_URL%"=="" (
    echo [ERROR] No GitHub repository URL provided.
    pause
    exit /b 1
)

echo.
echo [*] Configuring Git remote origin: %REPO_URL%
git remote remove origin >nul 2>&1
git remote add origin "%REPO_URL%"
git branch -M main

echo [*] Pushing repository to GitHub...
git push -u origin main

if %errorLevel% equ 0 (
    echo.
    echo =========================================================================
    echo [SUCCESS] Pushed to GitHub successfully!
    echo.
    echo GitHub Actions has now started building your Standalone Installer:
    echo 1. Open your repository on GitHub.
    echo 2. Go to the "Actions" tab.
    echo 3. Click the running workflow "Build Standalone Windows Installer".
    echo 4. Once finished, download "RPAMultiAudioSyncPro_Setup_v2.4.2" from Artifacts!
    echo =========================================================================
) else (
    echo.
    echo [!] Push failed. Please check your GitHub repository URL and permissions.
)

echo.
pause
