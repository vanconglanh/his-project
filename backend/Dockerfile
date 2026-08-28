# =====================================================================
# Pro-Diab HIS — Backend (.NET 8 Web API) — multi-stage build
# Build:  docker build -t prodiab-backend ./backend
# =====================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# --- Layer ON DINH: chi copy .csproj (khong copy .cs) de restore duoc cache ---
# Chi khi them/doi NuGet package (them dong duoi) layer nay moi invalidate;
# sua code .cs binh thuong se KHONG lam restore chay lai -> build lai rat nhanh.
COPY src/ProDiabHis.Domain/ProDiabHis.Domain.csproj          src/ProDiabHis.Domain/
COPY src/ProDiabHis.Contracts/ProDiabHis.Contracts.csproj    src/ProDiabHis.Contracts/
COPY src/ProDiabHis.Application/ProDiabHis.Application.csproj src/ProDiabHis.Application/
COPY src/ProDiabHis.Infrastructure/ProDiabHis.Infrastructure.csproj src/ProDiabHis.Infrastructure/
COPY src/ProDiabHis.Api/ProDiabHis.Api.csproj                 src/ProDiabHis.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/ProDiabHis.Api/ProDiabHis.Api.csproj

# --- Layer HAY DOI: source code that su ---
COPY src/ ./src/

# Publish (cache NuGet packages giua cac lan build de tang toc)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/ProDiabHis.Api/ProDiabHis.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl cho healthcheck; libfontconfig1/libfreetype6 cho SkiaSharp (QuestPDF sinh bao cao PDF)
RUN apt-get update && apt-get install -y --no-install-recommends \
        curl libfontconfig1 libfreetype6 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 5000
ENTRYPOINT ["dotnet", "ProDiabHis.Api.dll"]
