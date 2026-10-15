-- =============================================================================
-- 9218_bm56_pharmacy_report_permission.sql
-- BM-56 least-privilege: Duoc si khong xem bao cao doanh thu/tai chinh (go report.read)
-- nhung giu bao cao kho duoc qua report.pharmacy. Ma tran quyen day du cac vai khac
-- cho BA chot.
-- Backend: ReportsController pharmacy/top-drugs va pharmacy/inventory-value
-- chuyen sang RequirePermission("report.pharmacy").
-- Idempotent: YES.
-- =============================================================================
SET NAMES utf8mb4;

-- 1) Dam bao permission report.pharmacy ton tai
INSERT INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'report.pharmacy', 'report', 'pharmacy', N'Xem báo cáo dược', NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions WHERE code = 'report.pharmacy');

-- 2) GRANT report.pharmacy cho duoc_si, ke_toan (admin bypass)
DROP PROCEDURE IF EXISTS _grant_perm_9218;
DELIMITER $$
CREATE PROCEDURE _grant_perm_9218(IN p_role_code VARCHAR(50))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = 'report.pharmacy' LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions
                        WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;

CALL _grant_perm_9218('duoc_si');
CALL _grant_perm_9218('ke_toan');
DROP PROCEDURE IF EXISTS _grant_perm_9218;

-- 3) REVOKE report.read khoi duoc_si
DELETE rp
  FROM diab_his_sec_role_permissions rp
  JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
 WHERE rp.role_id = '00000000-0000-0000-0000-000000000004'
   AND p.code = 'report.read';
