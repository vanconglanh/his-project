-- ============================================================
-- Migration: 0047_seed_permissions_sprint9
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: US-BH01..BH06
-- Idempotent: YES
-- Ghi chu: Insert 7 permissions BHYT + role mapping cho Sprint 9
-- ============================================================
SET NAMES utf8mb4;

-- 1. Insert permissions (bo qua neu da ton tai)
INSERT IGNORE INTO `diab_his_iam_permissions` (`code`, `name`, `module`, `description`, `created_at`, `updated_at`)
VALUES
    ('bhyt.read',       'Xem ho so BHYT',            'BHYT', 'Doc danh sach va chi tiet ky export BHYT',           NOW(), NOW()),
    ('bhyt.export',     'Tao/Xoa ky export BHYT',    'BHYT', 'Tao ky export BHYT moi (DRAFT) va xoa ky DRAFT',    NOW(), NOW()),
    ('bhyt.generate',   'Generate XML BHYT',          'BHYT', 'Kich hoat build XML Bang 1-5 tu encounters',         NOW(), NOW()),
    ('bhyt.validate',   'Validate XSD BHYT',          'BHYT', 'Kiem tra XML theo chuan XSD QD 4750',                NOW(), NOW()),
    ('bhyt.sign',       'Ky so file BHYT',            'BHYT', 'Ky so XML bang chung thu so USB token / HSM',        NOW(), NOW()),
    ('bhyt.submit',     'Nop ho so BHYT',             'BHYT', 'Submit XML len cong giam dinh BHYT chinh thuc',      NOW(), NOW()),
    ('bhyt.reconcile',  'Doi soat ket qua BHYT',      'BHYT', 'Upload va xu ly ket qua doi soat giam dinh BHYT',   NOW(), NOW());

-- 2. Lay role IDs (KETOAN va ADMIN la cac role system)
-- KETOAN: nhan quyen bhyt.read, bhyt.export, bhyt.generate, bhyt.validate, bhyt.reconcile
-- ADMIN:  nhan them bhyt.sign, bhyt.submit (phe duyet + nop chinh thuc)

SET @role_ketoan = (SELECT `id` FROM `diab_his_iam_roles` WHERE `code` = 'KETOAN' AND `tenant_id` IS NULL LIMIT 1);
SET @role_admin  = (SELECT `id` FROM `diab_his_iam_roles` WHERE `code` = 'ADMIN'  AND `tenant_id` IS NULL LIMIT 1);

-- Helper: lay permission IDs
SET @p_read      = (SELECT `id` FROM `diab_his_iam_permissions` WHERE `code` = 'bhyt.read'      LIMIT 1);
SET @p_export    = (SELECT `id` FROM `diab_his_iam_permissions` WHERE `code` = 'bhyt.export'    LIMIT 1);
SET @p_generate  = (SELECT `id` FROM `diab_his_iam_permissions` WHERE `code` = 'bhyt.generate'  LIMIT 1);
SET @p_validate  = (SELECT `id` FROM `diab_his_iam_permissions` WHERE `code` = 'bhyt.validate'  LIMIT 1);
SET @p_sign      = (SELECT `id` FROM `diab_his_iam_permissions` WHERE `code` = 'bhyt.sign'      LIMIT 1);
SET @p_submit    = (SELECT `id` FROM `diab_his_iam_permissions` WHERE `code` = 'bhyt.submit'    LIMIT 1);
SET @p_reconcile = (SELECT `id` FROM `diab_his_iam_permissions` WHERE `code` = 'bhyt.reconcile' LIMIT 1);

-- 3. KETOAN: bhyt.read, bhyt.export, bhyt.generate, bhyt.validate, bhyt.reconcile
INSERT IGNORE INTO `diab_his_iam_role_permissions` (`role_id`, `permission_id`, `created_at`)
SELECT @role_ketoan, p.id, NOW()
FROM `diab_his_iam_permissions` p
WHERE p.`code` IN ('bhyt.read','bhyt.export','bhyt.generate','bhyt.validate','bhyt.reconcile')
  AND @role_ketoan IS NOT NULL;

-- 4. ADMIN: toan bo 7 quyen bhyt.*
INSERT IGNORE INTO `diab_his_iam_role_permissions` (`role_id`, `permission_id`, `created_at`)
SELECT @role_admin, p.id, NOW()
FROM `diab_his_iam_permissions` p
WHERE p.`code` IN ('bhyt.read','bhyt.export','bhyt.generate','bhyt.validate','bhyt.sign','bhyt.submit','bhyt.reconcile')
  AND @role_admin IS NOT NULL;
