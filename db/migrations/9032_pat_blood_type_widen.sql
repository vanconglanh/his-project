-- ============================================================
-- Migration: 9032_pat_blood_type_widen
-- Mo ta: Cot diab_his_pat_patients.blood_type dang VARCHAR(5) nhung enum FE
--   co gia tri 'AB_POS'/'AB_NEG'(6), 'UNKNOWN'(7) > 5 ky tu -> tao benh nhan
--   voi nhom mau AB gay "Data too long for column 'blood_type'" HTTP 500.
--   Noi cot len VARCHAR(10) de chua du moi gia tri enum.
-- Idempotent: YES (chi doi khi do dai < 10)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _widen_blood_type_9032;
DELIMITER $$
CREATE PROCEDURE _widen_blood_type_9032()
BEGIN
    DECLARE cur_len INT DEFAULT 0;
    SELECT CHARACTER_MAXIMUM_LENGTH INTO cur_len FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pat_patients' AND COLUMN_NAME = 'blood_type';
    IF cur_len IS NOT NULL AND cur_len < 10 THEN
        ALTER TABLE diab_his_pat_patients MODIFY COLUMN blood_type VARCHAR(10) NULL;
    END IF;
END$$
DELIMITER ;
CALL _widen_blood_type_9032();
DROP PROCEDURE IF EXISTS _widen_blood_type_9032;
