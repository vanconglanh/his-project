-- ============================================================
-- Migration: 9135_add_diabetes_template_cols
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-08-29 (API Sweep fix)
-- Lý do: diab_his_cli_diabetes_templates thiếu cột is_system, default_values, checklist
--   khiến GET /api/v1/diabetes-templates trả 500 (Unknown column 'is_system' in order clause)
-- Idempotent: YES (dùng stored procedure check information_schema)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _9135_add_col;
CREATE PROCEDURE _9135_add_col(
    IN tbl VARCHAR(64),
    IN col VARCHAR(64),
    IN coldef TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = tbl AND COLUMN_NAME = col
    ) THEN
        SET @sql = CONCAT('ALTER TABLE `', tbl, '` ADD COLUMN `', col, '` ', coldef);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END;

-- is_system: phân biệt template hệ thống (mặc định) vs template của tenant
CALL _9135_add_col('diab_his_cli_diabetes_templates', 'is_system',
    'TINYINT(1) NOT NULL DEFAULT 0 COMMENT ''Template hệ thống (1=hệ thống, 0=tenant tạo)''');

-- default_values: JSON chứa giá trị mặc định của template
CALL _9135_add_col('diab_his_cli_diabetes_templates', 'default_values',
    'JSON NULL COMMENT ''Giá trị mặc định của template (JSON)''');

-- checklist: JSON array danh sách checkbox
CALL _9135_add_col('diab_his_cli_diabetes_templates', 'checklist',
    'JSON NULL COMMENT ''Danh sách checklist của template (JSON array string)''');

DROP PROCEDURE IF EXISTS _9135_add_col;
