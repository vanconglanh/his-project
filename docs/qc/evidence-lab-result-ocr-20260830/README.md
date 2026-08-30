# Evidence — OCR đọc kết quả xét nghiệm (CLS/Lab)

Ngày: 2026-08-30. Tính năng: khi KTV/bác sĩ upload file PDF/ảnh kết quả XN cho 1 lượt khám
đang chờ kết quả, hệ thống OCR đọc file + dò khớp các xét nghiệm ĐANG CHỜ trong đúng LabOrder đó,
hiển thị màn xác nhận (sửa tay được) rồi mới lưu `LabResult`.

## Tái dùng hạ tầng (không viết lại engine đọc file)
- PDF: `IPdfTextExtractor` (PdfPig text-layer + fallback render trang → OCR ảnh) — đã có sẵn.
- Ảnh: `IOcrTextProvider` / `TesseractOcrProvider` (Tesseract vie+eng) — đã có sẵn.
- Provider mới `LabOcrTextProvider` chỉ ĐIỀU PHỐI 2 engine trên theo content-type.
- Parser mới `LabResultOcrParser` (thuần, unit-test được) dò số + đơn vị theo tên/mã XN đang chờ.
- Confirm TÁI DÙNG `CreateLabResultCommand` (đã có payment gate G02, tính flag, SoD, audit).

## 1. Unit test parser (9/9 PASS)
File: `backend/tests/ProDiabHis.UnitTests/LabResults/LabResultOcrParserTests.cs`
+ `LabResultOcrPdfIntegrationTests.cs` (đọc PDF THẬT bằng PdfPig rồi parse).
Lệnh: `dotnet test --filter FullyQualifiedName~LabResultOcr` → Passed 9, Failed 0.
Full suite: `dotnet test ProDiabHis.UnitTests` → Passed 927, Failed 0.

## 2. Đọc PDF thật (file trong thư mục này)
- `phieu-ket-qua-xn-test.pdf` — phiếu KQ sinh bằng QuestPDF (Glucose 7.2, HbA1c 8.10, Cholesterol 6.1, Triglyceride 2.30).
- `raw-text-doc-tu-pdf.txt` — text PdfPig đọc ra từ PDF đó.
- `ket-qua-parse.txt` — kết quả parser: 4 XN có trên phiếu đọc đúng, TSH (không có) → "chưa đọc được".

## 3. E2E qua HTTP thật (API chạy local port 5099, MySQL/Redis dev đang chạy)
Login KTV `ktv.test@prodiab.test` (quyền `lab_result.write`) → JWT.
Seed 2 LabOrder cho encounter `7e3d4eec-f54d-4640-8aac-3616debc15f5`: GLU (Glucose máu), HBA1C (HbA1c).

### 3a. POST /api/v1/lab-results/ocr-extract (upload PDF thật)
Response (rút gọn):
```
{ "data": { "encounter_id":"7e3d4eec-...","pending_count":3,"extracted_count":2,
  "fields":[
    {"lab_order_item_id":"5a49bb1d-...","test_code":"GLU_F", ...,"extracted":false},   // XN khác không có trên phiếu -> chưa đọc được
    {"lab_order_item_id":"e0000000-...-a1","test_code":"GLU","value":"7.2","value_numeric":7.2,"extracted":true},
    {"lab_order_item_id":"e0000000-...-a2","test_code":"HBA1C","value":"8.10","value_numeric":8.1,"extracted":true}
  ] } }
```
=> Đọc đúng, chỉ dò trong các XN đang chờ của đúng lượt khám, field không đọc được không chặn field khác.

### 3b. POST /api/v1/lab-results/ocr-confirm (mô phỏng SỬA TAY: GLU 7.2 -> 7.5)
Body items snake_case, include=true cho GLU+HBA1C. Response:
```
{ "data": { "created_count": 2, "failed_count": 0, "errors": [] } }
```

### 3c. Xác nhận DB `diab_his_lab_results`
```
test_code  test_name     value  value_numeric  unit     flag    status  source
GLU        Glucose máu   7.5    7.5000         mmol/L   NORMAL  DRAFT   MANUAL
HBA1C      HbA1c         8.10   8.1000         %        NORMAL  DRAFT   MANUAL
```
=> LabResult tạo đúng, giá trị đã sửa tay (7.5) được lưu, flag tính tự động, trạng thái DRAFT (đúng
luồng: phải người khác VERIFY — SoD), source MANUAL.

## Endpoints mới
- `POST /api/v1/lab-results/ocr-extract` — [lab_result.write] — upload file + OCR, KHÔNG ghi DB.
- `POST /api/v1/lab-results/ocr-confirm` — [lab_result.write] — tạo LabResult qua luồng có sẵn.

## Lưu ý
- Đơn vị (unit) đôi khi null khi trên phiếu số dán liền reference range ("7.2mmol/L3.9-6.4"); không
  chặn — value y tế trọng yếu vẫn chính xác, người dùng bổ sung đơn vị ở màn xác nhận.
- Permission: dùng lại `lab_result.write` (không tạo quyền mới).
