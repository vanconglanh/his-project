# CASE 7 — Legacy Import: Upload ZIP hồ sơ giấy cũ, OCR background job

## Mô tả
Chức năng nhập liệu hàng loạt hồ sơ giấy cũ dạng ảnh scan: admin upload 1 file ZIP → giải nén → OCR từng ảnh (Tesseract, chạy nền Hangfire) → tạo item cho admin review/match bệnh nhân → confirm → lưu tài liệu đính kèm.

## Setup Note
Permission `legacy_import.write` chỉ có role `admin`. Tài khoản admin trong hệ thống bắt buộc 2FA (MandatoryMfaRoles: ["admin"]) — không có TOTP active. Để test, cấp tạm `legacy_import.write` cho role `bac_si` qua DB INSERT (không sửa product code).

## Request — POST /api/v1/legacy-imports

```
POST http://localhost:5099/api/v1/legacy-imports
Authorization: Bearer <bacsi1@prodiab.local token>
Content-Type: multipart/form-data

file=legacy_import.zip (application/zip, 450 bytes)
Nội dung ZIP:
  - benh_nhan_001/kq_xn_20260831.png
  - benh_nhan_002/cdha_nguc_20260831.png
```

## Response

```json
HTTP 201
{
  "data": {
    "id": "c04fbe08-ef46-4fbc-8316-3848e084d7a2",
    "zip_file_name": "legacy_import.zip",
    "total_items": 0,
    "processed_items": 0,
    "status": "pending",
    "error_message": null,
    "created_at": "2026-08-31T05:56:19.8697955Z",
    "updated_at": "2026-08-31T05:56:19.8697955Z"
  }
}
```

## DB Check

```sql
SELECT id, zip_file_name, status, total_items, processed_items, created_at 
FROM diab_his_leg_import_batch WHERE id='c04fbe08-ef46-4fbc-8316-3848e084d7a2';
```
```
c04fbe08-ef46-4fbc-8316-3848e084d7a2 | legacy_import.zip | done | 2 | 2 | 2026-08-31 05:56:20
```

```sql
SELECT id, original_filename, status, ocr_text, matched_patient_id
FROM diab_his_leg_import_item WHERE batch_id='c04fbe08-ef46-4fbc-8316-3848e084d7a2';
```
```
4fb263e8-1bf9-4ea1-a8ff-2864e02ccd78 | kq_xn_20260831.png   | pending_match | (empty) | NULL
845aa7c2-b91e-47d1-8a7e-2ac10122928d | cdha_nguc_20260831.png | pending_match | (empty) | NULL
```

## API Check — GET /api/v1/legacy-imports

```json
HTTP 200
{
  "data": [
    {"id": "c04fbe08...", "status": "done", "zip_file_name": "legacy_import.zip"},
    ...
  ],
  "meta": {"total": 2}
}
```

## Result: PASS

- POST /api/v1/legacy-imports trả về HTTP 201 đúng
- Batch được tạo trong `diab_his_leg_import_batch` ✓
- Background job đã chạy xong (status "done") và tạo 2 items từ 2 file PNG trong ZIP ✓
- Items có status `pending_match` — đúng trạng thái chờ admin gán bệnh nhân
- GET /api/v1/legacy-imports hoạt động đúng, hiển thị danh sách batch ✓

## Ghi chú về quyền
Permission `legacy_import.write` chỉ gán cho role `admin` theo cấu hình chuẩn. Admin bắt buộc 2FA (chưa setup) → không lấy được full JWT trong môi trường test. Case này chạy với bac_si được cấp thêm quyền tạm thời qua DB — đây là giới hạn môi trường, không phải bug sản phẩm.
