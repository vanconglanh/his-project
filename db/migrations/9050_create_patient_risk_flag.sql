-- ============================================================
-- Migration: 9050_create_patient_risk_flag
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Bang phan tang nguy co benh nhan (risk stratification) cho dashboard/
--   risk-list (kieu TIDE/Glooko). 1 dong / benh nhan, duoc job
--   PatientRiskStratificationJob tinh va UPSERT dinh ky. Khong tinh realtime.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cli_patient_risk_flag (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      INT          NOT NULL,
    patient_id     CHAR(36)     NOT NULL,
    risk_level     VARCHAR(10)  NOT NULL DEFAULT 'LOW' COMMENT 'HIGH|MEDIUM|LOW',
    risk_score     DECIMAL(6,2) NOT NULL DEFAULT 0,
    reasons_json   JSON         NULL COMMENT 'Mang ly do: HbA1c cao, eGFR thap, HA cao, qua han...',
    latest_hba1c   DECIMAL(5,2) NULL,
    latest_egfr    DECIMAL(8,2) NULL,
    latest_bp_sys  INT          NULL,
    latest_bp_dia  INT          NULL,
    hba1c_trend    VARCHAR(12)  NULL COMMENT 'RISING|STABLE|FALLING',
    last_visit_at  DATETIME     NULL,
    last_hba1c_at  DATETIME     NULL,
    computed_at    DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    UNIQUE KEY uk_risk_patient (tenant_id, patient_id),
    INDEX idx_risk_level (tenant_id, risk_level),
    INDEX idx_risk_score (tenant_id, risk_score)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phan tang nguy co benh nhan (job upsert, phuc vu risk-list)';
