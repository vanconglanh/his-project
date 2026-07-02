-- ============================================================
-- Migration: 0038_create_dispense_records
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-PH-40 US-PH-41 (Sprint 6-7 EPIC 5)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Dispense Records header
CREATE TABLE IF NOT EXISTS `diab_his_pha_dispense_records` (
    `id`              CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT           NOT NULL,
    `prescription_id` INT           NOT NULL COMMENT 'FK -> pha_prescriptions.ID',
    `warehouse_id`    INT           NOT NULL COMMENT 'FK -> pha_warehouses.ID',
    `dispensed_at`    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `dispensed_by`    INT           NULL COMMENT 'FK -> sec_users.ID',
    `status`          ENUM('DISPENSED','REJECTED','RETURNED','PARTIAL')
                                    NOT NULL DEFAULT 'DISPENSED',
    `note`            TEXT          NULL,
    `total_amount`    DECIMAL(15,2) NOT NULL DEFAULT 0,
    `created_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`      INT           NULL,
    `updated_at`      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`      INT           NULL,
    `deleted_at`      DATETIME      NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_dispense_prescription` (`prescription_id`, `tenant_id`) COMMENT 'Idempotent: 1 don 1 phieu phat',
    INDEX `idx_dispense_tenant_status`   (`tenant_id`, `status`),
    INDEX `idx_dispense_dispensed_at`    (`dispensed_at`),
    INDEX `idx_dispense_warehouse`       (`warehouse_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phieu phat thuoc (Dispense Record)';

-- Dispense Items (tung thuoc trong phieu phat)
CREATE TABLE IF NOT EXISTS `diab_his_pha_dispense_items` (
    `id`                   CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`            INT           NOT NULL,
    `dispense_record_id`   CHAR(36)      NOT NULL COMMENT 'FK -> diab_his_pha_dispense_records.id',
    `prescription_item_id` CHAR(36)      NOT NULL COMMENT 'FK -> diab_his_pha_prescription_items.id',
    `drug_id`              INT           NOT NULL COMMENT 'FK -> pha_drug_master.ID',
    `batch_no`             VARCHAR(50)   NOT NULL COMMENT 'Lo thuoc FEFO pick',
    `expiry_date`          DATE          NOT NULL,
    `quantity`             DECIMAL(10,2) NOT NULL,
    `unit_cost`            DECIMAL(15,2) NOT NULL DEFAULT 0,
    `line_amount`          DECIMAL(15,2) AS (`quantity` * `unit_cost`) STORED,
    `is_returned`          TINYINT(1)    NOT NULL DEFAULT 0,
    `returned_quantity`    DECIMAL(10,2) NOT NULL DEFAULT 0,
    `created_at`           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`           DATETIME      NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_disp_item_record` (`dispense_record_id`),
    INDEX `idx_disp_item_drug`   (`drug_id`),
    INDEX `idx_disp_item_tenant` (`tenant_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi tiet thuoc trong phieu phat';

-- Add last_tested_at / last_test_ok to DTQG credentials (needed by credentials API)
CALL add_col_if_missing('diab_his_int_dtqg_credentials', 'is_active',
    "TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Credentials dang hoat dong'");

CALL add_col_if_missing('diab_his_int_dtqg_credentials', 'last_tested_at',
    "DATETIME NULL COMMENT 'Thoi diem test ket noi DTQG gan nhat'");

CALL add_col_if_missing('diab_his_int_dtqg_credentials', 'last_test_ok',
    "TINYINT(1) NULL COMMENT '1=test thanh cong, 0=that bai'");

CALL add_col_if_missing('diab_his_int_dtqg_submissions', 'last_retry_at',
    "DATETIME NULL COMMENT 'Thoi diem retry gan nhat'");

-- Warehouse reorder level for low-stock alerts
CALL add_col_if_missing('pha_stocks', 'reorder_level',
    "DECIMAL(10,2) NOT NULL DEFAULT 10 COMMENT 'Nguong ton kho toi thieu canh bao'");

CALL add_col_if_missing('pha_stocks', 'quantity_available',
    "DECIMAL(12,3) NULL COMMENT 'Ton kho hien tai kha dung (alias friendly name)'");

CALL add_col_if_missing('pha_stocks', 'quantity_reserved',
    "DECIMAL(12,3) NOT NULL DEFAULT 0 COMMENT 'So luong dat cho don chua phat'");
