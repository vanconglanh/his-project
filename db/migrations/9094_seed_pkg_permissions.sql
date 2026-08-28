-- ============================================================
-- Migration: 9094_seed_pkg_permissions
-- Muc dich: seed quyen quan ly "Goi dinh muc tra truoc" (FR-1201..1206)
--   va grant cho role he thong. Schema:
--     diab_his_sec_permissions(id,code,resource,action,description,created_at)
--     diab_his_sec_role_permissions(role_id,permission_id)
--   Role codes thuc te: admin, bac_si, duoc_si, ke_toan, ky_thuat_vien, le_tan
-- Phan quyen (an toan mac dinh, xem cau hoi Q9 trong doc):
--   - package.* (quan tri template) -> chi admin
--   - package_subscription.read     -> tat ca role lam sang/thu ngan (can biet BN con dinh muc)
--   - package_subscription.sell/collect -> le_tan (ban gioi thieu + thu coc), ke_toan (thu ngan chinh)
--   - package_subscription.cancel   -> admin, ke_toan (nghiep vu tai chinh nhay cam - hoan tien)
--   - package_subscription.update   -> admin, ke_toan (gia han)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'package.read'                 AS code, 'Xem danh sach/chi tiet goi dinh muc'         AS descr UNION ALL
    SELECT 'package.create',               'Tao goi dinh muc moi'                                          UNION ALL
    SELECT 'package.update',               'Cap nhat goi dinh muc'                                         UNION ALL
    SELECT 'package.delete',               'Vo hieu hoa/xoa goi dinh muc'                                  UNION ALL
    SELECT 'package_subscription.read',    'Xem subscription/so du dinh muc cua benh nhan'                 UNION ALL
    SELECT 'package_subscription.sell',    'Ban goi dinh muc cho benh nhan'                                UNION ALL
    SELECT 'package_subscription.collect', 'Thu not tien goi dinh muc'                                     UNION ALL
    SELECT 'package_subscription.cancel',  'Huy subscription (kem hoan tien theo ty le chua dung)'         UNION ALL
    SELECT 'package_subscription.update',  'Gia han / dieu chinh subscription'
) AS t;

DROP PROCEDURE IF EXISTS _grant_pkg_perm;
DELIMITER $$
CREATE PROCEDURE _grant_pkg_perm(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
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

-- admin: full quyen
CALL _grant_pkg_perm('admin', 'package.read');
CALL _grant_pkg_perm('admin', 'package.create');
CALL _grant_pkg_perm('admin', 'package.update');
CALL _grant_pkg_perm('admin', 'package.delete');
CALL _grant_pkg_perm('admin', 'package_subscription.read');
CALL _grant_pkg_perm('admin', 'package_subscription.sell');
CALL _grant_pkg_perm('admin', 'package_subscription.collect');
CALL _grant_pkg_perm('admin', 'package_subscription.cancel');
CALL _grant_pkg_perm('admin', 'package_subscription.update');

-- le_tan: ban goi + xem (tiep don, tu van goi)
CALL _grant_pkg_perm('le_tan', 'package.read');
CALL _grant_pkg_perm('le_tan', 'package_subscription.read');
CALL _grant_pkg_perm('le_tan', 'package_subscription.sell');

-- ke_toan: thu ngan chinh + huy/gia han (nghiep vu tai chinh)
CALL _grant_pkg_perm('ke_toan', 'package.read');
CALL _grant_pkg_perm('ke_toan', 'package_subscription.read');
CALL _grant_pkg_perm('ke_toan', 'package_subscription.sell');
CALL _grant_pkg_perm('ke_toan', 'package_subscription.collect');
CALL _grant_pkg_perm('ke_toan', 'package_subscription.cancel');
CALL _grant_pkg_perm('ke_toan', 'package_subscription.update');

-- bac_si / duoc_si / ky_thuat_vien: chi xem (de biet dinh muc con lai khi kham/ke don/CLS)
CALL _grant_pkg_perm('bac_si', 'package_subscription.read');
CALL _grant_pkg_perm('duoc_si', 'package_subscription.read');
CALL _grant_pkg_perm('ky_thuat_vien', 'package_subscription.read');

DROP PROCEDURE IF EXISTS _grant_pkg_perm;
