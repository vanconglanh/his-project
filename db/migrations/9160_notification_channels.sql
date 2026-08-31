-- ============================================================
-- Migration: 9160_notification_channels
-- FR ref: FR-112 (H-1) - Nhac lich hen tu dong qua SMS / Zalo ZNS
-- Mo ta:
--   1. Tao bang diab_his_int_notification_channels: cau hinh kenh gui
--      thong bao (SMS / Zalo ZNS) per-tenant/per-branch. config_encrypted
--      luu JSON da ma hoa AES-256-GCM (api_key, secret, access_token,
--      template mapping...). KHONG hardcode key trong code -> doi/reset
--      credential qua UI khong can deploy lai.
--   2. Seed 2 quyen notification_channel.read / notification_channel.write
--      vao catalog + cap cho role 'admin' (quan tri / quan ly chi nhanh).
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + stored proc check cot +
--   INSERT IGNORE theo code + NOT EXISTS khi grant)
-- Prereq: 0011_create_dtqg.sql (pattern credential), 9066 (permission catalog)
-- Convention: giong diab_his_int_dtqg_credentials (uu tien branch_id, fallback
--   branch_id NULL = dung chung tenant).
-- ============================================================
SET NAMES utf8mb4;

-- ─── 1. Bang cau hinh kenh thong bao ────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `diab_his_int_notification_channels` (
    `id`               CHAR(36)     NOT NULL PRIMARY KEY DEFAULT (UUID())    COMMENT 'Khoa chinh (UUID)',
    `tenant_id`        INT          NOT NULL                                  COMMENT 'FK -> diab_his_sys_tenants.id',
    `branch_id`        INT          NULL                                      COMMENT 'FK chi nhanh; NULL = dung chung toan tenant',
    `channel`          VARCHAR(20)  NOT NULL                                  COMMENT 'Loai kenh: SMS | ZALO_ZNS',
    `provider`         VARCHAR(30)  NOT NULL                                  COMMENT 'Nha cung cap: ESMS (SMS) | ZALO_OA (Zalo ZNS)',
    `config_encrypted` TEXT         NOT NULL                                  COMMENT 'JSON cau hinh da ma hoa AES-256-GCM (api_key, secret, access_token, template...)',
    `is_active`        TINYINT(1)   NOT NULL DEFAULT 1                         COMMENT 'Bat/tat kenh',
    `last_tested_at`   DATETIME     NULL                                      COMMENT 'Lan test ket noi gan nhat',
    `last_test_ok`     TINYINT(1)   NOT NULL DEFAULT 0                         COMMENT 'Ket qua test gan nhat (1=OK)',
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP        COMMENT 'Thoi diem tao',
    `created_by`       INT          NULL                                      COMMENT 'ID nguoi tao',
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP           COMMENT 'Thoi diem cap nhat',
    `updated_by`       INT          NULL                                      COMMENT 'ID nguoi cap nhat',
    `deleted_at`       DATETIME     NULL                                      COMMENT 'Thoi diem xoa mem',
    UNIQUE KEY `uq_notif_channel_scope` (`tenant_id`, `branch_id`, `channel`),
    INDEX `idx_notif_channel_tenant` (`tenant_id`),
    INDEX `idx_notif_channel_active` (`tenant_id`, `is_active`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='FR-112: cau hinh kenh gui thong bao SMS/Zalo ZNS per-tenant/branch';

-- Neu bang da ton tai tu lan chay truoc nhung thieu cot (idempotent an toan) -> bo sung
DROP PROCEDURE IF EXISTS _notif_add_col;
DELIMITER $$
CREATE PROCEDURE _notif_add_col(tbl VARCHAR(200), col VARCHAR(100), def TEXT)
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = tbl AND COLUMN_NAME = col
  ) THEN
    SET @sql = CONCAT('ALTER TABLE `', tbl, '` ADD COLUMN `', col, '` ', def);
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
  END IF;
END$$
DELIMITER ;

CALL _notif_add_col('diab_his_int_notification_channels', 'last_tested_at', 'DATETIME NULL');
CALL _notif_add_col('diab_his_int_notification_channels', 'last_test_ok',   'TINYINT(1) NOT NULL DEFAULT 0');
CALL _notif_add_col('diab_his_int_notification_channels', 'deleted_at',     'DATETIME NULL');

-- Cot danh dau "da nhac lich hen qua SMS/Zalo" tren appointment (chong gui trung -
-- SMS/ZNS ton phi). Job AppointmentReminderNotifyJob set reminder_sent_at khi gui thanh cong.
CALL _notif_add_col('diab_his_sch_appointments', 'reminder_sent_at', 'DATETIME NULL COMMENT "FR-112: thoi diem da gui nhac lich qua SMS/Zalo"');

DROP PROCEDURE IF EXISTS _notif_add_col;

-- ─── 2. Seed quyen vao catalog ──────────────────────────────────────────────
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING(t.code, LOCATE('.', t.code)+1), t.code, NOW()
FROM (
    SELECT 'notification_channel.read'  AS code
    UNION ALL SELECT 'notification_channel.write'
) AS t;

-- ─── 3. Cap quyen cho role admin (quan tri / quan ly chi nhanh) ──────────────
INSERT INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM diab_his_sec_roles r
JOIN diab_his_sec_permissions p ON p.code LIKE 'notification_channel.%'
WHERE r.code = 'admin' AND r.tenant_id IS NULL
  AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions rp
                  WHERE rp.role_id = r.id AND rp.permission_id = p.id);

-- Rollback:
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
--    WHERE p.code LIKE 'notification_channel.%';
--   DELETE FROM diab_his_sec_permissions WHERE code LIKE 'notification_channel.%';
--   DROP TABLE IF EXISTS diab_his_int_notification_channels;
