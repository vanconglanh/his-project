-- ============================================================
-- Migration: 9034_pat_add_patient_source
-- Mo ta: Them cot patient_source (nguon khach / kenh den cua benh nhan)
--   vao diab_his_pat_patients de phuc vu cot "Nguon khach" trong bao cao
--   Doanh Thu Ngay va thong ke nguon benh nhan (theo chuan HIS thi truong).
--   Gia tri goi y (application-layer enum, luu dang string):
--     WALK_IN   = Vang lai
--     REFERRAL  = Gioi thieu (BN / bac si / doi tac)
--     RETURN    = Tai kham
--     ONLINE    = Dat kham online / website
--     INSURANCE = BHYT / bao lanh
--     MARKETING = Chuong trinh marketing / quang cao
--     OTHER     = Khac
-- Idempotent: YES (chi them khi cot chua ton tai)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _add_patient_source_9034;
DELIMITER $$
CREATE PROCEDURE _add_patient_source_9034()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME   = 'diab_his_pat_patients'
           AND COLUMN_NAME  = 'patient_source'
    ) THEN
        ALTER TABLE diab_his_pat_patients
            ADD COLUMN patient_source VARCHAR(50)
            CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
            DEFAULT NULL
            COMMENT 'Nguon khach / kenh den: WALK_IN|REFERRAL|RETURN|ONLINE|INSURANCE|MARKETING|OTHER';

        -- Index phuc vu group-by/filter theo nguon trong bao cao (multi-tenant)
        ALTER TABLE diab_his_pat_patients
            ADD INDEX idx_patients_source (tenant_id, patient_source);
    END IF;
END$$
DELIMITER ;
CALL _add_patient_source_9034();
DROP PROCEDURE IF EXISTS _add_patient_source_9034;
