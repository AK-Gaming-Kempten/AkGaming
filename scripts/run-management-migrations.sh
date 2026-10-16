#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-list}"

export DOTNET_CLI_HOME=/tmp
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export PATH="$PATH:/root/.dotnet/tools"

if ! command -v dotnet-ef >/dev/null 2>&1; then
    dotnet tool install --global dotnet-ef --version '10.*'
fi

declare -A PROJECTS=(
    [MemberManagement]="AkGaming.Management/Modules/MemberManagement/Migrations/Postgres/AkGaming.Management.Modules.MemberManagement.Migrations.Postgres.csproj"
    [InvoiceManagement]="AkGaming.Management/Modules/InvoiceManagement/Migrations/Postgres/AkGaming.Management.Modules.InvoiceManagement.Migrations.Postgres.csproj"
    [Disbursements]="AkGaming.Management/Modules/Disbursements/Migrations/Postgres/AkGaming.Management.Modules.Disbursements.Migrations.Postgres.csproj"
    [GeneralMeetings]="AkGaming.Management/Modules/GeneralMeetings/Migrations/Postgres/AkGaming.Management.Modules.GeneralMeetings.Migrations.Postgres.csproj"
    [BoardManagement]="AkGaming.Management/Modules/BoardManagement/Migrations/Postgres/AkGaming.Management.Modules.BoardManagement.Migrations.Postgres.csproj"
)

declare -A CONTEXTS=(
    [MemberManagement]="MemberManagementDbContext"
    [InvoiceManagement]="InvoiceManagementDbContext"
    [Disbursements]="DisbursementsDbContext"
    [GeneralMeetings]="GeneralMeetingsDbContext"
    [BoardManagement]="BoardManagementDbContext"
)

ORDER=(
    MemberManagement
    InvoiceManagement
    Disbursements
    GeneralMeetings
    BoardManagement
)

for module in "${ORDER[@]}"; do
    project="${PROJECTS[$module]}"
    context="${CONTEXTS[$module]}"

    echo
    echo "===== $module ====="

    if [ "$module" = "MemberManagement" ]; then
        dotnet restore "$project"
    else
        dotnet restore "$project" -p:RestoreUseStaticGraphEvaluation=true
    fi

    case "$MODE" in
        list)
            dotnet ef migrations list \
                --project "$project" \
                --context "$context"
            ;;

        update)
            dotnet ef database update \
                --project "$project" \
                --context "$context"
            ;;

        *)
            echo "Usage: $0 [list|update]"
            exit 2
            ;;
    esac
done