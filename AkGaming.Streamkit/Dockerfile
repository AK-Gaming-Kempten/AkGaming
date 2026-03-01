FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AkGaming.Streamkit.csproj", "./"]
RUN dotnet restore "AkGaming.Streamkit.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "./AkGaming.Streamkit.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AkGaming.Streamkit.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AkGaming.Streamkit.dll"]

