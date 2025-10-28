FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /srv/managementtool
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Restore
RUN dotnet restore ./ManagementTool.sln
# If no .sln, then:
# RUN dotnet restore src/ManagementTool.WebApi/ManagementTool.WebApi.csproj

# Build
RUN dotnet build src/ManagementTool.WebApi/ManagementTool.WebApi.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish src/ManagementTool.WebApi/ManagementTool.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /srv/managementtool
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ManagementTool.WebApi.dll"]


