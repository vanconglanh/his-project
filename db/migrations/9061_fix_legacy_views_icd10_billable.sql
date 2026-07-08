-- ============================================================
-- Migration: 9061_fix_legacy_views_icd10_billable
-- Engine: MySQL 8.0+
-- Muc dich: sua cac gap phat hien khi check data toan bo man hinh
--   1. View legacy cli_visits (code Jobs/DiabetesHandlers query cli_visits)
--   2. View legacy cli_lab_outbound / cli_lab_inbound (LabIntegration stats)
--   3. Them cot is_billable cho diab_his_dict_icd10 (Icd10Handlers SELECT is_billable)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- 1. Encounter legacy view
CREATE OR REPLACE VIEW cli_visits AS SELECT * FROM diab_his_enc_encounters;

-- 2. Lab integration legacy views
CREATE OR REPLACE VIEW cli_lab_outbound AS SELECT * FROM diab_his_int_lab_orders_outbound;
CREATE OR REPLACE VIEW cli_lab_inbound  AS SELECT * FROM diab_his_int_lab_results_inbound;
CREATE OR REPLACE VIEW cli_lab_partners AS SELECT * FROM diab_his_int_lab_partners;

-- 2b. Cot status cho QR code thanh toan (entity QrCode.Status can persist PAID/EXPIRED)
DROP PROCEDURE IF EXISTS _fix_qr_status;
DELIMITER $$
CREATE PROCEDURE _fix_qr_status()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'diab_his_bil_qr_codes'
          AND COLUMN_NAME  = 'status'
    ) THEN
        ALTER TABLE diab_his_bil_qr_codes
            ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'PENDING';
    END IF;
END$$
DELIMITER ;
CALL _fix_qr_status();
DROP PROCEDURE IF EXISTS _fix_qr_status;

-- 3. is_billable cho dict_icd10 (bang co the da tao boi 0018 voi schema thieu cot)
DROP PROCEDURE IF EXISTS _fix_icd10_billable;
DELIMITER $$
CREATE PROCEDURE _fix_icd10_billable()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'diab_his_dict_icd10'
          AND COLUMN_NAME  = 'is_billable'
    ) THEN
        ALTER TABLE diab_his_dict_icd10
            ADD COLUMN is_billable TINYINT(1) NOT NULL DEFAULT 1;
    END IF;
END$$
DELIMITER ;
CALL _fix_icd10_billable();
DROP PROCEDURE IF EXISTS _fix_icd10_billable;
