-- ============================================================
-- Migration: 0029_create_diabetes_history
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: US-E04
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Tao bang neu 0015 chua tao
CREATE TABLE IF NOT EXISTS diab_his_cli_diabetes_assessments (
    id                   CHAR(36)      NOT NULL DEFAULT (UUID()),
    tenant_id            INT           NOT NULL,
    encounter_id         CHAR(36)      NOT NULL,
    patient_id           CHAR(36)      NOT NULL,
    hba1c                DECIMAL(5,2)  NULL,
    fasting_glucose      DECIMAL(8,2)  NULL,
    postprandial_glucose DECIMAL(8,2)  NULL,
    random_glucose       DECIMAL(8,2)  NULL,
    egfr                 DECIMAL(8,2)  NULL,
    serum_creatinine     DECIMAL(8,2)  NULL,
    urine_acr            DECIMAL(8,2)  NULL,
    bp_systolic          INT           NULL,
    bp_diastolic         INT           NULL,
    bmi                  DECIMAL(5,2)  NULL,
    waist_circumference  DECIMAL(5,2)  NULL,
    diabetes_type        VARCHAR(20)   NULL
                         COMMENT 'TYPE_1|TYPE_2|GESTATIONAL|MODY|OTHER',
    complications        JSON          NULL,
    treatment_target     JSON          NULL,
    note                 TEXT          NULL,
    assessed_at          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assessed_by          CHAR(36)      NULL,
    created_at           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by           CHAR(36)      NULL,
    updated_at           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    updated_by           CHAR(36)      NULL,
    deleted_at           DATETIME      NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_dm_encounter (encounter_id),
    INDEX idx_dm_patient_date (tenant_id, patient_id, assessed_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Diabetes-specific clinical assessment per encounter';

-- ADD cols neu bang da co tu migration cu
CALL add_col_if_missing('diab_his_cli_diabetes_assessments', 'treatment_target',    'JSON NULL');
CALL add_col_if_missing('diab_his_cli_diabetes_assessments', 'waist_circumference', 'DECIMAL(5,2) NULL');
CALL add_col_if_missing('diab_his_cli_diabetes_assessments', 'urine_acr',           'DECIMAL(8,2) NULL');
CALL add_col_if_missing('diab_his_cli_diabetes_assessments', 'complications',       'JSON NULL');

-- Add index neu chua co
DROP PROCEDURE IF EXISTS _add_idx_29;
DELIMITER $$
CREATE PROCEDURE _add_idx_29()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'diab_his_cli_diabetes_assessments'
          AND INDEX_NAME   = 'idx_dm_patient_date'
    ) THEN
        ALTER TABLE diab_his_cli_diabetes_assessments
            ADD INDEX idx_dm_patient_date (tenant_id, patient_id, assessed_at DESC);
    END IF;
END$$
DELIMITER ;
CALL _add_idx_29();
DROP PROCEDURE IF EXISTS _add_idx_29;

-- Diabetes assessment templates
CREATE TABLE IF NOT EXISTS diab_his_cli_diabetes_templates (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      INT          NULL,
    name           VARCHAR(200) NOT NULL,
    default_values JSON         NULL,
    checklist      JSON         NULL,
    is_system      TINYINT(1)   NOT NULL DEFAULT 0,
    created_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by     CHAR(36)     NULL,
    updated_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    updated_by     CHAR(36)     NULL,
    deleted_at     DATETIME     NULL,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Diabetes assessment form templates';

-- Seed 1 system template
INSERT IGNORE INTO diab_his_cli_diabetes_templates
    (id, tenant_id, name, default_values, checklist, is_system, created_at, updated_at)
VALUES (
    'bbbbbbbb-0001-0000-0000-000000000001',
    NULL,
    'Mẫu đánh giá ĐTĐ chuẩn',
    '{"hba1c_target": 7.0, "ldl_target": 2.6, "bp_target": "130/80"}',
    '["Đo HbA1c","Đường huyết đói","Đường huyết sau ăn 2h","eGFR","ACR","Khám bàn chân","Khám mắt","ECG"]',
    1,
    NOW(),
    NOW()
);
