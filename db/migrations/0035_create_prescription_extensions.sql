-- ============================================================
-- Migration: 0035_create_prescription_extensions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-PH-10 US-PH-11 US-PH-12 (Sprint 6-7 EPIC 5)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Extend pha_prescriptions with sprint 6-7 columns
CALL add_col_if_missing('pha_prescriptions', 'status',
    "ENUM('DRAFT','SIGNED','SUBMITTED_DTQG','DISPENSED','PARTIAL_DISPENSED','CANCELLED') NOT NULL DEFAULT 'DRAFT' COMMENT 'State machine: DRAFT->SIGNED->SUBMITTED_DTQG->DISPENSED|PARTIAL_DISPENSED|CANCELLED'");

CALL add_col_if_missing('pha_prescriptions', 'dtqg_code',
    "CHAR(14) NULL COMMENT 'Ma don thuoc Quoc gia 14 ky tu sau khi DTQG accept'");

CALL add_col_if_missing('pha_prescriptions', 'dtqg_status',
    "ENUM('NONE','PENDING','SUBMITTED','ACCEPTED','REJECTED') NOT NULL DEFAULT 'NONE' COMMENT 'Trang thai tich hop DTQG'");

CALL add_col_if_missing('pha_prescriptions', 'signed_at',
    "DATETIME NULL COMMENT 'Thoi diem ky so USB token'");

CALL add_col_if_missing('pha_prescriptions', 'signed_by',
    "INT NULL COMMENT 'ID bac si ky so'");

CALL add_col_if_missing('pha_prescriptions', 'signature_data',
    "LONGBLOB NULL COMMENT 'PKCS#7 detached signature blob (AES-256-GCM encrypted at rest)'");

CALL add_col_if_missing('pha_prescriptions', 'total_amount',
    "DECIMAL(15,2) NULL DEFAULT 0 COMMENT 'Tong tien don thuoc'");

CALL add_col_if_missing('pha_prescriptions', 'note',
    "TEXT NULL COMMENT 'Ghi chu don thuoc'");

-- Index for status queries
CREATE INDEX IF NOT EXISTS `idx_pha_pres_tenant_status` ON `pha_prescriptions` (`tenant_id`, `status`);
CREATE INDEX IF NOT EXISTS `idx_pha_pres_dtqg_code` ON `pha_prescriptions` (`dtqg_code`);

-- ============================================================
-- Prescription items (thuoc trong don)
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_prescription_items` (
    `id`               CHAR(36)        NOT NULL DEFAULT (UUID()) COMMENT 'UUID primary key',
    `tenant_id`        INT             NOT NULL                  COMMENT 'FK tenant (app-layer RLS)',
    `prescription_id`  INT             NOT NULL                  COMMENT 'FK -> pha_prescriptions.ID',
    `drug_id`          INT             NOT NULL                  COMMENT 'FK -> pha_drug_master.ID',
    `dosage`           VARCHAR(100)    NOT NULL                  COMMENT 'Lieu dung: 1 vien, 5ml, ...',
    `frequency`        VARCHAR(100)    NOT NULL                  COMMENT 'Tan suat: 2 lan/ngay, ...',
    `route`            ENUM('ORAL','IV','IM','SC','TOP','INH','OPH','OTIC','NAS','REC','OTHER')
                                       NOT NULL DEFAULT 'ORAL'   COMMENT 'Duong dung thuoc',
    `duration_days`    INT             NOT NULL                  COMMENT 'So ngay dung (>= 1)',
    `quantity`         DECIMAL(10,2)   NOT NULL                  COMMENT 'So luong ke don',
    `instructions`     TEXT            NULL                      COMMENT 'Huong dan dung thuoc (optional encrypt)',
    `batch_dispensed`  VARCHAR(500)    NULL                      COMMENT 'JSON array [{batch_no, quantity}] sau khi phat',
    `created_at`       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       INT             NULL,
    `updated_at`       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       INT             NULL,
    `deleted_at`       DATETIME        NULL,

    PRIMARY KEY (`id`),
    INDEX `idx_pres_item_tenant`       (`tenant_id`, `prescription_id`),
    INDEX `idx_pres_item_drug`         (`drug_id`),
    INDEX `idx_pres_item_prescription` (`prescription_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi tiet thuoc trong don ke don';

-- ============================================================
-- Drug-Drug Interaction rules
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_ddi_rules` (
    `id`             CHAR(36)   NOT NULL DEFAULT (UUID()) COMMENT 'UUID primary key',
    `drug1_id`       INT        NOT NULL                  COMMENT 'FK drug_master.ID (sorted: drug1_id < drug2_id)',
    `drug2_id`       INT        NOT NULL                  COMMENT 'FK drug_master.ID',
    `severity`       ENUM('MINOR','MODERATE','MAJOR','CONTRAINDICATED')
                                NOT NULL                  COMMENT 'Muc do tuong tac',
    `description`    TEXT       NOT NULL                  COMMENT 'Mo ta tuong tac (tieng Viet)',
    `evidence_level` CHAR(1)    NOT NULL DEFAULT 'B'      COMMENT 'Muc do bang chung A/B/C',
    `source`         VARCHAR(100) NULL                    COMMENT 'Nguon: Drugbank, manual, ...',
    `created_at`     DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`     INT        NULL,
    `updated_at`     DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`     INT        NULL,
    `deleted_at`     DATETIME   NULL,

    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_ddi_drug_pair` (`drug1_id`, `drug2_id`),
    INDEX `idx_ddi_drug1` (`drug1_id`),
    INDEX `idx_ddi_drug2` (`drug2_id`),
    INDEX `idx_ddi_severity` (`severity`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Quy tac tuong tac thuoc (Drug-Drug Interaction)';

-- ============================================================
-- Print history for prescriptions
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pha_prescription_print_history` (
    `id`              CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT          NOT NULL,
    `prescription_id` INT          NOT NULL,
    `printed_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `printed_by`      INT          NULL,
    `printer_name`    VARCHAR(100) NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_print_hist_pres` (`prescription_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lich su in don thuoc';
