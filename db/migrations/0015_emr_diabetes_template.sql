-- ============================================================
-- Migration: 0015_emr_diabetes_template
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-EMR-DM-01, US-EMR-DM-02, US-EMR-DM-03
-- Idempotent: YES
-- Ghi chú: Module chuyên biệt đái tháo đường — differentiator của
--   Pro-Diab HIS so với HIS thông thường. Lưu các chỉ số ĐTĐ quan trọng
--   (HbA1c, glucose, eGFR, ACR) và tracking biến chứng.
-- ============================================================
SET NAMES utf8mb4;

-- Bảng đánh giá chuyên sâu bệnh nhân đái tháo đường mỗi lượt khám
CREATE TABLE IF NOT EXISTS `diab_his_cli_diabetes_assessments` (
    `id`                    INT            NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`             INT            NULL                                  COMMENT 'ID tenant',
    `patient_id`            INT            NOT NULL                              COMMENT 'FK → pat_patients.ID',
    `encounter_id`          INT            NOT NULL                              COMMENT 'FK → cli_visits.ID',
    -- Chỉ số kiểm soát đường huyết
    `hba1c`                 DECIMAL(4,2)   NULL                                  COMMENT 'HbA1c (%) — chỉ số kiểm soát đường huyết 3 tháng',
    `fasting_glucose`       DECIMAL(6,2)   NULL                                  COMMENT 'Đường huyết lúc đói (mmol/L)',
    `postprandial_glucose`  DECIMAL(6,2)   NULL                                  COMMENT 'Đường huyết sau ăn 2 giờ (mmol/L)',
    -- Chức năng thận
    `egfr`                  DECIMAL(6,2)   NULL                                  COMMENT 'eGFR — độ lọc cầu thận ước tính (mL/phút/1.73m²)',
    `serum_creatinine`      DECIMAL(6,2)   NULL                                  COMMENT 'Creatinine huyết thanh (µmol/L)',
    `urine_acr`             DECIMAL(8,2)   NULL                                  COMMENT 'Tỷ lệ Albumin/Creatinine nước tiểu — ACR (mg/g)',
    -- Huyết áp
    `bp_systolic`           INT            NULL                                  COMMENT 'Huyết áp tâm thu (mmHg)',
    `bp_diastolic`          INT            NULL                                  COMMENT 'Huyết áp tâm trương (mmHg)',
    -- Nhân trắc học
    `bmi`                   DECIMAL(4,1)   NULL                                  COMMENT 'Chỉ số khối cơ thể BMI (kg/m²)',
    `waist_circumference`   DECIMAL(5,1)   NULL                                  COMMENT 'Vòng bụng (cm)',
    -- Phân loại đái tháo đường
    `diabetes_type`         ENUM('TYPE_1','TYPE_2','GESTATIONAL','MODY','OTHER')
                                           NULL                                  COMMENT 'Phân loại đái tháo đường',
    -- Biến chứng (JSON array các biến chứng đã xác định)
    `complications_json`    JSON           NULL                                  COMMENT 'Danh sách biến chứng: retinopathy, neuropathy, nephropathy, cad (bệnh mạch vành), pad (bệnh động mạch ngoại vi)',
    `assessed_at`           DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm đánh giá',
    `assessed_by`           INT            NULL                                  COMMENT 'ID bác sĩ thực hiện đánh giá',
    `created_at`            DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`            INT            NULL                                  COMMENT 'ID người tạo',
    `updated_at`            DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP
                                               ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`            INT            NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`            DATETIME       NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_dm_assess_patient`   (`patient_id`, `assessed_at`),
    INDEX `idx_dm_assess_encounter` (`encounter_id`),
    INDEX `idx_dm_assess_tenant`    (`tenant_id`, `assessed_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Đánh giá chuyên sâu bệnh nhân đái tháo đường mỗi lượt khám';

-- Template phiếu khám ĐTĐ tùy chỉnh của từng phòng khám
CREATE TABLE IF NOT EXISTS `diab_his_cli_diabetes_templates` (
    `id`            INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`     INT          NULL                                  COMMENT 'ID tenant — NULL nghĩa là template mặc định hệ thống',
    `name`          VARCHAR(255) NOT NULL                              COMMENT 'Tên template phiếu khám',
    `template_json` JSON         NOT NULL                              COMMENT 'Cấu hình template: danh sách trường hiển thị, thứ tự, nhãn, v.v.',
    `is_default`    TINYINT(1)   NOT NULL DEFAULT 0                   COMMENT 'Template mặc định của tenant (1 tenant 1 default)',
    `created_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`    INT          NULL                                  COMMENT 'ID người tạo',
    `updated_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                     ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`    INT          NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`    DATETIME     NULL                                  COMMENT 'Thời điểm xóa mềm',

    INDEX `idx_dm_tmpl_tenant` (`tenant_id`, `is_default`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Template phiếu khám đái tháo đường tùy chỉnh của phòng khám';
