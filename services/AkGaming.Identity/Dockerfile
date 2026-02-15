# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY AkGaming.Identity.Api/*.csproj AkGaming.Identity.Api/
COPY AkGaming.Identity.Application/*.csproj AkGaming.Identity.Application/
COPY AkGaming.Identity.Domain/*.csproj AkGaming.Identity.Domain/
COPY AkGaming.Identity.Infrastructure/*.csproj AkGaming.Identity.Infrastructure/

RUN dotnet restore AkGaming.Identity.Api/AkGaming.Identity.Api.csproj

COPY . .
RUN dotnet publish AkGaming.Identity.Api/AkGaming.Identity.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AkGaming.Identity.Api.dll"]
