-- Migration 9014: Fix schema cho dtqg_submissions, dtqg_credentials, tao bang api_partners
-- Khac phuc 500 cho GET /api/v1/dtqg/submissions, dtqg/credentials, api-partners

SET FOREIGN_KEY_CHECKS = 0;

-- ─── 1. diab_his_int_dtqg_submissions: them cac cot thieu ─────────────────────
DROP PROCEDURE IF EXISTS _add_col;
CREATE PROCEDURE _add_col(tbl VARCHAR(200), col VARCHAR(100), def TEXT)
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = tbl AND COLUMN_NAME = col
  ) THEN
    SET @sql = CONCAT('ALTER TABLE `', tbl, '` ADD COLUMN `', col, '` ', def);
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
  END IF;
END;

CALL _add_col('diab_his_int_dtqg_submissions', 'ma_don_thuoc',   'VARCHAR(50) NULL');
CALL _add_col('diab_his_int_dtqg_submissions', 'qr_payload',     'TEXT NULL');
CALL _add_col('diab_his_int_dtqg_submissions', 'error_code',     'VARCHAR(50) NULL');
CALL _add_col('diab_his_int_dtqg_submissions', 'error_message',  'TEXT NULL');
CALL _add_col('diab_his_int_dtqg_submissions', 'accepted_at',    'DATETIME NULL');
CALL _add_col('diab_his_int_dtqg_submissions', 'submitted_at',   'DATETIME NULL');
CALL _add_col('diab_his_int_dtqg_submissions', 'retry_count',    'INT NOT NULL DEFAULT 0');
CALL _add_col('diab_his_int_dtqg_submissions', 'last_retry_at',  'DATETIME NULL');
CALL _add_col('diab_his_int_dtqg_submissions', 'deleted_at',     'DATETIME NULL');

-- ─── 2. diab_his_int_dtqg_credentials: them cac cot thieu ────────────────────
CALL _add_col('diab_his_int_dtqg_credentials', 'partner_code',   'VARCHAR(50) NULL');
CALL _add_col('diab_his_int_dtqg_credentials', 'last_tested_at', 'DATETIME NULL');
CALL _add_col('diab_his_int_dtqg_credentials', 'last_test_ok',   'TINYINT(1) NOT NULL DEFAULT 0');
CALL _add_col('diab_his_int_dtqg_credentials', 'deleted_at',     'DATETIME NULL');

DROP PROCEDURE IF EXISTS _add_col;

-- ─── 3. Tao bang diab_his_api_partners neu chua co ───────────────────────────
CREATE TABLE IF NOT EXISTS diab_his_api_partners (
    id            BINARY(16)   NOT NULL PRIMARY KEY DEFAULT (UUID_TO_BIN(UUID())),
    tenant_id     INT          NOT NULL,
    name          VARCHAR(255) NOT NULL,
    contact_email VARCHAR(255) NULL,
    api_key_hash  VARCHAR(64)  NOT NULL,
    api_key_prefix VARCHAR(30) NOT NULL,
    scopes        JSON         NULL,
    rate_limit_per_min INT     NOT NULL DEFAULT 100,
    daily_quota   INT          NOT NULL DEFAULT 10000,
    status        VARCHAR(20)  NOT NULL DEFAULT 'ACTIVE',
    expires_at    DATETIME     NULL,
    ip_whitelist  JSON         NULL,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    deleted_at    DATETIME     NULL,
    INDEX idx_api_partners_tenant (tenant_id),
    INDEX idx_api_partners_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── 4. Tao bang diab_his_api_request_logs neu chua co ───────────────────────
CREATE TABLE IF NOT EXISTS diab_his_api_request_logs (
    id           BINARY(16)   NOT NULL PRIMARY KEY DEFAULT (UUID_TO_BIN(UUID())),
    partner_id   BINARY(16)   NOT NULL,
    tenant_id    INT          NOT NULL,
    method       VARCHAR(10)  NOT NULL,
    path         VARCHAR(500) NOT NULL,
    status_code  INT          NOT NULL,
    duration_ms  INT          NOT NULL,
    ip           VARCHAR(45)  NULL,
    called_at    DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    error_code   VARCHAR(100) NULL,
    INDEX idx_api_req_partner (partner_id),
    INDEX idx_api_req_tenant (tenant_id),
    INDEX idx_api_req_called_at (called_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
