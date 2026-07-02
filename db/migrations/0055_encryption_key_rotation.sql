-- Migration 0055: Encryption Key Rotation Table
-- Sprint 12 EPIC 10 Hardening
-- MySQL 8

CREATE TABLE IF NOT EXISTS `diab_his_sec_encryption_keys` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`             INT NULL COMMENT 'NULL = global key, INT = tenant-specific key',
    `key_version`           INT NOT NULL DEFAULT 1,
    `key_purpose`           ENUM('PII','BHYT','OAUTH_TOKEN','VAPID','OTHER') NOT NULL,
    `key_material_encrypted` VARBINARY(512) NOT NULL COMMENT 'Encrypted with master key (KEK)',
    `algorithm`             VARCHAR(20) NOT NULL DEFAULT 'AES-256-GCM',
    `is_active`             TINYINT(1) NOT NULL DEFAULT 1,
    `rotated_at`            DATETIME NULL COMMENT 'Thoi diem key bi rotate (deactivated)',
    `created_at`            DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_enc_keys_lookup` (`tenant_id`, `key_purpose`, `is_active`),
    INDEX `idx_enc_keys_version` (`tenant_id`, `key_purpose`, `key_version`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Quan ly encryption keys theo phien ban - Sprint 12';
