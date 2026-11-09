@echo off
title build a release of AHFMC
cd /d "%~dp0.."
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "([xml](Get-Content 'AHFMC.csproj')).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1"`) do set VER=%%v
if "%VER%"=="" ( echo Could not read the version from the .csproj & pause & exit /b 1 )
echo Building version %VER%

taskkill /f /im "AHFMC.exe" 2>nul
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionSingleFile=true
if errorlevel 1 ( echo Build failed & pause & exit /b 1 )

if not exist release mkdir release
set PUB=bin\Release\net10.0-windows\win-x64\publish

copy /y "%PUB%\AHFMC.exe" "release\AHFMC-Portable.exe" >nul

set ISCC=
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"
if "%ISCC%"=="" (
  echo.
  echo Inno Setup 6 not found - only the portable file was made. Install it from https://jrsoftware.org/isdl.php and run this again.
) else (
  "%ISCC%" /DAppVersion=%VER% "Builder_and_Installer\AHFMC.iss"
  if errorlevel 1 ( echo Installer build failed & pause & exit /b 1 )
)

echo.
echo Done. Upload these two files to a GitHub release tagged v%VER%:
dir /b release
echo.
pause
