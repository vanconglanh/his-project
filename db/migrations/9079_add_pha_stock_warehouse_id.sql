-- ============================================================
-- Migration: 9079_add_pha_stock_warehouse_id
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Generated: 2026-08-23
-- Story refs: BUG (Critical) — Nhap kho (GRN) / Dieu chinh kho / Chuyen
--   kho (WarehouseHandlers.cs: CreateGrnHandler, CreateAdjustmentHandler,
--   CreateTransferHandler) dang INSERT/UPDATE/WHERE vao VIEW `pha_stocks`
--   (SELECT * FROM diab_his_pha_stock) voi cac cot KHONG TON TAI:
--   warehouse_id, batch_no, manufacture_date, expiry_date,
--   quantity_available, quantity_reserved, unit_cost, reorder_level.
--   Bang goc `diab_his_pha_stock` (migration 9005) chi co:
--   lot_number, mfg_date, exp_date, quantity, import_price, location.
--   -> moi request GRN/Adjustment/Transfer nem MySqlException "Unknown
--   column" -> 500.
--
-- QUYET DINH VE warehouse_id (theo yeu cau tu-quyet-dinh cua task):
--   Da doc ky WarehouseHandlers.cs — tinh nang da-kho (multi-warehouse)
--   la nghiep vu THAT, khong phai code thua: bang `pha_warehouses` (INT
--   AUTO_INCREMENT, migration 9026) da ton tai va dang duoc dung that su
--   boi CRUD kho (ListWarehousesHandler...), va
--   `diab_his_pha_purchase_orders.warehouse_id INT NOT NULL` (migration
--   0037) da tham chieu toi `pha_warehouses.id`. Cac DTO ghi/doc kho cua
--   module Warehouse (StockAdjustmentRequest.WarehouseId,
--   TransferRequest.From/ToWarehouseId, GRN doc tu po.warehouse_id...)
--   deu dung kieu INT nhat quan voi `pha_warehouses.id`.
--   => them cot `warehouse_id INT NULL` (KHONG dat FK constraint, giu
--   dung quy uoc da co san trong repo: diab_his_pha_purchase_orders cung
--   chi co INDEX tren warehouse_id, khong co FK constraint) — day la
--   phuong an it rui ro nhat, giu nguyen tinh nang da-kho, khong doi kieu
--   du lieu cua toan bo cac DTO/handler khac cua module Warehouse.
--   (Luu y: day la mot quy uoc RIENG cho module Warehouse — khac voi
--   `diab_his_pha_stock_movements.warehouse_id` / cot cung ten tren
--   `diab_his_pha_dispense_records` da duoc migration 9025 doi sang
--   VARCHAR(36) cho mot khai niem "ma kho chuoi" rieng cua module
--   Dispensing (vd "default"), khong lien quan toi pha_warehouses.id.
--   Hai module hien dang dung 2 quy uoc warehouse-id khac nhau — ngoai
--   pham vi sua cua bug nay, KHONG hop nhat trong migration nay de tranh
--   pha vo module Dispensing dang hoat dong.)
--
-- QUYET DINH VE quantity_reserved / reorder_level: KHONG them cot moi.
--   - reorder_level: da ton tai san o cap DO THUOC
--     (diab_his_pha_drugs.reorder_level, migration 9005), khong phai cap
--     tung lo/stock. Cac handler doc (ListStocksHandler...) da JOIN dung
--     diab_his_pha_drugs de lay reorder_level — GRN/Adjustment/Transfer
--     se KHONG ghi gia tri nay vao diab_his_pha_stock nua (truoc day ghi
--     '10' cung boi vi cot khong ton tai, khong phai gia tri that).
--   - quantity_reserved: chua duoc theo doi thuc su o bat ky noi nao
--     trong schema that (cac handler doc deu hard-code "0 AS
--     quantity_reserved"). Giu nguyen quy uoc nay o phia ghi (luon 0,
--     khong ton tai cot rieng) de nhat quan voi phia doc, tranh tao cot
--     "mo coi" khong bao gio duoc cap nhat dung.
--
-- Idempotent: YES (kiem tra COLUMN_NAME qua information_schema)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _add_pha_stock_warehouse_id_9079;
DELIMITER $$
CREATE PROCEDURE _add_pha_stock_warehouse_id_9079()
BEGIN
    DECLARE v_db VARCHAR(64);
    SET v_db = DATABASE();

    -- 1) Them cot warehouse_id (INT NULL) neu chua co
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = v_db
           AND TABLE_NAME   = 'diab_his_pha_stock'
           AND COLUMN_NAME  = 'warehouse_id'
    ) THEN
        ALTER TABLE `diab_his_pha_stock`
            ADD COLUMN `warehouse_id` INT NULL COMMENT 'FK -> pha_warehouses.id (khong co FK constraint, theo quy uoc chung cua module Warehouse)'
            AFTER `tenant_id`;
    END IF;

    -- 2) Index phuc vu loc theo kho
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
         WHERE TABLE_SCHEMA = v_db
           AND TABLE_NAME   = 'diab_his_pha_stock'
           AND INDEX_NAME   = 'idx_stock_warehouse'
    ) THEN
        ALTER TABLE `diab_his_pha_stock`
            ADD INDEX `idx_stock_warehouse` (`tenant_id`, `warehouse_id`, `drug_id`);
    END IF;

    -- 3) UNIQUE KEY (tenant_id, warehouse_id, drug_id, lot_number) de
    --    CreateGrnHandler co the dung ON DUPLICATE KEY UPDATE khi nhap
    --    them cung mot lo thuoc/kho (gop so luong thay vi tao dong moi).
    --    Cac row cu (warehouse_id IS NULL, tao truoc migration nay) KHONG
    --    bi rang buoc boi UNIQUE nay vi MySQL coi moi NULL la khac nhau.
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
         WHERE TABLE_SCHEMA = v_db
           AND TABLE_NAME   = 'diab_his_pha_stock'
           AND INDEX_NAME   = 'uq_stock_tenant_wh_drug_lot'
    ) THEN
        ALTER TABLE `diab_his_pha_stock`
            ADD UNIQUE KEY `uq_stock_tenant_wh_drug_lot` (`tenant_id`, `warehouse_id`, `drug_id`, `lot_number`);
    END IF;
END$$
DELIMITER ;
CALL _add_pha_stock_warehouse_id_9079();
DROP PROCEDURE IF EXISTS _add_pha_stock_warehouse_id_9079;
