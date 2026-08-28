-- ============================================================
-- Migration: 9097_seed_telehealth_permissions
-- Muc dich: seed quyen quan tri mapping dich vu telehealth (Docosan).
-- Schema: diab_his_sec_permissions(id,code,resource,action,description,created_at)
--         diab_his_sec_role_permissions(role_id,permission_id)
-- Role codes thuc te: admin, bac_si, duoc_si, ke_toan, ky_thuat_vien, le_tan
-- Ly do: TelehealthAdminController dung [RequirePermission("telehealth.admin_mapping")]
--   nhung permission nay chua duoc seed o migration 9096 -> API luon bi tu choi quyen.
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'telehealth.admin_mapping' AS code,
           'Quan tri mapping dich vu/bac si/phong kham telehealth (Docosan)' AS descr
) AS t;

DROP PROCEDURE IF EXISTS _grant_telehealth_perm;
DELIMITER $$
CREATE PROCEDURE _grant_telehealth_perm(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
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

-- Chi Admin duoc quan tri mapping telehealth (anh huong toan tenant, quyet dinh ky thuat cao).
CALL _grant_telehealth_perm('admin', 'telehealth.admin_mapping');

DROP PROCEDURE IF EXISTS _grant_telehealth_perm;
