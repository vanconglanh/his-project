-- ============================================================
-- Migration: 9069_create_med_reminders
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-08
-- Story refs: Patient Portal — Phase 1 (nhac uong thuoc)
-- Mo ta: Lich nhac uong thuoc cua benh nhan, sinh tu don thuoc.
--   Moi dong = 1 thuoc x 1 khung gio (SANG/TRUA/CHIEU/TOI). MedReminderJob quet
--   moi 30 phut, gui push/email dung khung gio (dedupe theo ngay+slot qua
--   last_notified_date). enabled=1 moi gui.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_ptl_med_reminders` (
    `id`                  CHAR(36)     NOT NULL,
    `tenant_id`           INT          NOT NULL,
    `patient_id`          CHAR(36)     NOT NULL          COMMENT 'FK -> diab_his_pat_patients.id',
    `prescription_id`     CHAR(36)     NULL              COMMENT 'FK -> diab_his_pha_prescriptions.id (nguon sinh lich)',
    `drug_name`           VARCHAR(300) NOT NULL,
    `dose_label`          VARCHAR(150) NULL              COMMENT 'Vd: 1 vien sau an',
    `time_slot`           ENUM('SANG','TRUA','CHIEU','TOI') NOT NULL,
    `remind_time`         TIME         NOT NULL          COMMENT 'Gio nhac cu the (vd 07:00)',
    `start_date`          DATE         NOT NULL,
    `end_date`            DATE         NULL              COMMENT 'Het lieu (start_date + duration_days)',
    `enabled`             TINYINT(1)   NOT NULL DEFAULT 1,
    `last_notified_date`  DATE         NULL              COMMENT 'Ngay gui gan nhat (chong gui trung trong ngay)',
    `created_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`          DATETIME     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_medrem_patient` (`tenant_id`, `patient_id`, `enabled`),
    INDEX `idx_medrem_due`     (`tenant_id`, `enabled`, `remind_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lich nhac uong thuoc cua benh nhan (portal)';
