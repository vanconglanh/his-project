-- ============================================================
-- Migration: 9131_fix_lab_order_invalid_uuid
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Bug: BUG-005 (Blocker) lien quan - cung pattern voi 9130, phat hien khi ra soat
--   toan bo cac bang co du lieu seed UUID sai dinh dang.
-- Nguyen nhan goc:
--   Du lieu seed trong 9020_seed_rich_demo.sql sinh 10 dong
--   diab_his_lab_orders.id dang 'lo000001-0000-0000-0000-00000000000N'.
--   Tien to 'lo' KHONG phai ky tu hex (0-9a-f) nen driver MySqlConnector/
--   Dapper khong the parse CHAR(36) nay thanh System.Guid khi doc du lieu
--   -> loi tuong tu BUG-005 se xay ra tren cac API doc du lieu module CLS
--   (lab orders/results).
--
-- Xu ly: doi tien to 'lo' -> '10' (2 ky tu hex hop le, giu nguyen do dai
--   CHAR(36) va phan con lai cua chuoi) cho ca bang cha diab_his_lab_orders
--   VA bang con diab_his_lab_results (cot order_id) de khong mo coi du lieu
--   tham chieu.
--
-- Idempotent: YES - chi UPDATE cac dong con id/order_id bat dau bang 'lo'
-- ============================================================
SET NAMES utf8mb4;

-- Tam thoi tat kiem tra FK: bang con diab_his_lab_results.order_id co FK
-- tro ve diab_his_lab_orders.id nen UPDATE khoa chinh/khoa ngoai theo 2 buoc
-- rieng se vi pham rang buoc neu bat FOREIGN_KEY_CHECKS. Bat lai ngay sau khi
-- hoan tat.
SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------------
-- 1) Cap nhat khoa chinh o bang cha
-- ------------------------------------------------------------
UPDATE `diab_his_lab_orders`
SET `id` = CONCAT('10', SUBSTRING(`id`, 3))
WHERE `id` LIKE 'lo%';

-- ------------------------------------------------------------
-- 2) Cap nhat khoa ngoai o bang con tuong ung
-- ------------------------------------------------------------
UPDATE `diab_his_lab_results`
SET `order_id` = CONCAT('10', SUBSTRING(`order_id`, 3))
WHERE `order_id` LIKE 'lo%';

SET FOREIGN_KEY_CHECKS = 1;

-- ------------------------------------------------------------
-- 3) Xac minh (chi de tham khao khi chay tay; khong anh huong migration)
--    SELECT COUNT(*) FROM diab_his_lab_orders WHERE id NOT REGEXP '^[0-9a-fA-F]{8}-';
--    SELECT COUNT(*) FROM diab_his_lab_results WHERE order_id NOT REGEXP '^[0-9a-fA-F]{8}-';
--    Ca hai phai tra ve 0.
-- ------------------------------------------------------------
