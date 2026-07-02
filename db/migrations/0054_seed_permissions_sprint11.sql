-- Migration 0054: Seed permissions for Sprint 11 Reports/Dashboard
-- MySQL 8 compatible

INSERT IGNORE INTO sec_permissions (id, code, description, created_at, updated_at)
VALUES
    (UUID(), 'report.read',    'Xem báo cáo thống kê', NOW(), NOW()),
    (UUID(), 'report.export',  'Xuất báo cáo (Excel/PDF)', NOW(), NOW()),
    (UUID(), 'dashboard.read', 'Xem dashboard tổng quan', NOW(), NOW());

-- Role mapping: KETOAN, ADMIN, BACSI
INSERT IGNORE INTO sec_role_permissions (id, role_id, permission_id, created_at)
SELECT UUID(), r.id, p.id, NOW()
FROM sec_roles r
JOIN sec_permissions p ON p.code IN ('report.read', 'report.export', 'dashboard.read')
WHERE r.code IN ('KETOAN', 'ADMIN', 'BACSI');
