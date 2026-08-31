# Evidence — Smart-upload mở rộng: nhiều file cùng lúc HOẶC 1 file ZIP (2026-08-31)

Mở rộng tính năng smart-upload (mục P TASKLIST) từ **1 file/lần** sang **nhiều file cùng lúc HOẶC 1 file ZIP**.
Mỗi file được OCR + phân loại **độc lập**, kết quả trả theo từng file (KHÔNG gộp chung).

## Thiết kế (tóm tắt)

- **`SafeZipExtractor`** (`backend/src/ProDiabHis.Application/Common/SafeZipExtractor.cs`): tách cơ chế giải nén ZIP
  an toàn (chống zip bomb / path traversal / giới hạn số file + dung lượng mỗi file + tổng dung lượng) từ
  `LegacyOcrBatchJob` ra helper dùng chung. `LegacyOcrBatchJob` đã được refactor để **dùng lại chính helper này**
  (không còn logic giải nén trùng lặp).
- **`SmartUploadBatchCommandHandler`**: nếu đúng 1 file `.zip` → giải nén; nếu nhiều file → xử lý trực tiếp.
  Mỗi file **gọi lại `SmartUploadCommand` (luồng xử lý-1-file có sẵn) ĐỘC LẬP** rồi gom `SmartUploadItemResult`
  theo từng file. Xử lý **đồng bộ** (trả kết quả ngay, không polling — dùng hàng ngày). Cap **20 file/lần**
  (>20 → `DOC_TOO_MANY_FILES`, hướng dẫn dùng chức năng Nhập hồ sơ giấy cũ cho batch lớn). 1 file OCR lỗi
  KHÔNG làm hỏng các file khác (item `success=false`).
- **Endpoint** `POST /api/v1/documents/smart-upload`: đổi `IFormFile file` → `IFormFileCollection files`
  (gom mọi file trong request — **tương thích ngược** cả client cũ gửi field `file`). Giữ nguyên `patient_id`,
  `encounter_id`, giới hạn 20MB/file.
- **Frontend** `SmartUploadDialog.tsx`: input `multiple` + nhận `.zip`, danh sách file chờ (thêm/xoá từng cái),
  sau xử lý hiện **danh sách thẻ kết quả riêng từng file** (`FileResultCard`) mở rộng xem chi tiết + **xác nhận
  riêng** — tái dùng nguyên `InBodySmartConfirm`/`LabResultSmartConfirm`/`RadResultSmartConfirm`/panel mơ hồ.
  Lưu 1 file không đóng dialog (đánh dấu ✓) để xác nhận tiếp file khác.

## Verify

### 1. Build + test backend
- `dotnet build` → **0 error**.
- `dotnet test` (UnitTests) → **955 pass** (baseline 950 + 5 test mới).
  - `SmartUploadBatchCommandHandlerTests` (4): mỗi file route độc lập không gộp; 1 file OCR lỗi không hỏng file khác;
    cap >20 file; danh sách rỗng.
  - `SmartUploadBatchZipIntegrationTests` (1): **verify thật trên file PDF thật**.

### 2. Verify thật — 3 file khác loại trong 1 ZIP, xử lý độc lập
`SmartUploadBatchZipIntegrationTests.ZipWithThreeMixedFiles_EachClassifiedIndependently`:
đóng gói 3 PDF thật (QuestPDF) vào 1 ZIP → giải nén bằng `SafeZipExtractor` (cơ chế production) → đọc text từng
file bằng PdfPig → chạy `DocumentClassifierService` **thực** cho **từng file độc lập**.

Kết quả (xem `batch-zip-3-files-ket-qua.txt`, file zip mẫu `batch-zip-3-files.zip`):

| File | Loại nhận diện | Độ tin cậy |
|---|---|---|
| 01-inbody.pdf | InBody | 0.90 |
| 02-xetnghiem.pdf | LabResult | 0.90 |
| 03-hosocu.pdf | Legacy | 0.50 |

→ Cả 3 phân loại **riêng, đúng, không lẫn lộn**.

### 3. Frontend
- `npx tsc --noEmit` → **exit 0, sạch**.
- `eslint` file thay đổi → **0 error** (1 warning `no-html-link-for-pages` là **pre-existing** trong
  `AmbiguousTypePanel`, không thuộc thay đổi này).

## Còn tồn / giới hạn

- **E2E HTTP qua container**: KHÔNG chạy được do lỗi **pre-existing** của `backend/Dockerfile` — Debian bookworm
  không còn package `libleptonica6` (`E: Unable to locate package libleptonica6`) nên `docker build backend` fail
  (không liên quan thay đổi này; container `prodiab-backend` đang chạy vẫn là image cũ). Việc phân loại độc lập
  từng file trên **file thật** đã được chứng minh đầy đủ bằng integration test mục 2 (chạy qua đúng
  `SafeZipExtractor` + `DocumentClassifierService` thực). Sửa Dockerfile nằm ngoài phạm vi task (rủi ro ảnh hưởng
  image deploy đang dùng).
