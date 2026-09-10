-- =============================================================================
-- 9207_cls_order_item_is_free.sql
-- Nguon: Lark Bug-Feedback (BM-16) - "Role BS - Man hinh Kham benh - tab CLS -
-- Luong Chot dot dang lan ca nghiep vu thu tien cua Thu ngan".
--
-- PO xac nhan (2026-09-10):
--   1) Cong THEM tinh nang moi, KHONG thay the: van giu nguyen nut "Mien phi"
--      ap dung ca dot o buoc Chot dot (WaiveClsRoundCommandHandler, khong doi
--      gi). Bo sung THEM checkbox "Khong thu phi" theo TUNG dich vu, dat ngay
--      luc tao dot (khong phai luc chot).
--   2) Du lieu dot cu: KHONG can migrate gia tri - cot moi mac dinh 0 (co thu
--      phi binh thuong), dung hanh vi cu 100% cho moi dot da ton tai.
--
-- Them cot is_free (BOOLEAN, default 0) vao tung dong chi dinh XN/CDHA. Khi
-- tinh tong tien dot (RecalcTotalAsync), dich vu is_free=1 khong duoc cong vao
-- total_amount (nhung van hien thi tren phieu chi dinh de biet dich vu nao
-- duoc mien - xem ClsRoundOrderItemResponse.IsFree).
--
-- Idempotent: YES (add_col_if_missing tu 0000_helpers.sql).
-- =============================================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_cli_lab_orders', 'is_free',
    'TINYINT(1) NOT NULL DEFAULT 0 COMMENT ''BM-16: khong thu phi dich vu nay (chi dinh tung dich vu, khac voi mien phi ca dot)''');

CALL add_col_if_missing('diab_his_cli_rad_orders', 'is_free',
    'TINYINT(1) NOT NULL DEFAULT 0 COMMENT ''BM-16: khong thu phi dich vu nay (chi dinh tung dich vu, khac voi mien phi ca dot)''');

-- Rollback:
--   ALTER TABLE diab_his_cli_lab_orders DROP COLUMN is_free;
--   ALTER TABLE diab_his_cli_rad_orders DROP COLUMN is_free;
