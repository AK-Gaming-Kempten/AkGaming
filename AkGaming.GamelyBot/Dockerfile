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
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AkGaming.GamelyBot.dll"]
