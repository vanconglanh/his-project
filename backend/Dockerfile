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

# tessdata (eng/vie.traineddata) - ProDiabHis.Api.csproj tham chieu "..\..\tessdata" tu
# src/ProDiabHis.Api/ (tuc /src/tessdata khi WORKDIR=/src) de copy vao output publish.
# THIEU dong nay -> publish KHONG co tessdata -> TesseractEngine init loi luc runtime
# (container build duoc nhung OCR that bai khi goi that).
COPY tessdata/ ./tessdata/

# Publish (cache NuGet packages giua cac lan build de tang toc)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/ProDiabHis.Api/ProDiabHis.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl cho healthcheck; libfontconfig1/libfreetype6 cho SkiaSharp (QuestPDF sinh bao cao PDF)
# libtesseract5/liblept5 cho OCR (NuGet Tesseract 5.2.0 P/Invoke native lib tren Linux,
#   NuGet nay chi bundle native cho Windows -> Linux BAT BUOC cai lib he thong).
# LUU Y: goi Debian bookworm dung ten "liblept5" (khong phai "libleptonica6" - ten nay
#   khong ton tai trong repo APT, gay loi "Unable to locate package"). libtesseract5
#   tu dong keo theo liblept5 qua dependency nhung khai bao ro cho tuong minh.
# NuGet tim libtesseract.so/libleptonica.so (khong hau to phien ban) nen tao symlink.
RUN apt-get update && apt-get install -y --no-install-recommends \
        curl libfontconfig1 libfreetype6 libtesseract5 liblept5 \
    && ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /usr/lib/x86_64-linux-gnu/libtesseract.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /usr/lib/x86_64-linux-gnu/libleptonica.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /usr/lib/x86_64-linux-gnu/libtesseract-5.3.0.so \
    && ldconfig \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

# NuGet Tesseract 5.2.0 dung InteropDotNet, tren Linux no tim file .so THEO DUNG TEN FILE
# native windows (vd "leptonica-1.82.0.dll" -> "libleptonica-1.82.0.so") va CHI DO trong
# thu muc goc app / thu muc kien truc (x64) - KHONG fallback sang system search path/
# LD_LIBRARY_PATH khi file khong ton tai o do. Da xac nhan bang test that: chi tao symlink
# he thong (/usr/lib/x86_64-linux-gnu/...) KHONG du -> phai co symlink dung ten ngay canh app.
# InteropDotNet P/Invoke thang "libdl" (khong hau to .so.2) de tu goi dlopen() — glibc bookworm
# chi co libdl.so.2 (khong co ban khong hau to vi libdl da gop vao libc tu glibc 2.34), nen phai
# tu tao symlink libdl.so, khong thi loi "Unable to load shared library 'libdl'".
RUN ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/libleptonica-1.82.0.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/libtesseract50.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so

ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    LD_LIBRARY_PATH=/usr/lib/x86_64-linux-gnu

EXPOSE 5000
ENTRYPOINT ["dotnet", "ProDiabHis.Api.dll"]
