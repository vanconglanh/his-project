-- ============================================================
-- Migration: 9229_bm101_add_waist_to_vital_signs
-- Mục đích: BM-101 - thêm vòng eo (cm) vào sinh hiệu.
-- Idempotent: YES (stored procedure check information_schema)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS add_col_if_missing_9229;
DELIMITER $$
CREATE PROCEDURE add_col_if_missing_9229(IN p_table VARCHAR(100), IN p_col VARCHAR(100), IN p_def VARCHAR(300))
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_table AND COLUMN_NAME = p_col
    ) THEN
        SET @sql = CONCAT('ALTER TABLE `', p_table, '` ADD COLUMN `', p_col, '` ', p_def);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$
DELIMITER ;

CALL add_col_if_missing_9229('diab_his_enc_vital_signs', 'waist_cm', 'DECIMAL(5,1) NULL COMMENT "Vong eo (cm)"');

DROP PROCEDURE IF EXISTS add_col_if_missing_9229;

-- Rollback: ALTER TABLE diab_his_enc_vital_signs DROP COLUMN waist_cm;
