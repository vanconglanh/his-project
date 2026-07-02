# Apply Order — DB Migrations Pro-Diab HIS

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
