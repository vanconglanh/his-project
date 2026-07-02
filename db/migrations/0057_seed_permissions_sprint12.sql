-- Migration 0057: Seed Permissions Sprint 12
-- Sprint 12 EPIC 10 Hardening

-- Insert new permissions
INSERT IGNORE INTO `sec_permissions` (`code`, `description`, `module`, `created_at`)
VALUES
    ('audit.export',        'Xuat du lieu audit log ra CSV/Excel',  'audit',      NOW()),
    ('audit.review',        'Xem va danh gia audit log he thong',   'audit',      NOW()),
    ('encryption.rotate',   'Thuc hien rotation encryption keys',   'security',   NOW()),
    ('system.config',       'Cau hinh he thong (super admin)',       'system',     NOW());

-- Map audit.review to ADMIN role
INSERT IGNORE INTO `sec_role_permissions` (`role_code`, `permission_code`, `created_at`)
SELECT 'ADMIN', 'audit.review', NOW()
WHERE EXISTS (SELECT 1 FROM `sec_permissions` WHERE `code` = 'audit.review');

INSERT IGNORE INTO `sec_role_permissions` (`role_code`, `permission_code`, `created_at`)
SELECT 'ADMIN', 'audit.export', NOW()
WHERE EXISTS (SELECT 1 FROM `sec_permissions` WHERE `code` = 'audit.export');

-- Map encryption.rotate + system.config to SUPER_ADMIN role
INSERT IGNORE INTO `sec_role_permissions` (`role_code`, `permission_code`, `created_at`)
SELECT 'SUPER_ADMIN', 'encryption.rotate', NOW()
WHERE EXISTS (SELECT 1 FROM `sec_permissions` WHERE `code` = 'encryption.rotate');

INSERT IGNORE INTO `sec_role_permissions` (`role_code`, `permission_code`, `created_at`)
SELECT 'SUPER_ADMIN', 'system.config', NOW()
WHERE EXISTS (SELECT 1 FROM `sec_permissions` WHERE `code` = 'system.config');

INSERT IGNORE INTO `sec_role_permissions` (`role_code`, `permission_code`, `created_at`)
SELECT 'SUPER_ADMIN', 'audit.review', NOW()
WHERE EXISTS (SELECT 1 FROM `sec_permissions` WHERE `code` = 'audit.review');

INSERT IGNORE INTO `sec_role_permissions` (`role_code`, `permission_code`, `created_at`)
SELECT 'SUPER_ADMIN', 'audit.export', NOW()
WHERE EXISTS (SELECT 1 FROM `sec_permissions` WHERE `code` = 'audit.export');
