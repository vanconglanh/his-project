-- ============================================================
-- Migration: 9232_bm101_grant_clinical_write_letan_inbody
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mục đích: BM-101 WI-7 - Lễ tân upload Inbody tại tiếp đón.
--   Luồng upload Inbody dùng:
--     POST /api/v1/documents/smart-upload           (OCR + phan loai)
--     POST /api/v1/inbody-reports/{id}/confirm      (ghi sinh hieu + chi so Inbody)
--   Cả hai yêu cầu permission "patient.clinical.write". Role le_tan chỉ có
--   file.upload (BM-105) -> smart-upload tra 403 -> khong upload Inbody duoc.
--   Cấp "patient.clinical.write" cho le_tan de hoan tat luong WI-7.
--
--   ⚠️ LƯU Y BẢO MẬT (can BA/security review): permission nay con gate
--   them: them/xoa di ung (allergies) cua benh nhan. Tuc le_tan sau migration
--   co the: upload tai lieu/Inbody, confirm Inbody (ghi sinh hieu), them/xoa
--   di ung. KHONG gate chan doan/EMR (nhung dung permission khac). Neu nghiep
--   vu khong muon le_tan ghi di ung, can tach permission rieng cho Inbody.
--
-- Idempotent: YES (INSERT IGNORE). Le tan can dang nhap lai de JWT cap nhat.
-- ============================================================
SET NAMES utf8mb4;

INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.`id`, p.`id`
FROM `diab_his_sec_roles` r
JOIN `diab_his_sec_permissions` p ON p.`code` = 'patient.clinical.write'
WHERE r.`code` = 'le_tan';

-- Rollback:
-- DELETE rp FROM diab_his_sec_role_permissions rp
--   JOIN diab_his_sec_roles r ON r.id=rp.role_id
--   JOIN diab_his_sec_permissions p ON p.id=rp.permission_id
--   WHERE r.code='le_tan' AND p.code='patient.clinical.write';
