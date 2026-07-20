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

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl --fail --silent --show-error --max-time 5 http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "AkGaming.Identity.Api.dll"]
