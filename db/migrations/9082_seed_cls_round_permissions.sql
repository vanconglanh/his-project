-- ============================================================
-- Migration: 9082_seed_cls_round_permissions
-- Story refs: G01 + G02 - quyen thao tac dot chi dinh CLS
-- Mo ta: seed quyen cls_round.* vao catalog + cap het cho role 'admin'
--   (theo dung pattern 9066_seed_all_gated_permissions.sql: cot
--    id/code/resource/action/description/created_at, role code lowercase).
-- Idempotent: YES (INSERT IGNORE theo code + NOT EXISTS khi grant)
-- Prereq: 9066_seed_all_gated_permissions.sql
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING(t.code, LOCATE('.', t.code)+1), t.code, NOW()
FROM (
    SELECT 'cls_round.create' AS code
    UNION ALL SELECT 'cls_round.read'
    UNION ALL SELECT 'cls_round.submit'
    UNION ALL SELECT 'cls_round.pay'
    UNION ALL SELECT 'cls_round.waive'
    UNION ALL SELECT 'cls_round.cancel'
) AS t;

-- Cap toan bo quyen cls_round.* cho role admin
INSERT INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM diab_his_sec_roles r
JOIN diab_his_sec_permissions p ON p.code LIKE 'cls_round.%'
WHERE r.code = 'admin' AND r.tenant_id IS NULL
  AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions rp
                  WHERE rp.role_id = r.id AND rp.permission_id = p.id);

-- Rollback:
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
--    WHERE p.code LIKE 'cls_round.%';
--   DELETE FROM diab_his_sec_permissions WHERE code LIKE 'cls_round.%';
