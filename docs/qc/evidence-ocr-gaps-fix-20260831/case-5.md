# CASE 5 — GAP-2 (Rad): source_file_id và ocr_raw_text được lưu vào diab_his_rad_results

## Mô tả
Migration 9188 bổ sung 2 cột vào `diab_his_rad_results`:
- `source_file_id` (CHAR 36): FK tới fil_files.id — file gốc phiếu KQ CĐHA
- `ocr_raw_text` (TEXT): text OCR gốc (findings+conclusion) để đối chiếu với nội dung xác nhận

Case này tạo rad result qua OCR confirm (POST /api/v1/rad-results/ocr-confirm) với cả 2 field mới.

## Setup
- Rad order `2f0cf8c2-726c-4f1b-8d19-000f38c4d5b7` (CT, status=ordered)
- Encounter `6f750284-41c8-4625-9d41-587ba0c149a6`
- source_file_id từ CASE 4: `b6872446-bcf8-41d8-9d28-b74dd967424c`

## Request

```
POST http://localhost:5099/api/v1/rad-results/ocr-confirm
Authorization: Bearer <ktv.test@prodiab.test token>
Content-Type: application/json

{
  "rad_order_id": "2f0cf8c2-726c-4f1b-8d19-000f38c4d5b7",
  "findings": "Khong phat hien ton thuong khu tru - QC test GAP-2",
  "impression": "Hinh anh CT nguc binh thuong",
  "conclusion": "Binh thuong - QC GAP-2 test",
  "recommendations": "Tai kham sau 6 thang",
  "performed_at": "2026-08-31T08:00:00Z",
  "source_file_id": "b6872446-bcf8-41d8-9d28-b74dd967424c",
  "ocr_raw_text": "Mo ta: Khong phat hien ton thuong\nKet luan: Binh thuong [OCR goc GAP-2 test 20260831]"
}
```

## Response

```json
HTTP 201
{
  "data": {
    "id": "ab7244b7-a927-4796-a121-639b8d472b32",
    "status": "DRAFT"
  }
}
```

## DB Check

```sql
SELECT id, source_file_id, ocr_raw_text, status 
FROM diab_his_rad_results WHERE id='ab7244b7-a927-4796-a121-639b8d472b32';
```

Kết quả:
```
ab7244b7-a927-4796-a121-639b8d472b32 | b6872446-bcf8-41d8-9d28-b74dd967424c | Mo ta: Khong phat hien ton thuong\nKet luan: Binh thuong [OCR goc GAP-2 test 20260831] | DRAFT
```

## Result: PASS

- `source_file_id` được lưu đúng vào DB (cột migration 9188 ✓)
- `ocr_raw_text` được lưu đúng vào DB (cột migration 9188 ✓)
- HTTP 201 Created đúng theo contract API
- field `ocr_raw_text` lưu text gốc để đối chiếu với `conclusion` người dùng đã xác nhận (GAP-2 cho Rad ✓)
