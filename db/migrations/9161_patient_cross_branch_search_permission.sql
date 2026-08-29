-- ============================================================================
-- 9161_patient_cross_branch_search_permission.sql
-- E/Dot2: quyen mo khoa tim kiem benh nhan cross-branch (BR-25/BR-33, H-2/FR-203)
--   patient.cross_branch_search : mo khoa tim kiem MO (theo ten/danh sach) cross-branch
--                                  (khac voi tim theo dinh danh chinh xac - luon duoc phep).
--   cross_branch_view           : quyen tong hop mo khoa xem du lieu cross-branch noi chung,
--                                  map tuong duong branch.cross_view/branch.group_view (FR-203).
-- Can chuc: docs/prd/dinh-nghia-nghiep-vu-da-chi-nhanh-20260829.md muc 2.2/3.2 (BR-24, BR-25, BR-33).
-- Idempotent: INSERT IGNORE + NOT EXISTS check. Can 9150/9151 da chay truoc (co bang permissions).
-- ============================================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, 'patient', SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'patient.cross_branch_search' AS code,
           'Mo khoa tim kiem benh nhan (theo ten/danh sach) tren toan bo cac chi nhanh - BR-25/BR-33' AS descr
    UNION ALL
    SELECT 'cross_branch_view',
           'Quyen tong hop mo khoa xem du lieu cross-branch (tuong duong branch.cross_view/branch.group_view) - FR-203'
) AS t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = t.code);

-- Cap mac dinh: admin (bypass san, khong bat buoc) + ke_toan/quan_ly_khu_vuc (neu co role tuong ung).
DROP PROCEDURE IF EXISTS _grant_cross_branch_perm;
DELIMITER $$
CREATE PROCEDURE _grant_cross_branch_perm(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm_code LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_cross_branch_perm('admin', 'patient.cross_branch_search');
CALL _grant_cross_branch_perm('admin', 'cross_branch_view');
CALL _grant_cross_branch_perm('ke_toan', 'patient.cross_branch_search');
CALL _grant_cross_branch_perm('ke_toan', 'cross_branch_view');
DROP PROCEDURE IF EXISTS _grant_cross_branch_perm;
