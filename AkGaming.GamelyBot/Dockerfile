FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["AkGaming.GamelyBot/AkGaming.GamelyBot.csproj", "AkGaming.GamelyBot/"]
COPY ["AkGaming.Core/Notifications/AkGaming.Core.Notifications.csproj", "AkGaming.Core/Notifications/"]
RUN dotnet restore "AkGaming.GamelyBot/AkGaming.GamelyBot.csproj"
COPY . .
RUN dotnet publish "AkGaming.GamelyBot/AkGaming.GamelyBot.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AkGaming.GamelyBot.dll"]
