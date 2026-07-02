-- ============================================================
-- Migration: 9003_create_encounter
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tác giả: Pro-Diab Team
-- Mô tả: Tạo 4 bảng quản lý lượt khám bệnh (prefix diab_his_enc_*)
--        Bao gồm: encounters, diagnoses, vital_signs, emr_contents
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- Bảng lượt khám bệnh
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_enc_encounters` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID phòng khám (tenant)',
    `patient_id`        CHAR(36)        NOT NULL                            COMMENT 'UUID bệnh nhân',
    `doctor_id`         CHAR(36)        NULL                                COMMENT 'UUID bác sĩ phụ trách',
    `room_id`           CHAR(36)        NULL                                COMMENT 'UUID phòng khám',
    `encounter_type`    VARCHAR(20)     NOT NULL DEFAULT 'FIRST_VISIT'      COMMENT 'Loại khám: FIRST_VISIT, FOLLOW_UP, EMERGENCY, CONSULTATION',
    `status`            VARCHAR(20)     NOT NULL DEFAULT 'WAITING'          COMMENT 'Trạng thái: WAITING, IN_PROGRESS, DONE, CANCELLED',
    `reason_for_visit`  VARCHAR(500)    NULL                                COMMENT 'Lý do khám',
    `chief_complaint`   TEXT            NULL                                COMMENT 'Triệu chứng chính bệnh nhân mô tả',
    `primary_icd10`     VARCHAR(10)     NULL                                COMMENT 'Mã ICD-10 chẩn đoán chính',
    `secondary_icd10`   VARCHAR(500)    NULL                                COMMENT 'Mã ICD-10 chẩn đoán phụ (phân cách phẩy)',
    `encounter_no`      VARCHAR(30)     NULL                                COMMENT 'Số phiếu khám trong ngày',
    `started_at`        DATETIME        NULL                                COMMENT 'Thời điểm bắt đầu khám',
    `finished_at`       DATETIME        NULL                                COMMENT 'Thời điểm kết thúc khám',
    `alert_sent_at`     DATETIME        NULL                                COMMENT 'Thời điểm gửi cảnh báo tái khám',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    INDEX `idx_enc_tenant_patient`  (`tenant_id`, `patient_id`),
    INDEX `idx_enc_tenant_status`   (`tenant_id`, `status`),
    INDEX `idx_enc_tenant_doctor`   (`tenant_id`, `doctor_id`),
    INDEX `idx_enc_created`         (`tenant_id`, `created_at`),
    CONSTRAINT `fk_enc_patient` FOREIGN KEY (`patient_id`)
        REFERENCES `diab_his_pat_patients` (`id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lượt khám bệnh của bệnh nhân';

-- ============================================================
-- Bảng chẩn đoán ICD-10 theo lượt khám
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_enc_diagnoses` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `encounter_id`  CHAR(36)        NOT NULL                            COMMENT 'UUID lượt khám',
    `icd10_code`    VARCHAR(10)     NOT NULL                            COMMENT 'Mã ICD-10',
    `name`          VARCHAR(255)    NOT NULL                            COMMENT 'Tên chẩn đoán tiếng Việt',
    `type`          VARCHAR(20)     NOT NULL DEFAULT 'PRIMARY'          COMMENT 'Loại: PRIMARY (chính), SECONDARY (phụ)',
    `note`          TEXT            NULL                                COMMENT 'Ghi chú chẩn đoán',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`    CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`    CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    INDEX `idx_diagnoses_encounter` (`tenant_id`, `encounter_id`),
    INDEX `idx_diagnoses_icd10`     (`tenant_id`, `icd10_code`),
    CONSTRAINT `fk_diagnoses_encounter` FOREIGN KEY (`encounter_id`)
        REFERENCES `diab_his_enc_encounters` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chẩn đoán ICD-10 theo lượt khám';

-- ============================================================
-- Bảng chỉ số sinh hiệu (vital signs)
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_enc_vital_signs` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `encounter_id`      CHAR(36)        NOT NULL                            COMMENT 'UUID lượt khám',
    `patient_id`        CHAR(36)        NOT NULL                            COMMENT 'UUID bệnh nhân',
    `recorded_at`       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm đo',
    `recorded_by`       CHAR(36)        NULL                                COMMENT 'UUID người đo',
    `record_sequence`   INT             NOT NULL DEFAULT 1                  COMMENT 'Lần đo thứ mấy trong lượt khám',
    `temperature_c`     DECIMAL(4,1)    NULL                                COMMENT 'Nhiệt độ cơ thể (độ C)',
    `heart_rate_bpm`    INT             NULL                                COMMENT 'Nhịp tim (lần/phút)',
    `respiratory_rate`  INT             NULL                                COMMENT 'Nhịp thở (lần/phút)',
    `bp_systolic`       INT             NULL                                COMMENT 'Huyết áp tâm thu (mmHg)',
    `bp_diastolic`      INT             NULL                                COMMENT 'Huyết áp tâm trương (mmHg)',
    `spo2_percent`      INT             NULL                                COMMENT 'Độ bão hòa oxy SpO2 (%)',
    `weight_kg`         DECIMAL(6,2)    NULL                                COMMENT 'Cân nặng (kg)',
    `height_cm`         DECIMAL(5,1)    NULL                                COMMENT 'Chiều cao (cm)',
    `pain_scale`        INT             NULL                                COMMENT 'Thang điểm đau 0-10',
    `glucose_mg_dl`     DECIMAL(6,2)    NULL                                COMMENT 'Đường huyết (mg/dL)',
    `note`              TEXT            NULL                                COMMENT 'Ghi chú',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    INDEX `idx_vital_encounter`     (`tenant_id`, `encounter_id`),
    INDEX `idx_vital_patient`       (`tenant_id`, `patient_id`),
    CONSTRAINT `fk_vital_encounter` FOREIGN KEY (`encounter_id`)
        REFERENCES `diab_his_enc_encounters` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chỉ số sinh hiệu đo trong lượt khám';

-- ============================================================
-- Bảng nội dung bệnh án điện tử (EMR)
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_enc_emr_contents` (
    `id`            CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`     INT             NOT NULL                            COMMENT 'ID tenant',
    `encounter_id`  CHAR(36)        NOT NULL                            COMMENT 'UUID lượt khám (1-1)',
    `content_json`  MEDIUMTEXT      NOT NULL                            COMMENT 'Nội dung bệnh án dạng JSON (mặc định {})',
    `content_html`  MEDIUMTEXT      NULL                                COMMENT 'Nội dung render HTML để in',
    `template_id`   CHAR(36)        NULL                                COMMENT 'UUID mẫu bệnh án sử dụng',
    `version`       INT             NOT NULL DEFAULT 1                  COMMENT 'Phiên bản bệnh án',
    `signed_at`     DATETIME        NULL                                COMMENT 'Thời điểm ký số',
    `signed_by`     CHAR(36)        NULL                                COMMENT 'UUID bác sĩ ký',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`    CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                        ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`    CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`    DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`    CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_emr_encounter`   (`encounter_id`),
    INDEX `idx_emr_tenant`          (`tenant_id`),
    CONSTRAINT `fk_emr_encounter` FOREIGN KEY (`encounter_id`)
        REFERENCES `diab_his_enc_encounters` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Bệnh án điện tử (EMR) — 1 bản ghi per lượt khám';
