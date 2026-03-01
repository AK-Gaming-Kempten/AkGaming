# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY Api/*.csproj Api/
COPY Application/*.csproj Application/
COPY Contracts/*.csproj Contracts/
COPY Domain/*.csproj Domain/
COPY Infrastructure/*.csproj Infrastructure/

RUN dotnet restore Api/AkGaming.Identity.Api.csproj

COPY . .
RUN dotnet publish Api/AkGaming.Identity.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AkGaming.Identity.Api.dll"]
