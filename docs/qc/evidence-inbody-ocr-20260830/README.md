# Evidence — InBody OCR (backend) — 2026-08-30

Verify thật trên stack local (`docker compose ops/docker-compose.yml + docker-compose.local-app.yml`),
rebuild image `prodiab-dev-backend` với code mới, chạy migration `9173_inbody_reports.sql` vào
MySQL thật, gọi API thật qua curl (không mock).

## File PDF mẫu (sinh bằng QuestPDF, mô phỏng layout máy InBody)

- `sample-inbody-full.pdf` — đủ 9 label chuẩn (Weight/SMM/Body Fat Mass/BMI/PBF/Visceral Fat
  Level/TBW/BMR/InBody Score).
- `sample-inbody-partial.pdf` — mô phỏng máy đời cũ, chỉ có 4 label (Weight/SMM/PBF/BMI, dùng
  viết tắt), thiếu 5 field còn lại — kiểm tra parser không throw, đánh dấu đúng field thiếu.

## 1. Upload PDF đầy đủ → extract đúng cả 9 field

Request: `POST /api/v1/patients/f0000000-0000-0000-0000-000000000008/inbody-reports`
(multipart, `file=sample-inbody-full.pdf`, `encounter_id=764e58fe-d048-4765-bb0e-8b3e0ac3b75b`)

Response: `1-upload-full-pdf-response.json` — `extraction_status=pending`, cả 9 field
`extracted=true` với giá trị đúng như nhúng trong PDF (Weight 68.4kg, SMM 30.2kg, Body Fat Mass
14.1kg, PBF 20.6%, BMI 22.7, Visceral Fat Level 7, TBW 39.8L, BMR 1480kcal, InBody Score 79).

## 2. Upload PDF thiếu field → không throw, đánh dấu đúng field thiếu

Request: `POST /api/v1/patients/f0000000-0000-0000-0000-000000000008/inbody-reports`
(multipart, `file=sample-inbody-partial.pdf`, không kèm `encounter_id`)

Response: `2-upload-partial-pdf-response.json` — HTTP 200 (không phải lỗi 500), 4 field extract
đúng (Weight/SMM/PBF/BMI), 5 field còn lại `extracted=false, value=null` đúng như kỳ vọng
(BODY_FAT_MASS/VISCERAL_FAT/TBW/BMR/INBODY_SCORE).

## 3. Confirm → ghi VitalSigns + indicator_reading, audit log

Request: `POST /api/v1/inbody-reports/{id-cua-buoc-1}/confirm` — điều dưỡng SỬA giá trị cân nặng
từ 68.4 → 68.5 (mô phỏng sửa tay trước khi lưu), `include=false` cho BMI (không lưu riêng vì
VitalSigns không có cột BMI), 7 field còn lại `include=true`.

Response: `3-confirm-response.json` — `extraction_status=success`, `confirmed_by`/`confirmed_at`
có giá trị.

### Verify DB thật (query trực tiếp MySQL container `prodiab-mysql`)

- `diab_his_enc_vital_signs`: có bản ghi mới `weight_kg = 68.50` (giá trị ĐÃ SỬA, không phải
  68.4 gốc — xác nhận đúng field điều dưỡng nhập ở bước confirm được lưu, không phải giá trị
  extract thô), `note = 'Nhập từ kết quả máy InBody (đã xác nhận)'`.
- `diab_his_cli_indicator_reading`: đúng 7 dòng (SMM/BODY_FAT_MASS/PBF/VISCERAL_FAT/TBW/BMR/
  INBODY_SCORE), tất cả `source = 'inbody_ocr'`, `value`/`unit` khớp field đã confirm. Không có
  dòng BMI (đúng thiết kế — BMI không persist riêng).
- `diab_his_sec_audit_logs`: có đúng 2 dòng `resource_type = 'InBodyReport'` cho report vừa tạo
  — `CREATE` lúc upload, `CONFIRM` lúc xác nhận.

## 4. Multi-tenant / not-found

`GET /api/v1/patients/00000000-0000-0000-0000-000000000000/inbody-reports` (patient không tồn
tại trong tenant hiện tại) → HTTP 404 (`PATIENT_NOT_FOUND`) — verify bằng curl `-o /dev/null -w
"%{http_code}"` = `404`.

## 5. Unit test parser

`dotnet test --filter FullyQualifiedName~InBody` → 5/5 pass (test đủ field, test dùng viết
tắt/khoảng trắng khác nhau, test thiếu field không throw, test text rỗng, test null).

## 6. Toàn bộ test suite không bị phá

`dotnet test tests/ProDiabHis.UnitTests` → 858/858 pass (bao gồm 5 test InBody mới).
`dotnet test tests/ProDiabHis.ArchitectureTests` → 6/6 pass.
`dotnet build` (Debug) → Build succeeded, 0 error, các warning còn lại đều PRE-EXIST (Rooms/
Branches/QuestPdfReportExporter — không liên quan code InBody mới thêm).
