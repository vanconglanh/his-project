-- ============================================================
-- Migration: 9136_api_sweep_schema_fixes
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-08-29 (API Sweep — fix Loai A errors)
-- Idempotent: YES (dung stored procedure check information_schema)
-- ============================================================
-- Tong hop cac fix schema phat hien qua API Sweep 2026-08-29:
--   1. diab_his_pha_prescription_print_history: thieu cot printer_name
--   2. diab_his_api_request_logs: thieu cot tenant_id, called_at, path
--   3. diab_his_sys_tenants: thieu cot bhyt_token_encrypted, deleted_by
--   4. diab_his_pha_ddi_rules: thieu cot evidence_level
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _9136_add_col;
DELIMITER $$
CREATE PROCEDURE _9136_add_col(
    IN tbl VARCHAR(64),
    IN col VARCHAR(64),
    IN coldef TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = tbl AND COLUMN_NAME = col
    ) THEN
        SET @sql = CONCAT('ALTER TABLE `', tbl, '` ADD COLUMN `', col, '` ', coldef);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$
DELIMITER ;

-- 1. diab_his_pha_prescription_print_history: them printer_name
CALL _9136_add_col('diab_his_pha_prescription_print_history', 'printer_name',
    "VARCHAR(255) NULL COMMENT 'Ten may in / thiet bi in'");

-- 2a. diab_his_api_request_logs: them tenant_id
CALL _9136_add_col('diab_his_api_request_logs', 'tenant_id',
    "INT NULL COMMENT 'ID tenant'");

-- 2b. diab_his_api_request_logs: them called_at
CALL _9136_add_col('diab_his_api_request_logs', 'called_at',
    "DATETIME NULL COMMENT 'Thoi diem goi API (alias request_at)'");

-- 2c. diab_his_api_request_logs: them path
CALL _9136_add_col('diab_his_api_request_logs', 'path',
    "VARCHAR(500) NULL COMMENT 'Duong dan endpoint (alias endpoint)'");

-- 3a. diab_his_sys_tenants: them bhyt_token_encrypted
CALL _9136_add_col('diab_his_sys_tenants', 'bhyt_token_encrypted',
    "TEXT NULL COMMENT 'Token BHYT ma hoa AES-256-GCM'");

-- 3b. diab_his_sys_tenants: them deleted_by
CALL _9136_add_col('diab_his_sys_tenants', 'deleted_by',
    "CHAR(36) NULL COMMENT 'ID nguoi xoa (UUID)'");

-- 4. diab_his_pha_ddi_rules: them evidence_level
CALL _9136_add_col('diab_his_pha_ddi_rules', 'evidence_level',
    "VARCHAR(50) NULL COMMENT 'Muc do bang chung tuong tac (A/B/C/D)'");

DROP PROCEDURE IF EXISTS _9136_add_col;
