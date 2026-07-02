-- Migration 0059: FHIR extensions — them cot fhir_id vao cac bang chinh
-- Sprint 13 — FHIR R4 mapper + track FHIR resource id

-- Benh nhan
ALTER TABLE pat_patients
    ADD COLUMN IF NOT EXISTS fhir_id CHAR(36) NULL COMMENT 'FHIR Patient resource id',
    ADD INDEX IF NOT EXISTS idx_pat_patients_fhir_id (fhir_id);

-- Luot kham
ALTER TABLE cli_visits
    ADD COLUMN IF NOT EXISTS fhir_id CHAR(36) NULL COMMENT 'FHIR Encounter resource id',
    ADD INDEX IF NOT EXISTS idx_cli_visits_fhir_id (fhir_id);

-- Ket qua xet nghiem
ALTER TABLE cli_lab_results
    ADD COLUMN IF NOT EXISTS fhir_id CHAR(36) NULL COMMENT 'FHIR Observation/DiagnosticReport resource id',
    ADD INDEX IF NOT EXISTS idx_cli_lab_results_fhir_id (fhir_id);

-- Don thuoc
ALTER TABLE pha_prescriptions
    ADD COLUMN IF NOT EXISTS fhir_id CHAR(36) NULL COMMENT 'FHIR MedicationRequest bundle id',
    ADD INDEX IF NOT EXISTS idx_pha_prescriptions_fhir_id (fhir_id);
