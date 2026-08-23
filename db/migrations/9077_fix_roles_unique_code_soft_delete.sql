-- ============================================================
-- Migration: 9077_fix_roles_unique_code_soft_delete
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Generated: 2026-08-23
-- Story refs: BUG-01 (Major, QC final review + tester UTC) — tao lai
--   role trung ma sau khi da xoa mem -> HTTP 500
-- Idempotent: YES
-- ============================================================
--
-- VAN DE:
-- `diab_his_sec_roles` (9001_create_sec_all.sql:112) co
-- `UNIQUE KEY uq_roles_code (code)` KHONG tinh den `deleted_at`.
-- `CreateRoleCommandHandler` (backend/src/ProDiabHis.Application/Roles/
-- CreateRoleCommand.cs) kiem tra trung ma bang dieu kien
-- `Code == req.Code && DeletedAt == null` (chi coi role DANG ACTIVE la
-- "chiem cho" -> role da xoa mem duoc phep dung lai ma). Nhung khi
-- INSERT thuc su, DB van con row CU da bi xoa mem cung `code` -> vo
-- UNIQUE constraint tang DB -> loi khong duoc handler bat truoc ->
-- unhandled exception -> HTTP 500.
--
-- KHONG sua truc tiep 9001_create_sec_all.sql: migration do dung
-- `CREATE TABLE IF NOT EXISTS` nen co the da chay tren staging/prod
-- roi, sua UNIQUE KEY trong do se KHONG duoc ap dung lai cho bang da
-- ton tai (schema se lech giua cac moi truong). Thay vao do tao
-- migration MOI, dung ALTER TABLE de bien doi bang hien co.
--
-- HUONG XU LY DA CHON: doi UNIQUE tu cot `code` thuan sang mot COT
-- SINH (generated column) `code_active`, chi mang gia tri `code` khi
-- role CON active (`deleted_at IS NULL`), va NULL khi role da bi xoa
-- mem. UNIQUE index moi dat tren `code_active` thay vi `code`.
--
-- DA CAN NHAC 2 PHUONG AN (theo yeu cau QC), KET LUAN CHON PHUONG AN
-- "GENERATED COLUMN" (khong chon composite UNIQUE (code, deleted_at)):
--
--   * Phuong an composite UNIQUE (code, deleted_at) — DA LOAI BO:
--     MySQL/InnoDB coi cot NULL trong 1 composite UNIQUE index la
--     "khac nhau" khi so sanh giua cac row (dung chuan SQL: so sanh
--     NULL luon cho ket qua UNKNOWN, khong bao gio duoc coi la bang
--     nhau). Voi UNIQUE (code, deleted_at): 2 role DANG ACTIVE cung
--     `code` (ca hai co `deleted_at = NULL`) se KHONG bi MySQL coi la
--     trung, vi cot `deleted_at` cua ca 2 row deu NULL -> INSERT ca
--     hai deu thanh cong. Day la loi NGHIEM TRONG HON ban dau: 2 role
--     ACTIVE trung ma cung ton tai — dung truong hop QUAN TRONG NHAT
--     can duoc enforce lai khong duoc dam bao. Vi vay phuong an nay
--     KHONG dung cho bai toan nay du duoc goi y trong bug report.
--
--   * Phuong an generated column `code_active` — DA CHON:
--     - Role active (`deleted_at IS NULL`)  -> code_active = code
--       (khong NULL) -> UNIQUE index enforce dung "toi da 1 role
--       active / ma" (giai quyet dung yeu cau nghiep vu).
--     - Role da xoa mem (`deleted_at IS NOT NULL`) -> code_active =
--       NULL -> MySQL cho phep nhieu row co cung gia tri NULL trong
--       1 cot UNIQUE (kem ca don-cot UNIQUE index) -> nhieu role da bi
--       xoa mem cung `code` cu van ton tai binh thuong, dong thoi
--       KHONG lam mat kha nang chan trung role active.
--     MySQL 8.0.13+ da ho tro tao INDEX tren generated column (VIRTUAL
--     hoac STORED); moi truong test dung image mysql:8.0.36 (xem
--     MySqlTestFixture.cs) nen hoan toan tuong thich. Dung VIRTUAL de
--     khong ton them dung luong luu tru vat ly (chi tinh khi doc/ghi
--     index), phu hop vi cot chi phuc vu muc dich UNIQUE constraint.
--
-- Sau migration nay: CreateRoleCommandHandler khong can sua doi gi
-- them — logic check `DeletedAt == null` da dung tu truoc, chi la DB
-- truoc day khong "khop" voi logic do. Gio DB va logic ung dung da
-- thong nhat.
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_roles_unique_code_soft_delete;
DELIMITER $$
CREATE PROCEDURE _fix_roles_unique_code_soft_delete()
BEGIN
    DECLARE v_db              VARCHAR(64);
    DECLARE v_table_exists    INT DEFAULT 0;
    DECLARE v_col_exists      INT DEFAULT 0;
    DECLARE v_old_idx_exists  INT DEFAULT 0;
    DECLARE v_new_idx_exists  INT DEFAULT 0;

    SET v_db = DATABASE();

    SELECT COUNT(*) INTO v_table_exists
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = v_db AND TABLE_NAME = 'diab_his_sec_roles';

    IF v_table_exists = 1 THEN

        -- Buoc 1: them cot sinh `code_active` neu chua co
        SELECT COUNT(*) INTO v_col_exists
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = v_db
          AND TABLE_NAME   = 'diab_his_sec_roles'
          AND COLUMN_NAME  = 'code_active';

        IF v_col_exists = 0 THEN
            ALTER TABLE `diab_his_sec_roles`
                ADD COLUMN `code_active` VARCHAR(50)
                    GENERATED ALWAYS AS (IF(`deleted_at` IS NULL, `code`, NULL)) VIRTUAL
                    COMMENT 'Sinh tu code: = code khi role dang active (deleted_at IS NULL), NULL khi da xoa mem. Dung lam UNIQUE key thay cho code de cho phep tao lai role trung ma cu sau khi role cu da bi xoa mem (BUG-01)';
        END IF;

        -- Buoc 2: xoa UNIQUE KEY cu tren cot `code` (khong tinh soft-delete) neu con
        SELECT COUNT(*) INTO v_old_idx_exists
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = v_db
          AND TABLE_NAME   = 'diab_his_sec_roles'
          AND INDEX_NAME   = 'uq_roles_code';

        IF v_old_idx_exists = 1 THEN
            ALTER TABLE `diab_his_sec_roles` DROP INDEX `uq_roles_code`;
        END IF;

        -- Buoc 3: tao UNIQUE KEY moi tren `code_active` neu chua co
        SELECT COUNT(*) INTO v_new_idx_exists
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = v_db
          AND TABLE_NAME   = 'diab_his_sec_roles'
          AND INDEX_NAME   = 'uq_roles_code_active';

        IF v_new_idx_exists = 0 THEN
            ALTER TABLE `diab_his_sec_roles`
                ADD UNIQUE KEY `uq_roles_code_active` (`code_active`);
        END IF;

    END IF;
END$$
DELIMITER ;
CALL _fix_roles_unique_code_soft_delete();
DROP PROCEDURE IF EXISTS _fix_roles_unique_code_soft_delete;
