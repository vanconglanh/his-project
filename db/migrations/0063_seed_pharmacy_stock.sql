-- Migration: 0063_seed_pharmacy_stock.sql
-- Muc dich: Seed warehouse WH01 va 30 lo hang pha_stocks cho tenant_id=1 (test data)
-- Idempotent: dung INSERT IGNORE / ON DUPLICATE KEY UPDATE

SET @tid = 1;

-- 1. Seed warehouse WH01 neu chua co (dung CODE la unique key trong pha_warehouses)
INSERT INTO pha_warehouses (CODE, NAME, HOSPITAL_ID, TYPE, STATUS, STATUS_FLAG, tenant_id, is_active, CREATED_AT, LAST_UPDATED_AT)
SELECT 'WH01', N'Kho chính', 1, 'MAIN', 1, 1, @tid, 1, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM pha_warehouses WHERE CODE = 'WH01' AND tenant_id = @tid
);

SET @wid = (SELECT ID FROM pha_warehouses WHERE CODE = 'WH01' AND tenant_id = @tid LIMIT 1);

-- 2. Seed pha_stocks: moi thuoc 2 lo binh thuong + 5 lo gan het han
-- Dung INSERT IGNORE de tranh trung batch_no
INSERT IGNORE INTO pha_stocks
    (id, tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date, quantity_available, quantity_reserved, unit_cost, reorder_level, status, created_at, updated_at)
SELECT
    UUID(),
    @tid,
    @wid,
    dm.id,
    CONCAT('LOT', LPAD(FLOOR(RAND() * 9000 + 1000), 4, '0'), '-A'),
    '2025-01-01',
    DATE_ADD(CURDATE(), INTERVAL (180 + FLOOR(RAND() * 180)) DAY),
    FLOOR(RAND() * 400 + 100),
    0,
    ROUND(dm.price * 0.7, 2),
    20,
    'ACTIVE',
    NOW(),
    NOW()
FROM pha_drug_master dm
WHERE dm.tenant_id = @tid AND dm.deleted_at IS NULL;

INSERT IGNORE INTO pha_stocks
    (id, tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date, quantity_available, quantity_reserved, unit_cost, reorder_level, status, created_at, updated_at)
SELECT
    UUID(),
    @tid,
    @wid,
    dm.id,
    CONCAT('LOT', LPAD(FLOOR(RAND() * 9000 + 1000), 4, '0'), '-B'),
    '2025-06-01',
    DATE_ADD(CURDATE(), INTERVAL (360 + FLOOR(RAND() * 365)) DAY),
    FLOOR(RAND() * 300 + 50),
    0,
    ROUND(dm.price * 0.72, 2),
    20,
    'ACTIVE',
    NOW(),
    NOW()
FROM pha_drug_master dm
WHERE dm.tenant_id = @tid AND dm.deleted_at IS NULL;

-- 3. Seed 5 lo gan het han (< 30 ngay) cho test alert near-expiry
INSERT IGNORE INTO pha_stocks
    (id, tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date, quantity_available, quantity_reserved, unit_cost, reorder_level, status, created_at, updated_at)
SELECT
    UUID(),
    @tid,
    @wid,
    dm.id,
    CONCAT('NEAREXP', LPAD(ROW_NUMBER() OVER (ORDER BY dm.id), 3, '0')),
    '2024-01-01',
    DATE_ADD(CURDATE(), INTERVAL FLOOR(RAND() * 25) DAY),
    FLOOR(RAND() * 80 + 10),
    0,
    ROUND(dm.price * 0.65, 2),
    20,
    'ACTIVE',
    NOW(),
    NOW()
FROM pha_drug_master dm
WHERE dm.tenant_id = @tid AND dm.deleted_at IS NULL
LIMIT 5;

-- 4. Seed 3 lo sap het ton (below reorder_level) cho test alert low-stock
INSERT IGNORE INTO pha_stocks
    (id, tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date, quantity_available, quantity_reserved, unit_cost, reorder_level, status, created_at, updated_at)
SELECT
    UUID(),
    @tid,
    @wid,
    dm.id,
    CONCAT('LOWSTK', LPAD(ROW_NUMBER() OVER (ORDER BY dm.id), 3, '0')),
    '2025-03-01',
    DATE_ADD(CURDATE(), INTERVAL 200 DAY),
    FLOOR(RAND() * 8 + 1),
    0,
    ROUND(dm.price * 0.7, 2),
    20,
    'ACTIVE',
    NOW(),
    NOW()
FROM pha_drug_master dm
WHERE dm.tenant_id = @tid AND dm.deleted_at IS NULL
LIMIT 3;
