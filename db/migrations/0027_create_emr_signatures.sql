-- ============================================================
-- Migration: 0027_create_emr_signatures
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: US-E08, US-E09, US-E11, US-E13, US-E14
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- EMR content (1 per encounter)
CREATE TABLE IF NOT EXISTS diab_his_cli_emr_content (
    id           CHAR(36)  NOT NULL DEFAULT (UUID()),
    tenant_id    INT       NOT NULL,
    encounter_id CHAR(36)  NOT NULL,
    content_json LONGTEXT  NOT NULL COMMENT 'Tiptap JSON',
    content_html LONGTEXT  NULL     COMMENT 'Rendered HTML cache',
    template_id  CHAR(36)  NULL,
    version      INT       NOT NULL DEFAULT 1,
    signed_at    DATETIME  NULL,
    signed_by    CHAR(36)  NULL,
    created_at   DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by   CHAR(36)  NULL,
    updated_at   DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    updated_by   CHAR(36)  NULL,
    deleted_at   DATETIME  NULL,
    PRIMARY KEY (id),
    UNIQUE KEY  uk_emr_encounter (encounter_id),
    INDEX idx_emr_tenant (tenant_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Electronic Medical Record content per encounter';

-- EMR versions — snapshot on each save
CREATE TABLE IF NOT EXISTS diab_his_cli_emr_versions (
    id           CHAR(36)  NOT NULL DEFAULT (UUID()),
    emr_id       CHAR(36)  NOT NULL,
    tenant_id    INT       NOT NULL,
    version      INT       NOT NULL,
    content_json LONGTEXT  NOT NULL,
    bytes_size   INT       NOT NULL DEFAULT 0,
    saved_at     DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    saved_by     CHAR(36)  NULL,
    is_signed    TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    INDEX idx_emrv_emr (emr_id, version)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Snapshot history of EMR saves';

-- EMR digital signatures
CREATE TABLE IF NOT EXISTS diab_his_cli_emr_signatures (
    id                   CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id            INT          NOT NULL,
    emr_id               CHAR(36)     NOT NULL,
    encounter_id         CHAR(36)     NOT NULL,
    signed_at            DATETIME     NOT NULL,
    signed_by            CHAR(36)     NOT NULL,
    certificate_serial   VARCHAR(128) NULL,
    certificate_subject  TEXT         NULL,
    signature_algorithm  VARCHAR(50)  NOT NULL DEFAULT 'SHA256withRSA',
    signature_data       LONGBLOB     NOT NULL COMMENT 'PKCS#7 detached signature bytes',
    created_at           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_emrsig_emr     (emr_id),
    INDEX idx_emrsig_encounter (encounter_id),
    INDEX idx_emrsig_tenant  (tenant_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Digital signatures for EMR (PKCS#7 detached)';
