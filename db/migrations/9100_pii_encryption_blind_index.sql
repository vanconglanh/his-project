-- ============================================================
-- Migration: 9100_pii_encryption_blind_index
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Hạng mục 6: Mã hóa thông tin cá nhân bệnh nhân (PII) — AES-256-GCM
-- Mô tả:
--   1) Thêm cột ciphertext cho PII chưa mã hóa: phone, street, reception_note
--   2) Thêm cột blind index (HMAC-SHA256, hex 64) cho các trường CẦN TRA CỨU:
--        - diab_his_pat_patients.phone_bidx      (số điện thoại)
--        - diab_his_pat_patients.id_number_bidx  (CMND/CCCD)
--        - diab_his_pat_insurances.card_no_bidx  (số thẻ BHYT)
--   3) Index (tenant_id, *_bidx) để tra cứu exact-match nhanh
--
-- LƯU Ý QUAN TRỌNG:
--   - Migration này CHỈ tạo cấu trúc. Việc chuyển dữ liệu cũ plaintext -> ciphertext
--     KHÔNG thể làm bằng SQL thuần (AES-256-GCM + nonce ngẫu nhiên nằm ở tầng ứng dụng).
--     Backfill chạy bằng job C#:
--        POST /api/v1/admin/encryption/pii-backfill   (permission: encryption.rotate)
--     Job idempotent: chỉ xử lý bản ghi chưa mã hóa (nhận biết qua tiền tố "enc:v1:"),
--     sau khi ghi cột *_enc sẽ XÓA plaintext ở cột cũ.
--   - Cột cũ (phone, street, reception_note) được GIỮ LẠI (không DROP) để có đường lùi
--     trong quá trình chuyển đổi. Sau khi backfill xong và xác nhận ổn định
--     -> tạo migration 91xx riêng để DROP.
--
-- Idempotent: YES (dùng add_col_if_missing / add_index_if_missing từ 0000_helpers.sql)
-- Yêu cầu: chạy 0000_helpers.sql trước
-- ============================================================
SET NAMES utf8mb4;

-- (0000_helpers.sql phải được apply trước migration này)

-- ------------------------------------------------------------
-- diab_his_pat_patients
-- ------------------------------------------------------------
CALL add_col_if_missing('diab_his_pat_patients', 'phone_enc',
    "VARCHAR(500) NULL COMMENT 'Số điện thoại đã mã hóa AES-256-GCM (tiền tố enc:v1:)'");

CALL add_col_if_missing('diab_his_pat_patients', 'phone_masked',
    "VARCHAR(30) NULL COMMENT 'Số điện thoại hiển thị đã che (09****678)'");

CALL add_col_if_missing('diab_his_pat_patients', 'phone_bidx',
    "CHAR(64) NULL COMMENT 'Blind index HMAC-SHA256 của SĐT đã chuẩn hóa (tra cứu exact-match)'");

CALL add_col_if_missing('diab_his_pat_patients', 'id_number_bidx',
    "CHAR(64) NULL COMMENT 'Blind index HMAC-SHA256 của CMND/CCCD đã chuẩn hóa'");

CALL add_col_if_missing('diab_his_pat_patients', 'street_enc',
    "VARCHAR(1000) NULL COMMENT 'Địa chỉ chi tiết đã mã hóa AES-256-GCM'");

CALL add_col_if_missing('diab_his_pat_patients', 'reception_note_enc',
    "TEXT NULL COMMENT 'Ghi chú tiếp đón / bệnh án đã mã hóa AES-256-GCM'");

CALL add_index_if_missing('diab_his_pat_patients', 'idx_patients_phone_bidx', '(`tenant_id`, `phone_bidx`)');
CALL add_index_if_missing('diab_his_pat_patients', 'idx_patients_idnum_bidx', '(`tenant_id`, `id_number_bidx`)');

-- ------------------------------------------------------------
-- diab_his_pat_insurances
-- ------------------------------------------------------------
CALL add_col_if_missing('diab_his_pat_insurances', 'card_no_bidx',
    "CHAR(64) NULL COMMENT 'Blind index HMAC-SHA256 của số thẻ BHYT đã chuẩn hóa'");

CALL add_index_if_missing('diab_his_pat_insurances', 'idx_insurances_card_bidx', '(`tenant_id`, `card_no_bidx`)');

-- ------------------------------------------------------------
-- Nới lỏng ràng buộc cột cũ để backfill có thể set NULL sau khi mã hóa
-- (chỉ áp dụng nếu cột đang NOT NULL)
-- ------------------------------------------------------------
DROP PROCEDURE IF EXISTS _pii_relax_legacy_cols_9100;
DELIMITER $$
CREATE PROCEDURE _pii_relax_legacy_cols_9100()
BEGIN
    DECLARE v_nullable VARCHAR(3);

    SELECT IS_NULLABLE INTO v_nullable
      FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_NAME = 'diab_his_pat_patients'
       AND COLUMN_NAME = 'phone';

    IF v_nullable = 'NO' THEN
        ALTER TABLE `diab_his_pat_patients`
            MODIFY COLUMN `phone` VARCHAR(30) NULL COMMENT 'DEPRECATED - chuyển sang phone_enc';
    END IF;
END$$
DELIMITER ;
CALL _pii_relax_legacy_cols_9100();
DROP PROCEDURE IF EXISTS _pii_relax_legacy_cols_9100;
