-- ============================================================
-- Migration: 9024_fix_pha_prescription_items_fk_types
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-02
-- Mo ta: BUG - POST /api/v1/prescriptions/{id}/items (va tao don co
--   items) tra ve 400. Nguyen nhan goc: bang
--   diab_his_pha_prescription_items dang mang schema CU (`prescription_id`
--   INT FK -> pha_prescriptions.ID, `drug_id` INT FK -> pha_drug_master.ID)
--   ke thua tu schema dump goc, trong khi migration 9005_create_pharmacy.sql
--   da dinh nghia dung CHAR(36) nhung khong ap dung duoc vi
--   `CREATE TABLE IF NOT EXISTS` bi no-op (bang da ton tai san).
--   Thuc te: diab_his_pha_prescriptions.id va diab_his_pha_drugs.id deu
--   la CHAR(36) UUID. Voi STRICT_TRANS_TABLES bat, INSERT tu
--   CreatePrescriptionHandler / AddPrescriptionItemsHandler (Dapper, GUID
--   string) se loi "Incorrect integer value" khi ghi vao cot INT.
--   Doi prescription_id, drug_id -> CHAR(36) de khop kieu voi
--   diab_his_pha_prescriptions.id / diab_his_pha_drugs.id.
--   (Ke thua pattern tu 0062_fix_queue_tickets_patient_id_type.sql /
--   9023_fix_rcp_queue_tickets_patient_id_type.sql.)
-- Idempotent: YES (kiem tra DATA_TYPE truoc khi ALTER)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS fix_pha_prescription_items_fk_types;
DELIMITER $$
CREATE PROCEDURE fix_pha_prescription_items_fk_types()
BEGIN
    DECLARE presc_id_type VARCHAR(100);
    DECLARE drug_id_type VARCHAR(100);

    SELECT DATA_TYPE INTO presc_id_type
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME   = 'diab_his_pha_prescription_items'
      AND COLUMN_NAME  = 'prescription_id';

    IF presc_id_type = 'int' THEN
        ALTER TABLE `diab_his_pha_prescription_items`
            MODIFY COLUMN `prescription_id` CHAR(36) NOT NULL
            COMMENT 'FK -> diab_his_pha_prescriptions.id (UUID CHAR(36))';
    END IF;

    SELECT DATA_TYPE INTO drug_id_type
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME   = 'diab_his_pha_prescription_items'
      AND COLUMN_NAME  = 'drug_id';

    IF drug_id_type = 'int' THEN
        ALTER TABLE `diab_his_pha_prescription_items`
            MODIFY COLUMN `drug_id` CHAR(36) NOT NULL
            COMMENT 'FK -> diab_his_pha_drugs.id (UUID CHAR(36))';
    END IF;
END$$
DELIMITER ;

CALL fix_pha_prescription_items_fk_types();
DROP PROCEDURE IF EXISTS fix_pha_prescription_items_fk_types;
