-- ============================================================
-- Migration: 9029_pha_drugs_name_nullable
-- Mo ta: diab_his_pha_drugs con cot legacy `name` NOT NULL khong default;
--   CreateDrugHandler INSERT dung `name_vi` (khong set `name`) ->
--   "Field 'name' doesn't have a default value" 500. App dung name_vi,
--   nen cho `name` nullable de INSERT thanh cong.
-- Idempotent: YES (chi doi khi name dang NOT NULL)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_drug_name_9029;
DELIMITER $$
CREATE PROCEDURE _fix_drug_name_9029()
BEGIN
    DECLARE nn VARCHAR(3);
    SELECT IS_NULLABLE INTO nn FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pha_drugs' AND COLUMN_NAME = 'name';
    IF nn = 'NO' THEN
        ALTER TABLE `diab_his_pha_drugs` MODIFY COLUMN `name` VARCHAR(255) NULL DEFAULT NULL;
    END IF;
END$$
DELIMITER ;
CALL _fix_drug_name_9029();
DROP PROCEDURE IF EXISTS _fix_drug_name_9029;
