-- ============================================================
-- Migration: 0025_create_encounter_extensions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-E01..E15
-- Idempotent: YES (add_col_if_missing + CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;

-- ADD cols vao cli_visits neu thieu
CALL add_col_if_missing('cli_visits', 'encounter_type',   "VARCHAR(30) NOT NULL DEFAULT 'FIRST_VISIT' COMMENT 'FIRST_VISIT|FOLLOW_UP|EMERGENCY|CONSULTATION'");
CALL add_col_if_missing('cli_visits', 'status',           "VARCHAR(20) NOT NULL DEFAULT 'WAITING' COMMENT 'WAITING|IN_PROGRESS|DONE|CANCELLED'");
CALL add_col_if_missing('cli_visits', 'started_at',       'DATETIME NULL');
CALL add_col_if_missing('cli_visits', 'finished_at',      'DATETIME NULL');
CALL add_col_if_missing('cli_visits', 'chief_complaint',  'TEXT NULL');
CALL add_col_if_missing('cli_visits', 'reason_for_visit', 'VARCHAR(1000) NULL');
CALL add_col_if_missing('cli_visits', 'alert_sent_at',    'DATETIME NULL');
CALL add_col_if_missing('cli_visits', 'room_id',          'CHAR(36) NULL');
CALL add_col_if_missing('cli_visits', 'doctor_id',        'CHAR(36) NULL');

-- Indexes
DROP PROCEDURE IF EXISTS _add_idx_25;
DELIMITER $$
CREATE PROCEDURE _add_idx_25()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'cli_visits'
          AND INDEX_NAME   = 'idx_encounter_tenant_status_started'
    ) THEN
        ALTER TABLE cli_visits
            ADD INDEX idx_encounter_tenant_status_started (tenant_id, status, started_at);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'cli_visits'
          AND INDEX_NAME   = 'idx_encounter_patient_created'
    ) THEN
        ALTER TABLE cli_visits
            ADD INDEX idx_encounter_patient_created (tenant_id, patient_id, created_at DESC);
    END IF;
END$$
DELIMITER ;
CALL _add_idx_25();
DROP PROCEDURE IF EXISTS _add_idx_25;

-- ---------------------------------------------------------------
-- Bảng chẩn đoán ICD-10 theo encounter
-- ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS diab_his_cli_encounter_diagnoses (
    id              CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id       INT          NOT NULL,
    encounter_id    CHAR(36)     NOT NULL,
    icd10_code      VARCHAR(10)  NOT NULL,
    name            VARCHAR(500) NOT NULL DEFAULT '',
    type            ENUM('PRIMARY','SECONDARY') NOT NULL DEFAULT 'PRIMARY',
    note            TEXT         NULL,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by      CHAR(36)     NULL,
    updated_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    updated_by      CHAR(36)     NULL,
    deleted_at      DATETIME     NULL,
    PRIMARY KEY (id),
    INDEX idx_diag_encounter (encounter_id),
    INDEX idx_diag_tenant    (tenant_id, encounter_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='ICD-10 diagnoses per encounter (Sprint 3-4)';
