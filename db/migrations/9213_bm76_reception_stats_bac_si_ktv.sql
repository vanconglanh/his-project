-- BM-76: Thẻ thống kê Tiếp đón không hiển thị với Bác sĩ / KTV vì thiếu
--        quyền reception.stats.read. Cấp thêm cho 2 role này.
-- Không ảnh hưởng các role đã có (le_tan đã có từ 9139).

SET @bacsi = (SELECT id FROM diab_his_sec_roles WHERE code = 'bac_si' LIMIT 1);
SET @ktv   = (SELECT id FROM diab_his_sec_roles WHERE code = 'ktv' LIMIT 1);

INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT @bacsi, id FROM diab_his_sec_permissions WHERE code = 'reception.stats.read';

INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT @ktv, id FROM diab_his_sec_permissions WHERE code = 'reception.stats.read';
