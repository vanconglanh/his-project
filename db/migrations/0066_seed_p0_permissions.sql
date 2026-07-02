-- ============================================================
-- Migration: 0066_seed_p0_permissions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-06-01
-- Story refs: P0 CRUD gap — billing.print, cashier.print_receipt, dtqg.submit
--             (docs/api/crud-gap-p0-contracts.md)
-- Idempotent: YES (INSERT IGNORE + ON DUPLICATE KEY UPDATE)
-- Schema: diab_his_sec_permissions / diab_his_sec_role_permissions / diab_his_sec_roles
--         (xem 9001_create_sec_all.sql — schema production chinh xac)
-- ============================================================
SET NAMES utf8mb4;

-- ============================================================
-- 1. Seed 3 permission moi
--    PK = id CHAR(36) UUID, unique key tren code
--    Cot bat buoc: code, resource, action, description
--    Khong co: module, updated_at (khong ton tai trong diab_his_sec_permissions)
-- ============================================================
INSERT INTO `diab_his_sec_permissions` (`id`, `code`, `resource`, `action`, `description`, `created_at`)
VALUES
    (UUID(), 'billing.print',         'billing',  'print',         'In hoa don A5 chinh thuc, archive MinIO, ghi audit',      NOW()),
    (UUID(), 'cashier.print_receipt', 'cashier',  'print_receipt', 'In bien lai K80 nhiet cho giao dich thu tien',            NOW()),
    (UUID(), 'dtqg.submit',           'dtqg',     'submit',        'Day don thuoc len Cong Don thuoc Quoc gia (DTQG)',         NOW())
ON DUPLICATE KEY UPDATE
    `resource`    = VALUES(`resource`),
    `action`      = VALUES(`action`),
    `description` = VALUES(`description`);

-- ============================================================
-- 2. billing.print → admin, ke_toan
--    Dung subquery UUID de lay permission_id va role_id chinh xac
--    Role code trong diab_his_sec_roles la snake_case lowercase
--    (admin, bac_si, duoc_si, ke_toan, le_tan, ky_thuat_vien)
-- ============================================================
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'billing.print'
WHERE r.`code` IN ('admin', 'ke_toan');

-- ============================================================
-- 3. cashier.print_receipt → admin, ke_toan, le_tan
-- ============================================================
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'cashier.print_receipt'
WHERE r.`code` IN ('admin', 'ke_toan', 'le_tan');

-- ============================================================
-- 4. dtqg.submit → admin, bac_si, duoc_si
-- ============================================================
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'dtqg.submit'
WHERE r.`code` IN ('admin', 'bac_si', 'duoc_si');

-- ============================================================
-- Verify (chay sau khi apply de kiem tra)
-- SELECT code, resource, action FROM diab_his_sec_permissions
-- WHERE code IN ('billing.print','cashier.print_receipt','dtqg.submit');
-- ============================================================
