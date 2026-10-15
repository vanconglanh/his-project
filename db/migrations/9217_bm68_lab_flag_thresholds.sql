-- =============================================================================
-- 9217_bm68_lab_flag_thresholds.sql
-- BM-68: Nguong co (flag) HbA1c cau hinh duoc.
-- Can cu: Chuan ADA 2024: HbA1c <5.7% binh thuong; 5.7-<10% Cao (kiem soat kem);
-- >=10% Nguy kich (kiem soat rat kem). Nguong nguy kich mac dinh 10%, phong kham
-- co the cau hinh per-test qua cot flag_critical_low / flag_critical_high.
-- Khi 2 cot = NULL -> backend giu logic ti le cu (backward compat).
-- Idempotent (MySQL 8.0.23 khong ho tro ADD COLUMN IF NOT EXISTS).
-- =============================================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS add_col_if_missing_9217;
DELIMITER $$
CREATE PROCEDURE add_col_if_missing_9217()
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'diab_his_dict_lab_tests'
      AND column_name = 'flag_critical_low'
  ) THEN
    ALTER TABLE `diab_his_dict_lab_tests`
      ADD COLUMN `flag_critical_low` DECIMAL(18,4) NULL
      COMMENT 'Nguong nguy kich duoi (value <= nguong -> CRITICAL), NULL = dung logic ti le';
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'diab_his_dict_lab_tests'
      AND column_name = 'flag_critical_high'
  ) THEN
    ALTER TABLE `diab_his_dict_lab_tests`
      ADD COLUMN `flag_critical_high` DECIMAL(18,4) NULL
      COMMENT 'Nguong nguy kich tren (value >= nguong -> CRITICAL), NULL = dung logic ti le';
  END IF;
END$$
DELIMITER ;

CALL add_col_if_missing_9217();
DROP PROCEDURE IF EXISTS add_col_if_missing_9217;

-- Seed HbA1c: nguy kich >= 10.0% (ADA 2024). Chi ghi khi chua cau hinh (khong de len gia tri phong kham da chinh).
UPDATE diab_his_dict_lab_tests
   SET flag_critical_high = 10.0
 WHERE code = 'HBA1C' AND flag_critical_high IS NULL;
