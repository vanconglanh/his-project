-- ============================================================
-- Migration: 9039_seed_appointment_read_permission
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Bug tien nhiem #5: AppointmentsController.SlipPdf yeu cau permission
--   "appointment.read" ([RequirePermission("appointment.read")]) nhung
--   permission nay chua duoc seed trong diab_his_sec_permissions -> moi
--   role (ke ca admin) deu bi 403 khi goi GET /appointments/{id}/slip-pdf.
-- Schema: diab_his_sec_permissions / diab_his_sec_role_permissions /
--   diab_his_sec_roles (xem 9001_create_sec_all.sql, theo dung pattern
--   0066_seed_p0_permissions.sql).
-- Gan cho: admin, bac_si, le_tan (cac role thao tac lich hen / in giay hen).
-- Idempotent: YES (INSERT ... ON DUPLICATE KEY UPDATE + INSERT IGNORE)
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `diab_his_sec_permissions` (`id`, `code`, `resource`, `action`, `description`, `created_at`)
VALUES
    (UUID(), 'appointment.read', 'appointment', 'read', 'Xem/in giay hen tai kham', NOW())
ON DUPLICATE KEY UPDATE
    `resource`    = VALUES(`resource`),
    `action`      = VALUES(`action`),
    `description` = VALUES(`description`);

INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'appointment.read'
WHERE r.`code` IN ('admin', 'bac_si', 'le_tan');
