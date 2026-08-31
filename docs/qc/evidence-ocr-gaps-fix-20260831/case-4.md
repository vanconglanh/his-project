# CASE 4 — GAP-8 (Rad): Rad OCR Extract trả về source_file_id

## Mô tả
Migration 9188 bổ sung cột `source_file_id` vào `diab_his_rad_results`. Khi upload file lên endpoint OCR extract CĐHA, file gốc được lưu vào MinIO (bảng `fil_files`) với bucket `rad-ocr-sources` và ID được trả về.

## Request

```
POST http://localhost:5099/api/v1/rad-results/ocr-extract
Authorization: Bearer <ktv.test@prodiab.test token>
Content-Type: multipart/form-data

file=rad_test.png (image/png, 1x1 pixel PNG)
```

## Response

```json
HTTP 200
{
  "data": {
    "findings": null,
    "impression": null,
    "conclusion": null,
    "recommendations": null,
    "has_any_extracted": false,
    "raw_text": "",
    "source_file_id": "b6872446-bcf8-41d8-9d28-b74dd967424c"
  }
}
```

## DB Check

```sql
SELECT id, file_name, mime_type, bucket, object_key, created_at 
FROM fil_files WHERE id='b6872446-bcf8-41d8-9d28-b74dd967424c';
```

Kết quả:
```
b6872446-bcf8-41d8-9d28-b74dd967424c | rad_test.png | image/png | rad-ocr-sources | rad-ocr/1/2026/08/31/b6872446-bcf8-41d8-9d28-b74dd967424c.png | 2026-08-31 05:54:20
```

## Result: PASS

- Endpoint hoạt động đúng, trả về HTTP 200
- `source_file_id` được trả về trong response (GAP-8 cho Rad ✓)
- File được lưu vào `fil_files` với bucket `rad-ocr-sources` đúng theo thiết kế
- Tách bucket riêng cho Lab OCR (`lab-ocr-sources`) và Rad OCR (`rad-ocr-sources`) hoạt động đúng
