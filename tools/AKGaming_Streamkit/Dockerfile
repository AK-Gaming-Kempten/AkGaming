FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ─── build stage ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# copy and restore only csproj first for cache
COPY *.csproj ./
RUN dotnet restore

# copy everything and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ─── runtime stage ───────────────────────────────────────
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# serve the published wwwroot
COPY --from=build /app/publish/wwwroot .


