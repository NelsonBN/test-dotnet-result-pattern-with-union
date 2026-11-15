#!/usr/bin/env sh
# Mutation tests with Stryker.NET (local tool from .config/dotnet-tools.json).
# Run from anywhere. The HTML report is NOT opened unless you pass -o (or --open);
# with -o one report is opened per project. Every other argument goes to Stryker, for example:
#   ./mutation-tests.sh -o --since:main
# Projects listed in IGNORED are skipped: Stryker 5.1.0 cannot parse the C# 15 `union` declaration (ResultUnion).
set -eu
cd "$(dirname "$0")"

IGNORED=" ResultUnion "

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
for dir in src/*/; do
  name=$(basename "$dir")
  case "$IGNORED" in
    *" $name "*) echo "SKIP $name" ;;
    *) dotnet stryker --config-file stryker-config.json --project "$name.csproj" ${open_report:+"$open_report"} "$@" ;;
  esac
done
