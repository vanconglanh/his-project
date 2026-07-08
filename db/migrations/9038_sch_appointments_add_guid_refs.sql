-- ============================================================
-- Migration: 9038_sch_appointments_add_guid_refs
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Bug tien nhiem #4: diab_his_sch_appointments.patient_id / doctor_id la INT
--   (kieu legacy), khong tuong thich voi diab_his_pat_patients.id /
--   diab_his_sec_users.id (CHAR(36) GUID) -> khong JOIN duoc de lay ten
--   that cho "giay hen tai kham" (chi co cot du phong patient_name_temp).
--
-- QUYET DINH: KHONG doi kieu patient_id/doctor_id hien co sang CHAR(36).
--   Ly do: 2 luong khac (PublicApiHandlers.BookAppointmentHandler,
--   PortalHandlers Portal-booking) DA gia dinh patient_id/doctor_id la
--   BINARY(16) UUID (dung UUID_TO_BIN/BIN_TO_UUID) va tham chieu them cac
--   cot/bang khong ton tai trong schema hien tai (appointment_code,
--   service_id, partner_reference, bang "his_patient", bang "sec_users"
--   dung nhu VIEW) -> 2 luong nay DA hong tu truoc, doc lap voi bug #4,
--   ngoai pham vi 5 bug duoc giao. Doi thang kieu patient_id/doctor_id co
--   the va cham them vao 2 luong nay theo huong khong luong truoc duoc.
--   -> Chon phuong an AN TOAN NHAT: them 2 cot GUID moi rieng biet
--   (patient_ref, doctor_ref) chi phuc vu muc tieu "giay hen hien thi ten
--   BN/BS that", khong dung/xoa cot patient_id/doctor_id INT cu.
--
-- Du lieu hien tai: bang chi co 1 dong seed, patient_id/doctor_id da NULL
--   san va ten trong patient_name_temp ("Nguyen Van An") KHONG khop chinh
--   xac voi ban ghi benh nhan nao trong diab_his_pat_patients (chi trung
--   ten voi 1 BAC SI cung ten) -> KHONG the tu dong map, giu NULL (trung
--   thuc, khong doan mo).
--
-- Idempotent: YES (add_col_if_missing tu 0000_helpers.sql)
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_sch_appointments', 'patient_ref', 'CHAR(36) NULL AFTER patient_id');
CALL add_col_if_missing('diab_his_sch_appointments', 'doctor_ref',  'CHAR(36) NULL AFTER doctor_id');

CALL add_index_if_missing('diab_his_sch_appointments', 'idx_sch_appt_patient_ref', '(patient_ref)');
CALL add_index_if_missing('diab_his_sch_appointments', 'idx_sch_appt_doctor_ref',  '(doctor_ref)');
