FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AkGaming.GamelyBot/AkGaming.GamelyBot.csproj", "AkGaming.GamelyBot/"]
COPY ["AkGaming.GamelyBot/Infrastructure/Persistence/AkGaming.GamelyBot.Persistence.csproj", "AkGaming.GamelyBot/Infrastructure/Persistence/"]
COPY ["AkGaming.GamelyBot/Migrations/Postgres/AkGaming.GamelyBot.Migrations.Postgres.csproj", "AkGaming.GamelyBot/Migrations/Postgres/"]
COPY ["AkGaming.GamelyBot/Migrations/Sqlite/AkGaming.GamelyBot.Migrations.Sqlite.csproj", "AkGaming.GamelyBot/Migrations/Sqlite/"]
COPY ["AkGaming.Core/Notifications/AkGaming.Core.Notifications.csproj", "AkGaming.Core/Notifications/"]
RUN dotnet restore "AkGaming.GamelyBot/AkGaming.GamelyBot.csproj"
COPY . .
RUN dotnet publish "AkGaming.GamelyBot/AkGaming.GamelyBot.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl --fail --silent --show-error --max-time 5 http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "AkGaming.GamelyBot.dll"]
