-- ============================================================
-- Migration: 9001_create_sec_all
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tác giả: Pro-Diab Team
-- Mô tả: Tạo 7 bảng xác thực và phân quyền (prefix diab_his_sec_*)
--        Bao gồm: users, roles, permissions, user_roles,
--        role_permissions, sessions, audit_logs
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- Bảng tenant (phòng khám) — phải tạo trước vì các bảng khác có FK vào đây
-- Copy từ 0001_create_tenants.sql, dùng IF NOT EXISTS để idempotent
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sys_tenants` (
    `id`                INT             NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `code`              VARCHAR(20)     NOT NULL UNIQUE                      COMMENT 'Mã ngắn định danh tenant (slug)',
    `name`              VARCHAR(255)    NOT NULL                              COMMENT 'Tên đầy đủ phòng khám',
    `cskcb_code`        VARCHAR(20)     NULL                                  COMMENT 'Mã cơ sở khám chữa bệnh do BYT cấp',
    `status`            ENUM('ACTIVE','SUSPENDED','TERMINATED')
                                        NOT NULL DEFAULT 'ACTIVE'             COMMENT 'Trạng thái hoạt động của tenant',
    `tax_code`          VARCHAR(20)     NULL                                  COMMENT 'Mã số thuế doanh nghiệp',
    `address`           TEXT            NULL                                  COMMENT 'Địa chỉ phòng khám',
    `phone`             VARCHAR(20)     NULL                                  COMMENT 'Số điện thoại liên hệ',
    `company_name`      VARCHAR(255)    NULL                                  COMMENT 'Tên công ty / pháp nhân',
    `email`             VARCHAR(255)    NULL                                  COMMENT 'Email liên hệ chính thức',
    `email_support`     VARCHAR(255)    NULL                                  COMMENT 'Email hỗ trợ khách hàng',
    `logo_url`          VARCHAR(500)    NULL                                  COMMENT 'URL logo phòng khám',
    `subdomain`         VARCHAR(63)     NULL UNIQUE                           COMMENT 'Subdomain truy cập',
    `storage_quota_gb`  INT             NOT NULL DEFAULT 20                   COMMENT 'Hạn mức lưu trữ tính bằng GB',
    `expires_at`        DATETIME        NULL                                  COMMENT 'Ngày hết hạn gói dịch vụ',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`        INT             NULL,
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`        INT             NULL,
    `deleted_at`        DATETIME        NULL,
    `deleted_by`        INT             NULL,
    `bhyt_token_encrypted` VARCHAR(1000) NULL                                COMMENT 'Token BHYT mã hóa AES-256-GCM',

    INDEX `idx_tenants_status`  (`status`),
    INDEX `idx_tenants_cskcb`   (`cskcb_code`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh sách phòng khám (tenant) trong hệ thống SaaS';

-- ============================================================
-- Bảng người dùng hệ thống
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sec_users` (
    `id`                        CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`                 INT             NOT NULL                            COMMENT 'ID phòng khám (tenant)',
    `email`                     VARCHAR(255)    NOT NULL                            COMMENT 'Email đăng nhập',
    `password_hash`             VARCHAR(500)    NOT NULL                            COMMENT 'Mật khẩu băm BCrypt',
    `full_name`                 VARCHAR(255)    NOT NULL                            COMMENT 'Họ và tên đầy đủ',
    `phone`                     VARCHAR(30)     NULL                                COMMENT 'Số điện thoại',
    `avatar_url`                VARCHAR(500)    NULL                                COMMENT 'URL ảnh đại diện',
    `user_status`               VARCHAR(20)     NOT NULL DEFAULT 'PENDING'          COMMENT 'Trạng thái: ACTIVE, PENDING, SUSPENDED, LOCKED',
    `is_active`                 TINYINT(1)      NOT NULL DEFAULT 1                  COMMENT 'Tài khoản còn hoạt động hay không',
    `last_login_at`             DATETIME        NULL                                COMMENT 'Thời điểm đăng nhập gần nhất',
    `failed_login_count`        INT             NOT NULL DEFAULT 0                  COMMENT 'Số lần đăng nhập sai liên tiếp',
    `locked_until`              DATETIME        NULL                                COMMENT 'Khóa tài khoản đến thời điểm này',
    `invite_token`              VARCHAR(64)     NULL                                COMMENT 'Token mời người dùng',
    `invite_token_expires_at`   DATETIME        NULL                                COMMENT 'Hạn sử dụng token mời',
    `two_fa_secret`             VARCHAR(255)    NULL                                COMMENT 'Secret khóa 2FA (TOTP)',
    `two_fa_enabled`            TINYINT(1)      NOT NULL DEFAULT 0                  COMMENT 'Bật/tắt xác thực 2 yếu tố',
    `two_fa_recovery_codes`     JSON            NULL                                COMMENT 'Mã dự phòng 2FA dạng JSON array',
    `password_reset_token`      VARCHAR(64)     NULL                                COMMENT 'Token đặt lại mật khẩu',
    `password_reset_expires_at` DATETIME        NULL                                COMMENT 'Hạn token đặt lại mật khẩu',
    `created_at`                DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`                CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`                DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                                    ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`                CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`                DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`                CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_users_email_tenant`  (`email`, `tenant_id`),
    INDEX `idx_users_tenant_status`     (`tenant_id`, `user_status`),
    INDEX `idx_users_invite_token`      (`invite_token`),
    INDEX `idx_users_reset_token`       (`password_reset_token`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Tài khoản người dùng trong hệ thống';

-- ============================================================
-- Bảng vai trò (role)
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sec_roles` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `code`          VARCHAR(50)     NOT NULL                            COMMENT 'Mã vai trò (unique), vd: admin, bac_si',
    `name`          VARCHAR(100)    NOT NULL                            COMMENT 'Tên vai trò tiếng Việt',
    `description`   VARCHAR(500)    NULL                                COMMENT 'Mô tả vai trò',
    `role_type`     VARCHAR(20)     NOT NULL DEFAULT 'SYSTEM'           COMMENT 'Loại: SYSTEM (mặc định) hoặc CUSTOM (tenant tự tạo)',
    `tenant_id`     INT             NULL                                COMMENT 'NULL = vai trò hệ thống, có giá trị = vai trò riêng tenant',
    `is_active`     TINYINT(1)      NOT NULL DEFAULT 1                  COMMENT 'Còn kích hoạt hay không',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`    CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`    CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_roles_code` (`code`),
    INDEX `idx_roles_tenant`   (`tenant_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Vai trò trong hệ thống phân quyền RBAC';

-- ============================================================
-- Bảng quyền hạn (permission)
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sec_permissions` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `code`          VARCHAR(100)    NOT NULL                            COMMENT 'Mã quyền, vd: patient.read',
    `resource`      VARCHAR(50)     NOT NULL                            COMMENT 'Tài nguyên, vd: patient, report',
    `action`        VARCHAR(50)     NOT NULL                            COMMENT 'Hành động, vd: read, create, update, delete',
    `description`   VARCHAR(255)    NULL                                COMMENT 'Mô tả quyền',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_permissions_code` (`code`),
    INDEX `idx_permissions_resource` (`resource`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh sách quyền hạn chi tiết theo resource.action';

-- ============================================================
-- Bảng gán vai trò cho người dùng
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sec_user_roles` (
    `user_id`       CHAR(36)        NOT NULL    COMMENT 'UUID người dùng',
    `role_id`       CHAR(36)        NOT NULL    COMMENT 'UUID vai trò',
    `tenant_id`     INT             NOT NULL    COMMENT 'Phạm vi tenant áp dụng',

    PRIMARY KEY (`user_id`, `role_id`),
    INDEX `idx_user_roles_tenant` (`tenant_id`),
    CONSTRAINT `fk_user_roles_user` FOREIGN KEY (`user_id`)
        REFERENCES `diab_his_sec_users` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_user_roles_role` FOREIGN KEY (`role_id`)
        REFERENCES `diab_his_sec_roles` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Ánh xạ người dùng - vai trò';

-- ============================================================
-- Bảng gán quyền cho vai trò
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sec_role_permissions` (
    `role_id`       CHAR(36)        NOT NULL    COMMENT 'UUID vai trò',
    `permission_id` CHAR(36)        NOT NULL    COMMENT 'UUID quyền hạn',

    PRIMARY KEY (`role_id`, `permission_id`),
    CONSTRAINT `fk_role_perms_role` FOREIGN KEY (`role_id`)
        REFERENCES `diab_his_sec_roles` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_role_perms_perm` FOREIGN KEY (`permission_id`)
        REFERENCES `diab_his_sec_permissions` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Ánh xạ vai trò - quyền hạn';

-- ============================================================
-- Bảng phiên đăng nhập / refresh token
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sec_sessions` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID phiên',
    `user_id`           CHAR(36)        NOT NULL                            COMMENT 'UUID người dùng',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `token`             VARCHAR(500)    NOT NULL                            COMMENT 'Refresh token (hashed)',
    `expires_at`        DATETIME        NOT NULL                            COMMENT 'Thời điểm hết hạn token',
    `is_revoked`        TINYINT(1)      NOT NULL DEFAULT 0                  COMMENT 'Token đã thu hồi chưa',
    `replaced_by_token` VARCHAR(500)    NULL                                COMMENT 'Token thay thế khi rotate',
    `ip_address`        VARCHAR(50)     NULL                                COMMENT 'IP đăng nhập',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo phiên',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_sessions_token`  (`token`(255)),
    INDEX `idx_sessions_user`       (`user_id`),
    INDEX `idx_sessions_tenant`     (`tenant_id`),
    CONSTRAINT `fk_sessions_user` FOREIGN KEY (`user_id`)
        REFERENCES `diab_his_sec_users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phiên đăng nhập và refresh token';

-- ============================================================
-- Bảng audit log bảo mật
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sec_audit_logs` (
    `id`                    CHAR(36)        NOT NULL                            COMMENT 'UUID bản ghi audit',
    `tenant_id`             INT             NOT NULL                            COMMENT 'ID tenant',
    `user_id`               CHAR(36)        NULL                                COMMENT 'UUID người thực hiện',
    `user_email`            VARCHAR(255)    NULL                                COMMENT 'Email người thực hiện',
    `action`                VARCHAR(30)     NOT NULL                            COMMENT 'Hành động: LOGIN, LOGOUT, CREATE, UPDATE, DELETE...',
    `resource_type`         VARCHAR(50)     NULL                                COMMENT 'Loại tài nguyên tác động',
    `resource_id`           VARCHAR(100)    NULL                                COMMENT 'ID tài nguyên tác động',
    `ip_address`            VARCHAR(50)     NULL                                COMMENT 'Địa chỉ IP',
    `user_agent`            VARCHAR(500)    NULL                                COMMENT 'User-Agent trình duyệt',
    `details`               JSON            NULL                                COMMENT 'Chi tiết thay đổi dạng JSON',
    `severity`              VARCHAR(20)     NULL                                COMMENT 'Mức độ: INFO, WARNING, CRITICAL',
    `cross_tenant_attempt`  TINYINT(1)      NOT NULL DEFAULT 0                  COMMENT 'Có phải cố truy cập dữ liệu tenant khác không',
    `request_id`            VARCHAR(100)    NULL                                COMMENT 'Correlation ID của request',
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm ghi log',

    PRIMARY KEY (`id`),
    INDEX `idx_audit_tenant_created`    (`tenant_id`, `created_at`),
    INDEX `idx_audit_user`              (`user_id`),
    INDEX `idx_audit_cross_tenant`      (`cross_tenant_attempt`, `created_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Nhật ký kiểm tra bảo mật toàn hệ thống';
