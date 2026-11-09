@echo off
title AHFMC
cd /d "%~dp0.."
taskkill /f /im "AHFMC.exe" 2>nul
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionSingleFile=true
cd /d "%~dp0..\bin\Release\net10.0-windows\win-x64\publish"
start "" "AHFMC.exe"
echo Press ENTER to exit...
set /p "="
