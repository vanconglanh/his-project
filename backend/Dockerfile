# =====================================================================
# Pro-Diab HIS — Backend (.NET 8 Web API) — multi-stage build
# Build:  docker build -t prodiab-backend ./backend
# =====================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore (copy csproj tree first for layer caching)
COPY src/ ./src/
RUN dotnet restore src/ProDiabHis.Api/ProDiabHis.Api.csproj

# Publish
RUN dotnet publish src/ProDiabHis.Api/ProDiabHis.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl cho healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 5000
ENTRYPOINT ["dotnet", "ProDiabHis.Api.dll"]
