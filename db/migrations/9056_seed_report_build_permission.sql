-- ============================================================
-- Migration: 9056_seed_report_build_permission
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Seed quyen 'report.build' (Report Builder P1 — tu tao bao cao qua UI,
--   khong can lap trinh vien) + gan cho power user: admin, ke_toan, bac_si, duoc_si.
--   Theo dung pattern 9039_seed_appointment_read_permission.sql.
--   Schema: diab_his_sec_permissions / diab_his_sec_role_permissions / diab_his_sec_roles.
-- Idempotent: YES (ON DUPLICATE KEY UPDATE + INSERT IGNORE).
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `diab_his_sec_permissions` (`id`, `code`, `resource`, `action`, `description`, `created_at`)
VALUES
    (UUID(), 'report.build', 'report', 'build', 'Tu tao/sua/xoa bao cao qua Report Builder (khong can code)', NOW())
ON DUPLICATE KEY UPDATE
    `resource`    = VALUES(`resource`),
    `action`      = VALUES(`action`),
    `description` = VALUES(`description`);

INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'report.build'
WHERE r.`code` IN ('admin', 'ke_toan', 'bac_si', 'duoc_si');
