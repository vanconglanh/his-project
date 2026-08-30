# PRD — Đọc kết quả máy InBody (trích PDF) — 2026-08-30

## 1. Bối cảnh

Phòng khám đo thành phần cơ thể bằng máy InBody (270/370/570/770...). Máy in ra file PDF kết
quả. Hiện tại điều dưỡng phải gõ tay từng chỉ số vào hệ thống — chậm và dễ sai. Yêu cầu: tự
động trích số liệu từ PDF để điền sẵn, điều dưỡng chỉ cần xác nhận/sửa trước khi lưu.

## 2. User story

- Là điều dưỡng, tôi upload file PDF kết quả InBody của bệnh nhân, hệ thống tự động đọc và hiển
  thị các chỉ số đã trích được để tôi xác nhận/sửa trước khi lưu vào hồ sơ.
- Là điều dưỡng, nếu PDF không đọc được một số chỉ số (thiếu field/máy quét ảnh), tôi vẫn thấy
  rõ field nào bị thiếu để tự nhập tay, không bị chặn luồng làm việc.
- Là bác sĩ, tôi xem lại lịch sử các lần đo InBody của bệnh nhân kèm file gốc.

## 3. Giới hạn MVP (quan trọng)

- **CHỈ đọc text layer của PDF** (`UglyToad.PdfPig`), **KHÔNG OCR ảnh**. Nếu máy InBody xuất
  PDF dạng scan ảnh (không có text layer), hệ thống sẽ trả về `extraction_status = failed`
  hoặc rỗng toàn bộ field — điều dưỡng phải nhập tay 100%. Đây là giới hạn đã biết, ghi rõ
  trong code (`InBodyPdfTextProvider`) và cảnh báo cho người dùng ở FE (việc của agent frontend).
- Parser dựa trên **label tiếng Anh** (mặc định máy InBody xuất tiếng Anh). Nếu máy cấu hình
  xuất ngôn ngữ khác, field tương ứng sẽ không đọc được → cần nhập tay.
- **Không tự động commit** kết quả vào hồ sơ bệnh nhân — luôn qua bước xác nhận (endpoint
  `confirm` riêng) để giảm rủi ro sai số liệu y tế.
- Định hướng tương lai: khi tích hợp thẳng API InBody, chỉ cần thêm implementation mới
  `InBodyApiProvider` cho interface `IInBodyDataProvider` — không đổi domain/DB/API contract.

## 4. Kiến trúc

- `ProDiabHis.Application.InBody.IInBodyDataProvider` — interface, MVP dùng
  `InBodyPdfTextProvider` (Infrastructure), tương lai thêm `InBodyApiProvider`.
- `InBodyReportParser` (Application, thuần, không phụ thuộc PdfPig) — parser label-based bằng
  regex, tách riêng để unit test độc lập bằng chuỗi text mẫu.

## 5. Bảng mapping label → indicator_type

| Label PDF (tiếng Anh, không phân biệt hoa/thường) | `indicator_type` | Đơn vị | Lưu vào |
|---|---|---|---|
| `Weight` | `WEIGHT_KG` | kg | `VitalSigns.WeightKg` (bảng sinh hiệu hiện có) |
| `BMI` | `BMI` | kg/m² | Chỉ hiển thị FE (VitalSigns không có cột BMI riêng — BMI tính lại từ weight/height theo `VitalSignsValidator.ComputeBmi`), KHÔNG persist riêng |
| `Skeletal Muscle Mass` / `SMM` | `SMM` | kg | `diab_his_cli_indicator_reading` |
| `Body Fat Mass` | `BODY_FAT_MASS` | kg | `diab_his_cli_indicator_reading` |
| `Percent Body Fat` / `PBF` | `PBF` | % | `diab_his_cli_indicator_reading` |
| `Visceral Fat Level` | `VISCERAL_FAT` | (số nguyên, không đơn vị) | `diab_his_cli_indicator_reading` |
| `Total Body Water` / `TBW` | `TBW` | L | `diab_his_cli_indicator_reading` |
| `Basal Metabolic Rate` / `BMR` | `BMR` | kcal | `diab_his_cli_indicator_reading` |
| `InBody Score` | `INBODY_SCORE` | (điểm) | `diab_his_cli_indicator_reading` |

Ghi chú: dự án **không có sẵn bảng `ClinicalIndicator` generic** (đã grep xác nhận — chỉ có
bảng cố định schema `diab_his_cli_diabetes_assessments` cho HbA1c/glucose/eGFR/BP/BMI, không
phù hợp lưu chỉ số InBody). Do đó tạo mới bảng generic nhỏ `diab_his_cli_indicator_reading`
(patient_id, indicator_type, value, unit, source, recorded_at, encounter_id nullable) —
`source = 'inbody_ocr'` cho mọi bản ghi sinh ra từ luồng này, để phân biệt với nguồn nhập tay
khác nếu sau này mở rộng.

## 6. Luồng nghiệp vụ

1. `POST /api/v1/patients/{patientId}/inbody-reports` (multipart, field `file`, optional
   `encounter_id`) — upload PDF → lưu file vào MinIO bucket `inbody-reports` → chạy
   `IInBodyDataProvider.ExtractAsync` → lưu bản ghi `InBodyReport` với `extraction_status =
   pending` (chưa có xác nhận) → trả về danh sách field trích được (kèm cờ `extracted`
   true/false cho từng field) cho FE hiển thị màn xác nhận. **Không** ghi vào
   `VitalSigns`/`diab_his_cli_indicator_reading` ở bước này.
2. `POST /api/v1/inbody-reports/{id}/confirm` — nhận danh sách field đã được điều dưỡng xác
   nhận/sửa (`indicator_type`, `value`, `unit`, `include`) + `encounter_id` (bắt buộc nếu
   report chưa có sẵn) → ghi `WEIGHT_KG` (nếu `include=true`) vào `VitalSigns` (tạo bản ghi
   sinh hiệu mới gắn với encounter), các field còn lại ghi vào
   `diab_his_cli_indicator_reading` → set `extraction_status = success` (đủ field) hoặc
   `partial` (thiếu field), `confirmed_by`/`confirmed_at`.
3. `GET /api/v1/patients/{patientId}/inbody-reports` — lịch sử các lần đo, kèm signed URL file
   gốc.

## 7. Acceptance criteria

- AC-1: Upload PDF có đủ text layer với 9 label chuẩn → extract đúng toàn bộ 9 giá trị số +
  đơn vị, `extraction_status = pending`, không có field nào bị ghi nhận "không đọc được".
- AC-2: Upload PDF thiếu 1-2 label (mô phỏng máy đời cũ layout khác) → các field khác vẫn
  extract đúng, field thiếu trả `extracted = false, value = null`, không throw exception,
  HTTP 200 vẫn trả về (không phải lỗi 500).
- AC-3: Gọi `confirm` với field `WEIGHT_KG.include = true` → tạo đúng 1 bản ghi `VitalSigns`
  mới gắn `encounter_id` với `weight_kg` đúng giá trị đã xác nhận (có thể khác giá trị extract
  gốc nếu điều dưỡng sửa tay).
- AC-4: Gọi `confirm` với các field còn lại `include = true` → mỗi field tạo 1 bản ghi
  `diab_his_cli_indicator_reading` với `indicator_type` đúng bảng mapping, `source =
  'inbody_ocr'`.
- AC-5: Multi-tenant — request `inbody-reports` của bệnh nhân tenant khác trả 404
  (`PATIENT_NOT_FOUND` hoặc `INBODY_REPORT_NOT_FOUND`), không lộ dữ liệu chéo tenant.
- AC-6: Audit log ghi `CREATE` (upload) và `CONFIRM`/`UPDATE` (xác nhận) trên resource
  `InBodyReport`.
- AC-7: Permission `patient.clinical.write` bắt buộc cho cả 2 endpoint upload + confirm; `GET`
  lịch sử dùng `patient.clinical.read` (nếu có) hoặc `patient.read` — xem code controller để
  biết permission thực tế áp dụng.

## 8. Việc còn lại cho Frontend (ngoài phạm vi PRD này — agent FE làm sau)

- Màn upload PDF trong hồ sơ bệnh nhân/encounter (nút "Nhập kết quả InBody").
- Màn xác nhận: bảng field trích được (label, giá trị, đơn vị, cờ "chưa đọc được" tô màu cảnh
  báo), cho phép sửa tay từng field, checkbox include, nút "Lưu vào hồ sơ" gọi `confirm`.
  Cảnh báo rõ ràng cho người dùng nếu PDF là dạng scan ảnh (toàn bộ field `extracted=false`).
  Chi tiết API + response schema: xem `InBodyReportsController` (`backend/src/ProDiabHis.Api/
  Controllers/InBodyReportsController.cs`) và DTO trong
  `backend/src/ProDiabHis.Application/InBody/InBodyDtos.cs`.
  Tab lịch sử đo InBody trong hồ sơ bệnh nhân (danh sách + xem lại file gốc qua signed URL).
