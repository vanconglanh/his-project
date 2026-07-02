-- ============================================================
-- Migration: 0024_seed_permissions_sprint2
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-RC01..RC06, US-P07..P12, SUNS-I.3
-- Idempotent: YES (INSERT ON DUPLICATE KEY UPDATE)
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `sec_permissions` (`code`, `resource`, `action`, `description`) VALUES
-- Reception
  ('reception.checkin',          'reception',   'checkin',          'Tiep don benh nhan, tao ticket hang doi'),
  ('reception.queue.manage',     'reception',   'queue.manage',     'Quan ly hang doi tiep don'),
  ('reception.rooms.read',       'reception',   'rooms.read',       'Xem danh sach phong kham'),
  ('reception.stats.read',       'reception',   'stats.read',       'Xem thong ke tiep don'),
-- Patient extended
  ('patient.read.pii',           'patient',     'read.pii',         'Xem du lieu PII plaintext (CMND, BHYT)'),
  ('patient.avatar.write',       'patient',     'avatar.write',     'Upload anh dai dien benh nhan'),
-- CLS upload
  ('cls_upload.read',            'cls_upload',  'read',             'Xem tai lieu CLS'),
  ('cls_upload.delete',          'cls_upload',  'delete',           'Xoa tai lieu CLS'),
-- File generic
  ('file.upload',                'file',        'upload',           'Upload file generic'),
  ('file.delete',                'file',        'delete',           'Xoa file')
ON DUPLICATE KEY UPDATE
  `resource`    = VALUES(`resource`),
  `action`      = VALUES(`action`),
  `description` = VALUES(`description`);

-- LETAN: them quyen reception
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('LETAN', 'reception.checkin'),
  ('LETAN', 'reception.queue.manage'),
  ('LETAN', 'reception.rooms.read'),
  ('LETAN', 'reception.stats.read'),
  ('LETAN', 'patient.avatar.write'),
  ('LETAN', 'cls_upload.read'),
  ('LETAN', 'cls_upload.delete'),
  ('LETAN', 'file.upload'),
  ('LETAN', 'file.delete')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- BACSI: them quyen cls, file, reception read
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('BACSI', 'reception.queue.manage'),
  ('BACSI', 'reception.rooms.read'),
  ('BACSI', 'cls_upload.read'),
  ('BACSI', 'cls_upload.create'),
  ('BACSI', 'file.upload'),
  ('BACSI', 'patient.read.pii')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);

-- ADMIN: toan quyen
INSERT INTO `sec_role_permissions` (`role_code`, `permission_code`) VALUES
  ('ADMIN', 'reception.checkin'),
  ('ADMIN', 'reception.queue.manage'),
  ('ADMIN', 'reception.rooms.read'),
  ('ADMIN', 'reception.stats.read'),
  ('ADMIN', 'patient.read.pii'),
  ('ADMIN', 'patient.avatar.write'),
  ('ADMIN', 'cls_upload.read'),
  ('ADMIN', 'cls_upload.delete'),
  ('ADMIN', 'file.upload'),
  ('ADMIN', 'file.delete'),
  ('ADMIN', 'patient.delete')
ON DUPLICATE KEY UPDATE `role_code` = VALUES(`role_code`);
