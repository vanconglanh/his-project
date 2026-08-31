-- ============================================================================
-- 9188_lab_rad_ocr_source_and_raw.sql
--
-- GAP-8: luu file goc OCR (Lab/Rad) len MinIO + tham chieu qua fil_files.
--   - diab_his_lab_results.source_file_id : id dong fil_files chua file goc phieu KQ XN.
--   - diab_his_rad_results.source_file_id : id dong fil_files chua file goc phieu KQ CDHA.
--
-- GAP-2: luu ban OCR goc de doi chieu voi gia tri nguoi dung xac nhan/sua.
--   - diab_his_lab_results.ocr_raw_value  : gia tri OCR doc duoc goc cho 1 chi so XN.
--   - diab_his_rad_results.ocr_raw_text   : text OCR goc (findings+conclusion) cua phieu CDHA.
--
-- Idempotent: dung stored procedure check information_schema (MySQL 8.0.23 khong ho tro
-- ADD COLUMN IF NOT EXISTS).
-- ============================================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS add_col_if_missing_9188;

DELIMITER //
CREATE PROCEDURE add_col_if_missing_9188(IN tbl VARCHAR(64), IN col VARCHAR(64), IN col_def TEXT)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = tbl AND COLUMN_NAME = col
    ) THEN
        SET @sql = CONCAT('ALTER TABLE `', tbl, '` ADD COLUMN `', col, '` ', col_def);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END //
DELIMITER ;

-- Lab results
CALL add_col_if_missing_9188('diab_his_lab_results', 'source_file_id', 'CHAR(36) NULL COMMENT ''fil_files.id - file goc phieu KQ XN (OCR)''');
CALL add_col_if_missing_9188('diab_his_lab_results', 'ocr_raw_value',  'VARCHAR(255) NULL COMMENT ''gia tri OCR doc duoc goc (GAP-2 - doi chieu voi gia tri confirm)''');

-- Rad results
CALL add_col_if_missing_9188('diab_his_rad_results', 'source_file_id', 'CHAR(36) NULL COMMENT ''fil_files.id - file goc phieu KQ CDHA (OCR)''');
CALL add_col_if_missing_9188('diab_his_rad_results', 'ocr_raw_text',   'TEXT NULL COMMENT ''text OCR goc phieu CDHA (GAP-2 - doi chieu voi noi dung confirm)''');

DROP PROCEDURE IF EXISTS add_col_if_missing_9188;
