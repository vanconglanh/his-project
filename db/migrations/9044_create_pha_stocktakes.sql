-- ============================================================
-- Migration: 9044_create_pha_stocktakes
-- Mo ta: Bang phien kiem ke kho thuoc (Dot 11 BI + Kiem ke kho).
--   diab_his_pha_stocktakes  : 1 dong / phien kiem ke (theo ngay, dia diem).
--   diab_his_pha_stocktake_items: chi tiet tung thuoc/lo trong phien kiem ke
--   (ton he thong tai thoi diem kiem, ton thuc te dem tay, chenh lech).
--   Descriptor bao cao "kiem-ke-kho" (ReportRegistry, group=Pharmacy) doc tu
--   2 bang nay.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + INSERT ... WHERE NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

-- 1) Bang phien kiem ke -----------------------------------------------------
CREATE TABLE IF NOT EXISTS diab_his_pha_stocktakes (
    id              CHAR(36)       NOT NULL DEFAULT (UUID()),
    tenant_id       INT            NOT NULL,
    code            VARCHAR(30)    NULL COMMENT 'So phien kiem ke (vd KK-2026-07)',
    stocktake_date  DATE           NOT NULL,
    location        VARCHAR(50)    NULL COMMENT 'Vi tri/kho kiem ke (neu co nhieu kho)',
    status          VARCHAR(10)    NOT NULL DEFAULT 'COMPLETED' COMMENT 'DRAFT|COMPLETED',
    note            TEXT           NULL,
    created_at      DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by      CHAR(36)       NULL,
    updated_at      DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by      CHAR(36)       NULL,
    deleted_at      DATETIME(3)    NULL,
    deleted_by      CHAR(36)       NULL,
    PRIMARY KEY (id),
    INDEX idx_stocktakes_tenant_date (tenant_id, stocktake_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2) Bang chi tiet phien kiem ke ---------------------------------------------
CREATE TABLE IF NOT EXISTS diab_his_pha_stocktake_items (
    id              CHAR(36)       NOT NULL DEFAULT (UUID()),
    tenant_id       INT            NOT NULL,
    stocktake_id    CHAR(36)       NOT NULL,
    drug_id         CHAR(36)       NOT NULL,
    lot_number      VARCHAR(50)    NULL,
    system_qty      INT            NOT NULL DEFAULT 0 COMMENT 'Ton he thong (pha_stock.quantity) tai thoi diem kiem',
    counted_qty     INT            NOT NULL DEFAULT 0 COMMENT 'Ton thuc te dem duoc',
    difference      INT            NOT NULL DEFAULT 0 COMMENT 'counted_qty - system_qty',
    note            TEXT           NULL,
    created_at      DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by      CHAR(36)       NULL,
    updated_at      DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by      CHAR(36)       NULL,
    deleted_at      DATETIME(3)    NULL,
    deleted_by      CHAR(36)       NULL,
    PRIMARY KEY (id),
    INDEX idx_stocktake_items_tenant_stocktake (tenant_id, stocktake_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3) SEED DEMO (dev) — 1 phien kiem ke cho tenant_id=1, ngay 2026-07-07,
--    voi 5 item lay tu diab_his_pha_stock hien co (system_qty = pha_stock.quantity
--    that tai thoi diem seed), counted_qty lech nhe mot vai item de thay chenh
--    lech khi review bao cao "kiem-ke-kho".
--    Day la du lieu DEMO, KHONG dung cho production/UAT that.
--    Idempotent qua WHERE NOT EXISTS theo (tenant_id, code).
SET @stocktake_id = (SELECT id FROM diab_his_pha_stocktakes WHERE tenant_id = 1 AND code = 'KK-2026-07');
SET @stocktake_id = COALESCE(@stocktake_id, UUID());

INSERT INTO diab_his_pha_stocktakes (id, tenant_id, code, stocktake_date, location, status, note, created_at)
SELECT @stocktake_id, 1, 'KK-2026-07', '2026-07-07', 'Kho chinh', 'COMPLETED',
       N'Phien kiem ke dinh ky thang 7 (du lieu DEMO)', '2026-07-07 09:00:00'
 WHERE NOT EXISTS (SELECT 1 FROM diab_his_pha_stocktakes WHERE tenant_id = 1 AND code = 'KK-2026-07');

-- Item 1: Paracetamol 500mg / LOT-P002 — dem du (khong chenh lech)
INSERT INTO diab_his_pha_stocktake_items (id, tenant_id, stocktake_id, drug_id, lot_number, system_qty, counted_qty, difference, note)
SELECT UUID(), 1, @stocktake_id, s.drug_id, s.lot_number, s.quantity, s.quantity, 0, N'Khop ton he thong'
FROM diab_his_pha_stock s
WHERE s.tenant_id = 1 AND s.lot_number = 'LOT-P002'
  AND NOT EXISTS (
        SELECT 1 FROM diab_his_pha_stocktake_items i
         WHERE i.tenant_id = 1 AND i.stocktake_id = @stocktake_id AND i.lot_number = 'LOT-P002')
LIMIT 1;

-- Item 2: Paracetamol 500mg / LOT-P001 — thieu 5 vien
INSERT INTO diab_his_pha_stocktake_items (id, tenant_id, stocktake_id, drug_id, lot_number, system_qty, counted_qty, difference, note)
SELECT UUID(), 1, @stocktake_id, s.drug_id, s.lot_number, s.quantity, s.quantity - 5, -5, N'Thieu hut, can kiem tra lai'
FROM diab_his_pha_stock s
WHERE s.tenant_id = 1 AND s.lot_number = 'LOT-P001'
  AND NOT EXISTS (
        SELECT 1 FROM diab_his_pha_stocktake_items i
         WHERE i.tenant_id = 1 AND i.stocktake_id = @stocktake_id AND i.lot_number = 'LOT-P001')
LIMIT 1;

-- Item 3: Metformin 500mg / LOT-M002 — khop
INSERT INTO diab_his_pha_stocktake_items (id, tenant_id, stocktake_id, drug_id, lot_number, system_qty, counted_qty, difference, note)
SELECT UUID(), 1, @stocktake_id, s.drug_id, s.lot_number, s.quantity, s.quantity, 0, N'Khop ton he thong'
FROM diab_his_pha_stock s
WHERE s.tenant_id = 1 AND s.lot_number = 'LOT-M002'
  AND NOT EXISTS (
        SELECT 1 FROM diab_his_pha_stocktake_items i
         WHERE i.tenant_id = 1 AND i.stocktake_id = @stocktake_id AND i.lot_number = 'LOT-M002')
LIMIT 1;

-- Item 4: Metformin 500mg / LOT-M001 — du 3 vien (nhap thua chua ghi nhan)
INSERT INTO diab_his_pha_stocktake_items (id, tenant_id, stocktake_id, drug_id, lot_number, system_qty, counted_qty, difference, note)
SELECT UUID(), 1, @stocktake_id, s.drug_id, s.lot_number, s.quantity, s.quantity + 3, 3, N'Du so voi he thong, can doi chieu phieu nhap'
FROM diab_his_pha_stock s
WHERE s.tenant_id = 1 AND s.lot_number = 'LOT-M001'
  AND NOT EXISTS (
        SELECT 1 FROM diab_his_pha_stocktake_items i
         WHERE i.tenant_id = 1 AND i.stocktake_id = @stocktake_id AND i.lot_number = 'LOT-M001')
LIMIT 1;

-- Item 5: Omeprazole 20mg / LOT-O002 — khop
INSERT INTO diab_his_pha_stocktake_items (id, tenant_id, stocktake_id, drug_id, lot_number, system_qty, counted_qty, difference, note)
SELECT UUID(), 1, @stocktake_id, s.drug_id, s.lot_number, s.quantity, s.quantity, 0, N'Khop ton he thong'
FROM diab_his_pha_stock s
WHERE s.tenant_id = 1 AND s.lot_number = 'LOT-O002'
  AND NOT EXISTS (
        SELECT 1 FROM diab_his_pha_stocktake_items i
         WHERE i.tenant_id = 1 AND i.stocktake_id = @stocktake_id AND i.lot_number = 'LOT-O002')
LIMIT 1;
