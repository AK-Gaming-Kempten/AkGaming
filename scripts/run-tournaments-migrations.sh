#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-list}"

export DOTNET_CLI_HOME=/tmp
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export PATH="$PATH:/tmp/.dotnet/tools"

if ! command -v dotnet-ef >/dev/null 2>&1; then
    dotnet tool install --global dotnet-ef --version '10.*'
fi

PROJECT="AkGaming.Tournaments/Infrastructure/Postgres/AkGaming.Tournaments.Infrastructure.Postgres.csproj"
STARTUP_PROJECT="AkGaming.Tournaments/WebApi/AkGaming.Tournaments.WebApi.csproj"
CONTEXT="TournamentDbContext"

dotnet restore "$PROJECT"
dotnet restore "$STARTUP_PROJECT"

if ! find AkGaming.Tournaments/Infrastructure/Postgres \
    -path '*/Migrations/*.cs' \
    -print -quit | grep -q .
then
    echo "No tournaments migrations were found. Skipping."
    exit 0
fi

case "$MODE" in
    list)
        dotnet ef migrations list \
            --project "$PROJECT" \
            --startup-project "$STARTUP_PROJECT" \
            --context "$CONTEXT"
        ;;

    update)
        dotnet ef database update \
            --project "$PROJECT" \
            --startup-project "$STARTUP_PROJECT" \
            --context "$CONTEXT"
        ;;

    *)
        echo "Usage: $0 [list|update]"
        exit 2
        ;;
esac