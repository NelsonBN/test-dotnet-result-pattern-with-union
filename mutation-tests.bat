@echo off
rem Mutation tests with Stryker.NET (local tool from .config/dotnet-tools.json).
rem Run from anywhere; extra arguments go to Stryker, for example:
rem   mutation-tests.bat --since:main
rem Projects listed in IGNORED are skipped: Stryker 5.1.0 cannot parse the C# 15 `union` declaration (ResultUnion).
setlocal
cd /d "%~dp0"

set IGNORED=ResultUnion

dotnet tool restore || exit /b 1
dotnet restore || exit /b 1
dotnet build --no-restore || exit /b 1
for /d %%d in (src\*) do (
  echo ;%IGNORED%; | find /i ";%%~nxd;" >nul
  if errorlevel 1 (
    dotnet stryker --config-file stryker-config.json --project %%~nxd.csproj %* || exit /b 1
  ) else (
    echo SKIP %%~nxd
  )
)
