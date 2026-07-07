-- ============================================================
-- Migration: 9042_fix_prescription_print_history_id_type
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-07
-- Story refs: Phat hien khi verify header trang cho PDF don thuoc
--   (task redesign letterhead). KHONG lien quan header nhung la bug
--   chan hoan toan endpoint GET /prescriptions/{id}/pdf tu truoc gio:
--   diab_his_pha_prescription_print_history.prescription_id dang la
--   INT trong khi diab_his_pha_prescriptions.id la CHAR(36) (GUID) tu
--   sau dot rework INT->GUID cho module don thuoc/cap phat.
--   INSERT ghi print history luon nem MySqlException "Data truncated
--   for column 'prescription_id'" -> handler fail 500.
-- Idempotent: YES (kiem tra COLUMN_TYPE truoc khi ALTER)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_print_history_pres_id_9042;
DELIMITER $$
CREATE PROCEDURE _fix_print_history_pres_id_9042()
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME   = 'diab_his_pha_prescription_print_history'
           AND COLUMN_NAME  = 'prescription_id'
           AND DATA_TYPE    = 'int'
    ) THEN
        ALTER TABLE diab_his_pha_prescription_print_history
            MODIFY COLUMN prescription_id CHAR(36)
            CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci
            NOT NULL COMMENT 'FK -> diab_his_pha_prescriptions.id (GUID)';
    END IF;
END$$
DELIMITER ;
CALL _fix_print_history_pres_id_9042();
DROP PROCEDURE IF EXISTS _fix_print_history_pres_id_9042;
