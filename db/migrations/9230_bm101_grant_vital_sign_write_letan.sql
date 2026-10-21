-- ============================================================
-- Migration: 9230_bm101_grant_vital_sign_write_letan
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mục đích: BM-101 - Lễ tân cần quyền lưu sinh hiệu tại tiếp đón.
--   Endpoint ghi sinh hiệu yêu cầu permission "vital_sign.write", nhưng role le_tan
--   mới có vital_sign.read -> lễ tân không lưu được sinh hiệu.
--   Cấp "vital_sign.write" cho role le_tan (permission đã tồn tại sẵn, không tạo mới).
-- Idempotent: YES (INSERT IGNORE).
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'vital_sign.write'
WHERE r.`code` = 'le_tan';

-- Rollback:
-- DELETE rp FROM diab_his_sec_role_permissions rp
--   JOIN diab_his_sec_roles r ON r.id=rp.role_id
--   JOIN diab_his_sec_permissions p ON p.id=rp.permission_id
--   WHERE r.code='le_tan' AND p.code='vital_sign.write';
