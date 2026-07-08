-- ============================================================
-- Migration: 9063_seed_emr_template_permissions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Seed quyen 'emr_template.read' / 'emr_template.write' (Mau benh an) +
--   gan cho role bac_si, admin. FIX: migration 0030 dinh nghia quyen nay nhung
--   cap bang role code HOA ('BACSI') trong khi role code THAT la thuong ('bac_si')
--   -> cap truot -> bac si khong co emr_template.read -> GET /emr-templates 403
--   -> dropdown mau benh an RONG -> khong dung duoc mau trong kham.
--   Theo dung pattern 9056_seed_report_build_permission.sql (role code LOWERCASE).
-- Idempotent: YES (ON DUPLICATE KEY UPDATE + INSERT IGNORE).
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `diab_his_sec_permissions` (`id`, `code`, `resource`, `action`, `description`, `created_at`)
VALUES
    (UUID(), 'emr_template.read',  'emr_template', 'read',  'Xem/su dung mau benh an trong kham', NOW()),
    (UUID(), 'emr_template.write', 'emr_template', 'write', 'Tao/sua/xoa mau benh an', NOW())
ON DUPLICATE KEY UPDATE
    `resource`    = VALUES(`resource`),
    `action`      = VALUES(`action`),
    `description` = VALUES(`description`);

INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` IN ('emr_template.read', 'emr_template.write')
WHERE r.`code` IN ('admin', 'bac_si');
