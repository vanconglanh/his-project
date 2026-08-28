-- ============================================================
-- Migration: 9088_create_pat_guardians_and_id_hash
-- Muc dich (FR-101 P0 - Tao/tra cuu ho so benh nhan):
--   1) Bang diab_his_pat_guardians: thong tin nguoi giam ho, bat buoc
--      cho benh nhan < 72 thang tuoi (theo BR trong SRS FR-101).
--      Tach bang con (khong phinh diab_his_pat_patients).
--   2) Cot id_number_hash tren diab_his_pat_patients: SHA-256(CCCD/CMND
--      da chuan hoa) de tim trung nhanh. Ly do khong the index truc tiep
--      id_number_enc: AES-256-GCM la ma hoa non-deterministic (nonce ngau
--      nhien moi lan ma hoa) nen 2 lan ma hoa cung 1 CCCD se cho ra 2
--      ciphertext khac nhau -> khong the dung lam khoa tra cuu/tim trung.
--      Dung hash mot chieu (khong hoi phuc duoc plaintext) chi de so sanh
--      bang, van phai giai ma id_number_enc qua IEncryptionService khi
--      can hien thi.
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- 1) Bang nguoi giam ho ------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_pat_guardians` (
    `id`             CHAR(36)     NOT NULL,
    `tenant_id`      INT          NOT NULL,
    `patient_id`     CHAR(36)     NOT NULL COMMENT 'FK -> diab_his_pat_patients.id',
    `full_name`      VARCHAR(255) NOT NULL,
    `relationship`   VARCHAR(50)  NOT NULL COMMENT 'Quan he voi benh nhan: CHA/ME/ONG/BA/NGUOI_GIAM_HO_KHAC...',
    `phone`          VARCHAR(30)  NOT NULL,
    `id_number_enc`  VARCHAR(500) NULL COMMENT 'CCCD/CMND nguoi giam ho, ma hoa AES-256-GCM',
    `id_number_masked` VARCHAR(20) NULL,
    `created_at`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`     CHAR(36)     NULL,
    `updated_at`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`     CHAR(36)     NULL,
    `deleted_at`     DATETIME     NULL,
    `deleted_by`     CHAR(36)     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_guardian_patient` (`tenant_id`, `patient_id`),
    INDEX `idx_guardian_tenant`  (`tenant_id`, `deleted_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Thong tin nguoi giam ho cho benh nhan < 72 thang tuoi';

-- 2) Cot id_number_hash tren diab_his_pat_patients de tim trung CCCD nhanh ---
CALL add_col_if_missing('diab_his_pat_patients', 'id_number_hash', 'CHAR(64) NULL COMMENT ''SHA-256 cua CCCD/CMND da chuan hoa, dung de tim trung, KHONG hoi phuc plaintext''');

CALL add_index_if_missing('diab_his_pat_patients', 'idx_patients_id_hash', '(`tenant_id`, `id_number_hash`)');

-- Index ho tro tim trung theo (phone, full_name, date_of_birth) khi khong co CCCD
CALL add_index_if_missing('diab_his_pat_patients', 'idx_patients_dup_lookup', '(`tenant_id`, `phone`, `date_of_birth`)');
