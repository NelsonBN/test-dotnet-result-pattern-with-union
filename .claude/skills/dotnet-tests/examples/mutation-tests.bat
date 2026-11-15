@echo off
rem Mutation tests with Stryker.NET (local tool from .config/dotnet-tools.json).
rem Run from anywhere. The HTML report is NOT opened unless you pass -o (or --open).
rem Every other argument goes to Stryker, for example:
rem   mutation-tests.bat -o --project MyProject.csproj --since:main
setlocal
cd /d "%~dp0"

set "OPEN="
set "ARGS="
:parse
if "%~1"=="" goto run
if /i "%~1"=="-o" (set "OPEN=--open-report") else if /i "%~1"=="--open" (set "OPEN=--open-report") else (set ARGS=%ARGS% %1)
shift
goto parse

:run
dotnet tool restore || exit /b 1
dotnet restore || exit /b 1
dotnet build --no-restore || exit /b 1
dotnet stryker --config-file stryker-config.json %OPEN% %ARGS% || exit /b 1
