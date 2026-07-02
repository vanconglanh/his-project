-- ============================================================
-- Migration: 0034_seed_permissions_sprint5
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: US-SUNS-13, US-SUNS-14, US-SUNS-15
-- Idempotent: YES (INSERT IGNORE)
-- ============================================================
SET NAMES utf8mb4;

-- 15 permissions moi cho Sprint 5
INSERT IGNORE INTO `iam_permissions` (`code`, `name`, `module`)
VALUES
    ('lab_result.read',         'Xem ket qua XN',                'LabResults'),
    ('lab_result.write',        'Nhap / sua ket qua XN',         'LabResults'),
    ('lab_result.verify',       'Xac thuc ket qua XN',           'LabResults'),
    ('lab_result.import',       'Import ket qua XN hang loat',   'LabResults'),
    ('rad_result.read',         'Xem ket qua CDHA',              'RadResults'),
    ('rad_result.write',        'Nhap / sua ket qua CDHA',       'RadResults'),
    ('rad_result.verify',       'Xac thuc ket qua CDHA',         'RadResults'),
    ('lab_partner.read',        'Xem doi tac lab',               'LabPartners'),
    ('lab_partner.write',       'Tao / sua doi tac lab',         'LabPartners'),
    ('lab_partner.admin',       'Quan tri doi tac lab (xoa, rotate key)', 'LabPartners'),
    ('lab_integration.send',    'Gui chi dinh XN ra doi tac',    'LabIntegration'),
    ('lab_integration.retry',   'Retry / reprocess tich hop',   'LabIntegration'),
    ('lab_integration.webhook', 'Webhook nhan ket qua (public)', 'LabIntegration'),
    ('lab_result.unverify',     'Huy xac thuc ket qua XN',       'LabResults'),
    ('rad_result.unverify',     'Huy xac thuc ket qua CDHA',     'RadResults');

-- ─────────────────────────────────────────────────
-- Role mapping
-- Gia dinh cac role co ID trong iam_roles:
--   KTV=5, KTV_TRUONG=6, BS_CDHA=7, ADMIN=2
-- Dung INSERT IGNORE, JOIN de lay ID
-- ─────────────────────────────────────────────────

-- KTV: lab_result.read/write/import + lab_integration.send
INSERT IGNORE INTO `iam_role_permissions` (`role_id`, `permission_id`)
SELECT r.id, p.id
FROM `iam_roles` r, `iam_permissions` p
WHERE r.code = 'KTV'
  AND p.code IN ('lab_result.read','lab_result.write','lab_result.import','lab_integration.send');

-- KTV_TRUONG: KTV + lab_result.verify/unverify + lab_partner.read + lab_integration.retry + rad_result.read
INSERT IGNORE INTO `iam_role_permissions` (`role_id`, `permission_id`)
SELECT r.id, p.id
FROM `iam_roles` r, `iam_permissions` p
WHERE r.code = 'KTV_TRUONG'
  AND p.code IN (
    'lab_result.read','lab_result.write','lab_result.import',
    'lab_result.verify','lab_result.unverify',
    'lab_integration.send','lab_integration.retry',
    'lab_partner.read',
    'rad_result.read'
  );

-- BS_CDHA: rad_result.read/write/verify/unverify + lab_result.read
INSERT IGNORE INTO `iam_role_permissions` (`role_id`, `permission_id`)
SELECT r.id, p.id
FROM `iam_roles` r, `iam_permissions` p
WHERE r.code = 'BS_CDHA'
  AND p.code IN (
    'rad_result.read','rad_result.write','rad_result.verify','rad_result.unverify',
    'lab_result.read'
  );

-- ADMIN: tat ca 15 permissions moi
INSERT IGNORE INTO `iam_role_permissions` (`role_id`, `permission_id`)
SELECT r.id, p.id
FROM `iam_roles` r, `iam_permissions` p
WHERE r.code = 'ADMIN'
  AND p.code IN (
    'lab_result.read','lab_result.write','lab_result.verify','lab_result.import','lab_result.unverify',
    'rad_result.read','rad_result.write','rad_result.verify','rad_result.unverify',
    'lab_partner.read','lab_partner.write','lab_partner.admin',
    'lab_integration.send','lab_integration.retry','lab_integration.webhook'
  );
