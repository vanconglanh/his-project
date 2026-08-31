# CASE 1 — GAP-1: InBody Report Soft-Delete với deleted_by + delete_reason

## Mô tả
Migration 9189 bổ sung 2 cột `deleted_by` (CHAR(36)) và `delete_reason` (VARCHAR(500)) vào bảng `diab_his_cli_inbody_report`. Case này kiểm tra endpoint DELETE /api/v1/inbody-reports/{id} lưu đúng 2 cột mới.

## Request

```
DELETE http://localhost:5099/api/v1/inbody-reports/078c6039-e0b7-4913-bf4c-8c037f50cf63
Authorization: Bearer <bacsi1@prodiab.local token>
Content-Type: application/json

{"reason":"Nhap nham ho so benh nhan khac - QC test GAP-1"}
```

## Response

```json
HTTP 200
{"data":{"deleted":true}}
```

## DB Check

```sql
SELECT id, deleted_at IS NOT NULL as is_deleted, deleted_by, delete_reason
FROM diab_his_cli_inbody_report
WHERE id='078c6039-e0b7-4913-bf4c-8c037f50cf63';
```

Kết quả:
```
078c6039-e0b7-4913-bf4c-8c037f50cf63 | 1 (TRUE) | a0000000-0000-0000-0000-000000000002 | Nhap nham ho so benh nhan khac - QC test GAP-1
```

### Kiểm tra thêm - Delete không có reason (optional field)

```
DELETE http://localhost:5099/api/v1/inbody-reports/124da92d-fd79-4df8-bb69-6b46a95702aa
Body: {}
→ HTTP 200 {"data":{"deleted":true}}
DB: deleted_by = a0000000-0000-0000-0000-000000000002, delete_reason = NULL ✓
```

## Result: PASS

- Cột `deleted_at` được set (soft-delete hoạt động)
- Cột `deleted_by` được ghi đúng user ID của người thực hiện (migration 9189 ✓)
- Cột `delete_reason` được ghi đúng lý do truyền vào (migration 9189 ✓)
- Delete không kèm reason → delete_reason = NULL (field optional ✓)
