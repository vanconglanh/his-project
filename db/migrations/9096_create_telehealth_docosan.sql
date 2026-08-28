-- ============================================================
-- Migration: 9096_create_telehealth_docosan
-- Muc dich: Tich hop Telehealth (FR-801..803) voi he thong Docosan
--   - Bang mapping clinic/branch/doctor/service HIS <-> Docosan
--   - Bang mapping benh nhan HIS <-> Docosan user
--   - Bang phien tu van tu xa (tham chieu, khong tu quan ly video)
--   - Bang outbox/retry cho loi goi API Docosan
--   - Bo sung cot telehealth cho diab_his_sch_appointments
-- Phu thuoc: 0000_helpers.sql (add_col_if_missing, add_index_if_missing)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- 1. Mapping phong kham / chi nhanh HIS -> Docosan clinic_id, branch_id
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_int_docosan_clinic_mapping` (
    `id`                    CHAR(36)        NOT NULL                COMMENT 'UUID khoa chinh',
    `tenant_id`             INT             NOT NULL                COMMENT 'ID phong kham (tenant)',
    `branch_id`             INT             NULL                    COMMENT 'FK -> diab_his_sys_branches.id; NULL = mac dinh cho tenant',
    `docosan_clinic_id`     INT             NOT NULL                COMMENT 'clinic_id ben Docosan',
    `docosan_branch_id`     INT             NULL                    COMMENT 'branch_id ben Docosan (optional)',
    `docosan_clinic_name`   VARCHAR(255)    NULL                    COMMENT 'Ten hien thi lay tu profile-clinic-diab (cache)',
    `environment`           VARCHAR(20)     NOT NULL DEFAULT 'production' COMMENT 'staging | production',
    `is_active`             TINYINT(1)      NOT NULL DEFAULT 1,
    `synced_at`             DATETIME        NULL                    COMMENT 'Lan cuoi dong bo profile tu Docosan',
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)        NULL,
    `updated_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)        NULL,
    `deleted_at`            DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_doco_clinic_tenant_branch_env` (`tenant_id`, `branch_id`, `environment`),
    KEY `idx_doco_clinic_tenant` (`tenant_id`, `is_active`),
    KEY `idx_doco_clinic_remote` (`docosan_clinic_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT 'Anh xa phong kham/chi nhanh HIS sang clinic_id cua Docosan';

-- ------------------------------------------------------------
-- 2. Mapping bac si HIS (sec_users) -> Docosan doctor_id
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_int_docosan_doctor_mapping` (
    `id`                    CHAR(36)        NOT NULL                COMMENT 'UUID khoa chinh',
    `tenant_id`             INT             NOT NULL,
    `branch_id`             INT             NULL,
    `user_id`               CHAR(36)        NOT NULL                COMMENT 'FK -> diab_his_sec_users.id (bac si HIS)',
    `docosan_doctor_id`     INT             NOT NULL                COMMENT 'doctor_id ben Docosan',
    `docosan_clinic_id`     INT             NOT NULL                COMMENT 'clinic_id ben Docosan ma bac si truc thuoc',
    `docosan_doctor_name`   VARCHAR(255)    NULL                    COMMENT 'Ten hien thi cache tu Docosan',
    `environment`           VARCHAR(20)     NOT NULL DEFAULT 'production',
    `is_active`             TINYINT(1)      NOT NULL DEFAULT 1,
    `synced_at`             DATETIME        NULL,
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)        NULL,
    `updated_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)        NULL,
    `deleted_at`            DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_doco_doctor_tenant_user_env` (`tenant_id`, `user_id`, `environment`),
    KEY `idx_doco_doctor_tenant` (`tenant_id`, `is_active`),
    KEY `idx_doco_doctor_remote` (`docosan_doctor_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT 'Anh xa bac si HIS sang doctor_id cua Docosan';

-- ------------------------------------------------------------
-- 3. Mapping dich vu telehealth: service_id Docosan dung khi tao don
--    (service_type = 'telemedicine' ben Docosan quyet dinh apt_mode)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_int_docosan_service_mapping` (
    `id`                    CHAR(36)        NOT NULL,
    `tenant_id`             INT             NOT NULL,
    `branch_id`             INT             NULL,
    `his_service_id`        CHAR(36)        NULL                    COMMENT 'FK -> dich vu HIS (diab_his_bil_services.id) neu can doi soat doanh thu',
    `docosan_service_id`    INT             NOT NULL                COMMENT 'services[].id gui trong payment_info',
    `docosan_service_type`  VARCHAR(30)     NOT NULL DEFAULT 'telemedicine' COMMENT 'telemedicine | at_clinic | at_home - quyet dinh apt_mode ben Docosan',
    `service_name`          VARCHAR(255)    NULL,
    `default_quantity`      INT             NOT NULL DEFAULT 1,
    `environment`           VARCHAR(20)     NOT NULL DEFAULT 'production',
    `is_active`             TINYINT(1)      NOT NULL DEFAULT 1,
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)        NULL,
    `updated_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)        NULL,
    `deleted_at`            DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_doco_service_tenant_remote_env` (`tenant_id`, `docosan_service_id`, `environment`),
    KEY `idx_doco_service_tenant_type` (`tenant_id`, `docosan_service_type`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT 'Anh xa dich vu telehealth HIS <-> service Docosan';

-- ------------------------------------------------------------
-- 4. Mapping benh nhan HIS -> Docosan user/patient + access token
--    access_token luu MA HOA AES-256-GCM (khong luu plaintext)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_int_docosan_patient_mapping` (
    `id`                    CHAR(36)        NOT NULL,
    `tenant_id`             INT             NOT NULL,
    `patient_id`            CHAR(36)        NOT NULL                COMMENT 'FK -> diab_his_pat_patients.id',
    `docosan_user_id`       INT             NULL                    COMMENT 'user_id ben Docosan (tra ve tu api/register-internal)',
    `docosan_patient_id`    INT             NULL                    COMMENT 'patient_id ben Docosan (tra ve trong appointment)',
    `phone_number_hash`     CHAR(64)        NULL                    COMMENT 'SHA-256 sdt dung de doi chieu, khong luu plaintext',
    `access_token_enc`      VARBINARY(2048) NULL                    COMMENT 'NHAY CAM - AES-256-GCM: Bearer token benh nhan tren Docosan',
    `token_expires_at`      DATETIME        NULL,
    `environment`           VARCHAR(20)     NOT NULL DEFAULT 'production',
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`            CHAR(36)        NULL,
    `updated_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`            CHAR(36)        NULL,
    `deleted_at`            DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_doco_patient_tenant_patient_env` (`tenant_id`, `patient_id`, `environment`),
    KEY `idx_doco_patient_remote` (`docosan_user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT 'Anh xa benh nhan HIS <-> tai khoan Docosan + token';

-- ------------------------------------------------------------
-- 5. Phien tu van tu xa - BANG THAM CHIEU (Docosan lam chu video/lich)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_tel_sessions` (
    `id`                        CHAR(36)        NOT NULL,
    `tenant_id`                 INT             NOT NULL,
    `branch_id`                 INT             NULL,
    `appointment_id`            CHAR(36)        NULL                COMMENT 'FK -> diab_his_sch_appointments.id (lich HIS tuong ung)',
    `patient_id`                CHAR(36)        NOT NULL            COMMENT 'FK -> diab_his_pat_patients.id',
    `doctor_user_id`            CHAR(36)        NULL                COMMENT 'FK -> diab_his_sec_users.id',
    `encounter_id`              CHAR(36)        NULL                COMMENT 'FK -> diab_his_enc_encounters.id - tao khi bac si vao kham (FR-803)',
    `docosan_appointment_id`    INT             NOT NULL            COMMENT 'appointment.id ben Docosan',
    `docosan_telemedicine_id`   INT             NULL                COMMENT 'teleMedicine.id ben Docosan (dung dung link video)',
    `docosan_clinic_id`         INT             NOT NULL,
    `docosan_doctor_id`         INT             NOT NULL,
    `docosan_mode`              VARCHAR(20)     NOT NULL DEFAULT 'telemedicine' COMMENT 'telemedicine | at_clinic | at_home',
    `docosan_status`            VARCHAR(20)     NOT NULL DEFAULT 'request' COMMENT 'request | approve | reject | on-hold (nguyen van tu Docosan)',
    `his_status`                VARCHAR(30)     NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING|CONFIRMED|CANCELLED|COMPLETED|NO_SHOW|FAILED',
    `scheduled_start`           DATETIME        NOT NULL,
    `scheduled_end`             DATETIME        NULL,
    `join_url_enc`              VARBINARY(2048) NULL                COMMENT 'NHAY CAM - AES-256-GCM: appointment_link (short URL kem token 2h)',
    `join_url_expires_at`       DATETIME        NULL                COMMENT 'TTL cua short URL (mac dinh 120 phut)',
    `symptom`                   TEXT            NULL,
    `payment_status`            VARCHAR(20)     NULL                COMMENT 'Trang thai thanh toan do Docosan quan ly (tham chieu)',
    `eligibility_encounter_id`  CHAR(36)        NULL                COMMENT 'FR-801: lan kham truc tiep dung de xac thuc du dieu kien',
    `last_synced_at`            DATETIME        NULL,
    `sync_error`                VARCHAR(500)    NULL,
    `raw_payload`               JSON            NULL                COMMENT 'Ban ghi tho tu Docosan phuc vu doi soat',
    `created_at`                DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`                CHAR(36)        NULL,
    `updated_at`                DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`                CHAR(36)        NULL,
    `deleted_at`                DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_tel_tenant_docosan_apt` (`tenant_id`, `docosan_appointment_id`),
    KEY `idx_tel_tenant_status_start` (`tenant_id`, `his_status`, `scheduled_start`),
    KEY `idx_tel_tenant_patient` (`tenant_id`, `patient_id`),
    KEY `idx_tel_tenant_doctor_start` (`tenant_id`, `doctor_user_id`, `scheduled_start`),
    KEY `idx_tel_appointment` (`appointment_id`),
    KEY `idx_tel_sync` (`his_status`, `last_synced_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT 'Phien tu van tu xa - tham chieu sang Docosan (HIS khong tu host video)';

-- ------------------------------------------------------------
-- 6. Outbox / retry khi goi Docosan loi (pattern giong DTQG retry queue)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `diab_his_int_docosan_outbox` (
    `id`                CHAR(36)        NOT NULL,
    `tenant_id`         INT             NOT NULL,
    `operation`         VARCHAR(50)     NOT NULL                COMMENT 'CREATE_ORDER|CANCEL|RESCHEDULE|SYNC_DETAIL|REGISTER_USER',
    `session_id`        CHAR(36)        NULL                    COMMENT 'FK -> diab_his_tel_sessions.id',
    `idempotency_key`   VARCHAR(100)    NOT NULL                COMMENT 'Chong tao trung lich khi retry',
    `request_payload`   JSON            NOT NULL,
    `response_payload`  JSON            NULL,
    `status`            VARCHAR(20)     NOT NULL DEFAULT 'PENDING' COMMENT 'PENDING|SENT|FAILED|DEAD',
    `attempt_count`     INT             NOT NULL DEFAULT 0,
    `next_attempt_at`   DATETIME        NULL,
    `last_error`        VARCHAR(1000)   NULL,
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`        CHAR(36)        NULL,
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`        CHAR(36)        NULL,
    `deleted_at`        DATETIME        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_doco_outbox_idem` (`tenant_id`, `idempotency_key`),
    KEY `idx_doco_outbox_due` (`status`, `next_attempt_at`),
    KEY `idx_doco_outbox_session` (`session_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT 'Hang doi retry cho cac loi goi API Docosan';

-- ------------------------------------------------------------
-- 7. Bo sung cot telehealth cho lich hen HIS
-- ------------------------------------------------------------
CALL add_col_if_missing('diab_his_sch_appointments', 'visit_mode',
     'VARCHAR(20) NOT NULL DEFAULT ''AT_CLINIC'' COMMENT ''AT_CLINIC | TELEHEALTH''');
CALL add_col_if_missing('diab_his_sch_appointments', 'telehealth_session_id',
     'CHAR(36) NULL COMMENT ''FK -> diab_his_tel_sessions.id''');
CALL add_index_if_missing('diab_his_sch_appointments', 'idx_appt_tenant_visit_mode',
     '(`tenant_id`, `visit_mode`)');

-- ------------------------------------------------------------
-- 8. Danh dau lan kham co nguon goc telehealth tren encounter (FR-803)
-- ------------------------------------------------------------
CALL add_col_if_missing('diab_his_enc_encounters', 'telehealth_session_id',
     'CHAR(36) NULL COMMENT ''FK -> diab_his_tel_sessions.id; NOT NULL = lan kham tu xa''');
CALL add_index_if_missing('diab_his_enc_encounters', 'idx_enc_telehealth',
     '(`tenant_id`, `telehealth_session_id`)');
