-- ============================================================
-- Migration: 0008_create_api_partners
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-08, US-SUNS-10, US-SUNS-11, US-SUNS-12
-- Idempotent: YES
-- Ghi chú: Quản lý đối tác tích hợp API (app đặt lịch, app bệnh nhân,
--   đối tác bảo hiểm). API key dùng SHA-256 hash để so sánh.
-- ============================================================
SET NAMES utf8mb4;

-- Bảng đối tác API (ứng dụng bên ngoài gọi API Pro-Diab)
CREATE TABLE IF NOT EXISTS `diab_his_api_partners` (
    `id`                INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`         INT          NULL                                  COMMENT 'ID tenant — NULL nếu là đối tác cấp hệ thống',
    `name`              VARCHAR(255) NOT NULL                              COMMENT 'Tên đối tác / ứng dụng',
    `api_key_hash`      CHAR(64)     NOT NULL UNIQUE                       COMMENT 'SHA-256 hash của API key (không lưu plaintext)',
    `secret_hash`       CHAR(64)     NULL                                  COMMENT 'SHA-256 hash của API secret (HMAC signing)',
    `status`            ENUM('ACTIVE','SUSPENDED','REVOKED')
                                     NOT NULL DEFAULT 'ACTIVE'             COMMENT 'Trạng thái API key',
    `rate_limit_per_min` INT         NOT NULL DEFAULT 60                   COMMENT 'Giới hạn số request / phút',
    `daily_quota`       INT          NULL                                  COMMENT 'Tổng request tối đa / ngày (NULL = không giới hạn)',
    `contact_email`     VARCHAR(100) NULL                                  COMMENT 'Email liên hệ đối tác',
    `expires_at`        DATETIME     NULL                                  COMMENT 'Ngày hết hạn API key (NULL = không giới hạn)',
    `created_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`        INT          NULL                                  COMMENT 'ID người tạo',
    `updated_at`        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                         ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`        INT          NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`        DATETIME     NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_api_partners_tenant` (`tenant_id`, `status`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Đối tác tích hợp API bên ngoài (app đặt lịch, portal bệnh nhân)';

-- Phạm vi quyền của từng đối tác API
CREATE TABLE IF NOT EXISTS `diab_his_api_partner_scopes` (
    `id`         INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `partner_id` INT          NOT NULL                              COMMENT 'FK → diab_his_api_partners.id',
    `scope`      VARCHAR(100) NOT NULL                              COMMENT 'Phạm vi quyền, vd: patient.register, appointment.book, catalog.read, service.read, visit.read',
    `created_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm gán quyền',

    UNIQUE KEY `uq_partner_scope` (`partner_id`, `scope`),
    INDEX `idx_partner_scopes_partner` (`partner_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh sách scope (quyền truy cập) của mỗi đối tác API';

-- Log request từ đối tác API (phục vụ giám sát và debug)
CREATE TABLE IF NOT EXISTS `diab_his_api_request_logs` (
    `id`               INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `partner_id`       INT          NOT NULL                              COMMENT 'FK → diab_his_api_partners.id',
    `endpoint`         VARCHAR(255) NOT NULL                              COMMENT 'Endpoint được gọi (vd: /api/v1/appointments)',
    `http_method`      VARCHAR(10)  NOT NULL                              COMMENT 'HTTP method: GET, POST, PUT, DELETE',
    `status_code`      SMALLINT     NOT NULL                              COMMENT 'HTTP status code trả về',
    `response_time_ms` INT          NULL                                  COMMENT 'Thời gian phản hồi tính bằng millisecond',
    `ip_address`       VARCHAR(45)  NULL                                  COMMENT 'IP address của đối tác (hỗ trợ IPv6)',
    `request_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm nhận request',
    `request_id`       CHAR(36)     NULL                                  COMMENT 'UUID của request (X-Request-ID header)',

    INDEX `idx_api_logs_partner_time` (`partner_id`, `request_at`),
    INDEX `idx_api_logs_status`       (`status_code`, `request_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Log request API từ các đối tác tích hợp';
