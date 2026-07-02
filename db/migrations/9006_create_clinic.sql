-- ============================================================
-- Migration: 9006_create_clinic
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tác giả: Pro-Diab Team
-- Mô tả: Tạo 3 bảng cấu hình phòng khám (prefix diab_his_sys_*)
--        Bao gồm: clinics, branches, rooms
--        Cần thiết cho cross-tenant check và module Reports
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- Bảng thông tin phòng khám (liên kết với tenant)
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sys_clinics` (
    `id`                INT             NOT NULL AUTO_INCREMENT             COMMENT 'Khóa chính tự tăng',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant sở hữu phòng khám này',
    `code`              VARCHAR(20)     NOT NULL                            COMMENT 'Mã phòng khám ngắn',
    `name`              VARCHAR(255)    NOT NULL                            COMMENT 'Tên phòng khám',
    `cskcb_code`        VARCHAR(20)     NULL                                COMMENT 'Mã CSKCB do Bộ Y tế cấp',
    `address`           TEXT            NULL                                COMMENT 'Địa chỉ đầy đủ',
    `phone`             VARCHAR(30)     NULL                                COMMENT 'Số điện thoại',
    `email`             VARCHAR(100)    NULL                                COMMENT 'Email phòng khám',
    `head_doctor_id`    CHAR(36)        NULL                                COMMENT 'UUID bác sĩ phụ trách/trưởng khoa',
    `working_hours`     VARCHAR(255)    NULL                                COMMENT 'Giờ làm việc (vd: T2-T6: 7:30-17:00)',
    `is_active`         TINYINT(1)      NOT NULL DEFAULT 1                  COMMENT 'Còn hoạt động',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_clinics_code_tenant`     (`tenant_id`, `code`),
    INDEX `idx_clinics_tenant`              (`tenant_id`),
    INDEX `idx_clinics_cskcb`               (`cskcb_code`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Thông tin chi tiết phòng khám (1 tenant có thể có nhiều cơ sở)';

-- ============================================================
-- Bảng chi nhánh / cơ sở
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sys_branches` (
    `id`            INT             NOT NULL AUTO_INCREMENT             COMMENT 'Khóa chính tự tăng',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `clinic_id`     INT             NOT NULL                            COMMENT 'ID phòng khám chính',
    `code`          VARCHAR(20)     NOT NULL                            COMMENT 'Mã chi nhánh',
    `name`          VARCHAR(255)    NOT NULL                            COMMENT 'Tên chi nhánh',
    `address`       TEXT            NULL                                COMMENT 'Địa chỉ chi nhánh',
    `phone`         VARCHAR(30)     NULL                                COMMENT 'Số điện thoại',
    `is_active`     TINYINT(1)      NOT NULL DEFAULT 1                  COMMENT 'Còn hoạt động',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`    CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`    CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_branches_code_tenant`    (`tenant_id`, `code`),
    INDEX `idx_branches_clinic`             (`tenant_id`, `clinic_id`),
    CONSTRAINT `fk_branches_clinic` FOREIGN KEY (`clinic_id`)
        REFERENCES `diab_his_sys_clinics` (`id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi nhánh / cơ sở của phòng khám';

-- ============================================================
-- Bảng phòng khám / phòng chức năng
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_sys_rooms` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `branch_id`     INT             NULL                                COMMENT 'ID chi nhánh (NULL = phòng khám chính)',
    `code`          VARCHAR(20)     NOT NULL                            COMMENT 'Mã phòng',
    `name`          VARCHAR(100)    NOT NULL                            COMMENT 'Tên phòng (vd: Phòng khám số 1)',
    `room_type`     VARCHAR(30)     NOT NULL DEFAULT 'EXAM'             COMMENT 'Loại phòng: EXAM, LAB, RADIOLOGY, WAITING, CASHIER...',
    `floor`         VARCHAR(10)     NULL                                COMMENT 'Tầng',
    `capacity`      INT             NOT NULL DEFAULT 1                  COMMENT 'Số bệnh nhân tối đa cùng lúc',
    `is_active`     TINYINT(1)      NOT NULL DEFAULT 1                  COMMENT 'Còn sử dụng',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`    CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`    CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_rooms_code_tenant`   (`tenant_id`, `code`),
    INDEX `idx_rooms_tenant`            (`tenant_id`, `room_type`),
    INDEX `idx_rooms_branch`            (`tenant_id`, `branch_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phòng khám / phòng chức năng';
