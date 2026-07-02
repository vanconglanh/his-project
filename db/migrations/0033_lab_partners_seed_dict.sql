-- ============================================================
-- Migration: 0033_lab_partners_seed_dict
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: US-SUNS-13, US-SUNS-14
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- ─────────────────────────────────────────────────
-- cli_lab_partners: danh sach doi tac XN (thay the diab_his_int_lab_partners)
-- Dung CHAR(36) UUID thay INT auto-increment de dong bo voi cac bang moi
-- ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `cli_lab_partners` (
    `id`                        CHAR(36)        NOT NULL DEFAULT (UUID()),
    `tenant_id`                 INT             NOT NULL,
    `code`                      VARCHAR(50)     NOT NULL        COMMENT 'Ma dinh danh ngan (MEDLATEC, DIAG...)',
    `name`                      VARCHAR(255)    NOT NULL,
    `endpoint_url`              VARCHAR(500)    NOT NULL,
    `auth_type`                 ENUM('NONE','API_KEY','BEARER')
                                                NOT NULL        DEFAULT 'API_KEY',
    `api_key_encrypted`         VARBINARY(512)  NULL            COMMENT 'AES-256-GCM encrypted',
    `bearer_token_encrypted`    VARBINARY(1024) NULL            COMMENT 'AES-256-GCM encrypted',
    `api_key_masked`            VARCHAR(32)     NULL            COMMENT 'sk_***XXXX hien thi UI',
    `transport`                 ENUM('REST','HL7_MLLP')
                                                NOT NULL        DEFAULT 'REST',
    `supported_tests`           JSON            NULL            COMMENT '["GLU","HBA1C"]',
    `status`                    ENUM('ACTIVE','INACTIVE')
                                                NOT NULL        DEFAULT 'INACTIVE',
    `contact_email`             VARCHAR(255)    NULL,
    `contact_phone`             VARCHAR(30)     NULL,
    `created_at`                DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP,
    `created_by`                CHAR(36)        NULL,
    `updated_at`                DATETIME        NOT NULL        DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`                CHAR(36)        NULL,
    `deleted_at`                DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_partner_tenant_code` (`tenant_id`, `code`),
    INDEX `idx_partner_tenant_status`   (`tenant_id`, `status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Doi tac xet nghiem ben ngoai (Medlatec, Diag...)';

-- ─────────────────────────────────────────────────
-- cli_lab_outbound: theo doi lenh gui ra partner
-- ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `cli_lab_outbound` (
    `id`                CHAR(36)    NOT NULL DEFAULT (UUID()),
    `tenant_id`         INT         NOT NULL,
    `lab_order_id`      CHAR(36)    NOT NULL    COMMENT 'FK -> diab_his_cli_lab_orders.id',
    `lab_partner_id`    CHAR(36)    NOT NULL    COMMENT 'FK -> cli_lab_partners.id',
    `external_order_id` VARCHAR(100) NULL       COMMENT 'Ma don XN phia partner cap sau khi nhan',
    `payload_json`      JSON        NULL        COMMENT 'Noi dung request JSON gui doi tac',
    `status`            ENUM('PENDING','SENT','ACKED','FAILED')
                                    NOT NULL    DEFAULT 'PENDING',
    `retry_count`       INT         NOT NULL    DEFAULT 0,
    `error_message`     TEXT        NULL,
    `sent_at`           DATETIME    NULL,
    `acked_at`          DATETIME    NULL,
    `created_at`        DATETIME    NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `created_by`        CHAR(36)    NULL,
    `updated_at`        DATETIME    NOT NULL    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`        CHAR(36)    NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_outbound_status`     (`tenant_id`, `status`, `created_at`),
    INDEX `idx_outbound_partner`    (`tenant_id`, `lab_partner_id`),
    INDEX `idx_outbound_order`      (`tenant_id`, `lab_order_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Log lich su gui chi dinh XN ra doi tac ngoai';

-- ─────────────────────────────────────────────────
-- cli_lab_inbound: nhan ket qua tu partner qua webhook
-- ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `cli_lab_inbound` (
    `id`                        CHAR(36)        NOT NULL DEFAULT (UUID()),
    `tenant_id`                 INT             NOT NULL,
    `lab_partner_id`            CHAR(36)        NOT NULL    COMMENT 'FK -> cli_lab_partners.id',
    `external_result_id`        VARCHAR(100)    NOT NULL    COMMENT 'ID ket qua phia doi tac (idempotent key)',
    `outbound_id`               CHAR(36)        NULL        COMMENT 'FK -> cli_lab_outbound.id (neu khop duoc)',
    `payload_json`              JSON            NULL        COMMENT 'Raw JSON nhan tu partner',
    `raw_hl7_message`           MEDIUMTEXT      NULL        COMMENT 'HL7 ORU^R01 message neu co',
    `headers`                   JSON            NULL        COMMENT 'HTTP headers cua webhook request',
    `status`                    ENUM('RECEIVED','PROCESSED','FAILED')
                                                NOT NULL    DEFAULT 'RECEIVED',
    `received_at`               DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `processed_at`              DATETIME        NULL,
    `processed_result_count`    INT             NOT NULL    DEFAULT 0,
    `error_message`             TEXT            NULL,
    `created_at`                DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP,
    `created_by`                CHAR(36)        NULL,
    `updated_at`                DATETIME        NOT NULL    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_inbound_idempotent` (`lab_partner_id`, `external_result_id`),
    INDEX `idx_inbound_status`      (`tenant_id`, `status`, `received_at`),
    INDEX `idx_inbound_partner`     (`tenant_id`, `lab_partner_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Log ket qua XN nhan tu doi tac qua webhook (inbound)';

-- ─────────────────────────────────────────────────
-- ADD cols vao diab_his_dict_lab_tests (reference range)
-- ─────────────────────────────────────────────────
CALL add_col_if_missing('diab_his_dict_lab_tests', 'reference_range_low',  'DECIMAL(18,4) NULL');
CALL add_col_if_missing('diab_his_dict_lab_tests', 'reference_range_high', 'DECIMAL(18,4) NULL');
CALL add_col_if_missing('diab_his_dict_lab_tests', 'unit',                 'VARCHAR(32) NULL');

-- ─────────────────────────────────────────────────
-- Seed 2 doi tac mau (tenant_id=NULL = default template, admin tung tenant bat)
-- ─────────────────────────────────────────────────
INSERT IGNORE INTO `cli_lab_partners`
    (`id`, `tenant_id`, `code`, `name`, `endpoint_url`, `auth_type`, `transport`,
     `supported_tests`, `status`, `contact_email`)
VALUES
    (UUID(), 0, 'MEDLATEC', 'Medlatec Lab',
     'https://api.medlatec.vn/his/v1', 'API_KEY', 'REST',
     JSON_ARRAY('GLU','HBA1C','CHOL','LDL','HDL','TG'),
     'INACTIVE', 'integration@medlatec.vn'),
    (UUID(), 0, 'DIAG', 'Diag Lab',
     'https://api.diag.vn/partner/v1', 'API_KEY', 'REST',
     JSON_ARRAY('CBC','HBA1C'),
     'INACTIVE', 'partner@diag.vn');
