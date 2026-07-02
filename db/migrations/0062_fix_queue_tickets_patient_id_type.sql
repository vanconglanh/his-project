-- ============================================================
-- Migration: 0062_fix_queue_tickets_patient_id_type
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-24
-- Story refs: BUG-01 fix — patient_id phải là CHAR(36) UUID
--   để khớp với pat_patients.id CHAR(36) (thêm qua CreatePatient)
-- Idempotent: YES (kiểm tra kiểu cột trước khi ALTER)
-- ============================================================
SET NAMES utf8mb4;

-- Đổi patient_id từ INT sang CHAR(36) để khớp pat_patients.id UUID
DROP PROCEDURE IF EXISTS fix_patient_id_type;
CREATE PROCEDURE fix_patient_id_type()
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
            COMMENT 'FK → pat_patients.id (UUID CHAR(36))';
    END IF;
END;
CALL fix_patient_id_type();
DROP PROCEDURE IF EXISTS fix_patient_id_type;

-- Cũng đảm bảo his_rooms và diab_his_rcp_queue_tickets dùng cùng collation
-- để tránh "Illegal mix of collations" khi JOIN
ALTER TABLE `his_rooms`
    CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

ALTER TABLE `diab_his_rcp_queue_tickets`
    CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- Cập nhật index sau khi đổi type (idempotent)
ALTER TABLE `diab_his_rcp_queue_tickets`
    DROP INDEX IF EXISTS `idx_rcp_ticket_patient`,
    ADD INDEX `idx_rcp_ticket_patient` (`tenant_id`, `patient_id`);
