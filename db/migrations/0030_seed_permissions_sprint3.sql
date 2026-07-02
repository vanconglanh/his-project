-- ============================================================
-- Migration: 0030_seed_permissions_sprint3
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: Sprint 3-4 EPIC 3 permissions
-- Idempotent: YES (INSERT IGNORE)
-- ============================================================
SET NAMES utf8mb4;

-- ---------------------------------------------------------------
-- 1. Insert permissions
-- ---------------------------------------------------------------
INSERT IGNORE INTO diab_his_sec_permissions (code, description, module, created_at, updated_at) VALUES
-- Encounter
('encounter.read',   'Xem lượt khám',       'encounter', NOW(), NOW()),
('encounter.create', 'Tạo lượt khám',       'encounter', NOW(), NOW()),
('encounter.update', 'Cập nhật lượt khám',  'encounter', NOW(), NOW()),
('encounter.start',  'Bắt đầu khám',        'encounter', NOW(), NOW()),
('encounter.close',  'Đóng lượt khám',      'encounter', NOW(), NOW()),
-- Vital signs
('vital_sign.read',   'Xem sinh hiệu',    'vital_sign', NOW(), NOW()),
('vital_sign.write',  'Nhập sinh hiệu',   'vital_sign', NOW(), NOW()),
('vital_sign.delete', 'Xóa sinh hiệu',    'vital_sign', NOW(), NOW()),
-- EMR
('emr.read',           'Xem bệnh án',          'emr', NOW(), NOW()),
('emr.write',          'Soạn bệnh án',         'emr', NOW(), NOW()),
('emr.sign',           'Ký số bệnh án',        'emr', NOW(), NOW()),
('emr.unsign',         'Hủy ký bệnh án',       'emr', NOW(), NOW()),
('emr.export',         'Xuất PDF bệnh án',     'emr', NOW(), NOW()),
('emr_template.read',  'Xem mẫu bệnh án',      'emr', NOW(), NOW()),
('emr_template.write', 'Quản lý mẫu bệnh án',  'emr', NOW(), NOW()),
-- Diabetes
('diabetes.assess', 'Đánh giá ĐTĐ chuyên khoa', 'diabetes', NOW(), NOW()),
-- Lab orders
('lab_order.read',   'Xem chỉ định XN',    'cls', NOW(), NOW()),
('lab_order.create', 'Tạo chỉ định XN',    'cls', NOW(), NOW()),
('lab_order.update', 'Cập nhật chỉ định XN','cls', NOW(), NOW()),
('lab_order.delete', 'Xóa chỉ định XN',    'cls', NOW(), NOW()),
-- Rad orders
('rad_order.read',   'Xem chỉ định CĐHA',    'cls', NOW(), NOW()),
('rad_order.create', 'Tạo chỉ định CĐHA',    'cls', NOW(), NOW()),
('rad_order.update', 'Cập nhật chỉ định CĐHA','cls', NOW(), NOW()),
('rad_order.delete', 'Xóa chỉ định CĐHA',    'cls', NOW(), NOW()),
-- ICD-10
('icd10.read', 'Tra cứu ICD-10', 'dict', NOW(), NOW());

-- ---------------------------------------------------------------
-- 2. Role-permission mapping
-- Lấy role_id qua code (system roles có tenant_id IS NULL)
-- ---------------------------------------------------------------

-- Helper: map permission code -> role code
DROP PROCEDURE IF EXISTS _grant_perm;
DELIMITER $$
CREATE PROCEDURE _grant_perm(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
BEGIN
    DECLARE v_role_id  CHAR(36);
    DECLARE v_perm_id  CHAR(36);

    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm_code LIMIT 1;

    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL THEN
        INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id, created_at)
        VALUES (v_role_id, v_perm_id, NOW());
    END IF;
END$$
DELIMITER ;

-- BacSi: encounter.* (trừ delete), vital_sign.read, emr.*, diabetes.assess, lab_order.*, rad_order.*, icd10.read
CALL _grant_perm('BACSI', 'encounter.read');
CALL _grant_perm('BACSI', 'encounter.create');
CALL _grant_perm('BACSI', 'encounter.update');
CALL _grant_perm('BACSI', 'encounter.start');
CALL _grant_perm('BACSI', 'encounter.close');
CALL _grant_perm('BACSI', 'vital_sign.read');
CALL _grant_perm('BACSI', 'emr.read');
CALL _grant_perm('BACSI', 'emr.write');
CALL _grant_perm('BACSI', 'emr.sign');
CALL _grant_perm('BACSI', 'emr.export');
CALL _grant_perm('BACSI', 'emr_template.read');
CALL _grant_perm('BACSI', 'emr_template.write');
CALL _grant_perm('BACSI', 'diabetes.assess');
CALL _grant_perm('BACSI', 'lab_order.read');
CALL _grant_perm('BACSI', 'lab_order.create');
CALL _grant_perm('BACSI', 'lab_order.update');
CALL _grant_perm('BACSI', 'lab_order.delete');
CALL _grant_perm('BACSI', 'rad_order.read');
CALL _grant_perm('BACSI', 'rad_order.create');
CALL _grant_perm('BACSI', 'rad_order.update');
CALL _grant_perm('BACSI', 'rad_order.delete');
CALL _grant_perm('BACSI', 'icd10.read');

-- DieuDuong: encounter.read/update, vital_sign.*, emr.read, lab_order.read/update, icd10.read
CALL _grant_perm('DIEUDUONG', 'encounter.read');
CALL _grant_perm('DIEUDUONG', 'encounter.update');
CALL _grant_perm('DIEUDUONG', 'vital_sign.read');
CALL _grant_perm('DIEUDUONG', 'vital_sign.write');
CALL _grant_perm('DIEUDUONG', 'vital_sign.delete');
CALL _grant_perm('DIEUDUONG', 'emr.read');
CALL _grant_perm('DIEUDUONG', 'lab_order.read');
CALL _grant_perm('DIEUDUONG', 'lab_order.update');
CALL _grant_perm('DIEUDUONG', 'icd10.read');

-- KyThuatVien: lab_order.read/update, rad_order.read/update
CALL _grant_perm('KYTHUATVIEN', 'lab_order.read');
CALL _grant_perm('KYTHUATVIEN', 'lab_order.update');
CALL _grant_perm('KYTHUATVIEN', 'rad_order.read');
CALL _grant_perm('KYTHUATVIEN', 'rad_order.update');

-- LeTan: encounter.read/create, vital_sign.read
CALL _grant_perm('LETAN', 'encounter.read');
CALL _grant_perm('LETAN', 'encounter.create');
CALL _grant_perm('LETAN', 'vital_sign.read');

-- Admin: tat ca + emr.unsign
CALL _grant_perm('ADMIN', 'encounter.read');
CALL _grant_perm('ADMIN', 'encounter.create');
CALL _grant_perm('ADMIN', 'encounter.update');
CALL _grant_perm('ADMIN', 'encounter.start');
CALL _grant_perm('ADMIN', 'encounter.close');
CALL _grant_perm('ADMIN', 'vital_sign.read');
CALL _grant_perm('ADMIN', 'vital_sign.write');
CALL _grant_perm('ADMIN', 'vital_sign.delete');
CALL _grant_perm('ADMIN', 'emr.read');
CALL _grant_perm('ADMIN', 'emr.write');
CALL _grant_perm('ADMIN', 'emr.sign');
CALL _grant_perm('ADMIN', 'emr.unsign');
CALL _grant_perm('ADMIN', 'emr.export');
CALL _grant_perm('ADMIN', 'emr_template.read');
CALL _grant_perm('ADMIN', 'emr_template.write');
CALL _grant_perm('ADMIN', 'diabetes.assess');
CALL _grant_perm('ADMIN', 'lab_order.read');
CALL _grant_perm('ADMIN', 'lab_order.create');
CALL _grant_perm('ADMIN', 'lab_order.update');
CALL _grant_perm('ADMIN', 'lab_order.delete');
CALL _grant_perm('ADMIN', 'rad_order.read');
CALL _grant_perm('ADMIN', 'rad_order.create');
CALL _grant_perm('ADMIN', 'rad_order.update');
CALL _grant_perm('ADMIN', 'rad_order.delete');
CALL _grant_perm('ADMIN', 'icd10.read');

DROP PROCEDURE IF EXISTS _grant_perm;
