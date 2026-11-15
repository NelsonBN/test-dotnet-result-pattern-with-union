#!/usr/bin/env sh
# Mutation tests with Stryker.NET (local tool from .config/dotnet-tools.json).
# Run from anywhere; extra arguments go to Stryker, for example:
#   ./mutation-tests.sh --project MyProject.csproj --since:main
set -eu
cd "$(dirname "$0")"

dotnet tool restore
dotnet restore
dotnet build --no-restore
dotnet stryker --config-file stryker-config.json --open-report "$@"
