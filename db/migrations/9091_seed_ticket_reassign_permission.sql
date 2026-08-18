-- ============================================================
-- Migration: 9091_seed_ticket_reassign_permission
-- Story refs: G05 — quyen dieu phoi luot kham
-- Mo ta: seed quyen reception.ticket.reassign vao catalog + cap cho role
--        admin / le_tan / bac_si (pham vi chi tiet enforce o service layer:
--        IN_PROGRESS chi doi phong va chi BS chu ca / admin).
-- Idempotent: YES (INSERT IGNORE theo code + NOT EXISTS khi grant)
-- Prereq: 9001_create_sec_all.sql, 9066_seed_all_gated_permissions.sql
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING(t.code, LOCATE('.', t.code)+1), t.descr, NOW()
FROM (
    SELECT 'reception.ticket.reassign' AS code,
           'Dieu phoi luot kham (doi bac si / doi phong)' AS descr
) AS t;

INSERT INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM diab_his_sec_roles r
JOIN diab_his_sec_permissions p ON p.code = 'reception.ticket.reassign'
WHERE r.code IN ('admin', 'le_tan', 'bac_si') AND r.tenant_id IS NULL
  AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions rp
                  WHERE rp.role_id = r.id AND rp.permission_id = p.id);

-- Rollback:
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
--    WHERE p.code = 'reception.ticket.reassign';
--   DELETE FROM diab_his_sec_permissions WHERE code = 'reception.ticket.reassign';
