-- ============================================================
-- Migration: 0023_patient_extensions_sprint2
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-P07, SUNS-I.1
-- Idempotent: YES (add_col_if_missing, overlap voi 0004 an toan)
-- ============================================================
SET NAMES utf8mb4;

-- reception_note (co the da co tu 0004 — idempotent)
CALL add_col_if_missing('pat_patients', 'reception_note', 'TEXT NULL COMMENT \'Ghi chu nhanh cua le tan khi tiep don\'');

-- avatar_url (co the da co tu 0004 — idempotent)
CALL add_col_if_missing('pat_patients', 'avatar_url', 'VARCHAR(500) NULL COMMENT \'URL anh dai dien benh nhan (MinIO)\'');

-- allergies_summary (denormalized top-3 di ung nghiem trong)
CALL add_col_if_missing('pat_patients', 'allergies_summary', 'VARCHAR(500) NULL COMMENT \'Denormalized top-3 di ung nghiem trong\'');

-- status column (ACTIVE|INACTIVE|DECEASED)
CALL add_col_if_missing('pat_patients', 'status', 'VARCHAR(20) NOT NULL DEFAULT \'ACTIVE\' COMMENT \'ACTIVE|INACTIVE|DECEASED\'');

-- blood_type
CALL add_col_if_missing('pat_patients', 'blood_type', 'VARCHAR(20) NULL COMMENT \'A_POS|A_NEG|B_POS|B_NEG|AB_POS|AB_NEG|O_POS|O_NEG|UNKNOWN\'');

-- pat_allergies table
CREATE TABLE IF NOT EXISTS `pat_allergies` (
    `id`         CHAR(36)    NOT NULL DEFAULT (UUID()),
    `tenant_id`  INT         NULL,
    `patient_id` INT         NOT NULL,
    `allergen`   VARCHAR(200) NOT NULL COMMENT 'Chat gay di ung',
    `reaction`   VARCHAR(500) NULL    COMMENT 'Phan ung di ung',
    `severity`   VARCHAR(30) NOT NULL COMMENT 'MILD|MODERATE|SEVERE|LIFE_THREATENING',
    `onset_date` DATE        NULL     COMMENT 'Ngay xuat hien',
    `note`       TEXT        NULL,
    `created_at` DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by` CHAR(36)    NULL,
    `updated_at` DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at` DATETIME    NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_allergy_patient` (`tenant_id`, `patient_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Di ung cua benh nhan';

-- pat_insurance table
CREATE TABLE IF NOT EXISTS `pat_insurance` (
    `id`               CHAR(36)    NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT         NULL,
    `patient_id`       INT         NOT NULL,
    `type`             VARCHAR(20) NOT NULL DEFAULT 'BHYT' COMMENT 'BHYT|PRIVATE|OTHER',
    `card_no_enc`      VARCHAR(500) NOT NULL COMMENT 'Ma the BHYT da ma hoa AES-256-GCM',
    `card_no_masked`   VARCHAR(20)  NULL    COMMENT 'Ma the da an bo: HC401******',
    `valid_from`       DATE        NOT NULL,
    `valid_to`         DATE        NOT NULL,
    `hospital_code`    VARCHAR(20) NULL     COMMENT 'Ma CSKCB',
    `coverage_percent` TINYINT     NULL     COMMENT '0-100',
    `created_at`       DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       CHAR(36)    NULL,
    `updated_at`       DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`       DATETIME    NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_insurance_patient` (`tenant_id`, `patient_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='The BHYT / bao hiem cua benh nhan';

-- pat_emergency_contacts table
CREATE TABLE IF NOT EXISTS `pat_emergency_contacts` (
    `id`           CHAR(36)    NOT NULL DEFAULT (UUID()),
    `tenant_id`    INT         NULL,
    `patient_id`   INT         NOT NULL,
    `full_name`    VARCHAR(200) NOT NULL,
    `relationship` VARCHAR(20)  NOT NULL COMMENT 'FATHER|MOTHER|SPOUSE|CHILD|SIBLING|OTHER',
    `phone`        VARCHAR(20)  NOT NULL,
    `address`      VARCHAR(500) NULL,
    `created_at`   DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`   CHAR(36)    NULL,
    `updated_at`   DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`   DATETIME    NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_emcontact_patient` (`tenant_id`, `patient_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Lien he khan cap cua benh nhan';

-- pat_consents table
CREATE TABLE IF NOT EXISTS `pat_consents` (
    `id`               CHAR(36)    NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT         NULL,
    `patient_id`       INT         NOT NULL,
    `consent_type`     VARCHAR(30) NOT NULL COMMENT 'TREATMENT|DATA_PROCESSING|MARKETING|SURGERY|RESEARCH',
    `signed_at`        DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `signed_by`        VARCHAR(200) NULL,
    `document_file_id` CHAR(36)    NULL     COMMENT 'FK → fil_files.id',
    `revoked_at`       DATETIME    NULL,
    `created_at`       DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       CHAR(36)    NULL,
    `updated_at`       DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`       DATETIME    NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_consent_patient` (`tenant_id`, `patient_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Van ban dong y cua benh nhan';

-- Add file_id column to cls_uploads (UUID ref to fil_files)
CALL add_col_if_missing('diab_his_fil_cls_uploads', 'file_id', 'CHAR(36) NULL COMMENT \'FK → fil_files.id\'');
CALL add_col_if_missing('diab_his_fil_cls_uploads', 'note', 'TEXT NULL COMMENT \'Ghi chu ve tai lieu\'');
