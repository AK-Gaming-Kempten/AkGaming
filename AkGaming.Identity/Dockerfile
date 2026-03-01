# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

# Restore & publish using monorepo paths
RUN dotnet restore "AkGaming.Identity/Api/AkGaming.Identity.Api.csproj"

RUN dotnet publish "AkGaming.Identity/Api/AkGaming.Identity.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AkGaming.Identity.Api.dll"]