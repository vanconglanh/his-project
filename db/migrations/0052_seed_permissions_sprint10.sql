-- Sprint 10 / EPIC 8: Permissions seed
-- MySQL 8

INSERT IGNORE INTO sec_permissions (code, description) VALUES
    ('api_partner.read',      'Xem danh sách và thống kê đối tác API'),
    ('api_partner.write',     'Tạo / cập nhật / xóa đối tác API'),
    ('api_partner.admin',     'Tạo lại API key, test call đối tác'),
    ('notification.read',     'Xem hộp thư thông báo, đăng ký web push'),
    ('notification.admin',    'Quản trị thông báo toàn tenant'),
    ('vapid.admin',           'Quản lý VAPID key của tenant');

-- ADMIN role: api_partner.* + notification.* + vapid.admin
INSERT IGNORE INTO sec_role_permissions (role_code, permission_code)
SELECT r.code, p.code
FROM sec_roles r
CROSS JOIN sec_permissions p
WHERE r.code = 'ADMIN'
  AND p.code IN ('api_partner.read','api_partner.write','api_partner.admin',
                 'notification.read','notification.admin','vapid.admin');

-- KeToan role: api_partner.read only
INSERT IGNORE INTO sec_role_permissions (role_code, permission_code)
SELECT r.code, 'api_partner.read'
FROM sec_roles r
WHERE r.code = 'KeToan';

-- All internal roles: notification.read
INSERT IGNORE INTO sec_role_permissions (role_code, permission_code)
SELECT r.code, 'notification.read'
FROM sec_roles r
WHERE r.code IN ('BacSi','LeTan','DuocSi','KeToan','KyThuatVien');
