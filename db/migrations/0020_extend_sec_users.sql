-- ============================================================
-- Migration: 0020_extend_sec_users
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-U01 US-U02 US-U05 US-U06 (Sprint 1 EPIC 1)
-- Idempotent: YES (dung add_col_if_missing + add_index_if_missing)
-- ============================================================
SET NAMES utf8mb4;

-- Goi stored procedure da tao o migration 0000_helpers
CALL add_col_if_missing('sec_users', 'avatar_url',
    'VARCHAR(500) NULL COMMENT ''Avatar URL cua nguoi dung''');

CALL add_col_if_missing('sec_users', 'two_fa_secret',
    'TEXT NULL COMMENT ''TOTP secret ma hoa AES-256-GCM''');

CALL add_col_if_missing('sec_users', 'two_fa_enabled',
    'TINYINT(1) NOT NULL DEFAULT 0 COMMENT ''2FA da kich hoat''');

CALL add_col_if_missing('sec_users', 'two_fa_recovery_codes',
    'JSON NULL COMMENT ''Recovery codes ma hoa AES-256-GCM''');

CALL add_col_if_missing('sec_users', 'invite_token',
    'CHAR(64) NULL COMMENT ''Token moi nguoi dung (hex 32 bytes)''');

CALL add_col_if_missing('sec_users', 'invite_token_expires_at',
    'DATETIME NULL COMMENT ''Thoi diem het han invite token''');

CALL add_col_if_missing('sec_users', 'user_status',
    "VARCHAR(20) NOT NULL DEFAULT 'PENDING' COMMENT 'Trang thai: PENDING/ACTIVE/LOCKED/DISABLED'");

CALL add_col_if_missing('sec_users', 'last_login_at',
    'DATETIME NULL COMMENT ''Thoi diem dang nhap cuoi''');

CALL add_col_if_missing('sec_users', 'failed_login_count',
    'INT NOT NULL DEFAULT 0 COMMENT ''So lan dang nhap that bai''');

CALL add_col_if_missing('sec_users', 'locked_until',
    'DATETIME NULL COMMENT ''Khoa tai khoan den thoi diem nay''');

CALL add_col_if_missing('sec_users', 'tenant_id',
    'CHAR(36) NULL COMMENT ''Tenant UUID (NULL cho SUPER_ADMIN)''');

CALL add_col_if_missing('sec_users', 'password_reset_token',
    'CHAR(64) NULL COMMENT ''Token dat lai mat khau''');

CALL add_col_if_missing('sec_users', 'password_reset_expires_at',
    'DATETIME NULL COMMENT ''Het han token dat lai mat khau''');

-- Index cho tenant_id + user_status
CALL add_index_if_missing('sec_users', 'IDX_SEC_USERS_TENANT_STATUS', '(tenant_id, user_status)');

-- Index cho invite_token (partial index khong support MySQL, dung unique index thay)
CALL add_index_if_missing('sec_users', 'IDX_SEC_USERS_INVITE_TOKEN', '(invite_token)');

-- Index cho password_reset_token
CALL add_index_if_missing('sec_users', 'IDX_SEC_USERS_RESET_TOKEN', '(password_reset_token)');
