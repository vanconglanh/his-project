-- ============================================================================
-- 9173_inbody_reports.sql
--
-- Doc ket qua may InBody (thanh phan co the) tu file PDF may in ra, tu dong trich
-- so lieu de dien san thay go tay. Xem PRD: docs/prd/inbody-ocr-20260830.md
--
--   1. diab_his_cli_inbody_report — 1 lan upload/doc 1 file PDF ket qua InBody cua
--      benh nhan. Luu raw_text (text da trich, phuc vu debug/parse lai neu can) +
--      extraction_status (pending/success/partial/failed).
--   2. diab_his_cli_indicator_reading — bang generic luu cac chi so lam sang roi
--      rac khong thuoc VitalSigns/diab_his_cli_diabetes_assessments (schema co dinh).
--      Du an CHUA co bang ClinicalIndicator generic san co (da grep xac nhan) nen
--      tao moi bang nay danh cho InBody (SMM/BODY_FAT_MASS/PBF/VISCERAL_FAT/TBW/BMR/
--      INBODY_SCORE) va co the tai su dung cho cac nguon do luong roi rac khac sau nay
--      (cot source phan biet nguon, vd 'inbody_ocr').
--
-- Idempotent: CREATE TABLE IF NOT EXISTS (bang moi hoan toan, an toan chay lai).
-- ============================================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_cli_inbody_report` (
    `id`                  CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`           INT          NOT NULL,
    `patient_id`          CHAR(36)     NOT NULL,
    `encounter_id`        CHAR(36)     NULL,
    `file_id`             CHAR(36)     NULL COMMENT 'FK toi fil_files.id (bucket inbody-reports)',
    `file_url`            VARCHAR(500) NULL COMMENT 'object_key tren bucket inbody-reports',
    `file_name`           VARCHAR(255) NULL,
    `raw_text`            LONGTEXT     NULL COMMENT 'Text da trich tu PDF (PdfPig), phuc vu debug/re-parse',
    `extracted_fields_json` JSON       NULL COMMENT 'Snapshot ket qua extract ban dau (truoc khi confirm)',
    `extraction_status`   VARCHAR(20)  NOT NULL DEFAULT 'pending' COMMENT 'pending|success|partial|failed',
    `extracted_by`        CHAR(36)    NULL,
    `confirmed_by`        CHAR(36)    NULL,
    `confirmed_at`        DATETIME    NULL,
    `created_at`          DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`          DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`          DATETIME    NULL,
    PRIMARY KEY (`id`),
    KEY `idx_inbody_report_tenant_patient` (`tenant_id`, `patient_id`),
    KEY `idx_inbody_report_tenant_encounter` (`tenant_id`, `encounter_id`),
    KEY `idx_inbody_report_status` (`tenant_id`, `extraction_status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Ket qua doc PDF may InBody - cho xac nhan truoc khi ghi vao ho so (khong tu dong commit)';

CREATE TABLE IF NOT EXISTS `diab_his_cli_indicator_reading` (
    `id`               CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT          NOT NULL,
    `patient_id`       CHAR(36)     NOT NULL,
    `encounter_id`     CHAR(36)     NULL,
    `indicator_type`   VARCHAR(50)  NOT NULL COMMENT 'vd SMM/BODY_FAT_MASS/PBF/VISCERAL_FAT/TBW/BMR/INBODY_SCORE',
    `value`            DECIMAL(12,4) NULL,
    `unit`             VARCHAR(20)  NULL,
    `source`           VARCHAR(30)  NOT NULL DEFAULT 'manual' COMMENT 'manual|inbody_ocr|...',
    `source_ref_id`    CHAR(36)     NULL COMMENT 'vd id cua diab_his_cli_inbody_report sinh ra ban ghi nay',
    `recorded_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `recorded_by`      CHAR(36)     NULL,
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `deleted_at`       DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_indicator_reading_tenant_patient_type` (`tenant_id`, `patient_id`, `indicator_type`),
    KEY `idx_indicator_reading_tenant_encounter` (`tenant_id`, `encounter_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi so lam sang roi rac (generic) - hien dung cho ket qua InBody, co the mo rong nguon khac sau nay';
