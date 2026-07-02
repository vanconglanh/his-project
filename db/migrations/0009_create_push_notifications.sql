-- ============================================================
-- Migration: 0009_create_push_notifications
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-16, US-SUNS-17, US-SUNS-18
-- Idempotent: YES
-- Ghi chú: Hỗ trợ thông báo realtime trong ứng dụng (in-app) và
--   Web Push Notification (Push API / VAPID).
-- ============================================================
SET NAMES utf8mb4;

-- Bảng lưu thông báo gửi đến người dùng
CREATE TABLE IF NOT EXISTS `diab_his_nti_notifications` (
    `id`                 INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`          INT          NULL                                  COMMENT 'ID tenant',
    `recipient_user_id`  INT          NOT NULL                              COMMENT 'FK → sec_users.ID — người nhận thông báo',
    `type`               VARCHAR(50)  NOT NULL                              COMMENT 'Loại thông báo: APPOINTMENT_REMINDER, LAB_RESULT_READY, PRESCRIPTION_READY, v.v.',
    `title`              VARCHAR(255) NOT NULL                              COMMENT 'Tiêu đề thông báo',
    `body`               TEXT         NULL                                  COMMENT 'Nội dung chi tiết thông báo',
    `data_json`          JSON         NULL                                  COMMENT 'Dữ liệu bổ sung dạng JSON (deep-link, entity ref)',
    `read_at`            DATETIME     NULL                                  COMMENT 'Thời điểm người dùng đã đọc (NULL = chưa đọc)',
    `created_at`         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo thông báo',

    INDEX `idx_nti_recipient_read`   (`recipient_user_id`, `read_at`),
    INDEX `idx_nti_tenant_created`   (`tenant_id`, `created_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Thông báo in-app gửi đến người dùng hệ thống';

-- Cài đặt thông báo cá nhân của mỗi người dùng
CREATE TABLE IF NOT EXISTS `diab_his_nti_user_preferences` (
    `id`                    INT         NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `user_id`               INT         NOT NULL UNIQUE                      COMMENT 'FK → sec_users.ID (1 user 1 bộ cài đặt)',
    `position`              ENUM('TOP_RIGHT','BOTTOM_RIGHT','CENTER')
                                        NOT NULL DEFAULT 'TOP_RIGHT'          COMMENT 'Vị trí hiển thị toast thông báo',
    `sound_enabled`         TINYINT(1)  NOT NULL DEFAULT 1                    COMMENT 'Bật/tắt âm thanh thông báo',
    `sound_name`            VARCHAR(50) NULL                                  COMMENT 'Tên file âm thanh tùy chọn',
    `browser_push_enabled`  TINYINT(1)  NOT NULL DEFAULT 1                    COMMENT 'Bật/tắt Web Push Notification trình duyệt',
    `updated_at`            DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật cài đặt'
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Cài đặt thông báo cá nhân của người dùng';

-- Đăng ký Web Push Subscription (VAPID) của trình duyệt người dùng
CREATE TABLE IF NOT EXISTS `diab_his_nti_web_push_subscriptions` (
    `id`           INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `user_id`      INT          NOT NULL                              COMMENT 'FK → sec_users.ID',
    `endpoint`     VARCHAR(500) NOT NULL                              COMMENT 'Push service endpoint URL của trình duyệt',
    `p256dh_key`   VARCHAR(255) NOT NULL                              COMMENT 'Public key ECDH P-256 (VAPID)',
    `auth_key`     VARCHAR(255) NOT NULL                              COMMENT 'Authentication secret (VAPID)',
    `user_agent`   VARCHAR(255) NULL                                  COMMENT 'User-Agent của trình duyệt đăng ký',
    `created_at`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm đăng ký subscription',
    `last_used_at` DATETIME     NULL                                  COMMENT 'Thời điểm gửi push gần nhất',

    INDEX `idx_push_sub_user`     (`user_id`),
    INDEX `idx_push_sub_last_use` (`last_used_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Đăng ký Web Push Notification (VAPID) của trình duyệt';
