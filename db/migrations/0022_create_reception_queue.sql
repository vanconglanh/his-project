-- ============================================================
-- Migration: 0022_create_reception_queue
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-RC01..US-RC06
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_rcp_queue_tickets` (
    `id`               CHAR(36)     NOT NULL DEFAULT (UUID())  COMMENT 'PK UUID',
    `tenant_id`        INT          NULL                        COMMENT 'ID tenant so huu ban ghi',
    `patient_id`       INT          NOT NULL                   COMMENT 'FK → pat_patients.ID',
    `room_id`          CHAR(36)     NOT NULL                   COMMENT 'FK → his_rooms.id',
    `doctor_id`        CHAR(36)     NULL                       COMMENT 'FK → sec_users.id (bac si truc)',
    `ticket_no`        VARCHAR(10)  NOT NULL                   COMMENT 'So thu tu trong ngay: 001, 002...',
    `ticket_date`      DATE         NOT NULL                   COMMENT 'Ngay tao ticket (local timezone)',
    `status`           VARCHAR(20)  NOT NULL DEFAULT 'WAITING' COMMENT 'WAITING|CALLED|IN_PROGRESS|DONE|SKIPPED|CANCELLED',
    `priority`         VARCHAR(20)  NOT NULL DEFAULT 'NORMAL'  COMMENT 'NORMAL|PRIORITY|EMERGENCY',
    `reason_for_visit` VARCHAR(1000) NULL                      COMMENT 'Ly do den kham',
    `note`             VARCHAR(1000) NULL                      COMMENT 'Ghi chu cua le tan',
    `cancel_reason`    VARCHAR(500)  NULL                      COMMENT 'Ly do huy (neu status=CANCELLED)',
    `service_packages` JSON         NULL                       COMMENT 'Danh sach goi dich vu JSON',
    `checked_in_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Thoi diem check-in',
    `called_at`        DATETIME     NULL                       COMMENT 'Thoi diem goi BN vao phong',
    `started_at`       DATETIME     NULL                       COMMENT 'Thoi diem bat dau kham',
    `finished_at`      DATETIME     NULL                       COMMENT 'Thoi diem ket thuc kham',
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       CHAR(36)     NULL,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       CHAR(36)     NULL,
    `deleted_at`       DATETIME     NULL,

    PRIMARY KEY (`id`),
    UNIQUE KEY `UK_TICKET_ROOM_DATE_NO` (`tenant_id`, `room_id`, `ticket_date`, `ticket_no`),
    INDEX `idx_rcp_ticket_tenant_date`  (`tenant_id`, `ticket_date`),
    INDEX `idx_rcp_ticket_patient`      (`tenant_id`, `patient_id`),
    INDEX `idx_rcp_ticket_room_status`  (`tenant_id`, `room_id`, `status`, `ticket_date`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Hang doi tiep don benh nhan';

-- Rooms table (neu chua co)
CREATE TABLE IF NOT EXISTS `his_rooms` (
    `id`            CHAR(36)    NOT NULL DEFAULT (UUID()),
    `tenant_id`     INT         NULL,
    `room_code`     VARCHAR(20) NOT NULL COMMENT 'Ma phong, vd P01',
    `name`          VARCHAR(100) NOT NULL COMMENT 'Ten phong, vd Phong kham 1',
    `max_per_day`   INT         NOT NULL DEFAULT 40 COMMENT 'So luot kham toi da moi ngay',
    `is_active`     TINYINT(1)  NOT NULL DEFAULT 1,
    `created_at`    DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`    DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME    NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_rooms_tenant` (`tenant_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Phong kham';

-- Room doctor duty (bac si truc phong theo ngay)
CREATE TABLE IF NOT EXISTS `his_room_doctor_duty` (
    `id`        CHAR(36) NOT NULL DEFAULT (UUID()),
    `tenant_id` INT      NULL,
    `room_id`   CHAR(36) NOT NULL,
    `doctor_id` CHAR(36) NOT NULL,
    `duty_date` DATE     NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `UK_ROOM_DOCTOR_DATE` (`room_id`, `duty_date`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci;

-- fil_files table (generic file storage metadata)
CREATE TABLE IF NOT EXISTS `fil_files` (
    `id`              CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT          NULL,
    `bucket`          VARCHAR(100) NOT NULL COMMENT 'MinIO bucket',
    `object_key`      VARCHAR(500) NOT NULL COMMENT 'MinIO object key',
    `file_name`       VARCHAR(255) NOT NULL COMMENT 'Ten file goc',
    `mime_type`       VARCHAR(100) NULL,
    `file_size_bytes` BIGINT       NULL,
    `category`        VARCHAR(50)  NULL     COMMENT 'AVATAR|CLS|CONSENT|EMR_ATTACHMENT',
    `uploaded_by`     CHAR(36)     NULL,
    `created_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`      DATETIME     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_files_tenant` (`tenant_id`, `created_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Metadata file tren MinIO';
