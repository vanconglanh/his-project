-- ============================================================
-- Migration: 9049_create_cli_allergies_v2
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Bang di ung the he moi (GUID + tenant_id) cho CDSS thuoc-di ung. Bang
--   dump cu `cli_allergies` (PK int, PATIENT_ID int, khong tenant) KHONG map duoc
--   sang benh nhan GUID nen tao bang moi. So khop CDSS theo `allergen_ingredient`
--   (hoat chat chuan hoa: thuong, khong dau, tieng Anh) va/hoac atc_code.
-- LUU Y: khong backfill tu bang cu vi khoa int khong anh xa duoc sang patient GUID;
--   du lieu di ung nhap moi qua UI/bang nay.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_cli_allergies (
    id                  CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id           INT          NOT NULL,
    patient_id          CHAR(36)     NOT NULL,
    allergen_type       VARCHAR(20)  NOT NULL DEFAULT 'DRUG' COMMENT 'DRUG|FOOD|ENVIRONMENT|OTHER',
    allergen_name       VARCHAR(200) NOT NULL COMMENT 'Ten di nguyen hien thi (co the co dau)',
    allergen_ingredient VARCHAR(120) NULL COMMENT 'Hoat chat chuan hoa de so khop CDSS',
    atc_code            VARCHAR(10)  NULL,
    severity            VARCHAR(16)  NULL COMMENT 'MILD|MODERATE|SEVERE|LIFE_THREATENING',
    reaction            VARCHAR(255) NULL COMMENT 'Bieu hien phan ung',
    note                TEXT         NULL,
    recorded_at         DATETIME(3)  NULL,
    is_active           TINYINT(1)   NOT NULL DEFAULT 1,
    created_at          DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by          CHAR(36)     NULL,
    updated_at          DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by          CHAR(36)     NULL,
    deleted_at          DATETIME(3)  NULL,
    PRIMARY KEY (id),
    INDEX idx_allergy_patient (tenant_id, patient_id, is_active),
    INDEX idx_allergy_ingredient (allergen_ingredient)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Di ung the he moi (GUID+tenant) cho CDSS thuoc-di ung';
