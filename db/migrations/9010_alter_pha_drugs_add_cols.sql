-- ============================================================
-- Migration: 9010_alter_pha_drugs_add_cols
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Mo ta: Them cac cot con thieu vao diab_his_pha_drugs de
--        tuong thich voi code Dapper (DrugHandlers)
-- Idempotent: YES (stored procedure kiem tra information_schema)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS add_drug_col;
DELIMITER $$
CREATE PROCEDURE add_drug_col(col_name VARCHAR(64), col_def TEXT)
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'diab_his_pha_drugs'
      AND column_name = col_name
  ) THEN
    SET @sql = CONCAT('ALTER TABLE diab_his_pha_drugs ADD COLUMN `', col_name, '` ', col_def);
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
  END IF;
END$$
DELIMITER ;

CALL add_drug_col('name_vi',               'VARCHAR(255) NULL');
CALL add_drug_col('name_en',               'VARCHAR(255) NULL');
CALL add_drug_col('form',                  'VARCHAR(100) NULL');
CALL add_drug_col('manufacturer',          'VARCHAR(255) NULL');
CALL add_drug_col('country',               'VARCHAR(100) NULL');
CALL add_drug_col('price',                 'DECIMAL(18,2) NULL');
CALL add_drug_col('category_id',           'CHAR(36) NULL');
CALL add_drug_col('requires_prescription', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL add_drug_col('is_psychotropic',       'TINYINT(1) NOT NULL DEFAULT 0');
CALL add_drug_col('is_narcotic',           'TINYINT(1) NOT NULL DEFAULT 0');
CALL add_drug_col('dtqg_drug_code',        'VARCHAR(50) NULL');
CALL add_drug_col('status',                "VARCHAR(20) NOT NULL DEFAULT 'ACTIVE'");

DROP PROCEDURE IF EXISTS add_drug_col;
