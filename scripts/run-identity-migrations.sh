#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-list}"

export DOTNET_CLI_HOME=/tmp
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export PATH="$PATH:/tmp/.dotnet/tools"

if ! command -v dotnet-ef >/dev/null 2>&1; then
    dotnet tool install --global dotnet-ef --version '10.*'
fi

PROJECT="AkGaming.Identity/Migrations/Postgres/AkGaming.Identity.Migrations.Postgres.csproj"
CONTEXT="AuthDbContext"

dotnet restore "$PROJECT"

case "$MODE" in
    list)
        dotnet ef migrations list \
            --project "$PROJECT" \
            --context "$CONTEXT"
        ;;

    update)
        dotnet ef database update \
            --project "$PROJECT" \
            --context "$CONTEXT"
        ;;

    *)
        echo "Usage: $0 [list|update]"
        exit 2
        ;;
esac