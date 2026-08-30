# Evidence — mo rong dinh dang file cho tinh nang "Nhap ho so cu qua OCR" (legacy-import)

Ngay: 2026-08-30
Agent: Thao (Backend Developer)

## Boi canh

Truoc khi lam task nay, `LegacyOcrBatchJob` chi nhan `.jpg/.jpeg/.png` trong file ZIP upload.
Task mo rong ho tro them **PDF** (uu tien cao nhat), **TIFF/BMP** (de, ho tro san qua Tesseract),
va danh gia/guard ro rang cho **HEIC/HEIF**.

## Thay doi code

- `backend/src/ProDiabHis.Application/LegacyImport/IPdfTextExtractor.cs` — interface moi.
- `backend/src/ProDiabHis.Infrastructure/Ocr/PdfTextExtractor.cs` — implementation 2 tang:
  - Tang 1: `UglyToad.PdfPig` doc text layer truc tiep (tai su dung thu vien da co cho InBody OCR).
  - Tang 2 (fallback khi tang 1 khong ra du text, nguong `MinTextLayerChars = 20` ky tu
    non-whitespace): dung goi **PDFtoImage** (moi them, wrap PDFium native, ho tro
    Windows/Linux/macOS khong can cai them goi he thong tren Linux Docker) render tung trang PDF
    thanh anh PNG -> tai su dung `IOcrTextProvider` (Tesseract co san) OCR tung trang -> gop text
    tat ca cac trang thanh 1 ket qua duy nhat cho item (1 file PDF nhieu trang = 1 bo ho so 1 benh
    nhan -> KHONG tach thanh nhieu item theo trang, de admin de quan ly/review hon).
  - Gioi han `MaxOcrPages = 30` trang/file de tranh 1 file PDF qua lon lam treo job.
- `backend/src/ProDiabHis.Infrastructure/Jobs/LegacyImportFileKind.cs` — tach logic phan loai
  file (`LegacyImportFileClassifier.Classify`) thanh static class doc lap, de unit test khong can
  DB/MinIO/Hangfire, dung chung boi `LegacyOcrBatchJob`.
- `backend/src/ProDiabHis.Infrastructure/Jobs/LegacyOcrBatchJob.cs`:
  - Zip-bomb/whitelist guard nay dung `LegacyImportFileClassifier` — PDF nam trong whitelist,
    khong bi chan nham nhu file la.
  - PDF: upload nguyen file PDF vao bucket `legacy-scans` (cot `image_object_key` dung chung,
    khong doi schema), OCR qua `IPdfTextExtractor`.
  - TIFF/BMP: xu ly nhu anh binh thuong (Tesseract/Leptonica ho tro san dinh dang nay, khong can
    thu vien them).
  - HEIC/HEIF: **KHONG xu ly** — tao item voi `status='failed'` va
    `item_error='Định dạng HEIC/HEIF chưa được hỗ trợ, vui lòng chuyển đổi sang JPG/PNG hoặc PDF trước khi upload'`.
    Ly do khong lam: khong tim duoc thu vien decode HEIC on dinh + license ro rang chay tren
    Linux Docker ma khong can them native binary phuc tap (rui ro build/deploy cao so voi loi ich
    — may scan van phong hau nhu khong xuat HEIC, chi dien thoai iPhone chup anh moi ra dinh dang
    nay va nguoi dung co the de dang chuyen sang JPG bang app Photos/Files co san tren iPhone).
- `backend/src/ProDiabHis.Infrastructure/DependencyInjection.cs` — dang ky `IPdfTextExtractor`.
- `backend/src/ProDiabHis.Infrastructure/ProDiabHis.Infrastructure.csproj` — them goi NuGet
  `PDFtoImage` 4.1.0 (MIT-style permissive, wrap `bblanchon.PDFium.*` native cho Win/Linux/macOS).

## Thu vien moi va ly do chon

| Thu vien | Ly do |
|---|---|
| `PDFtoImage` 4.1.0 | Render trang PDF -> `SKBitmap` (dung chung SkiaSharp da co san trong du an). Bundle san PDFium native cho Win32/Linux/macOS qua NuGet — khong can cai them goi he thong tren Docker Linux image, license permissive, API don gian (`Conversion.ToImage(bytes, index)`). |

**Khong them thu vien HEIC** — danh gia rui ro cao (native codec libheif phuc tap, license/build
tren Linux container rui ro) so voi loi ich thap (may scan phong kham hau nhu khong xuat HEIC).
Chon guard ro rang thay vi ep lam.

## Test

- `backend/tests/ProDiabHis.UnitTests/LegacyImport/PdfTextExtractorTests.cs` — 3 test (mock
  `IOcrTextProvider`): tang 1 (text layer, khong goi OCR), tang 2 fallback (khong text layer, co
  goi OCR), va file PDF hong -> failure ro rang.
- `backend/tests/ProDiabHis.UnitTests/LegacyImport/PdfOcrFallbackRealEngineVerifyTests.cs` —
  **VERIFY THAT khong mock**: dung Tesseract engine that (tessdata vie+eng co san trong repo) +
  PDFtoImage that de OCR 1 PDF "gia lap scan" (chu duoc ve thanh anh PNG roi nhung vao PDF nhu 1
  hinh, PdfPig khong trich duoc text tu day) -> xac nhan tang 2 hoat dong dung, OCR ra dung chuoi
  "HOSOBENHNHAN" da ve. Day la bang chung end-to-end that cho luong OCR fallback PDF-scan.
- `backend/tests/ProDiabHis.UnitTests/LegacyImport/LegacyImportFileClassifierTests.cs` — 13 case
  theory kiem tra dung whitelist/guard cho tat ca dinh dang: jpg/jpeg/png/tiff/tif/bmp -> Image,
  pdf -> Pdf, heic/heif -> UnsupportedGuard, cac duoi khac (txt/docx/exe) -> Ignored (khong throw,
  khong lot qua whitelist).

Ket qua chay toan bo unit test suite: xem `unittests-full-run.txt` — **918/918 pass** (baseline
truoc task ~901, tang them 17 test moi cho tinh nang nay, khong test nao cu bi fail).

Build: `dotnet build` toan solution — 0 error, 13 warning (toan bo la warning co san tu truoc khi
lam task nay, khong lien quan code moi — xem `dotnet-build-full.txt`).

## Dinh dang ho tro sau task nay

| Dinh dang | Trang thai | Ghi chu |
|---|---|---|
| JPG/JPEG | Da ho tro (khong doi) | OCR truc tiep bang Tesseract |
| PNG | Da ho tro (khong doi) | OCR truc tiep bang Tesseract |
| TIFF/TIF | **Moi ho tro** | OCR truc tiep bang Tesseract (Leptonica doc duoc san) |
| BMP | **Moi ho tro** | OCR truc tiep bang Tesseract (Leptonica doc duoc san) |
| PDF (co text layer) | **Moi ho tro** | Doc text truc tiep bang PdfPig, nhanh + chinh xac 100% |
| PDF (anh scan, khong text layer) | **Moi ho tro** | Render tung trang bang PDFtoImage (PDFium) + OCR Tesseract, gop text ca file thanh 1 item |
| HEIC/HEIF | **Chua ho tro — guard ro rang** | Tao item `status=failed` voi thong bao tieng Viet yeu cau chuyen doi dinh dang, khong am tham bo qua |
