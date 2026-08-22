-- ============================================================
-- Migration: 9076_rename_fil_files_diab_his_prefix
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Muc dich: fix QC Major M10 (batch 7, 08/07/2026) — migration
--   9062_fix_cls_uploads_guid.sql da tao bang MOI `fil_files`
--   (metadata file dung chung generic upload + CLS upload, xem
--   FileHandlers.cs) nhung KHONG dung prefix `diab_his_` theo dung
--   quy uoc dat ten bang moi trong CLAUDE.md muc 3
--   (`diab_his_<group>_<entity>`). Bang nay cung roi ra ngoai pham
--   vi quet cua 9057_fix_collation_0900.sql (chi LIKE 'diab_his_%').
--
--   KHONG sua truc tiep 9062 vi migration do co the da chay tren
--   staging/prod roi — sua nguoc se lech schema giua cac moi truong.
--   Thay vao do, migration nay RENAME bang sang dung ten chuan.
--
--   Luu y: schema dump production tham chieu (db/diab_his_fil_files.sql,
--   xem CLAUDE.md muc 3 "Schema dump production... KHONG sua") co MOT
--   bang KHAC cung ten `fil_files` nhung khac hoan toan cau truc (INT
--   AUTO_INCREMENT PK, khong co tenant_id, cot UPPERCASE — thuoc he
--   thong tham chieu cu). De tranh rename nham bang do neu no lo duoc
--   import vao mot moi truong nao (dev seed tu dump tham chieu chang
--   han), script chi rename khi bang `fil_files` hien tai CO cot
--   `tenant_id` (dac trung cua bang MOI tao boi 9062).
--
--   Sau khi rename, bang `diab_his_fil_files` se tu dong nam trong
--   pham vi quet cua cac script chuan hoa collation dang LIKE
--   'diab_his_%' (vd 9057) o nhung lan chay sau — ban than bang nay
--   da duoc tao voi collation utf8mb4_0900_ai_ci ngay tu 9062 nen
--   khong can convert lai ngay, chi ghi chu de cac dot ra soat sau
--   khong bo sot.
-- Idempotent: YES (chi rename khi bang cu ton tai, bang moi chua ton
--   tai, va bang cu dung dung schema "moi" — co cot tenant_id).
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _rename_fil_files_diab_his_prefix;
DELIMITER $$
CREATE PROCEDURE _rename_fil_files_diab_his_prefix()
BEGIN
    DECLARE old_exists INT DEFAULT 0;
    DECLARE new_exists INT DEFAULT 0;
    DECLARE old_has_tenant_col INT DEFAULT 0;

    SELECT COUNT(*) INTO old_exists
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'fil_files';

    SELECT COUNT(*) INTO new_exists
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_fil_files';

    SELECT COUNT(*) INTO old_has_tenant_col
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'fil_files'
      AND COLUMN_NAME = 'tenant_id';

    IF old_exists = 1 AND new_exists = 0 AND old_has_tenant_col = 1 THEN
        RENAME TABLE `fil_files` TO `diab_his_fil_files`;
    END IF;
END$$
DELIMITER ;
CALL _rename_fil_files_diab_his_prefix();
DROP PROCEDURE IF EXISTS _rename_fil_files_diab_his_prefix;
