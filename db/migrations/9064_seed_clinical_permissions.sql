-- ============================================================
-- Migration: 9064_seed_clinical_permissions
-- Engine: MySQL 8.0+
-- Muc dich: seed lai cac quyen lam sang ma 0030_seed_permissions_sprint3 KHONG tao duoc
--   (0030 INSERT cot `module`/`updated_at` khong ton tai + role code UPPERCASE
--   'BACSI' trong khi DB dung lowercase 'bac_si' + role_permissions khong co created_at
--   -> 0030 fail hoan toan). Hau qua: quyen `icd10.read` (va emr.*, lab/rad_order.*,
--   vital_sign.*, diabetes.assess, encounter.*) khong ton tai -> bac si (khong super_admin)
--   bi 403 tren tra cuu ICD-10 + nhieu thao tac lam sang.
-- Schema dung: diab_his_sec_permissions(id,code,resource,action,description,created_at);
--              diab_his_sec_role_permissions(role_id,permission_id).
-- Role codes thuc te: admin, bac_si, duoc_si, ke_toan, ky_thuat_vien, le_tan (tenant_id NULL).
-- Idempotent: YES (INSERT IGNORE theo code + NOT EXISTS khi grant).
-- ============================================================
SET NAMES utf8mb4;

-- 1. Seed permissions (id=UUID(), resource/action tach tu code)
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING_INDEX(t.code, '.', -1), t.descr, NOW()
FROM (
    SELECT 'encounter.read'     AS code, 'Xem luot kham' AS descr UNION ALL
    SELECT 'encounter.create',  'Tao luot kham' UNION ALL
    SELECT 'encounter.update',  'Cap nhat luot kham' UNION ALL
    SELECT 'encounter.start',   'Bat dau kham' UNION ALL
    SELECT 'encounter.close',   'Dong luot kham' UNION ALL
    SELECT 'vital_sign.read',   'Xem sinh hieu' UNION ALL
    SELECT 'vital_sign.write',  'Nhap sinh hieu' UNION ALL
    SELECT 'vital_sign.delete', 'Xoa sinh hieu' UNION ALL
    SELECT 'emr.read',          'Xem benh an' UNION ALL
    SELECT 'emr.write',         'Soan benh an' UNION ALL
    SELECT 'emr.sign',          'Ky so benh an' UNION ALL
    SELECT 'emr.unsign',        'Huy ky benh an' UNION ALL
    SELECT 'emr.export',        'Xuat PDF benh an' UNION ALL
    SELECT 'emr_template.read',  'Xem mau benh an' UNION ALL
    SELECT 'emr_template.write', 'Quan ly mau benh an' UNION ALL
    SELECT 'diabetes.assess',   'Danh gia DTD chuyen khoa' UNION ALL
    SELECT 'lab_order.read',    'Xem chi dinh XN' UNION ALL
    SELECT 'lab_order.create',  'Tao chi dinh XN' UNION ALL
    SELECT 'lab_order.update',  'Cap nhat chi dinh XN' UNION ALL
    SELECT 'lab_order.delete',  'Xoa chi dinh XN' UNION ALL
    SELECT 'rad_order.read',    'Xem chi dinh CDHA' UNION ALL
    SELECT 'rad_order.create',  'Tao chi dinh CDHA' UNION ALL
    SELECT 'rad_order.update',  'Cap nhat chi dinh CDHA' UNION ALL
    SELECT 'rad_order.delete',  'Xoa chi dinh CDHA' UNION ALL
    SELECT 'icd10.read',        'Tra cuu ICD-10'
) AS t;

-- 2. Grant role -> permission (idempotent qua NOT EXISTS)
DROP PROCEDURE IF EXISTS _grant_perm2;
DELIMITER $$
CREATE PROCEDURE _grant_perm2(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm_code LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions
                       WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;

-- bac_si: kham benh day du + tra cuu ICD
CALL _grant_perm2('bac_si', 'encounter.read');
CALL _grant_perm2('bac_si', 'encounter.create');
CALL _grant_perm2('bac_si', 'encounter.update');
CALL _grant_perm2('bac_si', 'encounter.start');
CALL _grant_perm2('bac_si', 'encounter.close');
CALL _grant_perm2('bac_si', 'vital_sign.read');
CALL _grant_perm2('bac_si', 'emr.read');
CALL _grant_perm2('bac_si', 'emr.write');
CALL _grant_perm2('bac_si', 'emr.sign');
CALL _grant_perm2('bac_si', 'emr.export');
CALL _grant_perm2('bac_si', 'emr_template.read');
CALL _grant_perm2('bac_si', 'emr_template.write');
CALL _grant_perm2('bac_si', 'diabetes.assess');
CALL _grant_perm2('bac_si', 'lab_order.read');
CALL _grant_perm2('bac_si', 'lab_order.create');
CALL _grant_perm2('bac_si', 'lab_order.update');
CALL _grant_perm2('bac_si', 'lab_order.delete');
CALL _grant_perm2('bac_si', 'rad_order.read');
CALL _grant_perm2('bac_si', 'rad_order.create');
CALL _grant_perm2('bac_si', 'rad_order.update');
CALL _grant_perm2('bac_si', 'rad_order.delete');
CALL _grant_perm2('bac_si', 'icd10.read');

-- ky_thuat_vien: lab/rad order read/update
CALL _grant_perm2('ky_thuat_vien', 'lab_order.read');
CALL _grant_perm2('ky_thuat_vien', 'lab_order.update');
CALL _grant_perm2('ky_thuat_vien', 'rad_order.read');
CALL _grant_perm2('ky_thuat_vien', 'rad_order.update');
CALL _grant_perm2('ky_thuat_vien', 'icd10.read');

-- le_tan: encounter read/create, vital_sign.read, icd10.read (tiep don co the tra cuu)
CALL _grant_perm2('le_tan', 'encounter.read');
CALL _grant_perm2('le_tan', 'encounter.create');
CALL _grant_perm2('le_tan', 'vital_sign.read');
CALL _grant_perm2('le_tan', 'icd10.read');

-- admin: tat ca (du la super_admin bypass, van grant cho day du)
CALL _grant_perm2('admin', 'encounter.read');
CALL _grant_perm2('admin', 'encounter.create');
CALL _grant_perm2('admin', 'encounter.update');
CALL _grant_perm2('admin', 'encounter.start');
CALL _grant_perm2('admin', 'encounter.close');
CALL _grant_perm2('admin', 'vital_sign.read');
CALL _grant_perm2('admin', 'vital_sign.write');
CALL _grant_perm2('admin', 'vital_sign.delete');
CALL _grant_perm2('admin', 'emr.read');
CALL _grant_perm2('admin', 'emr.write');
CALL _grant_perm2('admin', 'emr.sign');
CALL _grant_perm2('admin', 'emr.unsign');
CALL _grant_perm2('admin', 'emr.export');
CALL _grant_perm2('admin', 'emr_template.read');
CALL _grant_perm2('admin', 'emr_template.write');
CALL _grant_perm2('admin', 'diabetes.assess');
CALL _grant_perm2('admin', 'lab_order.read');
CALL _grant_perm2('admin', 'lab_order.create');
CALL _grant_perm2('admin', 'lab_order.update');
CALL _grant_perm2('admin', 'lab_order.delete');
CALL _grant_perm2('admin', 'rad_order.read');
CALL _grant_perm2('admin', 'rad_order.create');
CALL _grant_perm2('admin', 'rad_order.update');
CALL _grant_perm2('admin', 'rad_order.delete');
CALL _grant_perm2('admin', 'icd10.read');

DROP PROCEDURE IF EXISTS _grant_perm2;
