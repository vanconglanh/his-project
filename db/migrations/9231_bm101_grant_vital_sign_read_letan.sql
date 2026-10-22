-- ============================================================
-- Migration: 9231_bm101_grant_vital_sign_read_letan
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mục đích: BM-101 (regression) - Lễ tân cần quyền XEM lịch sử sinh hiệu.
--   Màn tiếp đón (BM-101) có khối "Lịch sử đo" gọi
--   GET /api/v1/patients/{patientId}/vital-signs/history
--   endpoint yêu cầu permission "vital_sign.read" (VitalSignsController).
--   Trên staging role le_tan CHƯA có vital_sign.read -> API tra 403 ->
--   UI bao "Khong tai duoc lich su sinh hieu".
--   Seed 9064 co y dinh cap vital_sign.read cho le_tan nhung khong hieu luc
--   tren staging -> cap lai o day (permission da ton tai san, khong tao moi).
-- Idempotent: YES (INSERT IGNORE). Le tan can dang nhap lai de JWT cap nhat.
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'vital_sign.read'
WHERE r.`code` = 'le_tan';

-- Rollback:
-- DELETE rp FROM diab_his_sec_role_permissions rp
--   JOIN diab_his_sec_roles r ON r.id=rp.role_id
--   JOIN diab_his_sec_permissions p ON p.id=rp.permission_id
--   WHERE r.code='le_tan' AND p.code='vital_sign.read';
