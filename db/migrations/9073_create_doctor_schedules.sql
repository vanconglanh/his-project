-- ============================================================
-- Migration: 9066_create_doctor_schedules
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-08
-- Story refs: Patient Portal — Phase 1 (dat lich theo khung gio bac si)
-- Mo ta: Lich lam viec bac si (theo thu trong tuan) + block nghi/khoa gio.
--   Portal sinh cac slot trong = lich lam viec - block - lich hen da dat.
--   doctor_ref CHAR(36) khop diab_his_sec_users.id.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_sch_doctor_schedules` (
    `id`             INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`      INT          NOT NULL,
    `doctor_ref`     CHAR(36)     NOT NULL                COMMENT 'FK -> diab_his_sec_users.id',
    `day_of_week`    TINYINT      NOT NULL                COMMENT '1=Thu 2 ... 7=Chu nhat (ISO: 1=Mon..7=Sun)',
    `start_time`     TIME         NOT NULL,
    `end_time`       TIME         NOT NULL,
    `slot_minutes`   INT          NOT NULL DEFAULT 15     COMMENT 'Do dai moi slot (phut)',
    `max_per_slot`   INT          NOT NULL DEFAULT 1      COMMENT 'So benh nhan toi da moi slot',
    `effective_from` DATE         NULL,
    `effective_to`   DATE         NULL,
    `enabled`        TINYINT(1)   NOT NULL DEFAULT 1,
    `created_at`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`     CHAR(36)     NULL,
    `updated_at`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`     CHAR(36)     NULL,
    `deleted_at`     DATETIME     NULL,
    INDEX `idx_sch_doctor_dow` (`tenant_id`, `doctor_ref`, `day_of_week`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lich lam viec bac si theo thu trong tuan';

CREATE TABLE IF NOT EXISTS `diab_his_sch_schedule_blocks` (
    `id`          INT        NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `tenant_id`   INT        NOT NULL,
    `doctor_ref`  CHAR(36)   NOT NULL,
    `block_date`  DATE       NOT NULL,
    `start_time`  TIME       NULL                         COMMENT 'NULL = ca ngay',
    `end_time`    TIME       NULL,
    `reason`      VARCHAR(255) NULL,
    `created_at`  DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`  CHAR(36)   NULL,
    `deleted_at`  DATETIME   NULL,
    INDEX `idx_sch_block_doctor_date` (`tenant_id`, `doctor_ref`, `block_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Block nghi/khoa gio bac si (ngay le, hop, nghi phep)';
