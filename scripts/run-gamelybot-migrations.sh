#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-list}"

export DOTNET_CLI_HOME=/tmp
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export PATH="$PATH:/tmp/.dotnet/tools"

if ! command -v dotnet-ef >/dev/null 2>&1; then
    dotnet tool install --global dotnet-ef --version '10.*'
fi

PROJECT="AkGaming.GamelyBot/Migrations/Postgres/AkGaming.GamelyBot.Migrations.Postgres.csproj"
CONTEXT="GamelyBotDbContext"

dotnet restore "$PROJECT" \
    -p:RestoreUseStaticGraphEvaluation=true

dotnet build "$PROJECT" \
    --configuration Release \
    --no-restore \
    -p:WarningsNotAsErrors=NU1903

case "$MODE" in
    list)
        dotnet ef migrations list \
            --project "$PROJECT" \
            --startup-project "$PROJECT" \
            --context "$CONTEXT" \
            --configuration Release \
            --no-build
        ;;

    update)
        dotnet ef database update \
            --project "$PROJECT" \
            --startup-project "$PROJECT" \
            --context "$CONTEXT" \
            --configuration Release \
            --no-build
        ;;

    *)
        echo "Usage: $0 [list|update]"
        exit 2
        ;;
esac