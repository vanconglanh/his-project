-- ============================================================
-- Migration: 9053_create_ai_suggestion_log
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Log toan bo goi y dieu tri AI (guideline-driven) phuc vu guardrail SaMD:
--   luu context grounding, khuyen nghi suy tu guideline (rule_derived), output LLM,
--   phien ban disclaimer, va trang thai bac si xu ly (human-in-the-loop).
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cli_ai_suggestion_log (
    id                 CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id          INT          NOT NULL,
    patient_id         CHAR(36)     NOT NULL,
    encounter_id       CHAR(36)     NULL,
    context_json       JSON         NULL COMMENT 'Du lieu grounding (chi so, target, chan doan)',
    rule_derived_json  JSON         NULL COMMENT 'Khuyen nghi suy tu guideline (nguon su that)',
    prompt_hash        VARCHAR(64)  NULL,
    model              VARCHAR(60)  NULL,
    llm_output_text    TEXT         NULL COMMENT 'Dien giai tieng Viet cua LLM (neu co)',
    fallback_used      TINYINT(1)   NOT NULL DEFAULT 0 COMMENT '1 = LLM loi, hien rule_derived text thuan',
    disclaimer_version VARCHAR(20)  NOT NULL DEFAULT 'v1',
    status             VARCHAR(12)  NOT NULL DEFAULT 'SHOWN' COMMENT 'SHOWN|ACCEPTED|REJECTED|EDITED',
    reviewed_by        CHAR(36)     NULL,
    reviewed_at        DATETIME(3)  NULL,
    created_at         DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by         CHAR(36)     NULL,
    PRIMARY KEY (id),
    INDEX idx_ai_sugg_patient (tenant_id, patient_id, created_at),
    INDEX idx_ai_sugg_status (tenant_id, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Log goi y dieu tri AI (guardrail SaMD, human-in-the-loop)';
