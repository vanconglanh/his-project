-- ============================================================
-- Migration: 0032_lab_rad_results
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: US-SUNS-13, US-SUNS-14, US-SUNS-15
-- Idempotent: YES (dung add_col_if_missing)
-- ============================================================
SET NAMES utf8mb4;

-- ─────────────────────────────────────────────────
-- cli_lab_results: bang ket qua xet nghiem
-- ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `cli_lab_results` (
    `id`                    CHAR(36)        NOT NULL DEFAULT (UUID()),
    `tenant_id`             INT             NOT NULL,
    `lab_order_id`          CHAR(36)        NOT NULL    COMMENT 'FK -> diab_his_cli_lab_orders.id',
    `lab_order_item_id`     CHAR(36)        NOT NULL    COMMENT 'item trong lab order (1 order co the co nhieu test)',
    `patient_id`            CHAR(36)        NOT NULL,
    `encounter_id`          CHAR(36)        NOT NULL,
    `test_code`             VARCHAR(50)     NOT NULL,
    `test_name`             VARCHAR(300)    NOT NULL    DEFAULT '',
    `value`                 VARCHAR(500)    NOT NULL    DEFAULT '' COMMENT 'Gia tri tho (co the la chuoi: Am tinh)',
    `value_numeric`         DECIMAL(18,4)   NULL,
    `unit`                  VARCHAR(32)     NULL,
    `reference_range_low`   DECIMAL(18,4)   NULL,
    `reference_range_high`  DECIMAL(18,4)   NULL,
    `flag`                  ENUM('NORMAL','H','L','HH','LL','CRITICAL')
                                            NOT NULL    DEFAULT 'NORMAL',
    `method`                VARCHAR(64)     NULL,
    `performed_at`          DATETIME        NOT NULL,
    `performed_by`          CHAR(36)        NULL,
    `status`                ENUM('DRAFT','VERIFIED','AMENDED')
                                            NOT NULL    DEFAULT 'DRAFT',
    `verified_at`           DATETIME        NULL,
    `verified_by`           CHAR(36)        NULL,
    `source`                ENUM('MANUAL','IMPORT','PARTNER')
                                            NOT NULL    DEFAULT 'MANUAL',
    `note`                  TEXT            NULL,
    `created_at`            DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)        NULL,
    `updated_at`            DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)        NULL,
    `deleted_at`            DATETIME        NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_lr_tenant_status`    (`tenant_id`, `status`),
    INDEX `idx_lr_tenant_flag`      (`tenant_id`, `flag`),
    INDEX `idx_lr_trend`            (`tenant_id`, `patient_id`, `test_code`, `performed_at`),
    INDEX `idx_lr_order`            (`tenant_id`, `lab_order_id`),
    INDEX `idx_lr_encounter`        (`tenant_id`, `encounter_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Ket qua xet nghiem (XN sinh hoa, huyet hoc, vi sinh...)';

-- ADD cols neu bang da ton tai (idempotent)
CALL add_col_if_missing('cli_lab_results', 'value_numeric',          'DECIMAL(18,4) NULL');
CALL add_col_if_missing('cli_lab_results', 'unit',                   'VARCHAR(32) NULL');
CALL add_col_if_missing('cli_lab_results', 'reference_range_low',    'DECIMAL(18,4) NULL');
CALL add_col_if_missing('cli_lab_results', 'reference_range_high',   'DECIMAL(18,4) NULL');
CALL add_col_if_missing('cli_lab_results', 'flag',                   "ENUM('NORMAL','H','L','HH','LL','CRITICAL') NOT NULL DEFAULT 'NORMAL'");
CALL add_col_if_missing('cli_lab_results', 'method',                 'VARCHAR(64) NULL');
CALL add_col_if_missing('cli_lab_results', 'status',                 "ENUM('DRAFT','VERIFIED','AMENDED') NOT NULL DEFAULT 'DRAFT'");
CALL add_col_if_missing('cli_lab_results', 'verified_at',            'DATETIME NULL');
CALL add_col_if_missing('cli_lab_results', 'verified_by',            'CHAR(36) NULL');
CALL add_col_if_missing('cli_lab_results', 'source',                 "ENUM('MANUAL','IMPORT','PARTNER') NOT NULL DEFAULT 'MANUAL'");

-- ─────────────────────────────────────────────────
-- cli_rad_results: bang ket qua chan doan hinh anh
-- ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `cli_rad_results` (
    `id`                CHAR(36)        NOT NULL DEFAULT (UUID()),
    `tenant_id`         INT             NOT NULL,
    `rad_order_id`      CHAR(36)        NOT NULL    COMMENT 'FK -> diab_his_cli_rad_orders.id',
    `patient_id`        CHAR(36)        NOT NULL,
    `encounter_id`      CHAR(36)        NOT NULL,
    `modality`          VARCHAR(20)     NOT NULL    COMMENT 'XRAY|US|CT|MRI|MAMMO|ECG',
    `findings`          TEXT            NOT NULL    COMMENT 'Mo ta hinh anh',
    `impression`        TEXT            NULL        COMMENT 'An tuong / danh gia',
    `conclusion`        TEXT            NOT NULL    COMMENT 'Ket luan chan doan',
    `recommendations`   TEXT            NULL,
    `performed_at`      DATETIME        NOT NULL,
    `performed_by`      CHAR(36)        NULL,
    `status`            ENUM('DRAFT','VERIFIED','AMENDED')
                                        NOT NULL    DEFAULT 'DRAFT',
    `verified_at`       DATETIME        NULL,
    `verified_by`       CHAR(36)        NULL,
    `dicom_count`       INT             NOT NULL    DEFAULT 0,
    `signed_pdf_path`   VARCHAR(512)    NULL        COMMENT 'MinIO path den PDF da ky',
    `note`              TEXT            NULL,
    `created_at`        DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `created_by`        CHAR(36)        NULL,
    `updated_at`        DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`        CHAR(36)        NULL,
    `deleted_at`        DATETIME        NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_rr_tenant_status`    (`tenant_id`, `status`),
    INDEX `idx_rr_order`            (`tenant_id`, `rad_order_id`),
    INDEX `idx_rr_encounter`        (`tenant_id`, `encounter_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Ket qua chan doan hinh anh (CDHA)';

-- ADD cols neu bang cu da ton tai
CALL add_col_if_missing('cli_rad_results', 'findings',          'TEXT NOT NULL DEFAULT \'\'');
CALL add_col_if_missing('cli_rad_results', 'impression',        'TEXT NULL');
CALL add_col_if_missing('cli_rad_results', 'conclusion',        'TEXT NOT NULL DEFAULT \'\'');
CALL add_col_if_missing('cli_rad_results', 'recommendations',   'TEXT NULL');
CALL add_col_if_missing('cli_rad_results', 'dicom_count',       'INT NOT NULL DEFAULT 0');
CALL add_col_if_missing('cli_rad_results', 'signed_pdf_path',   'VARCHAR(512) NULL');
CALL add_col_if_missing('cli_rad_results', 'status',            "ENUM('DRAFT','VERIFIED','AMENDED') NOT NULL DEFAULT 'DRAFT'");
CALL add_col_if_missing('cli_rad_results', 'verified_at',       'DATETIME NULL');
CALL add_col_if_missing('cli_rad_results', 'verified_by',       'CHAR(36) NULL');
