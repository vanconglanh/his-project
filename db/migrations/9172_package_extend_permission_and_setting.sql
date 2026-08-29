-- ============================================================================
-- 9172_package_extend_permission_and_setting.sql
--
-- H-14 (FR-1211) — Chinh sach BO da chot: cho phep GIA HAN goi da het han nhung
-- con dinh muc (keo dai expiry_date them X ngay tren chinh subscription do).
--   - Permission moi: package_subscription.extend (cap cho admin).
--   - Setting global: package_expiry_extension_days (default 0 = TAT tinh nang).
--     Provider uu tien row tenant roi fallback row global (tenant_id IS NULL).
--
-- Idempotent: INSERT IGNORE / NOT EXISTS + check thu tuc.
-- ============================================================================

-- --- 1) Permission ----------------------------------------------------------
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'package_subscription.extend', 'package_subscription', 'extend',
       'Gia han goi dich vu da het han (con dinh muc)', NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = 'package_subscription.extend'
);

-- --- 2) Cap permission cho role admin (Quan tri vien / Quan ly chi nhanh map admin) ---
DROP PROCEDURE IF EXISTS _grant_pkg_extend_perm;
DELIMITER $$
CREATE PROCEDURE _grant_pkg_extend_perm(IN p_role VARCHAR(50))
BEGIN
    DECLARE v_role_id CHAR(36); DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = 'package_subscription.extend' LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions WHERE role_id=v_role_id AND permission_id=v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;
CALL _grant_pkg_extend_perm('admin');
DROP PROCEDURE IF EXISTS _grant_pkg_extend_perm;

-- --- 3) Setting global default (0 = tat). Admin doi qua UI cau hinh de bat. ---
INSERT INTO diab_his_sys_settings (id, tenant_id, setting_key, setting_value, description)
SELECT UUID(), NULL, 'package_expiry_extension_days', '0',
       'So ngay gia han goi da het han con dinh muc (H-14/FR-1211). 0 = tat tinh nang.'
WHERE NOT EXISTS (
    SELECT 1 FROM diab_his_sys_settings s
    WHERE s.setting_key = 'package_expiry_extension_days' AND s.tenant_id IS NULL
);
