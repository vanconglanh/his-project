# CASE 3 — GAP-2 (Lab): source_file_id và ocr_raw_value được lưu vào diab_his_lab_results

## Mô tả
Migration 9188 bổ sung 2 cột vào `diab_his_lab_results`:
- `source_file_id` (CHAR 36): FK tới fil_files.id — file gốc phiếu KQ XN
- `ocr_raw_value` (VARCHAR 255): giá trị OCR đọc được gốc để đối chiếu với giá trị xác nhận

Case này tạo lab result trực tiếp qua POST /api/v1/lab-results với cả 2 field mới để kiểm tra lưu xuống DB đúng.

## Setup
- Lab order `c568da4f-a57a-47b7-892a-d2dec5c0eab7` (GLU_F) thuộc CLS round đã thanh toán (payment_status=PAID)
- Encounter `1c49b2c1-3f08-40a8-abbe-f3034553b0bd`

## Request

```
POST http://localhost:5099/api/v1/lab-results
Authorization: Bearer <ktv.test@prodiab.test token>
Content-Type: application/json

{
  "lab_order_item_id": "c568da4f-a57a-47b7-892a-d2dec5c0eab7",
  "value": "5.9",
  "value_numeric": 5.9,
  "unit": "mmol/L",
  "method": null,
  "performed_at": "2026-08-31T08:00:00Z",
  "note": "QC test GAP-2 ocr_raw_value",
  "source_file_id": "09b66dda-f87b-47f1-a4cd-a55c99c73ed7",
  "ocr_raw_value": "5.9 mmol/L [OCR goc, QC GAP-2 test 20260831]"
}
```

## Response

```json
HTTP 201
{
  "data": {
    "id": "0ae090ee-bbd8-42de-bac5-9861653bf5f0",
    "lab_order_id": "c568da4f-a57a-47b7-892a-d2dec5c0eab7",
    "test_code": "GLU_F",
    "test_name": "Đường huyết đói (Fasting Glucose)",
    "value": "5.9",
    "value_numeric": 5.9,
    "unit": "mmol/L",
    "reference_range_low": 3.9,
    "reference_range_high": 5.5,
    "flag": "H",
    "status": "DRAFT",
    ...
  }
}
```

## DB Check

```sql
SELECT id, source_file_id, ocr_raw_value, flag, status 
FROM diab_his_lab_results WHERE id='0ae090ee-bbd8-42de-bac5-9861653bf5f0';
```

Kết quả:
```
0ae090ee-bbd8-42de-bac5-9861653bf5f0 | 09b66dda-f87b-47f1-a4cd-a55c99c73ed7 | 5.9 mmol/L [OCR goc, QC GAP-2 test 20260831] | H | DRAFT
```

## Result: PASS

- `source_file_id` được lưu đúng vào DB (cột migration 9188 ✓)
- `ocr_raw_value` được lưu đúng vào DB (cột migration 9188 ✓)
- `flag = H` vì 5.9 > reference_range_high=5.5 (migration 9190 reference ranges hoạt động ✓)
- HTTP 201 Created đúng theo contract API
