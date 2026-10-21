-- ============================================================
-- Migration: 9228_bm105_grant_file_upload_letan
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mục đích: BM-105 - Lễ tân cần quyền upload Inbody/tài liệu.
--   Endpoint POST /api/v1/files/upload yêu cầu permission "file.upload"
--   (FilesController), nhưng role le_tan CHƯA được cấp -> lễ tân không upload được.
--   Cấp "file.upload" cho role le_tan (permission đã tồn tại sẵn, không tạo mới).
-- Idempotent: YES (INSERT IGNORE).
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'file.upload'
WHERE r.`code` = 'le_tan';

-- Rollback:
-- DELETE rp FROM diab_his_sec_role_permissions rp
--   JOIN diab_his_sec_roles r ON r.id=rp.role_id
--   JOIN diab_his_sec_permissions p ON p.id=rp.permission_id
--   WHERE r.code='le_tan' AND p.code='file.upload';
