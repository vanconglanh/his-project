# Apply Order — DB Migrations Pro-Diab HIS

## ⚠️ TRẠNG THÁI THẬT (kiểm chứng 2026-08-20 trên MySQL 8.0 sạch, Docker)

**Dựng DB mới từ số 0 HIỆN CHƯA chạy sạch được.** Đã build MySQL 8 tạm (Docker), nạp 64 file
`db/diab_his_*.sql` (base dump, 0 lỗi, 64 bảng), rồi apply tuần tự 150 file `db/migrations/*.sql`
theo thứ tự tên file — kết quả: **30/150 file lỗi SQL thật** (không phải suy đoán, đã chạy thật
và log lại nguyên văn lỗi). Danh sách 30 file kèm nguyên nhân: xem `README.md` mục "Trạng thái
migration chain".

Nguyên nhân gốc chính (đã xác nhận bằng cách so cột/bảng thật trong MySQL, không đoán):

1. **Bảng base dump dùng cột UPPERCASE kiểu cũ** (`ID`, `PATIENT_ID`, `PRESCRIBING_PROVIDER_ID`,
   `PRESCRIPTION_DATE`, `PRESCRIPTION_STATUS`...) nhưng nhiều migration dải 0018-0067 lại
   `ALTER`/`INSERT`/`INDEX` vào cột **lowercase kiểu mới** (`doctor_id`, `prescribed_at`,
   `status`, `resource`, `module`, `is_active`...) chưa từng tồn tại trên bảng base dump thật.
   Ví dụ xác nhận: `pha_prescriptions` base dump KHÔNG có cột `doctor_id`/`prescribed_at`
   (0035, 0058 lỗi vì lý do này).
2. **`9000_drop_legacy.sql` DROP toàn bộ bảng legacy không-prefix** (bao gồm cả những bảng mà
   migration 0018-0066 seed permission còn tham chiếu tới bảng MỚI `diab_his_sec_*` — bảng này
   chỉ được `CREATE` ở `9001_create_sec_all.sql`, tức là SAU 0018-0066 theo thứ tự tên file).
   Vì `add_col_if_missing`/`INSERT` không kiểm tra bảng có tồn tại trước khi ALTER/INSERT, các
   migration 0018/0021/0024/0030/0034/0039/0044/0047/0052/0054/0057/0060/0066 lỗi
   `Table 'diab_his_sec_permissions' doesn't exist` / cột không tồn tại vì chạy TRƯỚC khi bảng
   đích được tạo ở dải 9001+.
3. `9011_create_missing_tables.sql` tạo `VIEW diab_his_int_bhyt_exports` nhưng bảng cùng tên đã
   tồn tại (không phải VIEW) → lỗi 1347 "is not VIEW".
4. Một số file dùng cú pháp MySQL không hợp lệ: `CREATE INDEX IF NOT EXISTS` /
   `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` (MySQL 8.0 không hỗ trợ) — đã sửa cú pháp ở
   `0058_perf_indexes.sql` sang stored procedure `add_index_if_missing`, nhưng file vẫn lỗi vì
   lý do (1) ở trên (cột đích không tồn tại) — cần backend/architect xác nhận tên cột đúng
   trước khi sửa tiếp, KHÔNG tự đoán thêm cột mới vào bảng production.

**Các file 0035, 0036, 0045, 0056, 0059 cũng dùng cú pháp `ADD COLUMN/INDEX IF NOT EXISTS`
không hợp lệ VÀ tham chiếu cột chưa rõ nguồn gốc — CHƯA sửa vì rủi ro đoán sai tên cột trên
bảng production đang có dữ liệu thật. Cần agent backend/architect xác nhận trước.**

### ⚠️ Bẫy khi nạp base schema — GTID_PURGED

File `db/diab_his_*.sql` (dump mysqldump gốc) có dòng:
```sql
SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '0cde9779-...';
```
Dòng này gây lỗi **1840 / 3546** khi nạp tuần tự nhiều file dump vào cùng 1 server MySQL 8
(GTID_PURGED chỉ set được khi GTID set rỗng — lần nạp file thứ 2 trở đi sẽ lỗi). **Phải loại
bỏ dòng này trước khi nạp**, ví dụ:
```bash
sed '/SET @@GLOBAL.GTID_PURGED/d' db/diab_his_xxx.sql > /tmp/f.sql
mysql -u root -p diab_his < /tmp/f.sql
```
README trước đây không đề cập bẫy này — đã bổ sung.

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
