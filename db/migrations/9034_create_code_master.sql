-- ============================================================
-- Migration: 9034_create_code_master
-- Mo ta: He thong danh muc ma dung chung (thay hard-code enum o FE):
--   CODE_MASTER      = dinh nghia nhom ma (id 'CNTRY','GENDER'...)
--   CODE_DETAIL_MASTER = chi tiet tung ma trong nhom (code 'VN' -> name 'Viet Nam')
--   VD: master CNTRY (Quoc gia) -> detail (CNTRY, VN, 'Viet Nam').
--   CHI dung cho enum code cung; nhom da co master table rieng (ICD10, roles,
--   suppliers, drugs...) KHONG dua vao day.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS diab_his_sys_code_master (
    id          VARCHAR(30)  NOT NULL COMMENT 'ID nhom ma, VD GENDER/CNTRY/PATIENT_TYPE',
    name        VARCHAR(150) NOT NULL COMMENT 'Ten nhom tieng Viet',
    description VARCHAR(255) NULL,
    sort_order  INT          NOT NULL DEFAULT 0,
    is_active   TINYINT(1)   NOT NULL DEFAULT 1,
    created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS diab_his_sys_code_detail (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    code_master_id VARCHAR(30)  NOT NULL COMMENT 'FK -> code_master.id',
    code           VARCHAR(50)  NOT NULL COMMENT 'Ma chi tiet, VD VN/MALE/TABLET',
    name           VARCHAR(200) NOT NULL COMMENT 'Ten hien thi tieng Viet',
    name_en        VARCHAR(200) NULL,
    sort_order     INT          NOT NULL DEFAULT 0,
    is_active      TINYINT(1)   NOT NULL DEFAULT 1,
    extra          JSON         NULL COMMENT 'Thuoc tinh phu (mau badge, nhom...)',
    created_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_code_detail (code_master_id, code),
    INDEX idx_code_detail_master (code_master_id, sort_order),
    CONSTRAINT fk_code_detail_master FOREIGN KEY (code_master_id)
        REFERENCES diab_his_sys_code_master (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
