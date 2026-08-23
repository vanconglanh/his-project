-- ============================================================
-- Migration: 9078_fix_dtqg_id_types
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Generated: 2026-08-23
-- Story refs: BUG (Critical) — xac nhan THAT tren staging bang DESCRIBE +
--   goi song PUT /api/v1/dtqg/credentials -> 500 INTERNAL_ERROR.
-- Idempotent: YES (kiem tra DATA_TYPE qua information_schema truoc khi ALTER)
-- ============================================================
--
-- VAN DE:
-- `diab_his_int_dtqg_credentials.id` va `diab_his_int_dtqg_submissions.id` /
-- `.prescription_id` dang la INT (tao boi migration 0011_create_dtqg.sql,
-- chay TRUOC 9005/9009/9011). Migration 9011_create_missing_tables.sql co
-- dinh nghia lai 2 bang nay voi `id CHAR(36)` nhung dung
-- `CREATE TABLE IF NOT EXISTS` -> vi bang da ton tai tu 0011 nen la NO-OP,
-- schema CHAR(36) "mong muon" trong 9011 KHONG BAO GIO duoc ap dung that su.
-- (Day chinh la pattern bug da tung xay ra voi cac bang pharmacy khac,
-- xem chu thich trong 9025_fix_dispense_fk_types.sql.)
--
-- Trong khi do `DtqgHandlers.cs` (UpsertDtqgCredentialsHandler) ghi
-- `id` bang `UUID()` / `Guid.NewGuid().ToString()` (GUID 36 ky tu) vao cot
-- INT -> MySQL (STRICT_TRANS_TABLES mac dinh) nem loi "Incorrect integer
-- value" -> handler khong bat rieng -> unhandled exception -> HTTP 500.
-- Tuong tu, `diab_his_int_dtqg_submissions.prescription_id` phai khop kieu
-- CHAR(36) voi `diab_his_pha_prescriptions.id` (da la GUID tu migration 9005).
--
-- HUONG XU LY DU LIEU CU (neu bang da co san du lieu o moi truong khac):
-- KHONG xoa row nao. Dung MODIFY COLUMN truc tiep — MySQL tu dong chuyen
-- gia tri INT hien co sang chuoi so tuong ung (vd 1 -> '1') khi doi kieu
-- INT -> CHAR(36), giu nguyen toan bo du lieu nghiep vu that (cskcb_id,
-- partner_code, token_encrypted, ma_don_thuoc, status, v.v.). Day la cach
-- lam AN TOAN NHAT (khong mat du lieu, khong random hoa gia tri) va la
-- pattern da duoc dung thanh cong o 9024/9025/9042 cho cac cot FK tuong tu.
-- (Rieng gia tri PK `id` cu dang la surrogate key noi bo, khong duoc bat
-- ky bang nao FK toi -> chuyen thanh chuoi so khong anh huong tinh toan
-- ven du lieu; cac row cu se chi khong co dinh dang GUID chuan, ma app da
-- xu ly an toan qua Guid.TryParse(...) ?? Guid.Empty trong MapSubmission /
-- GetDtqgCredentialsHandler).
--
-- Sau migration nay: UpsertDtqgCredentialsHandler / SubmitDtqgHandler /
-- GetDtqgStatusHandler / RetryDtqgHandler / DtqgSubmitRetryJob khong can
-- sua schema gi them — code Dapper ghi UUID()/Guid string da dung tu dau,
-- chi la DB truoc day sai kieu.
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_dtqg_id_types_9078;
DELIMITER $$
CREATE PROCEDURE _fix_dtqg_id_types_9078()
BEGIN
    DECLARE v_db VARCHAR(64);
    SET v_db = DATABASE();

    -- ── diab_his_int_dtqg_credentials.id: INT -> CHAR(36), bo AUTO_INCREMENT ──
    -- (PRIMARY KEY tren cot nay duoc giu nguyen; UNIQUE KEY tren tenant_id
    -- khong bi dong den vi khac cot.)
    IF EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = v_db
           AND TABLE_NAME   = 'diab_his_int_dtqg_credentials'
           AND COLUMN_NAME  = 'id'
           AND DATA_TYPE    = 'int'
    ) THEN
        ALTER TABLE `diab_his_int_dtqg_credentials`
            MODIFY COLUMN `id` CHAR(36)
            CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci
            NOT NULL COMMENT 'UUID khoa chinh (truoc la INT AUTO_INCREMENT, xem 9078)';
    END IF;

    -- ── diab_his_int_dtqg_submissions.id: INT -> CHAR(36), bo AUTO_INCREMENT ─
    IF EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = v_db
           AND TABLE_NAME   = 'diab_his_int_dtqg_submissions'
           AND COLUMN_NAME  = 'id'
           AND DATA_TYPE    = 'int'
    ) THEN
        ALTER TABLE `diab_his_int_dtqg_submissions`
            MODIFY COLUMN `id` CHAR(36)
            CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci
            NOT NULL COMMENT 'UUID khoa chinh (truoc la INT AUTO_INCREMENT, xem 9078)';
    END IF;

    -- ── diab_his_int_dtqg_submissions.prescription_id: INT -> CHAR(36) ──────
    -- Phai khop kieu voi diab_his_pha_prescriptions.id (CHAR(36), migration 9005).
    IF EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = v_db
           AND TABLE_NAME   = 'diab_his_int_dtqg_submissions'
           AND COLUMN_NAME  = 'prescription_id'
           AND DATA_TYPE    = 'int'
    ) THEN
        ALTER TABLE `diab_his_int_dtqg_submissions`
            MODIFY COLUMN `prescription_id` CHAR(36)
            CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci
            NOT NULL COMMENT 'FK -> diab_his_pha_prescriptions.id (GUID, xem 9078)';
    END IF;
END$$
DELIMITER ;
CALL _fix_dtqg_id_types_9078();
DROP PROCEDURE IF EXISTS _fix_dtqg_id_types_9078;
