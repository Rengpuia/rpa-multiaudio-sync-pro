@echo off
title RPA MultiAudio Sync Pro - Launcher
echo ========================================================
echo   RPA MultiAudio Sync Pro (7.1 USB + Bluetooth Router)
echo ========================================================
echo.
echo Launching Interactive UI & Positional Sound Engine...
start "" "%~dp0index.html"
echo.
echo Started in your default browser!
echo To compile the native Windows C# application (.NET 8):
echo   1. Ensure .NET 8 SDK is installed: winget install Microsoft.DotNet.SDK.8
echo   2. Run: dotnet build -c Release
echo   3. Run: bin\Release\net8.0-windows\RPAMultiAudioSyncPro.exe
echo.
pause
