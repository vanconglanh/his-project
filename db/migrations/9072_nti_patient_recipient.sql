-- ============================================================
-- Migration: 9065_nti_patient_recipient
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-08
-- Story refs: Patient Portal — Phase 0 (thong bao cho benh nhan)
-- Mo ta:
--   He thong thong bao hien chi phuc vu USER noi bo (bang diab_his_nti_notifications,
--   diab_his_nti_web_push_subs deu khoa theo user_id). Portal can gui thong bao +
--   luu web-push subscription theo PATIENT. Them cot recipient_type + patient_id
--   CHAR(36) de tai dung ha tang notification san co thay vi tao he bang moi.
--   (Bang nti_* co nhieu dinh nghia chong nhau 0009/0031/0050/9006b/9030 -> chi dung
--    add_col_if_missing, khong CREATE/DROP.)
-- Idempotent: YES (add_col_if_missing)
-- ============================================================
SET NAMES utf8mb4;

-- Bang thong bao: phan biet nguoi nhan la USER (noi bo) hay PATIENT (portal)
CALL add_col_if_missing('diab_his_nti_notifications', 'recipient_type', "VARCHAR(10) NOT NULL DEFAULT 'USER' COMMENT 'USER | PATIENT'");
CALL add_col_if_missing('diab_his_nti_notifications', 'patient_id',     "CHAR(36) NULL COMMENT 'FK -> diab_his_pat_patients.id khi recipient_type=PATIENT'");
CALL add_index_if_missing('diab_his_nti_notifications', 'idx_nti_patient', '(tenant_id, patient_id, created_at)');

-- Web-push subscription: cho phep gan theo patient (portal) canh user (noi bo)
CALL add_col_if_missing('diab_his_nti_web_push_subs', 'patient_id', "CHAR(36) NULL COMMENT 'FK -> diab_his_pat_patients.id (subscription cua benh nhan portal)'");
CALL add_index_if_missing('diab_his_nti_web_push_subs', 'idx_push_patient', '(tenant_id, patient_id)');
