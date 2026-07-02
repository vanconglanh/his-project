-- ============================================================
-- Migration: 0016_create_appointments
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-09, US-RC-04
-- Idempotent: YES
-- Ghi chú: Lịch hẹn khám hỗ trợ đặt qua nhiều kênh (walk-in, điện thoại,
--   web, API đối tác, app). Bệnh nhân mới chưa có hồ sơ vẫn đặt được
--   qua patient_name_temp + patient_phone.
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_sch_appointments` (
    `id`                 INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `tenant_id`          INT          NULL                                  COMMENT 'ID tenant',
    `patient_id`         INT          NULL                                  COMMENT 'FK → pat_patients.ID (NULL nếu BN mới chưa có hồ sơ)',
    `patient_name_temp`  VARCHAR(255) NULL                                  COMMENT 'Tên bệnh nhân tạm thời khi chưa có hồ sơ',
    `patient_phone`      VARCHAR(20)  NULL                                  COMMENT 'Số điện thoại liên hệ bệnh nhân',
    `doctor_id`          INT          NULL                                  COMMENT 'FK → sta_doctors.ID (NULL = chưa phân công bác sĩ)',
    `department_id`      INT          NULL                                  COMMENT 'FK → sys_departments.ID',
    `service_package_id` INT          NULL                                  COMMENT 'FK → gói dịch vụ (nếu đặt theo gói)',
    `appointment_at`     DATETIME     NOT NULL                              COMMENT 'Thời điểm hẹn khám',
    `duration_minutes`   INT          NOT NULL DEFAULT 30                   COMMENT 'Thời gian dự kiến khám (phút)',
    `status`             ENUM('PENDING','CONFIRMED','CHECKED_IN','CANCELLED','NO_SHOW')
                                      NOT NULL DEFAULT 'PENDING'            COMMENT 'Trạng thái lịch hẹn',
    `source`             ENUM('WALK_IN','PHONE','WEB','API','APP')
                                      NOT NULL DEFAULT 'WALK_IN'            COMMENT 'Kênh đặt lịch',
    `source_partner_id`  INT          NULL                                  COMMENT 'FK → diab_his_api_partners.id (khi source=API)',
    `note`               TEXT         NULL                                  COMMENT 'Ghi chú lịch hẹn',
    `created_at`         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',
    `created_by`         INT          NULL                                  COMMENT 'ID người tạo (lễ tân hoặc system)',
    `updated_at`         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                          ON UPDATE CURRENT_TIMESTAMP       COMMENT 'Thời điểm cập nhật',
    `updated_by`         INT          NULL                                  COMMENT 'ID người cập nhật',
    `deleted_at`         DATETIME     NULL                                  COMMENT 'Thời điểm xóa mềm (hủy lịch)',

    INDEX `idx_appt_tenant_time`    (`tenant_id`, `appointment_at`),
    INDEX `idx_appt_doctor_time`    (`doctor_id`, `appointment_at`),
    INDEX `idx_appt_patient`        (`patient_id`),
    INDEX `idx_appt_status`         (`status`, `appointment_at`),
    INDEX `idx_appt_source_partner` (`source_partner_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lịch hẹn khám bệnh (walk-in, điện thoại, web, API đối tác)';
