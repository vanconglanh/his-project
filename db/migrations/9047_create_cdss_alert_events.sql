-- ============================================================
-- Migration: 9047_create_cdss_alert_events
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Log MOI lan CDSS ban canh bao (ke ca non-interruptive) de phan tich
--   alert fatigue va ty le override. Moi dong = 1 alert cu the trong 1 lan check.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cdss_alert_events (
    id              CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id       INT          NOT NULL,
    patient_id      CHAR(36)     NULL,
    encounter_id    CHAR(36)     NULL,
    prescription_id CHAR(36)     NULL,
    rule_type       VARCHAR(24)  NOT NULL COMMENT 'DRUG_DRUG|DRUG_ALLERGY|DUPLICATE_INGREDIENT|DRUG_LAB|CRITICAL_LAB',
    rule_code       VARCHAR(60)  NULL COMMENT 'code rule hoac cap hoat chat',
    severity        VARCHAR(16)  NOT NULL,
    is_interruptive TINYINT(1)   NOT NULL DEFAULT 0,
    title           VARCHAR(255) NULL,
    detail          TEXT         NULL,
    payload_json    JSON         NULL COMMENT 'Thuoc/hoat chat/lab lien quan',
    context         VARCHAR(20)  NOT NULL DEFAULT 'CHECK' COMMENT 'CHECK (realtime) | SIGN (chot ky don)',
    fired_at        DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    fired_by        CHAR(36)     NULL,
    PRIMARY KEY (id),
    INDEX idx_cdss_alert_tenant_time (tenant_id, fired_at),
    INDEX idx_cdss_alert_presc (prescription_id),
    INDEX idx_cdss_alert_rule (rule_type, severity)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='CDSS: log moi lan alert ban (do alert fatigue)';
