-- ============================================================
-- Migration: 9086_seed_branch_permissions
-- Muc dich: seed quyen quan ly chi nhanh + cross_view, grant cho role he thong.
-- Schema: diab_his_sec_permissions(id,code,resource,action,description,created_at)
--         diab_his_sec_role_permissions(role_id,permission_id)
-- Role codes thuc te: admin, bac_si, duoc_si, ke_toan, ky_thuat_vien, le_tan
-- Quyet dinh nghiep vu #3 (chot voi PO): ke_toan mac dinh CO branch.cross_view
--   (ke toan chuoi can tong hop doanh thu nhieu chi nhanh).
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'branch.read'        AS code, 'Xem danh sach chi nhanh'      AS descr UNION ALL
    SELECT 'branch.create',     'Tao chi nhanh moi'                     UNION ALL
    SELECT 'branch.update',     'Cap nhat thong tin chi nhanh'          UNION ALL
    SELECT 'branch.delete',     'Vo hieu hoa / xoa chi nhanh'           UNION ALL
    SELECT 'branch.assign_user','Gan nhan su vao chi nhanh'             UNION ALL
    SELECT 'branch.cross_view', 'Xem du lieu tat ca chi nhanh cua tenant'
) AS t;

DROP PROCEDURE IF EXISTS _grant_branch_perm;
DELIMITER $$
CREATE PROCEDURE _grant_branch_perm(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm_code LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions
                        WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;

CALL _grant_branch_perm('admin', 'branch.read');
CALL _grant_branch_perm('admin', 'branch.create');
CALL _grant_branch_perm('admin', 'branch.update');
CALL _grant_branch_perm('admin', 'branch.delete');
CALL _grant_branch_perm('admin', 'branch.assign_user');
CALL _grant_branch_perm('admin', 'branch.cross_view');

CALL _grant_branch_perm('bac_si',        'branch.read');
CALL _grant_branch_perm('le_tan',        'branch.read');
CALL _grant_branch_perm('duoc_si',       'branch.read');
CALL _grant_branch_perm('ky_thuat_vien', 'branch.read');
CALL _grant_branch_perm('ke_toan',       'branch.read');
CALL _grant_branch_perm('ke_toan',       'branch.cross_view');

DROP PROCEDURE IF EXISTS _grant_branch_perm;
