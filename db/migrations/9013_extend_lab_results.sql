-- Migration: 9013_extend_lab_results
-- Mo ta: Them cac cot con thieu vao diab_his_lab_results de phu hop EF Core entity
-- Idempotent: YES (stored procedure check IF NOT EXISTS)
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS add_lab_col;

DELIMITER //
CREATE PROCEDURE add_lab_col(IN tbl VARCHAR(64), IN col VARCHAR(64), IN col_def TEXT)
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

CALL add_lab_col('diab_his_lab_results', 'lab_order_id',     'CHAR(36) NULL');
CALL add_lab_col('diab_his_lab_results', 'lab_order_item_id','CHAR(36) NULL');
CALL add_lab_col('diab_his_lab_results', 'patient_id',       'CHAR(36) NULL');
CALL add_lab_col('diab_his_lab_results', 'encounter_id',     'CHAR(36) NULL');
CALL add_lab_col('diab_his_lab_results', 'value',            'VARCHAR(500) NULL');
CALL add_lab_col('diab_his_lab_results', 'value_numeric',    'DECIMAL(12,4) NULL');
CALL add_lab_col('diab_his_lab_results', 'unit',             'VARCHAR(50) NULL');
CALL add_lab_col('diab_his_lab_results', 'reference_range_low',  'DECIMAL(12,4) NULL');
CALL add_lab_col('diab_his_lab_results', 'reference_range_high', 'DECIMAL(12,4) NULL');
CALL add_lab_col('diab_his_lab_results', 'flag',             'VARCHAR(20) NULL DEFAULT ''NORMAL''');
CALL add_lab_col('diab_his_lab_results', 'method',           'VARCHAR(100) NULL');
CALL add_lab_col('diab_his_lab_results', 'status',           'VARCHAR(20) NULL DEFAULT ''PRELIMINARY''');
CALL add_lab_col('diab_his_lab_results', 'verified_at',      'DATETIME NULL');
CALL add_lab_col('diab_his_lab_results', 'verified_by',      'VARCHAR(36) NULL');
CALL add_lab_col('diab_his_lab_results', 'source',           'VARCHAR(20) NULL DEFAULT ''MANUAL''');
CALL add_lab_col('diab_his_lab_results', 'updated_by',       'CHAR(36) NULL');
CALL add_lab_col('diab_his_lab_results', 'deleted_by',       'CHAR(36) NULL');

-- Dong bo lab_order_id tu order_id neu chua co
UPDATE diab_his_lab_results SET lab_order_id = order_id WHERE lab_order_id IS NULL AND order_id IS NOT NULL;

DROP PROCEDURE IF EXISTS add_lab_col;
