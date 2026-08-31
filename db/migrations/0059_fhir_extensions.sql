-- Migration 0059: FHIR extensions — them cot fhir_id vao cac bang chinh
-- Sprint 13 — FHIR R4 mapper + track FHIR resource id
-- FIX (2026-08-31): MySQL 8 khong ho tro ADD COLUMN/INDEX IF NOT EXISTS -> dung helper.
-- LUU Y: cac bang short-name duoi day thuoc lop legacy va se bi 9000_drop_legacy xoa;
--   fhir_id o day chi ap cho schema legacy. (Xem APPLY_ORDER.md muc "Con lai / nhom B"
--   ve khoang trong fhir_id tren bang canonical diab_his_*.)
SET NAMES utf8mb4;

-- Benh nhan
CALL add_col_if_missing('pat_patients', 'fhir_id', "CHAR(36) NULL COMMENT 'FHIR Patient resource id'");
CALL add_index_if_missing('pat_patients', 'idx_pat_patients_fhir_id', '(fhir_id)');

-- Luot kham
CALL add_col_if_missing('cli_visits', 'fhir_id', "CHAR(36) NULL COMMENT 'FHIR Encounter resource id'");
CALL add_index_if_missing('cli_visits', 'idx_cli_visits_fhir_id', '(fhir_id)');

-- Ket qua xet nghiem
CALL add_col_if_missing('cli_lab_results', 'fhir_id', "CHAR(36) NULL COMMENT 'FHIR Observation/DiagnosticReport resource id'");
CALL add_index_if_missing('cli_lab_results', 'idx_cli_lab_results_fhir_id', '(fhir_id)');

-- Don thuoc
CALL add_col_if_missing('pha_prescriptions', 'fhir_id', "CHAR(36) NULL COMMENT 'FHIR MedicationRequest bundle id'");
CALL add_index_if_missing('pha_prescriptions', 'idx_pha_prescriptions_fhir_id', '(fhir_id)');
