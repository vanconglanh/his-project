-- ============================================================
-- Migration: 9054_seed_cdss_permissions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Seed permission cho cac tinh nang moi (CDSS, risk-list, recall, AI) +
--   gan role. Theo dung pattern 9039_seed_appointment_read_permission.sql.
--   Schema: diab_his_sec_permissions / diab_his_sec_role_permissions / diab_his_sec_roles.
-- Idempotent: YES (ON DUPLICATE KEY UPDATE + INSERT IGNORE).
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `diab_his_sec_permissions` (`id`, `code`, `resource`, `action`, `description`, `created_at`)
VALUES
    (UUID(), 'cdss.read',     'cdss',   'read',   'Kiem tra canh bao CDSS khi ke don',        NOW()),
    (UUID(), 'cdss.override', 'cdss',   'override','Vuot canh bao CDSS interruptive (co ly do)',NOW()),
    (UUID(), 'cdss.admin',    'cdss',   'admin',  'Quan tri rule/cap tuong tac CDSS',          NOW()),
    (UUID(), 'risk.read',     'risk',   'read',   'Xem danh sach phan tang nguy co benh nhan',  NOW()),
    (UUID(), 'recall.read',   'recall', 'read',   'Xem danh sach recall/nhac tai kham',         NOW()),
    (UUID(), 'recall.manage', 'recall', 'manage', 'Xu ly recall (goi/nhac/doi trang thai)',     NOW()),
    (UUID(), 'ai.suggest',    'ai',     'suggest','Xem goi y dieu tri AI (tham khao)',          NOW())
ON DUPLICATE KEY UPDATE
    `resource`    = VALUES(`resource`),
    `action`      = VALUES(`action`),
    `description` = VALUES(`description`);

-- Bac si: toan bo tinh nang lam sang
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p
  ON p.`code` IN ('cdss.read','cdss.override','risk.read','recall.read','ai.suggest')
WHERE r.`code` = 'bac_si';

-- Le tan: recall (goi dien nhac tai kham) + xem risk-list
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p
  ON p.`code` IN ('recall.read','recall.manage','risk.read')
WHERE r.`code` = 'le_tan';

-- Admin: tat ca (bao gom quan tri rule CDSS)
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p
  ON p.`code` IN ('cdss.read','cdss.override','cdss.admin','risk.read','recall.read','recall.manage','ai.suggest')
WHERE r.`code` = 'admin';
