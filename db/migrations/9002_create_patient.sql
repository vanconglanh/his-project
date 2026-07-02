-- ============================================================
-- Migration: 9002_create_patient
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tác giả: Pro-Diab Team
-- Mô tả: Tạo 5 bảng quản lý hồ sơ bệnh nhân (prefix diab_his_pat_*)
--        Bao gồm: patients, allergies, insurances,
--        emergency_contacts, consents
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- Bảng hồ sơ bệnh nhân
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pat_patients` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID phòng khám (tenant)',
    `code`              VARCHAR(30)     NOT NULL                            COMMENT 'Mã bệnh nhân nội bộ (BN00001)',
    `full_name`         VARCHAR(255)    NOT NULL                            COMMENT 'Họ và tên đầy đủ',
    `gender`            VARCHAR(10)     NULL                                COMMENT 'Giới tính: MALE, FEMALE, OTHER',
    `date_of_birth`     DATE            NULL                                COMMENT 'Ngày sinh',
    `id_number_enc`     VARCHAR(500)    NULL                                COMMENT 'CMND/CCCD đã mã hóa AES-256-GCM',
    `id_number_masked`  VARCHAR(20)     NULL                                COMMENT 'CMND/CCCD hiển thị có dấu *** (vd: 012***789)',
    `phone`             VARCHAR(30)     NULL                                COMMENT 'Số điện thoại liên hệ',
    `email`             VARCHAR(100)    NULL                                COMMENT 'Email bệnh nhân',
    `province_code`     VARCHAR(10)     NULL                                COMMENT 'Mã tỉnh/thành (theo danh mục hành chính)',
    `district_code`     VARCHAR(10)     NULL                                COMMENT 'Mã quận/huyện',
    `ward_code`         VARCHAR(10)     NULL                                COMMENT 'Mã phường/xã',
    `street`            VARCHAR(255)    NULL                                COMMENT 'Địa chỉ chi tiết (số nhà, tên đường)',
    `occupation`        VARCHAR(100)    NULL                                COMMENT 'Nghề nghiệp',
    `ethnicity`         VARCHAR(50)     NULL                                COMMENT 'Dân tộc',
    `blood_type`        VARCHAR(5)      NULL                                COMMENT 'Nhóm máu: A, B, AB, O (+/-)',
    `avatar_url`        VARCHAR(500)    NULL                                COMMENT 'URL ảnh bệnh nhân',
    `reception_note`    TEXT            NULL                                COMMENT 'Ghi chú tiếp đón',
    `allergies_summary` VARCHAR(500)    NULL                                COMMENT 'Tóm tắt dị ứng quan trọng',
    `status`            VARCHAR(20)     NOT NULL DEFAULT 'ACTIVE'           COMMENT 'Trạng thái: ACTIVE, INACTIVE, DECEASED',
    `id_card_issued_date`   DATE        NULL                                COMMENT 'Ngày cấp CMND/CCCD',
    `id_card_issued_place`  VARCHAR(100) NULL                               COMMENT 'Nơi cấp CMND/CCCD',
    `nationality`       VARCHAR(5)      NOT NULL DEFAULT 'VN'               COMMENT 'Quốc tịch (ISO 3166)',
    `patient_type`      VARCHAR(20)     NOT NULL DEFAULT 'SERVICE'          COMMENT 'Loại bệnh nhân: SERVICE (dịch vụ), BHYT',
    `marital_status`    VARCHAR(20)     NULL                                COMMENT 'Tình trạng hôn nhân',
    `visit_type`        VARCHAR(20)     NULL DEFAULT 'FIRST_VISIT'          COMMENT 'Loại lượt khám: FIRST_VISIT, FOLLOW_UP',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_patients_code_tenant`    (`tenant_id`, `code`),
    INDEX `idx_patients_tenant_status`      (`tenant_id`, `status`),
    INDEX `idx_patients_full_name`          (`tenant_id`, `full_name`),
    INDEX `idx_patients_phone`              (`tenant_id`, `phone`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Hồ sơ bệnh nhân';

-- ============================================================
-- Bảng dị ứng của bệnh nhân
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pat_allergies` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `patient_id`    CHAR(36)        NOT NULL                            COMMENT 'UUID bệnh nhân',
    `allergen`      VARCHAR(255)    NOT NULL                            COMMENT 'Tác nhân gây dị ứng (thuốc, thức ăn, môi trường)',
    `reaction`      VARCHAR(255)    NULL                                COMMENT 'Biểu hiện phản ứng dị ứng',
    `severity`      VARCHAR(20)     NOT NULL                            COMMENT 'Mức độ: MILD, MODERATE, SEVERE',
    `onset_date`    DATE            NULL                                COMMENT 'Ngày phát hiện dị ứng',
    `note`          TEXT            NULL                                COMMENT 'Ghi chú thêm',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',

    PRIMARY KEY (`id`),
    INDEX `idx_allergies_patient`   (`tenant_id`, `patient_id`),
    CONSTRAINT `fk_allergies_patient` FOREIGN KEY (`patient_id`)
        REFERENCES `diab_his_pat_patients` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh sách dị ứng của bệnh nhân';

-- ============================================================
-- Bảng thông tin bảo hiểm y tế
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pat_insurances` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `patient_id`        CHAR(36)        NOT NULL                            COMMENT 'UUID bệnh nhân',
    `type`              VARCHAR(20)     NOT NULL DEFAULT 'BHYT'             COMMENT 'Loại bảo hiểm: BHYT, commercial',
    `card_no_enc`       VARCHAR(500)    NOT NULL                            COMMENT 'Số thẻ BHYT đã mã hóa AES-256-GCM',
    `card_no_masked`    VARCHAR(30)     NULL                                COMMENT 'Số thẻ hiển thị có dấu ***',
    `valid_from`        DATE            NOT NULL                            COMMENT 'Ngày bắt đầu hiệu lực',
    `valid_to`          DATE            NOT NULL                            COMMENT 'Ngày hết hiệu lực',
    `hospital_code`     VARCHAR(20)     NULL                                COMMENT 'Mã bệnh viện đăng ký BHYT ban đầu',
    `coverage_percent`  INT             NULL                                COMMENT 'Tỷ lệ chi trả BHYT (%)',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',

    PRIMARY KEY (`id`),
    INDEX `idx_insurances_patient`  (`tenant_id`, `patient_id`),
    INDEX `idx_insurances_valid`    (`tenant_id`, `valid_to`),
    CONSTRAINT `fk_insurances_patient` FOREIGN KEY (`patient_id`)
        REFERENCES `diab_his_pat_patients` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Thông tin thẻ bảo hiểm y tế của bệnh nhân';

-- ============================================================
-- Bảng liên hệ khẩn cấp
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pat_emergency_contacts` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `patient_id`    CHAR(36)        NOT NULL                            COMMENT 'UUID bệnh nhân',
    `full_name`     VARCHAR(255)    NOT NULL                            COMMENT 'Họ tên người liên hệ',
    `relationship`  VARCHAR(50)     NOT NULL                            COMMENT 'Quan hệ với bệnh nhân (Vợ/Chồng, Con, Cha/Mẹ...)',
    `phone`         VARCHAR(30)     NOT NULL                            COMMENT 'Số điện thoại liên hệ khẩn cấp',
    `address`       VARCHAR(255)    NULL                                COMMENT 'Địa chỉ người liên hệ',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',

    PRIMARY KEY (`id`),
    INDEX `idx_emergency_patient`   (`tenant_id`, `patient_id`),
    CONSTRAINT `fk_emergency_patient` FOREIGN KEY (`patient_id`)
        REFERENCES `diab_his_pat_patients` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh sách liên hệ khẩn cấp của bệnh nhân';

-- ============================================================
-- Bảng đồng ý điều trị / văn bản pháp lý
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_pat_consents` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `patient_id`        CHAR(36)        NOT NULL                            COMMENT 'UUID bệnh nhân',
    `consent_type`      VARCHAR(50)     NOT NULL                            COMMENT 'Loại đồng ý: TREATMENT, SURGERY, DATA_SHARING...',
    `signed_at`         DATETIME        NOT NULL                            COMMENT 'Thời điểm ký đồng ý',
    `signed_by`         VARCHAR(255)    NULL                                COMMENT 'Tên người ký (bệnh nhân hoặc người giám hộ)',
    `document_file_id`  CHAR(36)        NULL                                COMMENT 'UUID file đính kèm (MinIO)',
    `revoked_at`        DATETIME        NULL                                COMMENT 'Thời điểm thu hồi đồng ý (nếu có)',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',

    PRIMARY KEY (`id`),
    INDEX `idx_consents_patient`    (`tenant_id`, `patient_id`),
    INDEX `idx_consents_type`       (`tenant_id`, `consent_type`),
    CONSTRAINT `fk_consents_patient` FOREIGN KEY (`patient_id`)
        REFERENCES `diab_his_pat_patients` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Văn bản đồng ý điều trị và các thủ tục y tế';
