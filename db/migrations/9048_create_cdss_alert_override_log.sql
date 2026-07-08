-- ============================================================
-- Migration: 9048_create_cdss_alert_override_log
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Ghi nhan khi bac si VUOT QUA (override) canh bao interruptive de van
--   ky don. Bat buoc co ly do (override_reason). Dung de audit + tinh chinh rule.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cdss_alert_override_log (
    id              CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id       INT          NOT NULL,
    alert_event_id  CHAR(36)     NULL COMMENT 'Tro toi diab_his_cdss_alert_events (neu co)',
    prescription_id CHAR(36)     NULL,
    encounter_id    CHAR(36)     NULL,
    rule_type       VARCHAR(24)  NOT NULL,
    rule_code       VARCHAR(60)  NULL,
    severity        VARCHAR(16)  NOT NULL,
    override_reason TEXT         NOT NULL COMMENT 'Ly do bac si van ke don (bat buoc)',
    reason_code     VARCHAR(40)  NULL COMMENT 'Ma ly do chuan hoa (neu chon tu dropdown)',
    overridden_by   CHAR(36)     NULL,
    signed_at       DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    PRIMARY KEY (id),
    INDEX idx_cdss_override_presc (prescription_id),
    INDEX idx_cdss_override_tenant_time (tenant_id, signed_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='CDSS: log override canh bao interruptive (bat buoc ly do)';
