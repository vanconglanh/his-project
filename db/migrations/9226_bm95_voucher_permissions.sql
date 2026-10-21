-- =============================================================================
-- 9226_bm95_voucher_permissions.sql
-- BM-95: seed quyen voucher.read / voucher.write / voucher.delete (deu duoc
-- VouchersController enforce qua [RequirePermission]).
-- Gan cho role admin (tenant_id NULL) va voucher.read cho Ke toan (thu ngan).
-- Idempotent: INSERT IGNORE theo code + NOT EXISTS khi grant.
-- =============================================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, 'voucher', SUBSTRING(t.code, LOCATE('.', t.code)+1), t.code, NOW()
FROM (
    SELECT 'voucher.read' AS code
    UNION ALL SELECT 'voucher.write'
    UNION ALL SELECT 'voucher.delete'
) AS t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions p WHERE p.code = t.code);

-- Admin: ca 3 quyen
INSERT INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM diab_his_sec_roles r
JOIN diab_his_sec_permissions p ON p.code IN ('voucher.read','voucher.write','voucher.delete')
WHERE r.code = 'admin' AND r.tenant_id IS NULL
  AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions rp
                  WHERE rp.role_id = r.id AND rp.permission_id = p.id);

-- Ke toan/thu ngan (role id co dinh nhu 9139): xem danh sach voucher
INSERT INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM diab_his_sec_roles r
JOIN diab_his_sec_permissions p ON p.code = 'voucher.read'
WHERE r.id = '00000000-0000-0000-0000-000000000005'
  AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions rp
                  WHERE rp.role_id = r.id AND rp.permission_id = p.id);
