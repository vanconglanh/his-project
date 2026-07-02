-- ============================================================
-- Migration: 0039_seed_permissions_sprint6_7
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-PH-10..50 (Sprint 6-7 EPIC 5)
-- Idempotent: YES (INSERT ... ON DUPLICATE KEY UPDATE)
-- ============================================================
SET NAMES utf8mb4;

-- ============================================================
-- 30 new permissions for Sprint 6-7
-- ============================================================
INSERT INTO `sec_permissions` (`code`, `resource`, `action`, `description`) VALUES
-- Prescription
  ('prescription.read',    'prescription', 'read',    'Xem don thuoc'),
  ('prescription.create',  'prescription', 'create',  'Tao don thuoc moi'),
  ('prescription.update',  'prescription', 'update',  'Cap nhat don thuoc (DRAFT)'),
  ('prescription.sign',    'prescription', 'sign',    'Ky so don thuoc bang USB token'),
  ('prescription.cancel',  'prescription', 'cancel',  'Huy don thuoc'),
-- DDI
  ('ddi.check',            'ddi',          'check',   'Kiem tra tuong tac thuoc (DDI)'),
-- DTQG
  ('dtqg.submit',          'dtqg',         'submit',  'Gui don thuoc len DTQG'),
  ('dtqg.retry',           'dtqg',         'retry',   'Thu lai gui DTQG khi that bai'),
  ('dtqg.admin',           'dtqg',         'admin',   'Quan tri tich hop DTQG (credentials, cancel portal)'),
-- Drug
  ('drug.read',            'drug',         'read',    'Xem danh muc thuoc'),
  ('drug.write',           'drug',         'write',   'Tao / sua / xoa thuoc'),
  ('drug.import',          'drug',         'import',  'Import thuoc tu Excel'),
  ('drug.sync',            'drug',         'sync',    'Dong bo CSDL Duoc QG (Cuc QLD)'),
-- Warehouse
  ('warehouse.read',       'warehouse',    'read',    'Xem kho duoc'),
  ('warehouse.write',      'warehouse',    'write',   'Tao / sua / xoa kho duoc'),
-- Stock
  ('stock.read',           'stock',        'read',    'Xem ton kho, bien dong kho'),
  ('stock.adjust',         'stock',        'adjust',  'Dieu chinh ton kho, chuyen kho, kiem ke'),
-- Dispense
  ('dispense.queue',       'dispense',     'queue',   'Xem hang doi phat thuoc'),
  ('dispense.perform',     'dispense',     'perform', 'Thuc hien phat thuoc FEFO'),
  ('dispense.reject',      'dispense',     'reject',  'Tu choi phat thuoc'),
  ('dispense.return',      'dispense',     'return',  'Hoan tra thuoc da phat')
ON DUPLICATE KEY UPDATE `description` = VALUES(`description`);

-- ============================================================
-- Role → Permission mapping
-- ============================================================

-- BACSI: ke don, ky so, DDI, gui DTQG, xem thuoc
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('BACSI', 'prescription.read'),
  ('BACSI', 'prescription.create'),
  ('BACSI', 'prescription.update'),
  ('BACSI', 'prescription.sign'),
  ('BACSI', 'prescription.cancel'),
  ('BACSI', 'ddi.check'),
  ('BACSI', 'dtqg.submit'),
  ('BACSI', 'drug.read')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- DUOCSI: tat ca drug/warehouse/stock/dispense + xem don + DDI
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('DUOCSI', 'drug.read'),
  ('DUOCSI', 'drug.write'),
  ('DUOCSI', 'drug.import'),
  ('DUOCSI', 'warehouse.read'),
  ('DUOCSI', 'warehouse.write'),
  ('DUOCSI', 'stock.read'),
  ('DUOCSI', 'stock.adjust'),
  ('DUOCSI', 'dispense.queue'),
  ('DUOCSI', 'dispense.perform'),
  ('DUOCSI', 'dispense.reject'),
  ('DUOCSI', 'dispense.return'),
  ('DUOCSI', 'prescription.read'),
  ('DUOCSI', 'ddi.check')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- ADMIN: tat ca + dtqg.admin + drug.sync
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('ADMIN', 'prescription.read'),
  ('ADMIN', 'prescription.create'),
  ('ADMIN', 'prescription.update'),
  ('ADMIN', 'prescription.sign'),
  ('ADMIN', 'prescription.cancel'),
  ('ADMIN', 'ddi.check'),
  ('ADMIN', 'dtqg.submit'),
  ('ADMIN', 'dtqg.retry'),
  ('ADMIN', 'dtqg.admin'),
  ('ADMIN', 'drug.read'),
  ('ADMIN', 'drug.write'),
  ('ADMIN', 'drug.import'),
  ('ADMIN', 'drug.sync'),
  ('ADMIN', 'warehouse.read'),
  ('ADMIN', 'warehouse.write'),
  ('ADMIN', 'stock.read'),
  ('ADMIN', 'stock.adjust'),
  ('ADMIN', 'dispense.queue'),
  ('ADMIN', 'dispense.perform'),
  ('ADMIN', 'dispense.reject'),
  ('ADMIN', 'dispense.return')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- LETAN: xem don + xem thuoc
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('LETAN', 'prescription.read'),
  ('LETAN', 'drug.read')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- KETOAN: xem don + xem ton kho
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('KETOAN', 'prescription.read'),
  ('KETOAN', 'stock.read')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);
