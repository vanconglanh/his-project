# Evidence — OCR đọc kết quả chẩn đoán hình ảnh (CĐHA)

Ngày: 2026-08-30. Tính năng: khi bác sĩ/KTV upload file PDF/ảnh phiếu kết quả CĐHA (X-quang/Siêu âm/CT),
hệ thống OCR đọc file + tách 2 đoạn văn bản chính (Mô tả, Kết luận), hiển thị màn xác nhận (2 ô lớn
điền sẵn, sửa tay được) rồi mới lưu `RadResult`.

## Khác biệt với Lab OCR
- Lab OCR: trích **giá trị số** theo tên xét nghiệm đang chờ.
- Rad OCR: trích **văn bản mô tả tự do** — 2 trường: "Mô tả"/Findings và "Kết luận"/Impression.

## Tái dùng hạ tầng (không viết lại engine đọc file)
- PDF: `IPdfTextExtractor` (PdfPig text-layer + fallback render trang → OCR ảnh) — đã có sẵn.
- Ảnh: `IOcrTextProvider` / `TesseractOcrProvider` (Tesseract vie+eng) — đã có sẵn.
- Provider mới `RadOcrTextProvider` chỉ ĐIỀU PHỐI 2 engine trên theo content-type.
- Parser mới `RadResultOcrParser` (thuần, unit-test được) — marker-based, tách 2 đoạn theo nhãn.
- Confirm TÁI DÙNG `CreateRadResultCommand` (đã có payment gate G02, audit).

## 1. Unit test parser (7/7 PASS)
- `backend/tests/ProDiabHis.UnitTests/RadResults/RadResultOcrParserTests.cs` (6 case: X-quang, có dấu
  tiếng Việt giữ nguyên, 2 nhãn cùng dòng, Nhận xét/Hình ảnh→findings, text rỗng, không có nhãn).
- `RadResultOcrPdfIntegrationTests.cs` (1 case: đọc **PDF thật** bằng PdfPig rồi parse).
- Full suite: `dotnet test ProDiabHis.UnitTests` → **946 pass, 0 fail**.

## 2. Đọc PDF thật (file trong thư mục này)
- `phieu-ket-qua-cdha-test.pdf` — phiếu X-quang ngực sinh bằng QuestPDF (Mô tả 3 dòng + Kết luận + Đề nghị + chữ ký BS).
- `raw-text-doc-tu-pdf.txt` — text PdfPig đọc ra (lưu ý: text-layer gộp cả trang thành 1 dòng, không có xuống dòng).
- `ket-qua-parse.txt` — kết quả parser: tách đúng Mô tả / Kết luận / Đề nghị, KHÔNG dính phần chữ ký bác sĩ.

## 3. E2E qua HTTP thật (API local :5099 + MySQL dev)
Login KTV `ktv.test@prodiab.test` (quyền `rad_result.write`, tenant 1). Chỉ định CĐHA có sẵn:
`2f0cf8c2-726c-4f1b-8d19-000f38c4d5b7` (CT, chưa có kết quả, round_id NULL → payment gate cho phép).

### 3a. POST /api/v1/rad-results/ocr-extract — `e2e-2-ocr-extract.json`
Response field names **snake_case** (đã verify đúng cảnh báo camelCase): `findings`, `impression`,
`conclusion`, `recommendations`, `has_any_extracted`, `raw_text`.
- `findings` = "Hai phế trường sáng, không thấy đám mờ bất thường..." (đọc đúng).
- `conclusion` = "Hình ảnh X-quang ngực trong giới hạn bình thường." (đọc đúng).
- `recommendations` = "Tái khám khi có triệu chứng ho kéo dài." (đọc đúng).

### 3b. POST /api/v1/rad-results/ocr-confirm — `e2e-3-body.json` (mô phỏng SỬA TAY), `e2e-3-ocr-confirm.json`
Body snake_case, findings + conclusion đã sửa tay. Response: `{ "data": { "id": "fc4084dc-...", "status": "DRAFT" } }`.

### 3c. Xác nhận DB `diab_his_rad_results` — `e2e-4-db-verify.txt`
```
mo_ta:    Hai phế trường sáng, không thấy đám mờ bất thường (đã sửa tay: bổ sung theo dõi)
ket_luan: Hình ảnh X-quang ngực trong giới hạn bình thường - ĐÃ SỬA TAY.
de_nghi:  Tái khám khi có triệu chứng ho kéo dài.
status:   DRAFT     performed_by: 60e291e1-... (KTV)
```
=> RadResult tạo đúng, **text đã sửa tay** được lưu, giữ nguyên dấu tiếng Việt, status DRAFT (đúng luồng
create — phải người khác VERIFY, SoD).

## Endpoints mới
- `POST /api/v1/rad-results/ocr-extract` — [rad_result.write] — upload file + OCR, KHÔNG ghi DB.
- `POST /api/v1/rad-results/ocr-confirm` — [rad_result.write] — tạo RadResult qua luồng create có sẵn.

## Lưu ý
- Parser dùng chiến lược **marker-based** (không tách theo dòng) vì engine đọc PDF text-layer thường
  trả cả trang trên 1 dòng không có ký tự xuống dòng. Ràng buộc dấu `:` sau nhãn loại được va chạm với
  tiêu đề phiếu ("PHIẾU KẾT QUẢ X-QUANG", "KHOA CHẨN ĐOÁN HÌNH ẢNH").
- Permission: dùng lại `rad_result.write` (không tạo quyền mới).
