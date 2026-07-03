-- ============================================================
-- Migration: 9025_fix_dispense_fk_types
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-03
-- Mo ta: BUG #6 (bo mo phong phong kham phat hien) - luong Cap phat /
--   Thu tien khong chay. Nguyen nhan goc: module Dispensing + Stock
--   movement duoc xay tren schema legacy INT (pha_prescriptions.ID,
--   pha_warehouses.ID, pha_drug_master.ID, pha_stocks.ID deu INT) trong
--   khi DB thuc te da chuyen sang schema moi CHAR(36) UUID
--   (diab_his_pha_prescriptions.id, diab_his_pha_drugs.id,
--   diab_his_pha_stock.id) va warehouse tra ve id chuoi "default".
--   Cac bang duoi day duoc tao boi migration cu 0013 / 0038 (chay TRUOC
--   9005/9011) nen 'CREATE TABLE IF NOT EXISTS' o cac migration moi bi
--   no-op -> cot van giu kieu INT, khong khop voi GUID/chuoi ma code (Dapper)
--   ghi vao. Voi STRICT_TRANS_TABLES, INSERT tu DispenseHandler /
--   FefoStrategy loi "Incorrect integer value".
--   Doi cac cot khoa ngoai sang CHAR(36) (UUID) / VARCHAR(36) (warehouse
--   id chuoi "default") de khop kieu.
--   (Ke thua pattern tu 9024_fix_pha_prescription_items_fk_types.sql.)
--   Khong co FK constraint tren cac cot nay (chi index) -> MODIFY an toan.
-- Idempotent: YES (kiem tra DATA_TYPE truoc khi ALTER)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS fix_dispense_fk_types;
DELIMITER $$
CREATE PROCEDURE fix_dispense_fk_types()
BEGIN
    DECLARE t VARCHAR(100);

    -- ── diab_his_pha_dispense_records ──────────────────────────────────
    SELECT DATA_TYPE INTO t FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_pha_dispense_records'
       AND COLUMN_NAME = 'prescription_id';
    IF t = 'int' THEN
        ALTER TABLE `diab_his_pha_dispense_records`
            MODIFY COLUMN `prescription_id` CHAR(36) NOT NULL
            COMMENT 'FK -> diab_his_pha_prescriptions.id (UUID CHAR(36))';
    END IF;

    SET t = NULL;
    SELECT DATA_TYPE INTO t FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_pha_dispense_records'
       AND COLUMN_NAME = 'warehouse_id';
    IF t = 'int' THEN
        ALTER TABLE `diab_his_pha_dispense_records`
            MODIFY COLUMN `warehouse_id` VARCHAR(36) NOT NULL
            COMMENT 'Ma kho phat (chuoi, vd "default")';
    END IF;

    -- ── diab_his_pha_dispense_items ────────────────────────────────────
    SET t = NULL;
    SELECT DATA_TYPE INTO t FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_pha_dispense_items'
       AND COLUMN_NAME = 'drug_id';
    IF t = 'int' THEN
        ALTER TABLE `diab_his_pha_dispense_items`
            MODIFY COLUMN `drug_id` CHAR(36) NOT NULL
            COMMENT 'FK -> diab_his_pha_drugs.id (UUID CHAR(36))';
    END IF;

    -- ── diab_his_pha_stock_movements ───────────────────────────────────
    SET t = NULL;
    SELECT DATA_TYPE INTO t FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_pha_stock_movements'
       AND COLUMN_NAME = 'stock_id';
    IF t = 'int' THEN
        ALTER TABLE `diab_his_pha_stock_movements`
            MODIFY COLUMN `stock_id` CHAR(36) NOT NULL
            COMMENT 'FK -> diab_his_pha_stock.id (UUID CHAR(36))';
    END IF;

    SET t = NULL;
    SELECT DATA_TYPE INTO t FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_pha_stock_movements'
       AND COLUMN_NAME = 'warehouse_id';
    IF t = 'int' THEN
        ALTER TABLE `diab_his_pha_stock_movements`
            MODIFY COLUMN `warehouse_id` VARCHAR(36) NOT NULL
            COMMENT 'Ma kho (chuoi, vd "default")';
    END IF;

    SET t = NULL;
    SELECT DATA_TYPE INTO t FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_pha_stock_movements'
       AND COLUMN_NAME = 'reference_id';
    IF t = 'int' THEN
        ALTER TABLE `diab_his_pha_stock_movements`
            MODIFY COLUMN `reference_id` VARCHAR(36) NULL
            COMMENT 'ID chung tu nguon (UUID GUID theo reference_type)';
    END IF;
END$$
DELIMITER ;

CALL fix_dispense_fk_types();
DROP PROCEDURE IF EXISTS fix_dispense_fk_types;
