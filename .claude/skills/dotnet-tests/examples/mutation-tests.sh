#!/usr/bin/env sh
# Mutation tests with Stryker.NET (local tool from .config/dotnet-tools.json).
# Run from anywhere. The HTML report is NOT opened unless you pass -o (or --open).
# Every other argument goes to Stryker, for example:
#   ./mutation-tests.sh -o --project MyProject.csproj --since:main
set -eu
cd "$(dirname "$0")"

open_report=""
for arg in "$@"; do
  shift
  case "$arg" in
    -o|--open) open_report="--open-report" ;;
    *) set -- "$@" "$arg" ;;
  esac
done

dotnet tool restore
dotnet restore
dotnet build --no-restore
dotnet stryker --config-file stryker-config.json ${open_report:+"$open_report"} "$@"
