-- ============================================================
-- Migration: 9011_create_missing_tables
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Mo ta: Tao cac bang con thieu ma code tham chieu
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- DDI Rules (tuong tac thuoc)
CREATE TABLE IF NOT EXISTS `diab_his_pha_ddi_rules` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `tenant_id`     INT             NOT NULL,
    `drug1_id`      CHAR(36)        NOT NULL,
    `drug2_id`      CHAR(36)        NOT NULL,
    `severity`      VARCHAR(20)     NOT NULL DEFAULT 'MODERATE',
    `description`   TEXT            NULL,
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME        NULL,
    INDEX `idx_ddi_drug1` (`drug1_id`),
    INDEX `idx_ddi_drug2` (`drug2_id`),
    INDEX `idx_ddi_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Drug categories
CREATE TABLE IF NOT EXISTS `diab_his_pha_drug_categories` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `tenant_id`     INT             NOT NULL,
    `code`          VARCHAR(50)     NOT NULL,
    `name`          VARCHAR(255)    NOT NULL,
    `is_active`     TINYINT(1)      NOT NULL DEFAULT 1,
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME        NULL,
    INDEX `idx_drug_cat_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Dispense items
CREATE TABLE IF NOT EXISTS `diab_his_pha_dispense_items` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `tenant_id`     INT             NOT NULL,
    `dispense_id`   CHAR(36)        NOT NULL,
    `drug_id`       CHAR(36)        NOT NULL,
    `quantity`      INT             NOT NULL DEFAULT 0,
    `unit_price`    DECIMAL(18,2)   NULL,
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME        NULL,
    INDEX `idx_disp_items_tenant` (`tenant_id`, `dispense_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Dispense records
CREATE TABLE IF NOT EXISTS `diab_his_pha_dispense_records` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `tenant_id`     INT             NOT NULL,
    `prescription_id` CHAR(36)      NULL,
    `patient_id`    CHAR(36)        NULL,
    `dispensed_by`  CHAR(36)        NULL,
    `status`        VARCHAR(20)     NOT NULL DEFAULT 'DISPENSED',
    `note`          TEXT            NULL,
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME        NULL,
    INDEX `idx_disp_rec_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Stock movements
CREATE TABLE IF NOT EXISTS `diab_his_pha_stock_movements` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `tenant_id`     INT             NOT NULL,
    `drug_id`       CHAR(36)        NOT NULL,
    `movement_type` VARCHAR(30)     NOT NULL,
    `quantity`      INT             NOT NULL,
    `note`          TEXT            NULL,
    `ref_id`        CHAR(36)        NULL,
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`    CHAR(36)        NULL,
    INDEX `idx_stock_mv_tenant` (`tenant_id`, `drug_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Prescription print history
CREATE TABLE IF NOT EXISTS `diab_his_pha_prescription_print_history` (
    `id`                CHAR(36)    NOT NULL PRIMARY KEY,
    `tenant_id`         INT         NOT NULL,
    `prescription_id`   CHAR(36)    NOT NULL,
    `printed_by`        CHAR(36)    NULL,
    `printed_at`        DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_pph_tenant` (`tenant_id`, `prescription_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- DTQG integration
CREATE TABLE IF NOT EXISTS `diab_his_int_dtqg_credentials` (
    `id`                CHAR(36)        NOT NULL PRIMARY KEY,
    `tenant_id`         INT             NOT NULL UNIQUE,
    `cskcb_id`          VARCHAR(50)     NULL,
    `token_encrypted`   VARCHAR(1000)   NULL,
    `is_active`         TINYINT(1)      NOT NULL DEFAULT 1,
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `diab_his_int_dtqg_submissions` (
    `id`                CHAR(36)    NOT NULL PRIMARY KEY,
    `tenant_id`         INT         NOT NULL,
    `prescription_id`   CHAR(36)    NOT NULL,
    `dtqg_code`         VARCHAR(50) NULL,
    `status`            VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    `response_json`     TEXT        NULL,
    `submitted_at`      DATETIME    NULL,
    `created_at`        DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX `idx_dtqg_sub_tenant` (`tenant_id`, `prescription_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Reception queue
CREATE TABLE IF NOT EXISTS `diab_his_rcp_queue_tickets` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `tenant_id`     INT             NOT NULL,
    `patient_id`    CHAR(36)        NOT NULL,
    `queue_number`  INT             NOT NULL,
    `room_id`       CHAR(36)        NULL,
    `status`        VARCHAR(20)     NOT NULL DEFAULT 'WAITING',
    `note`          TEXT            NULL,
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME        NULL,
    INDEX `idx_queue_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Appointments
CREATE TABLE IF NOT EXISTS `diab_his_sch_appointments` (
    `id`                CHAR(36)    NOT NULL PRIMARY KEY,
    `tenant_id`         INT         NOT NULL,
    `patient_id`        CHAR(36)    NOT NULL,
    `doctor_id`         CHAR(36)    NULL,
    `appointment_date`  DATE        NOT NULL,
    `appointment_time`  TIME        NULL,
    `status`            VARCHAR(20) NOT NULL DEFAULT 'SCHEDULED',
    `note`              TEXT        NULL,
    `created_at`        DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`        DATETIME    NULL,
    INDEX `idx_appt_tenant` (`tenant_id`, `appointment_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Encryption keys
CREATE TABLE IF NOT EXISTS `diab_his_sec_encryption_keys` (
    `id`            CHAR(36)        NOT NULL PRIMARY KEY,
    `tenant_id`     INT             NOT NULL,
    `key_name`      VARCHAR(100)    NOT NULL,
    `key_value`     VARCHAR(1000)   NOT NULL,
    `is_active`     TINYINT(1)      NOT NULL DEFAULT 1,
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME        NULL,
    INDEX `idx_enc_keys_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Feature flags
CREATE TABLE IF NOT EXISTS `diab_his_sys_feature_flags` (
    `id`            INT             NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`     INT             NOT NULL,
    `flag_key`      VARCHAR(100)    NOT NULL,
    `is_enabled`    TINYINT(1)      NOT NULL DEFAULT 0,
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uq_ff_tenant_key` (`tenant_id`, `flag_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- BHYT int alias views
CREATE OR REPLACE VIEW diab_his_int_bhyt_exports           AS SELECT * FROM diab_his_bhyt_exports;
CREATE OR REPLACE VIEW diab_his_int_bhyt_export_items      AS SELECT * FROM diab_his_bhyt_export_items;
CREATE OR REPLACE VIEW diab_his_int_bhyt_reconcile_items   AS SELECT * FROM diab_his_bhyt_reconcile_items;
CREATE OR REPLACE VIEW diab_his_int_bhyt_reconcile_uploads AS SELECT * FROM diab_his_bhyt_reconcile_uploads;

-- CLS uploads alias
CREATE OR REPLACE VIEW diab_his_fil_cls_uploads AS SELECT * FROM diab_his_cls_uploads;

SET FOREIGN_KEY_CHECKS = 1;
