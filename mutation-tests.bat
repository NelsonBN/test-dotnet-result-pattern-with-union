@echo off
rem Mutation tests with Stryker.NET (local tool from .config/dotnet-tools.json).
rem Run from anywhere. The HTML report is NOT opened unless you pass -o (or --open);
rem with -o one report is opened per project. Every other argument goes to Stryker, for example:
rem   mutation-tests.bat -o --since:main
rem Projects listed in IGNORED are skipped: Stryker 5.1.0 cannot parse the C# 15 `union` declaration (ResultUnion).
setlocal
cd /d "%~dp0"

set IGNORED=ResultUnion
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
for /d %%d in (src\*) do (
  echo ;%IGNORED%; | find /i ";%%~nxd;" >nul
  if errorlevel 1 (
    dotnet stryker --config-file stryker-config.json --project %%~nxd.csproj %OPEN% %ARGS% || exit /b 1
  ) else (
    echo SKIP %%~nxd
  )
)
