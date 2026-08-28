-- ============================================================
-- Migration: 9130_fix_prescription_invalid_uuid
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Bug: BUG-005 (Blocker) - GET /api/v1/prescriptions tra HTTP 500
-- Nguyen nhan goc:
--   Du lieu seed trong 9008_seed_demo.sql sinh 10 dong
--   diab_his_pha_prescriptions.id dang 'rx000001-0000-0000-0000-00000000000N'.
--   Tien to 'rx' KHONG phai ky tu hex (0-9a-f) nen driver MySqlConnector/
--   Dapper khong the parse CHAR(36) nay thanh System.Guid khi doc du lieu
--   -> System.FormatException -> HTTP 500 tren toan bo API GET /prescriptions.
--
-- Xu ly: doi tien to 'rx' -> '70' (2 ky tu hex hop le, giu nguyen do dai
--   CHAR(36) va phan con lai cua chuoi) cho ca bang cha
--   diab_his_pha_prescriptions VA bang con diab_his_pha_prescription_items
--   (cot prescription_id) de khong mo coi du lieu tham chieu.
--
-- Idempotent: YES - chi UPDATE cac dong con id/prescription_id bat dau bang 'rx'
-- ============================================================
SET NAMES utf8mb4;

-- Tam thoi tat kiem tra FK: bang cha co FK ON DELETE CASCADE tu bang con
-- (diab_his_pha_prescription_items.prescription_id -> diab_his_pha_prescriptions.id)
-- nen UPDATE khoa chinh/khoa ngoai theo 2 buoc rieng se vi pham rang buoc
-- neu bat FOREIGN_KEY_CHECKS. Bat lai ngay sau khi hoan tat.
SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------------
-- 1) Cap nhat khoa chinh o bang cha
-- ------------------------------------------------------------
UPDATE `diab_his_pha_prescriptions`
SET `id` = CONCAT('70', SUBSTRING(`id`, 3))
WHERE `id` LIKE 'rx%';

-- ------------------------------------------------------------
-- 2) Cap nhat khoa ngoai o bang con tuong ung
-- ------------------------------------------------------------
UPDATE `diab_his_pha_prescription_items`
SET `prescription_id` = CONCAT('70', SUBSTRING(`prescription_id`, 3))
WHERE `prescription_id` LIKE 'rx%';

SET FOREIGN_KEY_CHECKS = 1;

-- ------------------------------------------------------------
-- 3) Xac minh (chi de tham khao khi chay tay; khong anh huong migration)
--    SELECT COUNT(*) FROM diab_his_pha_prescriptions WHERE id NOT REGEXP '^[0-9a-fA-F]{8}-';
--    SELECT COUNT(*) FROM diab_his_pha_prescription_items WHERE prescription_id NOT REGEXP '^[0-9a-fA-F]{8}-';
--    Ca hai phai tra ve 0.
-- ------------------------------------------------------------
