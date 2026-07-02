-- ============================================================
-- Migration: 0017_create_patient_portal
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-22, US-EMR-PORTAL-01, US-EMR-PORTAL-02, US-EMR-PORTAL-03
-- Idempotent: YES
-- Ghi chú: Portal bệnh nhân xác thực qua OTP SMS (không cần password).
--   Mỗi tenant có namespace riêng (tenant_id, phone_e164 = unique).
--   OTP hash SHA-256 trước khi lưu, không lưu plaintext.
-- ============================================================
SET NAMES utf8mb4;

-- Tài khoản portal của bệnh nhân
CREATE TABLE IF NOT EXISTS `diab_his_pat_portal_accounts` (
    `id`                INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`         INT          NULL                                  COMMENT 'ID tenant',
    `patient_id`        INT          NOT NULL                              COMMENT 'FK → pat_patients.ID',
    `phone_e164`        VARCHAR(20)  NOT NULL                              COMMENT 'Số điện thoại định dạng E.164 (vd: +84901234567)',
    `phone_verified_at` DATETIME     NULL                                  COMMENT 'Thời điểm xác minh số điện thoại (NULL = chưa xác minh)',
    `status`            ENUM('ACTIVE','LOCKED','DISABLED')
                                     NOT NULL DEFAULT 'ACTIVE'             COMMENT 'Trạng thái tài khoản portal',
    `last_login_at`     DATETIME     NULL                                  COMMENT 'Thời điểm đăng nhập gần nhất',
    `created_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`        INT          NULL                                  COMMENT 'ID người tạo',
    `updated_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                         ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`        INT          NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`        DATETIME     NULL                                  COMMENT 'Thời điểm xóa mềm',

    UNIQUE KEY `uq_portal_tenant_phone`   (`tenant_id`, `phone_e164`),
    INDEX `idx_portal_patient`            (`patient_id`),
    INDEX `idx_portal_status`             (`status`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Tài khoản portal bệnh nhân (xác thực OTP SMS, không password)';

-- Token OTP và session của portal bệnh nhân
CREATE TABLE IF NOT EXISTS `diab_his_pat_portal_tokens` (
    `id`                  INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `portal_account_id`   INT          NOT NULL                              COMMENT 'FK → diab_his_pat_portal_accounts.id',
    `purpose`             ENUM('LOGIN_OTP','SESSION')
                                       NOT NULL                              COMMENT 'Mục đích token: OTP đăng nhập hoặc session sau xác thực',
    `otp_code_hash`       CHAR(64)     NULL                                  COMMENT 'SHA-256 hash của mã OTP 6 chữ số (không lưu plaintext)',
    `session_token_hash`  CHAR(64)     NULL                                  COMMENT 'SHA-256 hash của session token (không lưu plaintext)',
    `otp_expires_at`      DATETIME     NULL                                  COMMENT 'Thời điểm hết hạn OTP (thường 5 phút)',
    `attempt_count`       INT          NOT NULL DEFAULT 0                    COMMENT 'Số lần nhập OTP sai (khoá sau 5 lần)',
    `used_at`             DATETIME     NULL                                  COMMENT 'Thời điểm đã sử dụng (NULL = chưa dùng)',
    `ip_address`          VARCHAR(45)  NULL                                  COMMENT 'IP address khi tạo token',
    `user_agent`          VARCHAR(255) NULL                                  COMMENT 'User-Agent trình duyệt/app',
    `created_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo token',

    INDEX `idx_portal_token_account`  (`portal_account_id`, `purpose`, `used_at`),
    INDEX `idx_portal_token_expires`  (`otp_expires_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='OTP và session token cho portal bệnh nhân';
