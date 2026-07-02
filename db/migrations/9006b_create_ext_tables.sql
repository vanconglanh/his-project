-- ============================================================
-- Migration: 9006b_create_ext_tables
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tac gia: Pro-Diab Team
-- Mo ta: Tao cac bang phu (billing ext, notification, portal,
--        BHYT, EMR clinic, lab partners, diabetes assessment)
--        chua duoc tao boi 9001-9006
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- BUG-04 fix: dam bao tong so bang diab_his_* >= 50
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- BILLING EXTENSIONS
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_bil_services` (
    `id`            CHAR(36)       NOT NULL DEFAULT (UUID()),
    `tenant_id`     INT            NOT NULL,
    `code`          VARCHAR(50)    NOT NULL,
    `name`          VARCHAR(255)   NOT NULL,
    `category`      VARCHAR(20)    NOT NULL COMMENT 'CONSULTATION|PROCEDURE|LAB|RAD|PHARMACY|OTHER',
    `price`         DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `vat_rate`      TINYINT        NOT NULL DEFAULT 0 COMMENT '0|5|8|10',
    `bhyt_code`     VARCHAR(50)    NULL,
    `bhyt_max_amount` DECIMAL(15,2) NULL,
    `is_active`     TINYINT(1)     NOT NULL DEFAULT 1,
    `created_at`    DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_by`    CHAR(36)       NULL,
    `updated_at`    DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    `updated_by`    CHAR(36)       NULL,
    `deleted_at`    DATETIME(3)    NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_service_tenant_code` (`tenant_id`, `code`),
    INDEX `idx_service_tenant_active` (`tenant_id`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Danh muc dich vu / bang gia';

CREATE TABLE IF NOT EXISTS `diab_his_bil_service_packages` (
    `id`               CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT           NOT NULL,
    `code`             VARCHAR(50)   NOT NULL,
    `name`             VARCHAR(255)  NOT NULL,
    `discount_percent` DECIMAL(5,2)  NOT NULL DEFAULT 0.00,
    `valid_from`       DATE          NULL,
    `valid_to`         DATE          NULL,
    `is_active`        TINYINT(1)    NOT NULL DEFAULT 1,
    `created_at`       DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_by`       CHAR(36)      NULL,
    `updated_at`       DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    `updated_by`       CHAR(36)      NULL,
    `deleted_at`       DATETIME(3)   NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_pkg_tenant_code` (`tenant_id`, `code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Goi kham dich vu';

CREATE TABLE IF NOT EXISTS `diab_his_bil_service_package_items` (
    `id`         CHAR(36)   NOT NULL DEFAULT (UUID()),
    `package_id` CHAR(36)   NOT NULL,
    `service_id` CHAR(36)   NOT NULL,
    `quantity`   INT        NOT NULL DEFAULT 1,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_pkg_item` (`package_id`, `service_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Chi tiet item trong goi kham';

CREATE TABLE IF NOT EXISTS `diab_his_bil_payments` (
    `id`               CHAR(36)       NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT            NOT NULL,
    `billing_id`       CHAR(36)       NOT NULL,
    `cashier_shift_id` CHAR(36)       NULL,
    `amount`           DECIMAL(15,2)  NOT NULL,
    `method`           VARCHAR(20)    NOT NULL COMMENT 'CASH|BANK_TRANSFER|VISA|MASTER|QR_VIETQR|QR_MOMO|QR_VNPAY|OTHER',
    `status`           VARCHAR(20)    NOT NULL DEFAULT 'PENDING',
    `reference`        VARCHAR(100)   NULL,
    `provider`         VARCHAR(50)    NULL,
    `provider_txn_id`  VARCHAR(100)   NULL,
    `paid_at`          DATETIME(3)    NULL,
    `paid_by`          CHAR(36)       NULL,
    `note`             TEXT           NULL,
    `refunded_amount`  DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `created_at`       DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_by`       CHAR(36)       NULL,
    `updated_at`       DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (`id`),
    INDEX `idx_payment_billing` (`billing_id`),
    INDEX `idx_payment_tenant_status` (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Thanh toan hoa don';

CREATE TABLE IF NOT EXISTS `diab_his_bil_qr_codes` (
    `id`              CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT           NOT NULL,
    `billing_id`      CHAR(36)      NOT NULL,
    `provider`        VARCHAR(20)   NOT NULL COMMENT 'VIETQR|MOMO|VNPAY',
    `qr_payload`      MEDIUMTEXT    NOT NULL,
    `qr_url`          VARCHAR(500)  NULL,
    `amount`          DECIMAL(15,2) NOT NULL,
    `transaction_ref` VARCHAR(50)   NOT NULL,
    `expires_at`      DATETIME(3)   NOT NULL,
    `paid_at`         DATETIME(3)   NULL,
    `status`          VARCHAR(20)   NOT NULL DEFAULT 'PENDING',
    `created_at`      DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    PRIMARY KEY (`id`),
    INDEX `idx_qr_billing` (`billing_id`),
    INDEX `idx_qr_expires` (`expires_at`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='QR code thanh toan';

CREATE TABLE IF NOT EXISTS `diab_his_bil_einvoices` (
    `id`             CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`      INT           NOT NULL,
    `billing_id`     CHAR(36)      NOT NULL,
    `provider`       VARCHAR(10)   NOT NULL COMMENT 'MISA|VNPT|EFY',
    `invoice_no`     VARCHAR(50)   NULL,
    `invoice_series` VARCHAR(20)   NULL,
    `cqt_code`       VARCHAR(13)   NULL,
    `issue_date`     DATETIME(3)   NULL,
    `total_amount`   DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `vat_amount`     DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `status`         VARCHAR(20)   NOT NULL DEFAULT 'DRAFT',
    `pdf_url`        VARCHAR(500)  NULL,
    `xml_url`        VARCHAR(500)  NULL,
    `signed_at`      DATETIME(3)   NULL,
    `cancel_reason`  TEXT          NULL,
    `cancelled_at`   DATETIME(3)   NULL,
    `retry_count`    INT           NOT NULL DEFAULT 0,
    `last_error`     TEXT          NULL,
    `created_at`     DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_by`     CHAR(36)      NULL,
    `updated_at`     DATETIME(3)   NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (`id`),
    INDEX `idx_einvoice_billing` (`billing_id`),
    INDEX `idx_einvoice_tenant_status` (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Hoa don dien tu';

CREATE TABLE IF NOT EXISTS `diab_his_bil_cashier_shifts` (
    `id`                CHAR(36)       NOT NULL DEFAULT (UUID()),
    `tenant_id`         INT            NOT NULL,
    `cashier_user_id`   CHAR(36)       NOT NULL,
    `shift_date`        DATE           NOT NULL,
    `shift_start`       DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `shift_end`         DATETIME(3)    NULL,
    `opening_balance`   DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `closing_balance`   DECIMAL(15,2)  NULL,
    `expected_cash`     DECIMAL(15,2)  NULL,
    `actual_cash`       DECIMAL(15,2)  NULL,
    `difference`        DECIMAL(15,2)  NULL,
    `total_cash`        DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `total_card`        DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `total_transfer`    DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `total_qr`          DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `total_other`       DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `total_refund`      DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `total_void`        DECIMAL(15,2)  NOT NULL DEFAULT 0.00,
    `count_transactions` INT           NOT NULL DEFAULT 0,
    `breakdown_json`    JSON           NULL,
    `status`            VARCHAR(10)    NOT NULL DEFAULT 'OPEN' COMMENT 'OPEN|CLOSED',
    `note`              TEXT           NULL,
    `closed_by`         CHAR(36)       NULL,
    `created_at`        DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `updated_at`        DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    PRIMARY KEY (`id`),
    INDEX `idx_shift_tenant_user` (`tenant_id`, `cashier_user_id`),
    INDEX `idx_shift_date` (`tenant_id`, `shift_date`),
    INDEX `idx_shift_status` (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Ca thu ngan';

-- ============================================================
-- NOTIFICATIONS
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_nti_notifications` (
    `id`         CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`  INT           NOT NULL,
    `user_id`    CHAR(36)      NOT NULL,
    `type`       VARCHAR(100)  NOT NULL,
    `title`      VARCHAR(300)  NOT NULL,
    `body`       TEXT          NOT NULL,
    `data_json`  JSON          NULL,
    `read_at`    DATETIME      NULL,
    `created_at` DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_nti_user` (`tenant_id`, `user_id`, `created_at` DESC),
    INDEX `idx_nti_unread` (`tenant_id`, `user_id`, `read_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Thong bao in-app';

CREATE TABLE IF NOT EXISTS `diab_his_nti_web_push_subs` (
    `id`         CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`  INT           NOT NULL,
    `user_id`    CHAR(36)      NOT NULL,
    `endpoint`   VARCHAR(1000) NOT NULL,
    `p256dh_key` VARCHAR(200)  NOT NULL,
    `auth_key`   VARCHAR(100)  NOT NULL,
    `user_agent` VARCHAR(500)  NULL,
    `created_at` DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_push_endpoint` (`endpoint`(200)),
    INDEX `idx_push_user` (`tenant_id`, `user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Web Push VAPID subscription';

CREATE TABLE IF NOT EXISTS `diab_his_nti_preferences` (
    `id`                   CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`            INT          NOT NULL,
    `user_id`              CHAR(36)     NOT NULL,
    `position`             VARCHAR(20)  NOT NULL DEFAULT 'TOP_RIGHT',
    `sound_enabled`        TINYINT(1)   NOT NULL DEFAULT 1,
    `sound_name`           VARCHAR(50)  NOT NULL DEFAULT 'default',
    `browser_push_enabled` TINYINT(1)   NOT NULL DEFAULT 0,
    `types_disabled`       JSON         NULL,
    `created_at`           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_pref_user` (`tenant_id`, `user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Cai dat thong bao cua nguoi dung';

CREATE TABLE IF NOT EXISTS `diab_his_nti_vapid_keys` (
    `id`                    CHAR(36)       NOT NULL DEFAULT (UUID()),
    `tenant_id`             INT            NOT NULL,
    `public_key`            VARCHAR(255)   NOT NULL,
    `private_key_encrypted` VARBINARY(512) NOT NULL,
    `created_at`            DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_vapid_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='VAPID keys per tenant cho Web Push';

-- ============================================================
-- PATIENT PORTAL
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pat_portal_accounts` (
    `id`               CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`        INT          NOT NULL,
    `patient_id`       CHAR(36)     NOT NULL,
    `phone`            VARCHAR(20)  NOT NULL,
    `password_hash`    VARCHAR(500) NOT NULL,
    `is_active`        TINYINT(1)   NOT NULL DEFAULT 1,
    `last_login_at`    DATETIME     NULL,
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`       DATETIME     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `ux_portal_patient` (`tenant_id`, `patient_id`),
    INDEX `idx_portal_phone` (`tenant_id`, `phone`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Tai khoan benh nhan tu quan ly (Patient Portal)';

CREATE TABLE IF NOT EXISTS `diab_his_pat_portal_otp_log` (
    `id`           CHAR(36)    NOT NULL DEFAULT (UUID()),
    `tenant_id`    INT         NOT NULL,
    `phone`        VARCHAR(20) NOT NULL,
    `otp_hash`     VARCHAR(200) NOT NULL,
    `purpose`      VARCHAR(30) NOT NULL COMMENT 'LOGIN|REGISTER|RESET_PASSWORD',
    `attempts`     INT         NOT NULL DEFAULT 0,
    `expires_at`   DATETIME    NOT NULL,
    `used_at`      DATETIME    NULL,
    `created_at`   DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_otp_phone` (`tenant_id`, `phone`, `created_at` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Log OTP benh nhan portal';

CREATE TABLE IF NOT EXISTS `diab_his_pat_portal_sessions` (
    `id`           CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`    INT          NOT NULL,
    `account_id`   CHAR(36)     NOT NULL,
    `token_hash`   VARCHAR(500) NOT NULL,
    `expires_at`   DATETIME     NOT NULL,
    `revoked_at`   DATETIME     NULL,
    `created_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_portal_sess_account` (`account_id`),
    INDEX `idx_portal_sess_expires` (`expires_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Session benh nhan portal';

-- ============================================================
-- BHYT EXPORT & RECONCILE
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_bhyt_exports` (
    `id`              CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT          NOT NULL,
    `export_period`   VARCHAR(10)  NOT NULL COMMENT 'YYYY-MM',
    `export_type`     VARCHAR(20)  NOT NULL DEFAULT 'XML_4750',
    `status`          VARCHAR(20)  NOT NULL DEFAULT 'DRAFT',
    `file_url`        VARCHAR(500) NULL,
    `total_records`   INT          NOT NULL DEFAULT 0,
    `total_amount`    DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `submitted_at`    DATETIME     NULL,
    `note`            TEXT         NULL,
    `created_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`      CHAR(36)     NULL,
    `updated_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`      CHAR(36)     NULL,
    `deleted_at`      DATETIME     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_bhyt_exp_tenant_period` (`tenant_id`, `export_period`),
    INDEX `idx_bhyt_exp_status` (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Ho so xuat BHYT (XML 4750)';

CREATE TABLE IF NOT EXISTS `diab_his_bhyt_export_items` (
    `id`               CHAR(36)      NOT NULL DEFAULT (UUID()),
    `export_id`        CHAR(36)      NOT NULL,
    `tenant_id`        INT           NOT NULL,
    `encounter_id`     CHAR(36)      NOT NULL,
    `patient_id`       CHAR(36)      NOT NULL,
    `insurance_no`     VARCHAR(30)   NULL,
    `diagnosis_icd10`  VARCHAR(10)   NULL,
    `service_amount`   DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `bhyt_amount`      DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `patient_amount`   DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `status`           VARCHAR(20)   NOT NULL DEFAULT 'PENDING',
    `reject_reason`    TEXT          NULL,
    `created_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_bhyt_item_export` (`export_id`),
    INDEX `idx_bhyt_item_encounter` (`encounter_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Chi tiet ho so BHYT tung luot kham';

CREATE TABLE IF NOT EXISTS `diab_his_bhyt_reconcile_uploads` (
    `id`           CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`    INT          NOT NULL,
    `period`       VARCHAR(10)  NOT NULL,
    `file_url`     VARCHAR(500) NOT NULL,
    `status`       VARCHAR(20)  NOT NULL DEFAULT 'UPLOADED',
    `total`        INT          NOT NULL DEFAULT 0,
    `approved`     INT          NOT NULL DEFAULT 0,
    `rejected`     INT          NOT NULL DEFAULT 0,
    `note`         TEXT         NULL,
    `created_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`   CHAR(36)     NULL,
    `updated_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`   DATETIME     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_bhyt_recon_tenant` (`tenant_id`, `period`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='File ket qua giam dinh BHYT';

CREATE TABLE IF NOT EXISTS `diab_his_bhyt_reconcile_items` (
    `id`               CHAR(36)      NOT NULL DEFAULT (UUID()),
    `upload_id`        CHAR(36)      NOT NULL,
    `tenant_id`        INT           NOT NULL,
    `encounter_id`     CHAR(36)      NULL,
    `insurance_no`     VARCHAR(30)   NULL,
    `decision`         VARCHAR(20)   NOT NULL COMMENT 'APPROVED|REJECTED|PARTIAL',
    `approved_amount`  DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `rejected_amount`  DECIMAL(15,2) NOT NULL DEFAULT 0.00,
    `reject_code`      VARCHAR(10)   NULL,
    `reject_reason`    TEXT          NULL,
    `created_at`       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_bhyt_ri_upload` (`upload_id`),
    INDEX `idx_bhyt_ri_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Chi tiet ket qua giam dinh tung ho so BHYT';

-- ============================================================
-- CLINIC / EMR EXTENSIONS
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_cli_emr_contents` (
    `id`           CHAR(36)   NOT NULL DEFAULT (UUID()),
    `tenant_id`    INT        NOT NULL,
    `encounter_id` CHAR(36)   NOT NULL,
    `content_json` LONGTEXT   NOT NULL,
    `content_html` LONGTEXT   NULL,
    `template_id`  CHAR(36)   NULL,
    `version`      INT        NOT NULL DEFAULT 1,
    `signed_at`    DATETIME   NULL,
    `signed_by`    CHAR(36)   NULL,
    `created_at`   DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`   CHAR(36)   NULL,
    `updated_at`   DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`   CHAR(36)   NULL,
    `deleted_at`   DATETIME   NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_emr_encounter` (`encounter_id`),
    INDEX `idx_emr_tenant` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Noi dung benh an dien tu (EMR) tung luot kham';

CREATE TABLE IF NOT EXISTS `diab_his_cli_emr_versions` (
    `id`           CHAR(36)   NOT NULL DEFAULT (UUID()),
    `emr_id`       CHAR(36)   NOT NULL,
    `tenant_id`    INT        NOT NULL,
    `version`      INT        NOT NULL,
    `content_json` LONGTEXT   NOT NULL,
    `bytes_size`   INT        NOT NULL DEFAULT 0,
    `saved_at`     DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `saved_by`     CHAR(36)   NULL,
    `is_signed`    TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (`id`),
    INDEX `idx_emrv_emr` (`emr_id`, `version`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Lich su phien ban EMR';

CREATE TABLE IF NOT EXISTS `diab_his_cli_emr_signatures` (
    `id`                  CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`           INT          NOT NULL,
    `emr_id`              CHAR(36)     NOT NULL,
    `encounter_id`        CHAR(36)     NOT NULL,
    `signed_at`           DATETIME     NOT NULL,
    `signed_by`           CHAR(36)     NOT NULL,
    `certificate_serial`  VARCHAR(128) NULL,
    `certificate_subject` TEXT         NULL,
    `signature_algorithm` VARCHAR(50)  NOT NULL DEFAULT 'SHA256withRSA',
    `signature_data`      LONGBLOB     NOT NULL,
    `created_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_emrsig_emr` (`emr_id`),
    INDEX `idx_emrsig_encounter` (`encounter_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Chu ky dien tu tren benh an (PKCS#7)';

CREATE TABLE IF NOT EXISTS `diab_his_cli_emr_templates` (
    `id`           CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`    INT          NULL,
    `name`         VARCHAR(255) NOT NULL,
    `content_json` LONGTEXT     NOT NULL,
    `speciality`   VARCHAR(50)  NOT NULL DEFAULT 'GENERAL',
    `is_system`    TINYINT(1)   NOT NULL DEFAULT 0,
    `created_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`   CHAR(36)     NULL,
    `updated_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`   CHAR(36)     NULL,
    `deleted_at`   DATETIME     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_emr_tpl_tenant` (`tenant_id`),
    INDEX `idx_emr_tpl_spec` (`speciality`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Template benh an EMR (system + custom per tenant)';

CREATE TABLE IF NOT EXISTS `diab_his_cli_diabetes_assessments` (
    `id`                   CHAR(36)      NOT NULL DEFAULT (UUID()),
    `tenant_id`            INT           NOT NULL,
    `encounter_id`         CHAR(36)      NOT NULL,
    `patient_id`           CHAR(36)      NOT NULL,
    `hba1c`                DECIMAL(4,2)  NULL,
    `fasting_glucose`      DECIMAL(6,2)  NULL,
    `postprandial_glucose` DECIMAL(6,2)  NULL,
    `random_glucose`       DECIMAL(6,2)  NULL,
    `egfr`                 DECIMAL(6,2)  NULL,
    `serum_creatinine`     DECIMAL(6,2)  NULL,
    `urine_acr`            DECIMAL(8,2)  NULL,
    `bp_systolic`          INT           NULL,
    `bp_diastolic`         INT           NULL,
    `bmi`                  DECIMAL(4,1)  NULL,
    `waist_circumference`  DECIMAL(5,1)  NULL,
    `diabetes_type`        VARCHAR(20)   NULL,
    `complications_json`   JSON          NULL,
    `treatment_target_json` JSON         NULL,
    `note`                 TEXT          NULL,
    `assessed_at`          DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `assessed_by`          CHAR(36)      NULL,
    `created_at`           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`           CHAR(36)      NULL,
    `updated_at`           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`           CHAR(36)      NULL,
    `deleted_at`           DATETIME      NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_dm_assess_encounter` (`encounter_id`),
    INDEX `idx_dm_assess_patient` (`tenant_id`, `patient_id`, `assessed_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Danh gia chuyen sau benh nhan dai thao duong tung luot kham';

CREATE TABLE IF NOT EXISTS `diab_his_cli_diabetes_templates` (
    `id`                CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`         INT          NULL,
    `name`              VARCHAR(255) NOT NULL,
    `default_values_json` JSON       NULL,
    `checklist_json`    JSON         NULL,
    `is_system`         TINYINT(1)   NOT NULL DEFAULT 0,
    `created_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`        CHAR(36)     NULL,
    `updated_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`        CHAR(36)     NULL,
    `deleted_at`        DATETIME     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_dm_tmpl_tenant` (`tenant_id`, `is_system`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Template phieu kham dai thao duong';

-- ============================================================
-- LAB PARTNERS INTEGRATION
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_int_lab_partners` (
    `id`                   CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`            INT          NOT NULL,
    `code`                 VARCHAR(50)  NOT NULL,
    `name`                 VARCHAR(255) NOT NULL,
    `endpoint_url`         VARCHAR(500) NOT NULL,
    `auth_type`            VARCHAR(30)  NOT NULL DEFAULT 'API_KEY',
    `api_key_encrypted`    BLOB         NULL,
    `bearer_token_encrypted` BLOB       NULL,
    `api_key_masked`       VARCHAR(100) NULL,
    `transport`            VARCHAR(20)  NOT NULL DEFAULT 'REST',
    `supported_tests`      JSON         NULL,
    `status`               VARCHAR(20)  NOT NULL DEFAULT 'INACTIVE',
    `contact_email`        VARCHAR(255) NULL,
    `contact_phone`        VARCHAR(20)  NULL,
    `created_at`           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`           CHAR(36)     NULL,
    `updated_at`           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`           CHAR(36)     NULL,
    `deleted_at`           DATETIME     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_lab_partner_tenant_code` (`tenant_id`, `code`),
    INDEX `idx_lab_partner_status` (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Doi tac xet nghiem ben ngoai';

SET FOREIGN_KEY_CHECKS = 1;
