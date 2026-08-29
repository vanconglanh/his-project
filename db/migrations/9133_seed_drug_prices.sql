-- ============================================================
-- Migration: 9133_seed_drug_prices
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Muc dich:
--   Danh muc thuoc (diab_his_pha_drugs) co 2 cot gia:
--     - sell_price : cot goc (schema dump), DA co du lieu hop ly.
--     - price      : cot bo sung boi migration 9010, dang NULL toan bo.
--   DrugHandlers (Dapper) doc `d.price AS Price` -> FE nhan Price = NULL
--   -> khi tao dong hoa don / don thuoc, don gia = 0 -> billing luon 0 VND,
--      khong test duoc so tien / BHYT that.
--   Fix: dong bo price = sell_price cho moi thuoc dang thieu gia (price NULL
--   hoac <= 0). Gia lay tu sell_price da la gia thi truong thuoc generic
--   pho bien tai VN nen hop ly.
-- Idempotent: YES (chi update dong price NULL/<=0; chay lai khong doi gi them).
-- ============================================================
SET NAMES utf8mb4;

-- Dong bo price tu sell_price cho cac thuoc con thieu gia ban le
UPDATE diab_his_pha_drugs
SET price = sell_price
WHERE (price IS NULL OR price <= 0)
  AND sell_price IS NOT NULL
  AND sell_price > 0;

-- Phong truong hop thuoc chua co sell_price (0/NULL): dat gia san toi thieu
-- hop ly de tranh don gia 0 khi test (gia generic pho bien ~1.000 VND/vien).
UPDATE diab_his_pha_drugs
SET price = 1000, sell_price = 1000
WHERE (price IS NULL OR price <= 0)
  AND (sell_price IS NULL OR sell_price <= 0);
