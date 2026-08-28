-- ============================================================
-- Migration: 9095_create_sys_settings
-- Muc dich: D8 (docs/erd/goi-dich-vu-dinh-muc.md) - cau hinh dong cho
--   pkg.min_deposit_percent / pkg.expiry_remind_days / pkg.overdue_alert_days
--   thay vi hardcode hang so trong CreateSubscriptionHandler / PackageAlertJob.
-- Thiet ke: bang key-value don gian, tenant_id NULL = gia tri mac dinh
--   toan he thong; tenant_id cu the = override rieng cho 1 tenant.
--   ISettingsProvider doc theo uu tien: tenant-specific > global (NULL).
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + INSERT ... ON DUPLICATE KEY)
--
-- LUU Y KY THUAT (vá lỗi phát hiện khi review 2026-08-28):
--   UNIQUE KEY truc tiep tren (tenant_id, setting_key) KHONG hoat dung dung
--   khi tenant_id = NULL, vi MySQL coi moi NULL la mot gia tri PHAN BIET
--   trong unique index (NULL <> NULL) -> INSERT ... ON DUPLICATE KEY UPDATE
--   se KHONG phat hien trung, moi lan chay lai migration se chen them dong
--   moi (dong rac) cho 3 khoa global ben duoi. Khac phuc bang cot sinh
--   (generated column) `tenant_scope` quy doi NULL -> 0 chi de lam khoa
--   unique, khong dung de truy van nghiep vu (van doc/ghi qua `tenant_id`).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_sys_settings` (
    `id`         CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`  INT          NULL COMMENT 'NULL = gia tri mac dinh toan he thong (global)',
    `setting_key`   VARCHAR(100) NOT NULL COMMENT 'vd pkg.min_deposit_percent',
    `setting_value` VARCHAR(500) NOT NULL COMMENT 'luu duoi dang chuoi, parse theo nhu cau (int/decimal/bool/string)',
    `description`   VARCHAR(255) NULL,
    `tenant_scope` INT AS (COALESCE(`tenant_id`, 0)) STORED COMMENT 'Cot sinh: 0 the cho tenant_id NULL (global), chi dung lam khoa UNIQUE',
    `created_at` DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_by` CHAR(36)     NULL,
    `updated_at` DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    `updated_by` CHAR(36)     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_sys_settings_tenant_key` (`tenant_scope`, `setting_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Cau hinh he thong dang key-value (tenant_id NULL = global default)';

-- Neu bang da ton tai tu lan chay migration truoc khi co ban va nay (idempotent
-- cho moi truong da deploy 9095 ban loi) thi bo sung cot + doi lai unique key.
CALL add_col_if_missing('diab_his_sys_settings', 'tenant_scope',
    "INT AS (COALESCE(`tenant_id`, 0)) STORED COMMENT 'Cot sinh: 0 the cho tenant_id NULL (global), chi dung lam khoa UNIQUE'");

CALL drop_index_if_exists('diab_his_sys_settings', 'uq_sys_settings_tenant_key_old');

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_sys_settings'
      AND INDEX_NAME = 'uq_sys_settings_tenant_key' AND COLUMN_NAME = 'tenant_id'
);
SET @sql := IF(@idx_exists > 0,
    'ALTER TABLE `diab_his_sys_settings` RENAME INDEX `uq_sys_settings_tenant_key` TO `uq_sys_settings_tenant_key_old`, ADD UNIQUE KEY `uq_sys_settings_tenant_key` (`tenant_scope`, `setting_key`)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
CALL drop_index_if_exists('diab_his_sys_settings', 'uq_sys_settings_tenant_key_old');

-- Seed 3 khoa cau hinh cho module Goi dinh muc (FR-1201..1206, D8)
-- tenant_id = NULL vi day la GLOBAL DEFAULT dung chung toan he thong.
-- Idempotent thuc su: UNIQUE KEY nay dua tren tenant_scope (0 cho global)
-- nen ON DUPLICATE KEY UPDATE hoat dong dung tu lan chay thu 2 tro di.
INSERT INTO `diab_his_sys_settings` (`id`, `tenant_id`, `setting_key`, `setting_value`, `description`)
VALUES
    (UUID(), NULL, 'pkg.min_deposit_percent', '50', 'Ty le coc toi thieu (%) khi ban goi dinh muc tra truoc'),
    (UUID(), NULL, 'pkg.expiry_remind_days',  '7',  'So ngay truoc khi het han goi de gui canh bao nhac nho'),
    (UUID(), NULL, 'pkg.overdue_alert_days',  '30', 'So ngay qua han cong no (tinh tu ngay mua) de canh bao')
ON DUPLICATE KEY UPDATE `setting_value` = `setting_value`;
