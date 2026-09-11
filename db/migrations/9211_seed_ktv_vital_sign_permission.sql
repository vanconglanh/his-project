-- =============================================================================
-- 9211_seed_ktv_vital_sign_permission.sql
-- Nguon: Lark Bug-Feedback (BM-05) - QC full-test 2026-09-11 phat hien bug that:
-- role ky_thuat_vien bam "Dua vao kham" tai /reception mo duoc Panel "Ghi sinh
-- hieu" (BM-08 da cho phep) nhung bam "Luu sinh hieu" bi 403 Forbidden vi
-- POST /api/v1/encounters/{id}/vital-signs yeu cau permission "vital_sign.write"
-- ma ky_thuat_vien chua tung duoc cap (migration 9206 chi cap cho role "dieu_duong"
-- moi tao, quen mat ky_thuat_vien - role da co san tu truoc va cung dung chung
-- luong nay theo BM-08).
--
-- Fix: cap "vital_sign.read" + "vital_sign.write" cho ky_thuat_vien.
--
-- Idempotent: YES (check ton tai truoc khi grant, giong pattern 9206/9209).
-- =============================================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _grant_perm_9211;
DELIMITER $$
CREATE PROCEDURE _grant_perm_9211(IN p_role_code VARCHAR(100), IN p_perm_code VARCHAR(100))
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

CALL _grant_perm_9211('ky_thuat_vien', 'vital_sign.read');
CALL _grant_perm_9211('ky_thuat_vien', 'vital_sign.write');

DROP PROCEDURE IF EXISTS _grant_perm_9211;

-- Rollback:
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_roles r ON r.id = rp.role_id
--     JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
--    WHERE r.code = 'ky_thuat_vien' AND r.tenant_id IS NULL
--      AND p.code IN ('vital_sign.read', 'vital_sign.write');
