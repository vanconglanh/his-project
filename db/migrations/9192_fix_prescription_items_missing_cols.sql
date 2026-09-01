-- ============================================================
-- Migration: 9192_fix_prescription_items_missing_cols
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-09-01 (Leader smoke-test clean-DB fix)
-- ============================================================
-- LY DO: bang `diab_his_pha_prescription_items` duoc TAO TRUOC boi 0035
--   (lop 00xx) voi schema THIEU cot, nen `CREATE TABLE IF NOT EXISTS` o
--   9005_create_pharmacy.sql tro thanh NO-OP -> khi dung DB SACH tu chuoi
--   migration (khong con EnsureCreated), bang thieu 6 cot ma entity EF
--   PrescriptionItem + code doc (ListPrescriptions SUM line_total) can:
--     drug_name, drug_strength, unit, unit_price, line_total, bhyt_applicable
--   Trieu chung: GET /api/v1/prescriptions -> 500 "Unknown column 'i.line_total'".
--   (Integration test khong bat vi dung EnsureCreated tao du cot tu entity.)
-- Idempotent: YES (add_col_if_missing check information_schema; helper o 0000_helpers.sql)
-- Dinh nghia cot bam sat 9005_create_pharmacy.sql + entity PrescriptionItem.cs.
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_pha_prescription_items', 'drug_name',
    "VARCHAR(255) NOT NULL DEFAULT '' COMMENT 'Ten thuoc tai thoi diem ke don'");
CALL add_col_if_missing('diab_his_pha_prescription_items', 'drug_strength',
    "VARCHAR(100) NULL COMMENT 'Ham luong'");
CALL add_col_if_missing('diab_his_pha_prescription_items', 'unit',
    "VARCHAR(20) NOT NULL DEFAULT '' COMMENT 'Don vi'");
CALL add_col_if_missing('diab_his_pha_prescription_items', 'unit_price',
    "DECIMAL(12,2) NOT NULL DEFAULT 0 COMMENT 'Don gia'");
CALL add_col_if_missing('diab_his_pha_prescription_items', 'line_total',
    "DECIMAL(12,2) NOT NULL DEFAULT 0 COMMENT 'Thanh tien'");
CALL add_col_if_missing('diab_his_pha_prescription_items', 'bhyt_applicable',
    "TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'BHYT co chi tra khong'");
