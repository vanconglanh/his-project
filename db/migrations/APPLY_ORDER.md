# Apply Order — DB Migrations Pro-Diab HIS

---

## TRẠNG THÁI CHAIN (cập nhật 2026-08-31)

> **DỰNG DB SẠCH TỪ SỐ 0: THÀNH CÔNG — 141/141 migration, 0 lỗi.**
> Verify thật bằng container MySQL 8.0.36 mới hoàn toàn (fresh volume): nạp 64 file base dump
> (đã xử lý bẫy `GTID_PURGED`) + apply toàn bộ 141 file `migrations/*.sql` theo thứ tự tên.
> Bằng chứng: `docs/qc/evidence-full-coverage-fixes-20260831/viec3-migration/`
> (`before/summary.log` = 30 FAIL, `after/` và `final-fresh-volume/summary.log` = **OK=141 FAIL=0**).
> => App KHÔNG cần EF `EnsureCreated()` nữa; chain tự dựng đủ schema.

### Bối cảnh nợ kỹ thuật (4 nhóm nguyên nhân gốc)
Chain có 2 thế hệ: **lớp 00xx** (pre-Clean-Slate, thao tác trên bảng short-name của base dump)
và **lớp 90xx** (canonical). `9000_drop_legacy` XÓA mọi bảng không có prefix `diab_his_`
(trừ `hangfire_*`), rồi `9001-9006` tái tạo schema canonical. Vì vậy phần lớn thao tác 00xx
trên bảng short-name bị hủy ở 9000 — chỉ cần KHÔNG lỗi. 30 file lỗi thuộc 4 nhóm:
1. Cột UPPERCASE base dump vs lowercase migration (vd `RESOURCE_TYPE`/`VISIT_ID` vs `resource`/`encounter_id`).
2. `9000_drop_legacy` xóa bảng còn được lớp 00xx tham chiếu; seed 00xx chạy trước khi bảng canonical tồn tại.
3. VIEW trùng tên BASE TABLE (`CREATE OR REPLACE VIEW` trên tên đã là bảng thật → lỗi 1347).
4. Cú pháp `ADD COLUMN/INDEX IF NOT EXISTS`, `CREATE [UNIQUE] INDEX IF NOT EXISTS`,
   `DROP INDEX IF EXISTS` trong ALTER, và stored proc thiếu `DELIMITER` — không hợp lệ trên MySQL 8.

### Nhóm (a) — đã SỬA (an toàn, ít rủi ro)
**Helper mới trong `0000_helpers.sql`:** `add_unique_index_if_missing`, `create_alias_view_if_no_table`.

| File | Lỗi gốc | Cách sửa |
|---|---|---|
| `0005_vital_signs_multi_record` | index `encounter_id` không tồn tại (base dùng `VISIT_ID`) | Đổi index sang `VISIT_ID` |
| `0018_seed_master_data` | 1136 column-count (hàng DIEUDUONG thiếu 1 giá trị) | Bổ sung `CAN_IMPERSONATE=0`. **Cascade:** tạo được `dict_drug_units`/`dict_icd10` → sửa luôn 9065/9067 |
| `0028_seed_icd10` | (regression sau khi sửa 0018) thiếu cột `is_billable` | `add_col_if_missing(is_billable)` trước INSERT |
| `0029_create_diabetes_history` | cột `default_values` không tồn tại (bảng tạo bởi 0015 khác schema) | `add_col_if_missing` cho default_values/checklist/is_system |
| `0032_lab_rad_results` | TEXT không được default literal | `DEFAULT ('')` (biểu thức) |
| `0035_create_prescription_extensions` | `CREATE INDEX IF NOT EXISTS` | `add_index_if_missing`; thêm `note` (khớp schema 9005) |
| `0036_drug_master_extensions` | `CREATE UNIQUE INDEX IF NOT EXISTS` | `add_unique_index_if_missing` + `add_index_if_missing` |
| `0045_bhyt_export_extensions` | `ADD COLUMN IF NOT EXISTS` | `add_col_if_missing` + `add_index_if_missing` (bỏ AFTER) |
| `0048_create_appointments_extensions` | 1060 duplicate `source_partner_id` | `add_col_if_missing` + `add_index_if_missing` |
| `0056_audit_log_extensions` | `ADD COLUMN/INDEX IF NOT EXISTS` | helper |
| `0059_fhir_extensions` | `ADD COLUMN/INDEX IF NOT EXISTS` gộp | tách thành helper calls |
| `0062_fix_queue_tickets_patient_id_type` | proc thiếu `DELIMITER` + `DROP INDEX IF EXISTS` trong ALTER | thêm `DELIMITER`; proc drop-if-exists + `add_index_if_missing` |
| `9011_create_missing_tables` | 1347 `CREATE OR REPLACE VIEW` trên tên đã là bảng | `create_alias_view_if_no_table` (guard) |
| `9014_fix_dtqg_apipartners_schema` | inline proc thiếu `DELIMITER` | dùng `add_col_if_missing` |
| `9020_seed_rich_demo` | `user_id` chưa có (thêm ở 9030); `appointment_date/time` không tồn tại | `add_col_if_missing(user_id)`; đổi sang `appointment_at` (gộp ngày+giờ) |

### Nhóm (a-superseded) — VÔ HIỆU HÓA có chủ đích (no-op, đã VERIFY thay thế đầy đủ)
Các file seed thế hệ cũ, target sai bảng/cột và ĐÃ được lớp 90xx seed đầy đủ vào bảng canonical
`diab_his_sec_permissions`. Verify trên DB sạch: **180 permissions + 296 role-permission mappings**,
bao gồm mọi mã quyền các file này định seed (đã kiểm `billing.print`, `cashier.print_receipt`,
`dtqg.submit`, `patient.read`, `prescription.create`, ...). Nội dung cũ được thay bằng `SELECT 'no-op'`.

- Permission seeds: `0021`, `0024`, `0030`, `0034`, `0039`, `0044`, `0047`, `0052`, `0054`, `0057`, `0060`, `0066`
  (nguồn thay thế: `9066_seed_all_gated_permissions`, `9054/9063/9064_seed_*_permissions`).
- `0058_perf_indexes`: index trên bảng short-name legacy (cột không tồn tại) → thay bởi `9016`/`9021` trên bảng canonical.
- `0063_seed_pharmacy_stock`: demo tồn kho vào bảng short-name (bị 9000 xóa) → demo đã seed canonical ở `9020_seed_rich_demo`.

### Nhóm (b) — CÒN LẠI, KHÔNG sửa liều (không phải lỗi chain; là khoảng trống chức năng)
Chain chạy 0 lỗi. Không còn file nào lỗi. Duy nhất 1 điểm cần lưu ý về CHỨC NĂNG (không gây lỗi apply):

- **`fhir_id` trên bảng canonical:** `0059_fhir_extensions` chỉ thêm `fhir_id` cho các bảng short-name
  (`pat_patients`, `cli_visits`, `cli_lab_results`, `pha_prescriptions`) — vốn bị `9000_drop_legacy` xóa,
  nên các bảng canonical `diab_his_*` tương ứng KHÔNG có `fhir_id`. Migration vẫn hợp lệ (0 lỗi).
  **Chưa sửa vì:** cần xác định chắc bảng/khóa canonical cho từng resource FHIR và có thể cần thêm cột
  vào bảng production — thuộc phạm vi thiết kế FHIR mapper, rủi ro nếu đoán. Đề xuất: tạo file `90xx_fhir_ids`
  chạy SAU `9002/9003/9004/9005` để thêm `fhir_id` vào bảng canonical khi triển khai FHIR R4.

---

## Prerequisites

- MySQL 8.0+ với charset mặc định `utf8mb4`, collation `utf8mb4_0900_ai_ci`
- Database `diab_his` đã tồn tại và dump production đã được import
- User có quyền: `CREATE`, `ALTER`, `INSERT`, `SELECT` trên database

Kiểm tra version:
```sql
SELECT VERSION();
SHOW VARIABLES LIKE 'character_set_database';
SHOW VARIABLES LIKE 'collation_database';
```

---

## Apply tất cả migrations theo thứ tự

```bash
# Chạy từ root của project
for f in db/migrations/*.sql; do
  echo "Applying: $f"
  mysql -u root -p diab_his < "$f" || { echo "FAILED: $f"; break; }
  echo "OK: $f"
done
```

Windows PowerShell:
```powershell
Get-ChildItem "db\migrations\*.sql" | Sort-Object Name | ForEach-Object {
    Write-Host "Applying: $($_.Name)"
    mysql -u root -p diab_his < $_.FullName
    if ($LASTEXITCODE -ne 0) {
        Write-Error "FAILED: $($_.Name)"
        break
    }
    Write-Host "OK: $($_.Name)"
}
```

---

## Apply từng file riêng lẻ

```bash
mysql -u root -p diab_his < db/migrations/0000_helpers.sql
mysql -u root -p diab_his < db/migrations/0001_create_tenants.sql
mysql -u root -p diab_his < db/migrations/0002_add_tenant_id_columns.sql
mysql -u root -p diab_his < db/migrations/0003_add_audit_columns.sql
mysql -u root -p diab_his < db/migrations/0004_add_patient_extensions.sql
mysql -u root -p diab_his < db/migrations/0005_vital_signs_multi_record.sql
mysql -u root -p diab_his < db/migrations/0006_create_cls_uploads.sql
mysql -u root -p diab_his < db/migrations/0007_create_external_lab_integration.sql
mysql -u root -p diab_his < db/migrations/0008_create_api_partners.sql
mysql -u root -p diab_his < db/migrations/0009_create_push_notifications.sql
mysql -u root -p diab_his < db/migrations/0010_seed_nurse_role.sql
mysql -u root -p diab_his < db/migrations/0011_create_dtqg.sql
mysql -u root -p diab_his < db/migrations/0012_create_bhyt_export.sql
mysql -u root -p diab_his < db/migrations/0013_pharmacy_lot_expiry.sql
mysql -u root -p diab_his < db/migrations/0014_payment_qr_card.sql
mysql -u root -p diab_his < db/migrations/0015_emr_diabetes_template.sql
mysql -u root -p diab_his < db/migrations/0016_create_appointments.sql
mysql -u root -p diab_his < db/migrations/0017_create_patient_portal.sql
mysql -u root -p diab_his < db/migrations/0018_seed_master_data.sql
mysql -u root -p diab_his < db/migrations/0019_create_indexes.sql
```

**Quan trọng:** Luôn apply `0000_helpers.sql` trước tiên — các file tiếp theo phụ thuộc vào stored proc `add_col_if_missing` và `add_index_if_missing`.

---

## Smoke Tests

### 1. Kiểm tra bảng mới đã được tạo
```sql
SHOW TABLES LIKE 'diab_his_%';
```

Kết quả mong đợi (14 bảng mới):
```
diab_his_sys_tenants
diab_his_fil_cls_uploads
diab_his_int_lab_partners
diab_his_int_lab_orders_outbound
diab_his_int_lab_results_inbound
diab_his_api_partners
diab_his_api_partner_scopes
diab_his_api_request_logs
diab_his_nti_notifications
diab_his_nti_user_preferences
diab_his_nti_web_push_subscriptions
diab_his_int_dtqg_credentials
diab_his_int_dtqg_submissions
diab_his_int_bhyt_exports
diab_his_int_bhyt_export_items
diab_his_pha_stock_movements
diab_his_bil_qr_codes
diab_his_cli_diabetes_assessments
diab_his_cli_diabetes_templates
diab_his_sch_appointments
diab_his_pat_portal_accounts
diab_his_pat_portal_tokens
diab_his_dict_drug_units
diab_his_dict_icd10
diab_his_dict_doc_types
```

### 2. Kiểm tra cột tenant_id đã được thêm
```sql
SELECT TABLE_NAME, COLUMN_NAME
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'diab_his'
  AND COLUMN_NAME  = 'tenant_id'
ORDER BY TABLE_NAME;
```

### 3. Kiểm tra stored procedures
```sql
SHOW PROCEDURE STATUS WHERE Db = 'diab_his';
-- Mong đợi: add_col_if_missing, add_index_if_missing
```

### 4. Kiểm tra seed data
```sql
-- Roles
SELECT CODE, NAME FROM sec_roles WHERE CODE IN ('ADMIN','BACSI','LETAN','DUOCSI','KETOAN','KYTHUATVIEN','DIEUDUONG');

-- ICD-10 ĐTĐ
SELECT code, name_vi FROM diab_his_dict_icd10 WHERE code LIKE 'E1%' ORDER BY code;

-- Đơn vị thuốc
SELECT code, name FROM diab_his_dict_drug_units ORDER BY code;
```

### 5. Kiểm tra cột audit đã được thêm
```sql
SELECT TABLE_NAME, COLUMN_NAME
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'diab_his'
  AND COLUMN_NAME IN ('deleted_at', 'updated_by')
ORDER BY TABLE_NAME, COLUMN_NAME;
```

---

## Rollback

Không có auto-rollback. Để rollback thủ công:

```sql
-- Xóa bảng mới (nếu cần rollback 0001-0019)
DROP TABLE IF EXISTS diab_his_sys_tenants;
DROP TABLE IF EXISTS diab_his_fil_cls_uploads;
DROP TABLE IF EXISTS diab_his_int_lab_partners;
DROP TABLE IF EXISTS diab_his_int_lab_orders_outbound;
DROP TABLE IF EXISTS diab_his_int_lab_results_inbound;
DROP TABLE IF EXISTS diab_his_api_partners;
DROP TABLE IF EXISTS diab_his_api_partner_scopes;
DROP TABLE IF EXISTS diab_his_api_request_logs;
DROP TABLE IF EXISTS diab_his_nti_notifications;
DROP TABLE IF EXISTS diab_his_nti_user_preferences;
DROP TABLE IF EXISTS diab_his_nti_web_push_subscriptions;
DROP TABLE IF EXISTS diab_his_int_dtqg_credentials;
DROP TABLE IF EXISTS diab_his_int_dtqg_submissions;
DROP TABLE IF EXISTS diab_his_int_bhyt_exports;
DROP TABLE IF EXISTS diab_his_int_bhyt_export_items;
DROP TABLE IF EXISTS diab_his_pha_stock_movements;
DROP TABLE IF EXISTS diab_his_bil_qr_codes;
DROP TABLE IF EXISTS diab_his_cli_diabetes_assessments;
DROP TABLE IF EXISTS diab_his_cli_diabetes_templates;
DROP TABLE IF EXISTS diab_his_sch_appointments;
DROP TABLE IF EXISTS diab_his_pat_portal_accounts;
DROP TABLE IF EXISTS diab_his_pat_portal_tokens;
DROP TABLE IF EXISTS diab_his_dict_drug_units;
DROP TABLE IF EXISTS diab_his_dict_icd10;
DROP TABLE IF EXISTS diab_his_dict_doc_types;

-- Xóa stored procedures
DROP PROCEDURE IF EXISTS add_col_if_missing;
DROP PROCEDURE IF EXISTS add_index_if_missing;
```

Các cột đã ADD vào bảng cũ (tenant_id, updated_by, deleted_at, v.v.) cần DROP thủ công theo từng bảng nếu muốn rollback hoàn toàn.
