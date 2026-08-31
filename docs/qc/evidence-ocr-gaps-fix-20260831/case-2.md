# CASE 2 — GAP-8: Lab OCR Extract trả về source_file_id

## Mô tả
Migration 9188 bổ sung cột `source_file_id` vào `diab_his_lab_results`. Khi upload file lên endpoint OCR extract, file gốc được lưu vào MinIO (bảng `fil_files`) và ID được trả về để FE giữ và gửi lại ở bước confirm.

## Request

```
POST http://localhost:5099/api/v1/lab-results/ocr-extract
Authorization: Bearer <ktv.test@prodiab.test token>
Content-Type: multipart/form-data

file=lab_test.png (image/png, 1x1 pixel PNG)
encounter_id=7e3d4eec-f54d-4640-8aac-3616debc15f5
```

## Response

```json
HTTP 200
{
  "data": {
    "encounter_id": "7e3d4eec-f54d-4640-8aac-3616debc15f5",
    "pending_count": 1,
    "extracted_count": 0,
    "fields": [
      {
        "lab_order_item_id": "5a49bb1d-f2c6-40de-b3ea-6ef7afbe73fc",
        "test_code": "GLU_F",
        "test_name": "Đường huyết đói (Fasting Glucose)",
        "value": null,
        "value_numeric": null,
        "unit": null,
        "extracted": false,
        "out_of_plausible_range": false,
        "plausible_range_note": null
      }
    ],
    "source_file_id": "1a5b1a06-b3aa-4022-8ddb-aa3a408c16df"
  }
}
```

## DB Check

```sql
SELECT id, file_name, mime_type, bucket, object_key, created_at 
FROM fil_files WHERE id='1a5b1a06-b3aa-4022-8ddb-aa3a408c16df';
```

Kết quả:
```
1a5b1a06-b3aa-4022-8ddb-aa3a408c16df | lab_test.png | image/png | lab-ocr-sources | lab-ocr/1/2026/08/31/1a5b1a06-b3aa-4022-8ddb-aa3a408c16df.png | 2026-08-31 05:57:02
```

## Result: PASS

- Endpoint hoạt động đúng, trả về HTTP 200
- `source_file_id` được trả về trong response (GAP-8 ✓)
- File đã được lưu vào `fil_files` với bucket `lab-ocr-sources` đúng theo thiết kế
- File được map với encounter, trả về danh sách pending lab order items
