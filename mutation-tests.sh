#!/usr/bin/env sh
# Mutation tests with Stryker.NET (local tool from .config/dotnet-tools.json).
# Run from anywhere; extra arguments go to Stryker, for example:
#   ./mutation-tests.sh --since:main
# Projects listed in IGNORED are skipped: Stryker 5.1.0 cannot parse the C# 15 `union` declaration (ResultUnion).
set -eu
cd "$(dirname "$0")"

IGNORED=" ResultUnion "

dotnet tool restore
dotnet restore
dotnet build --no-restore
for dir in src/*/; do
  name=$(basename "$dir")
  case "$IGNORED" in
    *" $name "*) echo "SKIP $name" ;;
    *) dotnet stryker --config-file stryker-config.json --project "$name.csproj" "$@" ;;
  esac
done
