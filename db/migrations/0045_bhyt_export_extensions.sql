-- ============================================================
-- Migration: 0045_bhyt_export_extensions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-BH01..BH05
-- Idempotent: YES (dùng helper add_col_if_missing / add_index_if_missing)
-- Ghi chu: Mo rong bang diab_his_int_bhyt_exports (Sprint 9)
--   - Them cac status moi: GENERATED, VALIDATED, SIGNED, PARTIALLY_REJECTED
--   - Them cac cols: encounter_count, totals, timestamps, signed/response cols
--   - Mo rong diab_his_int_bhyt_export_items: row-level storage
-- FIX: MySQL 8 khong ho tro ALTER TABLE ADD COLUMN IF NOT EXISTS -> dung stored proc.
--   Bo menh de AFTER (thu tu cot khong anh huong nghiep vu).
-- ============================================================
SET NAMES utf8mb4;

-- 1. diab_his_int_bhyt_exports: them cac cot
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'scope_filter_json',      "JSON NULL COMMENT 'Filter scope (clinic_id, doctor_id, date_from/to...)'");
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'note',                   "VARCHAR(1000) NULL COMMENT 'Ghi chu ky export'");

--    Doi status ENUM: them GENERATED, VALIDATED, SIGNED, PARTIALLY_REJECTED
ALTER TABLE `diab_his_int_bhyt_exports`
    MODIFY COLUMN `status`
        ENUM('DRAFT','GENERATED','VALIDATED','SIGNED','SUBMITTED','APPROVED','PARTIALLY_REJECTED','REJECTED')
        NOT NULL DEFAULT 'DRAFT'
        COMMENT 'Trang thai ho so BHYT theo QD 4750';

CALL add_col_if_missing('diab_his_int_bhyt_exports', 'encounter_count',        "INT NULL DEFAULT 0 COMMENT 'So luot kham trong ky'");
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'total_requested_amount', "DECIMAL(18,2) NULL DEFAULT 0 COMMENT 'Tong tien yeu cau BHYT'");
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'total_approved_amount',  "DECIMAL(18,2) NULL DEFAULT 0 COMMENT 'Tong tien duoc duyet'");
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'total_rejected_amount',  "DECIMAL(18,2) NULL DEFAULT 0 COMMENT 'Tong tien bi tu choi'");
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'generated_at',           "DATETIME NULL COMMENT 'Thoi diem generate XML thanh cong'");
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'validated_at',           "DATETIME NULL COMMENT 'Thoi diem validate XSD thanh cong'");
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'signed_at',              "DATETIME NULL COMMENT 'Thoi diem ky so thanh cong'");
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'response_at',            "DATETIME NULL COMMENT 'Thoi diem nhan ket qua giam dinh'");
CALL add_col_if_missing('diab_his_int_bhyt_exports', 'bhyt_reference',         "VARCHAR(200) NULL COMMENT 'Ma tham chieu tu cong giam dinh BHYT'");

-- 2. diab_his_int_bhyt_export_items: row-level storage
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'record_index',        "INT NOT NULL DEFAULT 0 COMMENT 'Vi tri dong (index) trong bang N'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'row_data_json',       "JSON NULL COMMENT 'Noi dung dong du lieu BHYT (1 row Bang N)'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'source_encounter_id', "CHAR(36) NULL COMMENT 'FK luot kham goc (UUID)'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'source_billing_id',   "CHAR(36) NULL COMMENT 'FK billing goc (UUID)'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'ma_lien_ket',         "VARCHAR(200) NULL COMMENT 'Ma lien ket noi Bang 1 voi Bang 2/3/4/5'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'request_amount',      "DECIMAL(18,2) NOT NULL DEFAULT 0 COMMENT 'Tien yeu cau BHYT (dong nay)'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'approved_amount',     "DECIMAL(18,2) NULL COMMENT 'Tien duoc duyet (sau doi soat)'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'rejection_code',      "VARCHAR(50) NULL COMMENT 'Ma tu choi (BHYT error code)'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'rejection_reason',    "VARCHAR(500) NULL COMMENT 'Ly do tu choi'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'created_at',          "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Thoi diem tao'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'updated_at',          "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Thoi diem cap nhat'");
CALL add_col_if_missing('diab_his_int_bhyt_export_items', 'tenant_id',           "INT NULL COMMENT 'ID tenant (RLS)'");

-- 3. Index bo sung
CALL add_index_if_missing('diab_his_int_bhyt_export_items', 'idx_bhyt_items_export_table', '(`export_id`, `table_no`)');
CALL add_index_if_missing('diab_his_int_bhyt_export_items', 'idx_bhyt_items_ma_lien_ket',  '(`ma_lien_ket`)');
CALL add_index_if_missing('diab_his_int_bhyt_exports',      'idx_bhyt_export_period_tenant', '(`tenant_id`, `period_month`, `status`)');
