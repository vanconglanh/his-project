-- ============================================================
-- Migration: 9201_fix_dispensed_by_uuid
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-09-05
-- Ly do: BUG-F08 (QC flow-trace-20260904) - POST /pharmacy/dispense/{id}
--   luon tra dispensed_by: null, dispensed_by_name: null vi cot
--   diab_his_pha_dispense_records.dispensed_by van la kieu INT legacy
--   (tao boi 0038, tham chieu sec_users.ID kieu int cu) trong khi
--   diab_his_sec_users.id thuc te la UUID CHAR(36) (9001) -> code
--   (DispensingHandlers) khong the ghi ICurrentUser.UserId (Guid) vao
--   cot nay nen phai hard-code 0/NULL, mat hoan toan audit trail nguoi
--   cap phat thuoc (vi pham yeu cau audit du lieu benh nhan - CLAUDE.md).
--   Tuong tu, diab_his_pha_stock_movements.performed_by (0013) cung la
--   INT legacy - sua luon de dong bo audit xuat/nhap kho.
--   (Ke thua pattern tu 9025_fix_dispense_fk_types.sql.)
-- Idempotent: YES (kiem tra DATA_TYPE truoc khi ALTER)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS fix_dispensed_by_uuid;
DELIMITER $$
CREATE PROCEDURE fix_dispensed_by_uuid()
BEGIN
    DECLARE t VARCHAR(100);

    -- ── diab_his_pha_dispense_records.dispensed_by ──────────────────────
    SELECT DATA_TYPE INTO t FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_pha_dispense_records'
       AND COLUMN_NAME = 'dispensed_by';
    IF t = 'int' THEN
        ALTER TABLE `diab_his_pha_dispense_records`
            MODIFY COLUMN `dispensed_by` CHAR(36) NULL
            COMMENT 'FK -> diab_his_sec_users.id (UUID CHAR(36)) - nguoi cap phat';
    END IF;

    -- ── diab_his_pha_stock_movements.performed_by ───────────────────────
    SET t = NULL;
    SELECT DATA_TYPE INTO t FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_pha_stock_movements'
       AND COLUMN_NAME = 'performed_by';
    IF t = 'int' THEN
        ALTER TABLE `diab_his_pha_stock_movements`
            MODIFY COLUMN `performed_by` CHAR(36) NULL
            COMMENT 'FK -> diab_his_sec_users.id (UUID CHAR(36)) - nguoi thuc hien';
    END IF;
END$$
DELIMITER ;

CALL fix_dispensed_by_uuid();
DROP PROCEDURE IF EXISTS fix_dispensed_by_uuid;
