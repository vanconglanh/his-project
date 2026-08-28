-- ============================================================
-- Migration: 9098_create_cgm_device_integration
-- Muc dich: FR-711 [P2] Ket noi thiet bi do duong huyet/CGM qua API
--   - Bang lien ket tai khoan benh nhan HIS <-> nen tang CGM (Dexcom/LibreView/...)
--   - Bang readings dong bo tu CGM (KHONG dung chung diab_his_cli_vital_signs vi
--     bang do BAT BUOC encounter_id — CGM la du lieu ngoai lan kham, do lien tuc)
-- Phu thuoc: 0000_helpers.sql (add_col_if_missing, add_index_if_missing)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- 1. Lien ket tai khoan benh nhan HIS <-> tai khoan tren nen tang CGM
--    access_token/refresh_token luu MA HOA AES-256-GCM (khong luu plaintext)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_dev_cgm_links` (
    `id`                    CHAR(36)        NOT NULL                COMMENT 'UUID khoa chinh',
    `tenant_id`             INT             NOT NULL                COMMENT 'ID phong kham (tenant)',
    `patient_id`            CHAR(36)        NOT NULL                COMMENT 'FK -> diab_his_pat_patients.id',
    `provider`              VARCHAR(30)     NOT NULL                COMMENT 'Dexcom | LibreView | FreeStyle | ...',
    `external_account_id`   VARCHAR(100)    NOT NULL                COMMENT 'ID/username tai khoan benh nhan ben nen tang CGM',
    `access_token_enc`      VARBINARY(2048) NULL                    COMMENT 'NHAY CAM - AES-256-GCM: OAuth2 access_token',
    `refresh_token_enc`     VARBINARY(2048) NULL                    COMMENT 'NHAY CAM - AES-256-GCM: OAuth2 refresh_token',
    `token_expires_at`      DATETIME        NULL                    COMMENT 'Thoi diem access_token het han',
    `status`                VARCHAR(20)     NOT NULL DEFAULT 'ACTIVE' COMMENT 'ACTIVE|REVOKED|EXPIRED|ERROR',
    `last_sync_error`       VARCHAR(500)    NULL,
    `linked_at`             DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `last_synced_at`        DATETIME        NULL,
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)        NULL,
    `updated_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)        NULL,
    `deleted_at`            DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_cgm_link_tenant_patient_provider` (`tenant_id`, `patient_id`, `provider`),
    KEY `idx_cgm_link_tenant_status` (`tenant_id`, `status`),
    KEY `idx_cgm_link_sync` (`status`, `last_synced_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT 'FR-711: Lien ket tai khoan benh nhan HIS <-> nen tang CGM (OAuth2)';

-- ------------------------------------------------------------
-- 2. Ket qua do CGM dong bo ve (day tho, truoc khi ghi vao chi so lam sang)
--    idempotency: (tenant, patient, provider, device_id, reading_at) UNIQUE
--    chong insert trung khi job chay lai / API tra ve trung khoang thoi gian.
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_dev_cgm_readings` (
    `id`                    CHAR(36)        NOT NULL,
    `tenant_id`             INT             NOT NULL,
    `patient_id`            CHAR(36)        NOT NULL                COMMENT 'FK -> diab_his_pat_patients.id',
    `cgm_link_id`           CHAR(36)        NOT NULL                COMMENT 'FK -> diab_his_dev_cgm_links.id',
    `provider`              VARCHAR(30)     NOT NULL,
    `device_id`             VARCHAR(100)    NULL                    COMMENT 'ID thiet bi CGM (transmitter/sensor) do nen tang tra ve',
    `reading_at`            DATETIME        NOT NULL                COMMENT 'Thoi diem do (UTC, theo timestamp cua thiet bi)',
    `glucose_value_mg_dl`   DECIMAL(6,2)    NOT NULL,
    `trend_direction`       VARCHAR(20)     NULL                    COMMENT 'flat|rising|rising_rapidly|falling|falling_rapidly|... (chuan hoa tu provider)',
    `synced_to_vital_signs` TINYINT(1)      NOT NULL DEFAULT 0      COMMENT 'Da doi chieu/ghi nhan vao chi so lam sang chua',
    `raw_payload`           JSON            NULL                    COMMENT 'Ban ghi tho tu API CGM phuc vu doi soat',
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_cgm_reading_idem` (`tenant_id`, `patient_id`, `provider`, `device_id`, `reading_at`),
    KEY `idx_cgm_reading_tenant_patient_time` (`tenant_id`, `patient_id`, `reading_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT 'FR-711: Ket qua do duong huyet lien tuc (CGM) dong bo tu nen tang thiet bi';
