-- ============================================================
-- Migration: 9089_create_sec_digital_signatures
-- Muc dich (FR-302/FR-402 P0 - Ky so benh an / don thuoc):
--   Bang diab_his_sec_digital_signatures luu lai day du record ky so
--   theo kien truc adapter IDigitalSignatureProvider (Mock hoac CA that
--   nhu VNPT SmartCA/Viettel-CA), phuc vu audit/thanh kiem tra (muc 5.1 SRS).
--   Khong phu thuoc nha cung cap CA cu the - cot ca_provider phan biet
--   Mock/VnptSmartCa/ViettelCa/...
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_sec_digital_signatures` (
    `id`                   CHAR(36)     NOT NULL,
    `tenant_id`            INT          NOT NULL,
    `user_id`              CHAR(36)     NOT NULL COMMENT 'Nguoi ky (bac si/duoc si...)',
    `document_type`        VARCHAR(50)  NOT NULL COMMENT 'EMR | PRESCRIPTION | ...',
    `document_id`          CHAR(36)     NOT NULL COMMENT 'FK toi encounter_id/prescription_id tuy document_type',
    `ca_provider`          VARCHAR(50)  NOT NULL COMMENT 'Mock | VnptSmartCa | ViettelCa | ...',
    `certificate_serial`   VARCHAR(100) NULL COMMENT 'So serial chung thu so dung de ky',
    `certificate_subject`  VARCHAR(255) NULL,
    `algorithm`             VARCHAR(50)  NULL COMMENT 'VD: SHA256withRSA',
    `document_hash`        VARCHAR(128) NOT NULL COMMENT 'SHA-256 (hex) cua noi dung tai lieu tai thoi diem ky',
    `signature_data`       MEDIUMTEXT   NULL COMMENT 'Chu ky so (base64) - co the luu rieng o object storage neu qua lon',
    `is_valid`             TINYINT(1)   NOT NULL DEFAULT 1,
    `signed_at`            DATETIME     NOT NULL,
    `verified_at`          DATETIME     NULL COMMENT 'Lan xac thuc lai gan nhat (audit/thanh kiem tra)',
    `error_message`        VARCHAR(500) NULL,
    `created_at`           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`           CHAR(36)     NULL,
    `updated_at`           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`           CHAR(36)     NULL,
    `deleted_at`           DATETIME     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_digisig_tenant`   (`tenant_id`, `deleted_at`),
    INDEX `idx_digisig_document` (`tenant_id`, `document_type`, `document_id`),
    INDEX `idx_digisig_user`     (`tenant_id`, `user_id`),
    INDEX `idx_digisig_serial`   (`certificate_serial`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Record ky so bao mat cho EMR/don thuoc (FR-302/FR-402) - doc lap nha cung cap CA';
