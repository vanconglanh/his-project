-- ============================================================
-- Migration: 0036_drug_master_extensions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-PH-20 (Sprint 6-7 EPIC 5)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Extend pha_drug_master with new clinical/regulatory columns
CALL add_col_if_missing('pha_drug_master', 'atc_code',
    "VARCHAR(20) NULL COMMENT 'ATC code (Anatomical Therapeutic Chemical) WHO standard'");

CALL add_col_if_missing('pha_drug_master', 'form',
    "ENUM('TABLET','CAPSULE','SYRUP','INJ','CREAM','OINTMENT','DROP','INHALER','POWDER','SUPPOSITORY','OTHER') NULL COMMENT 'Dang bao che'");

CALL add_col_if_missing('pha_drug_master', 'requires_prescription',
    "TINYINT(1) NOT NULL DEFAULT 1 COMMENT '1=thuoc ke don (Rx), 0=OTC'");

CALL add_col_if_missing('pha_drug_master', 'is_psychotropic',
    "TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1=thuoc huong than'");

CALL add_col_if_missing('pha_drug_master', 'is_narcotic',
    "TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1=thuoc gay nghien'");

CALL add_col_if_missing('pha_drug_master', 'dtqg_drug_code',
    "VARCHAR(50) NULL COMMENT 'Ma thuoc tren he thong Don thuoc Quoc gia'");

CALL add_col_if_missing('pha_drug_master', 'price',
    "DECIMAL(15,2) NULL DEFAULT 0 COMMENT 'Don gia ban le (VND)'");

CALL add_col_if_missing('pha_drug_master', 'category_id',
    "INT NULL COMMENT 'FK -> pha_drug_categories.ID'");

CALL add_col_if_missing('pha_drug_master', 'name_en',
    "VARCHAR(255) NULL COMMENT 'Ten thuoc tieng Anh'");

CALL add_col_if_missing('pha_drug_master', 'generic_name',
    "VARCHAR(255) NULL COMMENT 'Ten hoat chat (generic)'");

CALL add_col_if_missing('pha_drug_master', 'status',
    "ENUM('ACTIVE','INACTIVE') NOT NULL DEFAULT 'ACTIVE' COMMENT 'Trang thai hoat dong'");

-- Drug categories (nhom thuoc)
CREATE TABLE IF NOT EXISTS `diab_his_pha_drug_categories` (
    `id`         CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`  INT          NOT NULL,
    `code`       VARCHAR(50)  NOT NULL,
    `name`       VARCHAR(255) NOT NULL,
    `parent_id`  CHAR(36)     NULL COMMENT 'Self-reference for hierarchy',
    `created_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at` DATETIME     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_drug_cat_tenant_code` (`tenant_id`, `code`),
    INDEX `idx_drug_cat_tenant` (`tenant_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Nhom thuoc (ATC / custom)';

-- Unique index on (tenant_id, code) for drug_master
CREATE UNIQUE INDEX IF NOT EXISTS `uk_drug_master_tenant_code` ON `pha_drug_master` (`tenant_id`, `CODE`);

-- Index for ATC code lookups (equivalents)
CREATE INDEX IF NOT EXISTS `idx_drug_master_atc` ON `pha_drug_master` (`atc_code`);
CREATE INDEX IF NOT EXISTS `idx_drug_master_status` ON `pha_drug_master` (`tenant_id`, `status`);
