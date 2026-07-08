-- ============================================================
-- Migration: 9063_recreate_portal_accounts_char36
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-08
-- Story refs: Patient Portal (app benh nhan) — Phase 0
-- Mo ta:
--   Portal benh nhan co HAI dinh nghia trung bang diab_his_pat_portal_accounts:
--     - 0017: id INT, patient_id INT, phone_e164 (+ bang _portal_tokens)
--     - 0051: id BINARY(16), patient_id BINARY(16), phone (+ _portal_otp_log, _portal_sessions)
--   Ca hai deu CREATE TABLE IF NOT EXISTS nen bang nao chay truoc "thang".
--   Ngoai ra id he thong benh nhan (diab_his_pat_patients.id) la CHAR(36), KHONG
--   phai BINARY(16) -> code portal dung UUID_TO_BIN/BIN_TO_UUID + query bang view
--   "his_patient" (khong ton tai) nen PORTAL CHUA TUNG CHAY duoc (0 row).
--   -> Chuan hoa 3 bang portal ve id CHAR(36) khop pat_patients, them cot dang
--   nhap bang PIN + ma kich hoat tai quay (khong phu thuoc SMS o giai doan MVP).
--
-- An toan: chi DROP + recreate khi bang RONG (row_cnt = 0) hoac chua ton tai.
--   Neu co du lieu -> log canh bao, khong dong cham (xu ly tay).
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Doi ten ban 0017 (INT) neu con ton tai theo cau truc cu (co cot phone_e164)
DROP PROCEDURE IF EXISTS _rename_legacy_portal_0017;
DELIMITER $$
CREATE PROCEDURE _rename_legacy_portal_0017()
BEGIN
    DECLARE has_e164 INT DEFAULT 0;
    DECLARE row_cnt INT DEFAULT 0;
    SELECT COUNT(*) INTO has_e164 FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pat_portal_accounts'
       AND COLUMN_NAME = 'phone_e164';
    IF has_e164 = 1 THEN
        SELECT COUNT(*) INTO row_cnt FROM diab_his_pat_portal_accounts;
        IF row_cnt = 0 THEN
            DROP TABLE IF EXISTS diab_his_pat_portal_tokens;
            DROP TABLE diab_his_pat_portal_accounts;
        END IF;
    END IF;
END$$
DELIMITER ;
CALL _rename_legacy_portal_0017();
DROP PROCEDURE IF EXISTS _rename_legacy_portal_0017;

-- Recreate diab_his_pat_portal_accounts ve CHAR(36) neu thieu cot pin_hash HOAC sai kieu id
DROP PROCEDURE IF EXISTS _recreate_portal_accounts_9063;
DELIMITER $$
CREATE PROCEDURE _recreate_portal_accounts_9063()
BEGIN
    DECLARE id_type VARCHAR(64) DEFAULT '';
    DECLARE has_pin INT DEFAULT 0;
    DECLARE row_cnt INT DEFAULT 0;
    DECLARE tbl_exists INT DEFAULT 0;

    SELECT COUNT(*) INTO tbl_exists FROM information_schema.TABLES
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pat_portal_accounts';

    IF tbl_exists = 1 THEN
        SELECT DATA_TYPE INTO id_type FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pat_portal_accounts'
           AND COLUMN_NAME = 'id';
        SELECT COUNT(*) INTO has_pin FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pat_portal_accounts'
           AND COLUMN_NAME = 'pin_hash';
        SELECT COUNT(*) INTO row_cnt FROM diab_his_pat_portal_accounts;
    END IF;

    -- Recreate khi: chua co bang, HOAC (sai kieu id / thieu pin_hash) VA bang rong
    IF tbl_exists = 0 OR ((id_type <> 'char' OR has_pin = 0) AND row_cnt = 0) THEN
        DROP TABLE IF EXISTS diab_his_pat_portal_accounts;
        CREATE TABLE diab_his_pat_portal_accounts (
            id                      CHAR(36)     NOT NULL,
            tenant_id               INT          NOT NULL,
            patient_id              CHAR(36)     NOT NULL COMMENT 'FK -> diab_his_pat_patients.id',
            phone                   VARCHAR(20)  NOT NULL COMMENT 'So dien thoai E.164',
            email                   VARCHAR(100) NULL     COMMENT 'Email nhan OTP quen PIN + fallback thong bao',
            pin_hash                VARCHAR(100) NULL     COMMENT 'BCrypt hash PIN 6 so (NULL = chua kich hoat)',
            activation_code_hash    VARCHAR(100) NULL     COMMENT 'BCrypt hash ma kich hoat 8 ky tu le tan cap',
            activation_expires_at   DATETIME     NULL     COMMENT 'Han dung ma kich hoat (72h)',
            activated_at            DATETIME     NULL     COMMENT 'Thoi diem dat PIN thanh cong',
            notify_prefs_json       JSON         NULL     COMMENT 'Tuy chon kenh thong bao {push:bool,email:bool}',
            failed_attempts         INT          NOT NULL DEFAULT 0,
            locked_until            DATETIME     NULL,
            last_otp_sent_at        DATETIME     NULL,
            created_at              DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at              DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            PRIMARY KEY (id),
            UNIQUE KEY ux_portal_phone_tenant (tenant_id, phone),
            INDEX idx_portal_patient (tenant_id, patient_id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
          COMMENT='Tai khoan portal benh nhan (dang nhap PIN + ma kich hoat tai quay)';
    ELSE
        -- Bang da dung shape CHAR(36) tu truoc: chi bo sung cot con thieu (idempotent)
        CALL add_col_if_missing('diab_his_pat_portal_accounts', 'email',                 'VARCHAR(100) NULL');
        CALL add_col_if_missing('diab_his_pat_portal_accounts', 'pin_hash',              'VARCHAR(100) NULL');
        CALL add_col_if_missing('diab_his_pat_portal_accounts', 'activation_code_hash',  'VARCHAR(100) NULL');
        CALL add_col_if_missing('diab_his_pat_portal_accounts', 'activation_expires_at', 'DATETIME NULL');
        CALL add_col_if_missing('diab_his_pat_portal_accounts', 'activated_at',          'DATETIME NULL');
        CALL add_col_if_missing('diab_his_pat_portal_accounts', 'notify_prefs_json',     'JSON NULL');
    END IF;
END$$
DELIMITER ;
CALL _recreate_portal_accounts_9063();
DROP PROCEDURE IF EXISTS _recreate_portal_accounts_9063;

-- diab_his_pat_portal_otp_log — CHAR(36) (dung cho OTP quen PIN qua email)
DROP PROCEDURE IF EXISTS _recreate_portal_otp_log_9063;
DELIMITER $$
CREATE PROCEDURE _recreate_portal_otp_log_9063()
BEGIN
    DECLARE id_type VARCHAR(64) DEFAULT '';
    DECLARE row_cnt INT DEFAULT 0;
    DECLARE tbl_exists INT DEFAULT 0;

    SELECT COUNT(*) INTO tbl_exists FROM information_schema.TABLES
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pat_portal_otp_log';
    IF tbl_exists = 1 THEN
        SELECT DATA_TYPE INTO id_type FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pat_portal_otp_log'
           AND COLUMN_NAME = 'id';
        SELECT COUNT(*) INTO row_cnt FROM diab_his_pat_portal_otp_log;
    END IF;

    IF tbl_exists = 0 OR (id_type <> 'char' AND row_cnt = 0) THEN
        DROP TABLE IF EXISTS diab_his_pat_portal_otp_log;
        CREATE TABLE diab_his_pat_portal_otp_log (
            id          CHAR(36)     NOT NULL,
            tenant_id   INT          NOT NULL,
            phone       VARCHAR(20)  NOT NULL,
            otp_hash    VARCHAR(100) NOT NULL COMMENT 'BCrypt hash OTP 6 so',
            purpose     ENUM('LOGIN','LOOKUP','RESET_PIN') NOT NULL DEFAULT 'LOGIN',
            sent_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
            verified_at DATETIME     NULL,
            expires_at  DATETIME     NOT NULL,
            attempts    INT          NOT NULL DEFAULT 0,
            PRIMARY KEY (id),
            INDEX idx_otp_phone (tenant_id, phone, sent_at)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
    END IF;
END$$
DELIMITER ;
CALL _recreate_portal_otp_log_9063();
DROP PROCEDURE IF EXISTS _recreate_portal_otp_log_9063;

-- diab_his_pat_portal_sessions — CHAR(36)
DROP PROCEDURE IF EXISTS _recreate_portal_sessions_9063;
DELIMITER $$
CREATE PROCEDURE _recreate_portal_sessions_9063()
BEGIN
    DECLARE pid_type VARCHAR(64) DEFAULT '';
    DECLARE row_cnt INT DEFAULT 0;
    DECLARE tbl_exists INT DEFAULT 0;

    SELECT COUNT(*) INTO tbl_exists FROM information_schema.TABLES
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pat_portal_sessions';
    IF tbl_exists = 1 THEN
        SELECT DATA_TYPE INTO pid_type FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_pat_portal_sessions'
           AND COLUMN_NAME = 'patient_id';
        SELECT COUNT(*) INTO row_cnt FROM diab_his_pat_portal_sessions;
    END IF;

    IF tbl_exists = 0 OR (pid_type <> 'char' AND row_cnt = 0) THEN
        DROP TABLE IF EXISTS diab_his_pat_portal_sessions;
        CREATE TABLE diab_his_pat_portal_sessions (
            id          CHAR(36)     NOT NULL,
            tenant_id   INT          NOT NULL,
            patient_id  CHAR(36)     NOT NULL,
            jti         VARCHAR(100) NOT NULL COMMENT 'JWT ID de thu hoi',
            issued_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
            expires_at  DATETIME     NOT NULL,
            revoked_at  DATETIME     NULL,
            PRIMARY KEY (id),
            UNIQUE KEY ux_session_jti (jti),
            INDEX idx_session_patient (tenant_id, patient_id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
    END IF;
END$$
DELIMITER ;
CALL _recreate_portal_sessions_9063();
DROP PROCEDURE IF EXISTS _recreate_portal_sessions_9063;
