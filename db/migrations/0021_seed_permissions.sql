-- ============================================================
-- Migration: 0021_seed_permissions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-U09 US-U10 (Sprint 1 EPIC 1)
-- Idempotent: YES (INSERT ... ON DUPLICATE KEY UPDATE)
-- ============================================================
SET NAMES utf8mb4;

-- Tao bang sec_permissions neu chua co (idempotent)
CREATE TABLE IF NOT EXISTS `sec_permissions` (
  `id` CHAR(36) NOT NULL DEFAULT (UUID()),
  `code` VARCHAR(100) NOT NULL COMMENT 'resource.action vd: patient.read',
  `resource` VARCHAR(50) NOT NULL COMMENT 'Nhom tai nguyen',
  `action` VARCHAR(50) NOT NULL COMMENT 'Hanh dong',
  `description` VARCHAR(255) NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UK_PERM_CODE` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh sach quyen he thong';

-- Tao bang sec_role_permissions neu chua co (idempotent)
CREATE TABLE IF NOT EXISTS `sec_role_permissions` (
  `role_code` VARCHAR(32) NOT NULL,
  `permission_code` VARCHAR(100) NOT NULL,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`role_code`, `permission_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Mapping role -> permission';

-- ============================================================
-- Seed permissions (~40 permission resource.action)
-- ============================================================
INSERT INTO `sec_permissions` (`code`, `resource`, `action`, `description`) VALUES
-- Tenant
  ('tenant.read',            'tenant',       'read',        'Xem thong tin phong kham'),
  ('tenant.write',           'tenant',       'write',       'Cap nhat thong tin phong kham'),
  ('tenant.suspend',         'tenant',       'suspend',     'Tam ngung / kich hoat phong kham'),
-- User
  ('user.read',              'user',         'read',        'Xem danh sach nguoi dung'),
  ('user.invite',            'user',         'invite',      'Moi nguoi dung moi'),
  ('user.write',             'user',         'write',       'Cap nhat thong tin nguoi dung'),
  ('user.disable',           'user',         'disable',     'Khoa/Mo khoa nguoi dung'),
  ('user.delete',            'user',         'delete',      'Xoa mem nguoi dung'),
  ('user.assign_role',       'user',         'assign_role', 'Gan / thu hoi role'),
-- Role
  ('role.read',              'role',         'read',        'Xem danh sach vai tro'),
  ('role.write',             'role',         'write',       'Tao / sua / xoa custom role'),
-- Patient
  ('patient.read',           'patient',      'read',        'Xem ho so benh nhan'),
  ('patient.write',          'patient',      'write',       'Tao / sua ho so benh nhan'),
  ('patient.delete',         'patient',      'delete',      'Xoa ho so benh nhan'),
-- Encounter
  ('encounter.read',         'encounter',    'read',        'Xem luot kham'),
  ('encounter.create',       'encounter',    'create',      'Tao luot kham moi'),
  ('encounter.update',       'encounter',    'update',      'Cap nhat luot kham'),
  ('encounter.close',        'encounter',    'close',       'Dong luot kham'),
-- Vital sign
  ('vital_sign.read',        'vital_sign',   'read',        'Xem chi so sinh ton'),
  ('vital_sign.write',       'vital_sign',   'write',       'Nhap chi so sinh ton'),
-- Lab
  ('lab.read',               'lab',          'read',        'Xem ket qua xet nghiem'),
  ('lab.order',              'lab',          'order',       'Chi dinh xet nghiem'),
  ('lab.result_enter',       'lab',          'result_enter','Nhap ket qua xet nghiem'),
-- Radiology
  ('rad.read',               'rad',          'read',        'Xem ket qua CDHA'),
  ('rad.order',              'rad',          'order',       'Chi dinh CDHA'),
  ('rad.result_enter',       'rad',          'result_enter','Nhap ket qua CDHA'),
-- Prescription
  ('prescription.read',      'prescription', 'read',        'Xem don thuoc'),
  ('prescription.create',    'prescription', 'create',      'Ke don thuoc'),
  ('prescription.sign',      'prescription', 'sign',        'Ky xac nhan don thuoc'),
  ('prescription.dtqg_submit','prescription','dtqg_submit', 'Day don thuoc len DTQG'),
-- Pharmacy
  ('pharmacy.read',          'pharmacy',     'read',        'Xem kho thuoc'),
  ('pharmacy.import',        'pharmacy',     'import',      'Nhap kho thuoc'),
  ('pharmacy.export',        'pharmacy',     'export',      'Xuat kho thuoc'),
  ('pharmacy.adjust',        'pharmacy',     'adjust',      'Dieu chinh ton kho'),
-- Drug
  ('drug.read',              'drug',         'read',        'Xem danh muc thuoc'),
  ('drug.write',             'drug',         'write',       'Them / sua danh muc thuoc'),
-- Billing
  ('billing.read',           'billing',      'read',        'Xem hoa don'),
  ('billing.collect',        'billing',      'collect',     'Thu tien'),
  ('billing.refund',         'billing',      'refund',      'Hoan tien'),
-- BHYT
  ('bhyt.read',              'bhyt',         'read',        'Xem du lieu BHYT'),
  ('bhyt.export',            'bhyt',         'export',      'Xuat XML BHYT'),
-- Report
  ('report.read',            'report',       'read',        'Xem bao cao thong ke'),
  ('report.export',          'report',       'export',      'Xuat bao cao'),
-- Audit
  ('audit.read',             'audit',        'read',        'Xem nhat ky thao tac'),
-- Appointment
  ('appointment.read',       'appointment',  'read',        'Xem lich hen'),
  ('appointment.write',      'appointment',  'write',       'Tao / sua lich hen'),
-- CLS upload
  ('cls_upload.create',      'cls_upload',   'create',      'Tai len ket qua CLS'),
-- API partner
  ('api_partner.read',       'api_partner',  'read',        'Xem danh sach doi tac API'),
  ('api_partner.write',      'api_partner',  'write',       'Quan ly doi tac API'),
-- EMR
  ('emr.read',               'emr',          'read',        'Xem benh an dien tu'),
  ('emr.write',              'emr',          'write',       'Viet benh an dien tu')
ON DUPLICATE KEY UPDATE
  `resource` = VALUES(`resource`),
  `action`   = VALUES(`action`),
  `description` = VALUES(`description`);

-- ============================================================
-- Seed role -> permission mapping theo Permission Matrix
-- SUPER_ADMIN: toan quyen (them vao neu can; cross-tenant bypass)
-- ============================================================

-- ADMIN
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('ADMIN','tenant.read'), ('ADMIN','tenant.write'),
  ('ADMIN','user.read'), ('ADMIN','user.invite'), ('ADMIN','user.write'),
  ('ADMIN','user.disable'), ('ADMIN','user.delete'), ('ADMIN','user.assign_role'),
  ('ADMIN','role.read'), ('ADMIN','role.write'), ('ADMIN','audit.read')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- BACSI
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('BACSI','patient.read'),
  ('BACSI','encounter.read'), ('BACSI','encounter.create'), ('BACSI','encounter.update'), ('BACSI','encounter.close'),
  ('BACSI','prescription.create'), ('BACSI','prescription.sign'),
  ('BACSI','lab.read'), ('BACSI','emr.read'), ('BACSI','emr.write'),
  ('BACSI','vital_sign.read')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- DIEUDUONG
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('DIEUDUONG','patient.read'),
  ('DIEUDUONG','encounter.read'),
  ('DIEUDUONG','vital_sign.read'), ('DIEUDUONG','vital_sign.write')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- LETAN
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('LETAN','patient.read'), ('LETAN','patient.write'),
  ('LETAN','encounter.create'),
  ('LETAN','appointment.read'), ('LETAN','appointment.write'),
  ('LETAN','cls_upload.create')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- DUOCSI
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('DUOCSI','prescription.read'),
  ('DUOCSI','pharmacy.read'), ('DUOCSI','pharmacy.import'), ('DUOCSI','pharmacy.export'), ('DUOCSI','pharmacy.adjust'),
  ('DUOCSI','drug.read'), ('DUOCSI','drug.write')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- KETOAN
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('KETOAN','billing.read'), ('KETOAN','billing.collect'), ('KETOAN','billing.refund'),
  ('KETOAN','report.read')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- KYTHUATVIEN
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('KYTHUATVIEN','lab.read'), ('KYTHUATVIEN','lab.order'), ('KYTHUATVIEN','lab.result_enter'),
  ('KYTHUATVIEN','rad.read'), ('KYTHUATVIEN','rad.order'), ('KYTHUATVIEN','rad.result_enter')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);
