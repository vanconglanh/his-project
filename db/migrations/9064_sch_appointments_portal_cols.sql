-- ============================================================
-- Migration: 9064_sch_appointments_portal_cols
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-08
-- Story refs: Patient Portal — Phase 0 (dat lich qua app benh nhan)
-- Mo ta:
--   Code portal (PortalHandlers.Create/Get/CancelPortalAppointment) va
--   PublicApiHandlers.BookAppointment SELECT/INSERT cac cot appointment_code,
--   partner_reference, source_partner_id (UUID) tren bang diab_his_sch_appointments,
--   nhung schema goc (0016) chi co: patient_id INT, doctor_id INT, source_partner_id INT.
--   Mig 9038 da them patient_ref/doctor_ref CHAR(36). Bo sung not cac cot con thieu
--   de code portal chay dung tren bang that (khong doi kieu patient_id/doctor_id INT cu).
-- Idempotent: YES (add_col_if_missing)
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_sch_appointments', 'appointment_code',  "VARCHAR(30) NULL COMMENT 'Ma lich hen hien thi (LHyyyyMMdd...)'");
CALL add_col_if_missing('diab_his_sch_appointments', 'partner_reference', "VARCHAR(100) NULL COMMENT 'Ma tham chieu ben doi tac (khi source=API)'");
CALL add_col_if_missing('diab_his_sch_appointments', 'uuid',              "CHAR(36) NULL COMMENT 'UUID cong khai cho lich hen (dong bo lien he thong)'");

-- source_partner_id da co kieu INT o 0016 (FK api_partners.id INT). Code portal doc
-- BIN_TO_UUID(source_partner_id) -> them cot ref rieng CHAR(36) neu can tham chieu GUID.
CALL add_col_if_missing('diab_his_sch_appointments', 'source_partner_ref', "CHAR(36) NULL COMMENT 'UUID doi tac nguon (khi source=API, thay cho source_partner_id INT)'");

CALL add_index_if_missing('diab_his_sch_appointments', 'idx_sch_appt_code', '(tenant_id, appointment_code)');
CALL add_index_if_missing('diab_his_sch_appointments', 'ux_sch_appt_uuid',  '(uuid)');
