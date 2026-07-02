-- ============================================================
-- Migration: 9023_fix_rcp_queue_tickets_patient_id_type
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-02
-- Mo ta: diab_his_rcp_queue_tickets.patient_id dang la INT (theo
--   0022_create_reception_queue.sql) nhung pat_patients.id (bang
--   diab_his_pat_patients) la CHAR(36) UUID. Voi STRICT_TRANS_TABLES
--   bat, INSERT tu ReceptionController.CheckIn se loi
--   "Incorrect integer value" hoac ket qua sai khi so sanh WHERE.
--   Doi patient_id -> CHAR(36) de khop kieu voi pat_patients.id.
--   (Ke thua y tuong tu 0062_fix_queue_tickets_patient_id_type.sql,
--   nhung file do thao tac them ca bang `his_rooms` khong ton tai
--   tren moi truong nay nen khong ap dung duoc phan sua patient_id.)
-- Idempotent: YES (kiem tra DATA_TYPE truoc khi ALTER)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS fix_rcp_queue_patient_id_type;
DELIMITER $$
CREATE PROCEDURE fix_rcp_queue_patient_id_type()
BEGIN
    DECLARE col_type VARCHAR(100);
    SELECT DATA_TYPE INTO col_type
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME   = 'diab_his_rcp_queue_tickets'
      AND COLUMN_NAME  = 'patient_id';

    IF col_type = 'int' THEN
        ALTER TABLE `diab_his_rcp_queue_tickets`
            MODIFY COLUMN `patient_id` CHAR(36) NOT NULL
            COMMENT 'FK -> pat_patients.id (UUID CHAR(36))';
    END IF;
END$$
DELIMITER ;

CALL fix_rcp_queue_patient_id_type();
DROP PROCEDURE IF EXISTS fix_rcp_queue_patient_id_type;
