-- ============================================================
-- Migration: 9212_prescription_dose_slots_and_version
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-09-26 (BM-32 - redesign tab Don thuoc man Kham benh)
-- ============================================================
-- LY DO:
--   1) BM-32 yeu cau nhap lieu dung theo tung buoi (sang/trua/chieu/toi) cho
--      moi dong thuoc trong don, thay vi chi 1 chuoi Dosage/Frequency tu do.
--      Bo sung 4 cot lieu dung dang so + 1 cot "cach dung" (huong dan rieng,
--      khac voi note/instructions chung).
--   2) Optimistic locking cho PUT /prescriptions/{id}: don thuoc truoc day
--      khong co cot version nen 2 nguoi cung sua se ghi de nhau am tham.
--      Them row_version, tang dan moi lan UPDATE, kiem tra WHERE id=@id AND
--      row_version=@expected trong handler - 0 dong anh huong => 409
--      PRESCRIPTION_CONFLICT.
-- Idempotent: YES (add_col_if_missing check information_schema; helper o 0000_helpers.sql)
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_pha_prescriptions', 'row_version',
    "INT NOT NULL DEFAULT 0 COMMENT 'Optimistic locking - tang moi lan UPDATE'");

CALL add_col_if_missing('diab_his_pha_prescription_items', 'morning_dose',
    "DECIMAL(10,2) NULL COMMENT 'Lieu dung buoi sang'");
CALL add_col_if_missing('diab_his_pha_prescription_items', 'noon_dose',
    "DECIMAL(10,2) NULL COMMENT 'Lieu dung buoi trua'");
CALL add_col_if_missing('diab_his_pha_prescription_items', 'afternoon_dose',
    "DECIMAL(10,2) NULL COMMENT 'Lieu dung buoi chieu'");
CALL add_col_if_missing('diab_his_pha_prescription_items', 'evening_dose',
    "DECIMAL(10,2) NULL COMMENT 'Lieu dung buoi toi'");
CALL add_col_if_missing('diab_his_pha_prescription_items', 'cach_dung',
    "VARCHAR(255) NULL COMMENT 'Cach dung, vd: Sau an'");
