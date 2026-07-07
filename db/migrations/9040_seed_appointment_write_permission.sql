-- ============================================================
-- Migration: 9040_seed_appointment_write_permission
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Muc dich: seed permission "appointment.write" cho module Lich hen
--   (AppointmentsController: POST/PUT /appointments, PATCH /status) —
--   theo dung pattern 9039_seed_appointment_read_permission.sql.
-- Gan cho: admin, bac_si, le_tan (cac role thao tac tao/sua lich hen).
-- Idempotent: YES (INSERT ... ON DUPLICATE KEY UPDATE + INSERT IGNORE)
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `diab_his_sec_permissions` (`id`, `code`, `resource`, `action`, `description`, `created_at`)
VALUES
    (UUID(), 'appointment.write', 'appointment', 'write', 'Tao/sua lich hen kham benh', NOW())
ON DUPLICATE KEY UPDATE
    `resource`    = VALUES(`resource`),
    `action`      = VALUES(`action`),
    `description` = VALUES(`description`);

INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'appointment.write'
WHERE r.`code` IN ('admin', 'bac_si', 'le_tan');
