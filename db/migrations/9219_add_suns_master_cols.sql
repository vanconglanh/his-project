-- ============================================================
-- Migration: 9219_add_suns_master_cols
-- Mục đích: Thêm cột title, license_number, suns_code cho users
--           và suns_code cho rooms — phục vụ nạp master SUNS.
-- Idempotent: YES (stored procedure check information_schema)
-- Tác giả: migration-agent (2026-10-16)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _add_col_9219;
DELIMITER $$
CREATE PROCEDURE _add_col_9219(IN p_table VARCHAR(100), IN p_col VARCHAR(100), IN p_def VARCHAR(300))
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

-- Nhân sự: chức danh (TS BS, ThS BS, BSCKI, BSCKII, BS...)
CALL _add_col_9219('diab_his_sec_users', 'title', 'VARCHAR(50) NULL COMMENT "Chức danh học hàm: BS/ThS/TS/BSCKI/BSCKII..."');
-- Số chứng chỉ hành nghề (CCHN)
CALL _add_col_9219('diab_his_sec_users', 'license_number', 'VARCHAR(100) NULL COMMENT "Số chứng chỉ hành nghề y"');
-- Mã nhân viên SUNS để truy vết nguồn gốc
CALL _add_col_9219('diab_his_sec_users', 'suns_code', 'VARCHAR(20) NULL COMMENT "Mã nhân sự hệ thống SUNS nguồn gốc"');

-- Phòng khám: mã SUNS để truy vết
CALL _add_col_9219('diab_his_sys_rooms', 'suns_code', 'VARCHAR(20) NULL COMMENT "Mã phòng hệ thống SUNS nguồn gốc"');

DROP PROCEDURE IF EXISTS _add_col_9219;

-- Rollback:
-- ALTER TABLE diab_his_sec_users DROP COLUMN IF EXISTS title;
-- ALTER TABLE diab_his_sec_users DROP COLUMN IF EXISTS license_number;
-- ALTER TABLE diab_his_sec_users DROP COLUMN IF EXISTS suns_code;
-- ALTER TABLE diab_his_sys_rooms DROP COLUMN IF EXISTS suns_code;
