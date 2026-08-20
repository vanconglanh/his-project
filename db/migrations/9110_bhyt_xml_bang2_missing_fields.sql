-- ============================================================
-- Migration: 9110_bhyt_xml_bang2_missing_fields
-- Engine: MySQL 8.0+, InnoDB, utf8mb4_0900_ai_ci
-- Muc dich: Bo sung cot con thieu de xuat Bang 2 (thuoc BHYT) trong XML 4210/QD 4750
--   - SO_DANG_KY (so dang ky luu hanh thuoc) va MA_NHA_THAU (ma nha thau trung thau)
--     chua co nguon du lieu nao trong he thong -> them cot NULL, CHUA backfill,
--     CHUA co nghiep vu nhap lieu. XML sinh ra se de trong 2 truong nay cho toi khi
--     co module quan ly dau thau / danh muc dang ky thuoc.
--   - Mahieu lo (batch_no) / han dung (expiry_date) da co san trong
--     diab_his_pha_dispense_items (migration 0038) -> KHONG can them cot, chi can JOIN
--     (xem BhytXmlSql.PrescriptionItems).
-- Idempotent: YES (add_col_if_missing tu 0000_helpers.sql)
-- Phu thuoc: 0000_helpers.sql, 9005_create_pharmacy.sql
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_pha_drugs', 'so_dang_ky',
    "VARCHAR(50) NULL COMMENT 'So dang ky luu hanh thuoc (Cuc Quan ly Duoc) - dung cho XML Bang 2 QD 4750, CHUA co nguon du lieu nhap lieu'");

CALL add_col_if_missing('diab_his_pha_drugs', 'ma_nha_thau',
    "VARCHAR(50) NULL COMMENT 'Ma nha thau trung thau cung ung thuoc - dung cho XML Bang 2 QD 4750, CHUA co nguon du lieu nhap lieu'");
