#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-list}"

case "$MODE" in
    list|update) ;;
    *)
        echo "Usage: $0 [list|update]"
        exit 2
        ;;
esac

export DOTNET_CLI_HOME=/tmp
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export PATH="$PATH:/tmp/.dotnet/tools"

if ! command -v dotnet-ef >/dev/null 2>&1; then
    dotnet tool install --global dotnet-ef --version '10.*'
fi

PROJECT="AkGaming.Gamenight/Migrations/Postgres/AkGaming.Gamenight.Migrations.Postgres.csproj"
CONTEXT="GamenightDbContext"

dotnet restore "$PROJECT"

case "$MODE" in
    list)
        dotnet ef migrations list \
            --project "$PROJECT" \
            --startup-project "$PROJECT" \
            --context "$CONTEXT"
        ;;

    update)
        dotnet ef database update \
            --project "$PROJECT" \
            --startup-project "$PROJECT" \
            --context "$CONTEXT"
        ;;
esac
