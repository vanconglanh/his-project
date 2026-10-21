-- ============================================================
-- Migration: 9223_bm91_add_customer_source
-- Mục đích: BM-91 (HIS-45) - thêm nguồn khách cho bệnh nhân.
--   customer_source        : WALK_IN | REFERRAL | B2B | MKT
--   customer_source_detail : mã giới thiệu (REFERRAL) / tên công ty (B2B)
-- Idempotent: YES (stored procedure check information_schema)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS add_col_if_missing_9223;
DELIMITER $$
CREATE PROCEDURE add_col_if_missing_9223(IN p_table VARCHAR(100), IN p_col VARCHAR(100), IN p_def VARCHAR(300))
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

CALL add_col_if_missing_9223('pat_patients', 'customer_source', 'VARCHAR(20) NULL COMMENT "Nguon khach: WALK_IN|REFERRAL|B2B|MKT"');
CALL add_col_if_missing_9223('pat_patients', 'customer_source_detail', 'VARCHAR(255) NULL COMMENT "Ma gioi thieu (REFERRAL) hoac ten cong ty (B2B)"');

DROP PROCEDURE IF EXISTS add_col_if_missing_9223;

-- Rollback:
-- ALTER TABLE pat_patients DROP COLUMN customer_source_detail;
-- ALTER TABLE pat_patients DROP COLUMN customer_source;
