-- ============================================================
-- Migration: 9051_create_followup_recall
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Danh sach recall/nhac tai kham chu dong theo nguy co lam sang (khac voi
--   nhac lich hen thong thuong). ChronicCareRecallJob quet cohort benh man tinh
--   (E10-E14) qua han tai kham/qua han HbA1c theo phac do QD 5481 -> tao recall.
--   Le tan xu ly qua RecallController (goi dien/nhac, doi status).
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS). Job dam bao khong tao trung theo
--   (patient_id, recall_type) khi con PENDING.
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cli_followup_recall (
    id            CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id     INT          NOT NULL,
    patient_id    CHAR(36)     NOT NULL,
    recall_type   VARCHAR(24)  NOT NULL COMMENT 'OVERDUE_VISIT|OVERDUE_HBA1C|RISK_ESCALATION',
    due_date      DATE         NULL,
    reason_json   JSON         NULL,
    priority      VARCHAR(10)  NOT NULL DEFAULT 'NORMAL' COMMENT 'HIGH|NORMAL',
    status        VARCHAR(12)  NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING|CONTACTED|SCHEDULED|DONE|DISMISSED',
    channel       VARCHAR(12)  NULL COMMENT 'SMS|WEBPUSH|PHONE|ZALO',
    note          TEXT         NULL,
    contacted_at  DATETIME(3)  NULL,
    contacted_by  CHAR(36)     NULL,
    created_at    DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at    DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    deleted_at    DATETIME(3)  NULL,
    PRIMARY KEY (id),
    INDEX idx_recall_worklist (tenant_id, status, due_date),
    INDEX idx_recall_patient (tenant_id, patient_id, recall_type, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Recall/nhac tai kham chu dong theo nguy co lam sang';
