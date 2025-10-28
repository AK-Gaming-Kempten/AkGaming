FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution + all csproj files
COPY *.sln ./
COPY src/**/**/*.csproj ./ 

RUN dotnet restore

# Copy remainder
COPY . .

RUN dotnet build src/ManagementTool.WebApi/ManagementTool.WebApi.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish src/ManagementTool.WebApi/ManagementTool.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ManagementTool.WebApi.dll"]

