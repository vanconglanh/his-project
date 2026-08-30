-- ============================================================
-- Migration: 9186_fix_two_fa_recovery_codes_text
-- Tac gia: Team Leader (Claude) - 2026-08-30
--
-- Bug (P0 chan 2FA hoan toan): cot diab_his_sec_users.two_fa_recovery_codes
--   duoc khai bao kieu JSON, nhung ung dung luu chuoi DA MA HOA (AES-256-GCM,
--   base64) qua IEncryptionService.Encrypt(JsonSerializer.Serialize(...)) —
--   KHONG phai JSON hop le -> MySQL bao "Invalid JSON text" -> POST me/2fa/enable
--   luon tra 500 -> KHONG the bat 2FA -> tinh nang xac thuc 2 lop khong dung duoc.
--
-- Fix: doi kieu cot sang TEXT de chua ciphertext (encryption-at-rest, giu nguyen
--   thiet ke ma hoa ma du phong). UserConfiguration.cs bo .HasColumnType("json").
--
-- Idempotent: YES (chi MODIFY khi kieu hien tai la 'json').
-- Backward compatible: cot NULL-able, du lieu cu (neu co) van doc duoc; thuc te
--   truoc fix khong the ghi thanh cong nen cot rong.
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS fix_two_fa_recovery_codes_col;
DELIMITER $$
CREATE PROCEDURE fix_two_fa_recovery_codes_col()
BEGIN
    DECLARE col_type VARCHAR(64);
    SELECT DATA_TYPE INTO col_type
      FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_sec_users'
       AND COLUMN_NAME = 'two_fa_recovery_codes'
     LIMIT 1;

    IF col_type = 'json' THEN
        ALTER TABLE diab_his_sec_users
            MODIFY COLUMN two_fa_recovery_codes TEXT NULL
            COMMENT 'Ma du phong 2FA (SHA256 hash JSON array) da ma hoa AES-256-GCM base64';
    END IF;
END$$
DELIMITER ;

CALL fix_two_fa_recovery_codes_col();
DROP PROCEDURE IF EXISTS fix_two_fa_recovery_codes_col;
