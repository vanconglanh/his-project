-- Migration 0044: Seed permissions Sprint 8 (Cashier + Billing)
-- Idempotent via INSERT IGNORE

INSERT IGNORE INTO sec_permissions (name, description, module, created_at)
VALUES
  -- Service catalog
  ('service.read',           'Xem danh muc dich vu',            'billing', NOW()),
  ('service.write',          'Them/sua dich vu',                'billing', NOW()),
  ('service_package.read',   'Xem goi kham',                    'billing', NOW()),
  ('service_package.write',  'Them/sua goi kham',               'billing', NOW()),
  -- Billing
  ('billing.read',           'Xem hoa don',                     'billing', NOW()),
  ('billing.create',         'Tao hoa don',                     'billing', NOW()),
  ('billing.update',         'Cap nhat hoa don (DRAFT)',         'billing', NOW()),
  ('billing.finalize',       'Finalize hoa don',                'billing', NOW()),
  ('billing.void',           'Huy hoa don',                     'billing', NOW()),
  ('billing.apply_bhyt',     'Ap dung BHYT vao hoa don',        'billing', NOW()),
  -- Payments
  ('payment.read',           'Xem thanh toan',                  'billing', NOW()),
  ('payment.collect',        'Thu tien',                        'billing', NOW()),
  ('payment.refund',         'Hoan tien',                       'billing', NOW()),
  ('payment.void',           'Huy thanh toan',                  'billing', NOW()),
  ('payment_qr.generate',    'Tao ma QR thanh toan',            'billing', NOW()),
  -- eInvoice
  ('einvoice.read',          'Xem hoa don dien tu',             'billing', NOW()),
  ('einvoice.issue',         'Phat hanh hoa don dien tu',       'billing', NOW()),
  ('einvoice.cancel',        'Huy hoa don dien tu',             'billing', NOW()),
  -- Cashier closing
  ('cashier.report',         'Bao cao ca thu ngan',             'billing', NOW()),
  ('cashier.shift_open',     'Mo ca thu ngan',                  'billing', NOW()),
  ('cashier.shift_close',    'Dong ca thu ngan',                'billing', NOW()),
  ('cashier.debt_view',      'Xem cong no benh nhan',           'billing', NOW());

-- Grant to KeToan role (id may vary per env; use name-based lookup)
INSERT IGNORE INTO sec_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM sec_roles r
CROSS JOIN sec_permissions p
WHERE r.name IN ('KeToan', 'Admin')
  AND p.name IN (
    'service.read','service.write','service_package.read','service_package.write',
    'billing.read','billing.create','billing.update','billing.finalize','billing.void','billing.apply_bhyt',
    'payment.read','payment.collect','payment.refund','payment.void','payment_qr.generate',
    'einvoice.read','einvoice.issue','einvoice.cancel',
    'cashier.report','cashier.shift_open','cashier.shift_close','cashier.debt_view'
  );

-- Grant read-only to LeTan
INSERT IGNORE INTO sec_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM sec_roles r
CROSS JOIN sec_permissions p
WHERE r.name = 'LeTan'
  AND p.name IN ('billing.read','payment.read','cashier.report','service.read');
