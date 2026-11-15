@echo off
rem Mutation tests with Stryker.NET (local tool from .config/dotnet-tools.json).
rem Run from anywhere; extra arguments go to Stryker, for example:
rem   mutation-tests.bat --project MyProject.csproj --since:main
setlocal
cd /d "%~dp0"

dotnet tool restore || exit /b 1
dotnet restore || exit /b 1
dotnet build --no-restore || exit /b 1
dotnet stryker --config-file stryker-config.json --open-report %* || exit /b 1
