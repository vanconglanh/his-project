-- ============================================================
-- Migration: 9096_seed_encounter_amend_permission_g03
-- Story refs: G03 - quyen dinh chinh benh an da khoa
-- Mo ta: seed quyen encounter.amend + encounter.amend.read vao catalog,
--        cap cho role admin va bac_si (amend), le_tan/ky_thuat_vien (chi doc).
--        Theo pattern 9066_seed_all_gated_permissions.sql / 9082.
-- Idempotent: YES (INSERT IGNORE theo code + NOT EXISTS khi grant)
-- Prereq: 9066_seed_all_gated_permissions.sql
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING(t.code, LOCATE('.', t.code)+1), t.descr, NOW()
FROM (
    SELECT 'encounter.amend'      AS code, 'Tao ban dinh chinh benh an da khoa' AS descr
    UNION ALL SELECT 'encounter.amend.read', 'Xem lich su dinh chinh benh an'
) AS t;

-- Quyen dinh chinh: admin + bac_si
INSERT INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM diab_his_sec_roles r
JOIN diab_his_sec_permissions p ON p.code IN ('encounter.amend', 'encounter.amend.read')
WHERE r.code IN ('admin', 'bac_si') AND r.tenant_id IS NULL
  AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions rp
                  WHERE rp.role_id = r.id AND rp.permission_id = p.id);

-- Chi xem lich su dinh chinh: le_tan + ky_thuat_vien + duoc_si + ke_toan
INSERT INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM diab_his_sec_roles r
JOIN diab_his_sec_permissions p ON p.code = 'encounter.amend.read'
WHERE r.code IN ('le_tan', 'ky_thuat_vien', 'duoc_si', 'ke_toan') AND r.tenant_id IS NULL
  AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions rp
                  WHERE rp.role_id = r.id AND rp.permission_id = p.id);

-- Rollback:
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
--    WHERE p.code IN ('encounter.amend','encounter.amend.read');
--   DELETE FROM diab_his_sec_permissions WHERE code IN ('encounter.amend','encounter.amend.read');
