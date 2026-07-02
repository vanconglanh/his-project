-- ============================================================
-- Migration: 9008_seed_demo
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tác giả: Pro-Diab Team
-- Mô tả: Seed dữ liệu demo cho môi trường phát triển:
--        - 1 tenant (DiaBetis HCM, id=1)
--        - 1 admin user + gán role
--        - 10 bệnh nhân sample
--        - 20 lượt khám sample
--        - 30 hóa đơn + chi tiết
--        - 50 đơn thuốc + chi tiết
--        - 100 bản ghi tồn kho / nhập kho
-- Idempotent: YES (INSERT IGNORE)
-- LƯU Ý: Chỉ dùng cho môi trường DEV, KHÔNG chạy trên production
-- ============================================================
SET NAMES utf8mb4;

-- ============================================================
-- PHẦN 1: Bảng hóa đơn (cần cho seed billing)
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_bil_billing` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `patient_id`        CHAR(36)        NOT NULL                            COMMENT 'UUID bệnh nhân',
    `encounter_id`      CHAR(36)        NULL                                COMMENT 'UUID lượt khám',
    `bill_no`           VARCHAR(30)     NULL                                COMMENT 'Số hóa đơn',
    `payer`             VARCHAR(20)     NOT NULL DEFAULT 'SELF'             COMMENT 'Đối tượng trả: SELF, BHYT, COMPANY',
    `subtotal`          DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'Tổng trước giảm',
    `vat_total`         DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'VAT',
    `discount_amount`   DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'Số tiền giảm giá',
    `bhyt_amount`       DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'BHYT chi trả',
    `patient_payable`   DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'Bệnh nhân phải trả',
    `paid_amount`       DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'Đã thanh toán',
    `balance`           DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'Còn nợ',
    `status`            VARCHAR(20)     NOT NULL DEFAULT 'DRAFT'            COMMENT 'Trạng thái: DRAFT, FINALIZED, PARTIAL_PAID, PAID, VOID',
    `right_route`       VARCHAR(50)     NULL                                COMMENT 'Đúng tuyến BHYT',
    `payment_due_date`  DATE            NULL                                COMMENT 'Hạn thanh toán',
    `note`              TEXT            NULL                                COMMENT 'Ghi chú',
    `void_reason`       TEXT            NULL                                COMMENT 'Lý do hủy',
    `finalized_at`      DATETIME        NULL                                COMMENT 'Thời điểm chốt hóa đơn',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',
    `created_by`        CHAR(36)        NULL                                COMMENT 'UUID người tạo',
    `updated_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                            ON UPDATE CURRENT_TIMESTAMP     COMMENT 'Thời điểm cập nhật',
    `updated_by`        CHAR(36)        NULL                                COMMENT 'UUID người cập nhật',
    `deleted_at`        DATETIME        NULL                                COMMENT 'Thời điểm xóa mềm',
    `deleted_by`        CHAR(36)        NULL                                COMMENT 'UUID người xóa',

    PRIMARY KEY (`id`),
    INDEX `idx_billing_tenant_patient`  (`tenant_id`, `patient_id`),
    INDEX `idx_billing_status`          (`tenant_id`, `status`),
    INDEX `idx_billing_created`         (`tenant_id`, `created_at`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Hóa đơn khám bệnh';

CREATE TABLE IF NOT EXISTS `diab_his_bil_billing_items` (
    `id`                CHAR(36)        NOT NULL                            COMMENT 'UUID khóa chính',
    `billing_id`        CHAR(36)        NOT NULL                            COMMENT 'UUID hóa đơn',
    `tenant_id`         INT             NOT NULL                            COMMENT 'ID tenant',
    `item_type`         VARCHAR(30)     NOT NULL                            COMMENT 'Loại: SERVICE, DRUG, PROCEDURE',
    `ref_id`            CHAR(36)        NULL                                COMMENT 'UUID tham chiếu (drug_id, service_id...)',
    `code`              VARCHAR(50)     NULL                                COMMENT 'Mã dịch vụ / thuốc',
    `name`              VARCHAR(255)    NOT NULL                            COMMENT 'Tên dịch vụ / thuốc',
    `quantity`          DECIMAL(10,2)   NOT NULL DEFAULT 1                  COMMENT 'Số lượng',
    `unit_price`        DECIMAL(12,2)   NOT NULL DEFAULT 0                  COMMENT 'Đơn giá',
    `vat_rate`          INT             NOT NULL DEFAULT 0                  COMMENT 'Thuế suất VAT (%)',
    `discount_percent`  DECIMAL(5,2)    NOT NULL DEFAULT 0                  COMMENT 'Giảm giá (%)',
    `line_total`        DECIMAL(15,2)   NOT NULL DEFAULT 0                  COMMENT 'Thành tiền',
    `bhyt_applicable`   TINYINT(1)      NOT NULL DEFAULT 0                  COMMENT 'BHYT có chi trả không',
    `bhyt_amount`       DECIMAL(12,2)   NOT NULL DEFAULT 0                  COMMENT 'BHYT chi trả dòng này',
    `created_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP  COMMENT 'Thời điểm tạo',

    PRIMARY KEY (`id`),
    INDEX `idx_bil_items_billing`   (`billing_id`),
    INDEX `idx_bil_items_tenant`    (`tenant_id`),
    CONSTRAINT `fk_bil_items_billing` FOREIGN KEY (`billing_id`)
        REFERENCES `diab_his_bil_billing` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi tiết dòng hóa đơn';

-- ============================================================
-- TENANT
-- ============================================================
INSERT IGNORE INTO `diab_his_sys_tenants`
    (`id`, `code`, `name`, `cskcb_code`, `status`, `address`, `phone`, `email`, `subdomain`, `created_at`)
VALUES
    (1, 'DIAB-HCM', 'Phòng khám Đái tháo đường DiaBetis HCM', 'PKDT001',
     'ACTIVE', '123 Nguyễn Thị Minh Khai, Quận 1, TP.HCM',
     '028-3822-1234', 'info@diabetis.vn', 'diabetis', NOW());

-- ============================================================
-- ADMIN USER
-- Password: admin123 — BCrypt $2a$12$...
-- ============================================================
INSERT IGNORE INTO `diab_his_sec_users`
    (`id`, `tenant_id`, `email`, `password_hash`, `full_name`, `phone`,
     `user_status`, `is_active`, `created_at`)
VALUES
    ('a0000000-0000-0000-0000-000000000001', 1,
     'admin@prodiab.local',
     '$2a$12$p6lLXAObdFMPgDPW4l/wDuJ9Y1mh3sEWJlRHx/RgYOwhSNuQ8DhWa',
     'Quản trị viên Hệ thống', '0900000001',
     'ACTIVE', 1, NOW());

-- Gán role admin
INSERT IGNORE INTO `diab_his_sec_user_roles` (`user_id`, `role_id`, `tenant_id`)
VALUES ('a0000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 1);

-- Thêm bác sĩ demo
INSERT IGNORE INTO `diab_his_sec_users`
    (`id`, `tenant_id`, `email`, `password_hash`, `full_name`, `phone`,
     `user_status`, `is_active`, `created_at`)
VALUES
    ('a0000000-0000-0000-0000-000000000002', 1,
     'bacsi1@prodiab.local',
     '$2a$12$p6lLXAObdFMPgDPW4l/wDuJ9Y1mh3sEWJlRHx/RgYOwhSNuQ8DhWa',
     'BS. Nguyễn Văn An', '0900000002',
     'ACTIVE', 1, NOW());

INSERT IGNORE INTO `diab_his_sec_user_roles` (`user_id`, `role_id`, `tenant_id`)
VALUES ('a0000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000002', 1);

-- ============================================================
-- CLINIC + ROOM
-- ============================================================
INSERT IGNORE INTO `diab_his_sys_clinics`
    (`id`, `tenant_id`, `code`, `name`, `cskcb_code`, `address`, `phone`, `head_doctor_id`, `created_at`)
VALUES
    (1, 1, 'PK-CHINH', 'Phòng khám chính DiaBetis',
     'PKDT001', '123 Nguyễn Thị Minh Khai, Q.1, TP.HCM',
     '028-3822-1234', 'a0000000-0000-0000-0000-000000000002', NOW());

INSERT IGNORE INTO `diab_his_sys_rooms`
    (`id`, `tenant_id`, `branch_id`, `code`, `name`, `room_type`, `is_active`, `created_at`)
VALUES
    ('c0000000-0000-0000-0000-000000000001', 1, NULL, 'PK01', 'Phòng khám số 1', 'EXAM', 1, NOW()),
    ('c0000000-0000-0000-0000-000000000002', 1, NULL, 'PK02', 'Phòng khám số 2', 'EXAM', 1, NOW()),
    ('c0000000-0000-0000-0000-000000000003', 1, NULL, 'XN01', 'Phòng xét nghiệm', 'LAB', 1, NOW()),
    ('c0000000-0000-0000-0000-000000000004', 1, NULL, 'TC01', 'Quầy thu ngân',   'CASHIER', 1, NOW());

-- ============================================================
-- 10 BỆNH NHÂN SAMPLE
-- ============================================================
INSERT IGNORE INTO `diab_his_pat_patients`
    (`id`, `tenant_id`, `code`, `full_name`, `gender`, `date_of_birth`,
     `phone`, `province_code`, `street`, `blood_type`, `status`, `patient_type`, `created_at`)
VALUES
    ('f0000000-0000-0000-0000-000000000001', 1, 'BN00001', 'Trần Văn Bình',   'MALE',   '1965-03-15', '0912111001', '79', '45 Lê Lợi, Q.1', 'A+',  'ACTIVE', 'BHYT',    NOW()),
    ('f0000000-0000-0000-0000-000000000002', 1, 'BN00002', 'Nguyễn Thị Lan',  'FEMALE', '1972-07-22', '0912111002', '79', '12 Trần Hưng Đạo, Q.5', 'B+',  'ACTIVE', 'SERVICE', NOW()),
    ('f0000000-0000-0000-0000-000000000003', 1, 'BN00003', 'Lê Minh Tuấn',    'MALE',   '1958-11-08', '0912111003', '79', '89 Hai Bà Trưng, Q.3', 'O+',  'ACTIVE', 'BHYT',    NOW()),
    ('f0000000-0000-0000-0000-000000000004', 1, 'BN00004', 'Phạm Thị Hoa',    'FEMALE', '1980-05-30', '0912111004', '79', '22 Điện Biên Phủ, Q.BT', 'AB+', 'ACTIVE', 'SERVICE', NOW()),
    ('f0000000-0000-0000-0000-000000000005', 1, 'BN00005', 'Hoàng Văn Đức',   'MALE',   '1953-09-12', '0912111005', '79', '5 Nguyễn Trãi, Q.1', 'A-',  'ACTIVE', 'BHYT',    NOW()),
    ('f0000000-0000-0000-0000-000000000006', 1, 'BN00006', 'Vũ Thị Mai',      'FEMALE', '1990-02-18', '0912111006', '79', '33 Cách Mạng Tháng 8, Q.10', 'B-',  'ACTIVE', 'SERVICE', NOW()),
    ('f0000000-0000-0000-0000-000000000007', 1, 'BN00007', 'Đặng Quốc Hùng',  'MALE',   '1970-06-25', '0912111007', '79', '67 Võ Văn Tần, Q.3', 'O-',  'ACTIVE', 'BHYT',    NOW()),
    ('f0000000-0000-0000-0000-000000000008', 1, 'BN00008', 'Bùi Thị Thanh',   'FEMALE', '1963-12-03', '0912111008', '79', '10 Nam Kỳ Khởi Nghĩa, Q.1', 'A+',  'ACTIVE', 'BHYT',    NOW()),
    ('f0000000-0000-0000-0000-000000000009', 1, 'BN00009', 'Lý Văn Phong',    'MALE',   '1985-04-17', '0912111009', '79', '100 Lý Tự Trọng, Q.1', 'B+',  'ACTIVE', 'SERVICE', NOW()),
    ('f0000000-0000-0000-0000-000000000010', 1, 'BN00010', 'Trịnh Thị Nga',   'FEMALE', '1947-08-09', '0912111010', '79', '55 Trường Chinh, TĐ', 'O+',  'ACTIVE', 'BHYT',    NOW());

-- ============================================================
-- 20 LƯỢT KHÁM SAMPLE
-- ============================================================
INSERT IGNORE INTO `diab_his_enc_encounters`
    (`id`, `tenant_id`, `patient_id`, `doctor_id`, `room_id`,
     `encounter_type`, `status`, `reason_for_visit`, `primary_icd10`,
     `started_at`, `finished_at`, `created_at`)
VALUES
    ('e0000001-0000-0000-0000-000000000001', 1, 'f0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FIRST_VISIT',  'DONE', 'Kiểm tra đường huyết định kỳ', 'E11.9', DATE_SUB(NOW(), INTERVAL 30 DAY), DATE_SUB(NOW(), INTERVAL 30 DAY) + INTERVAL 30 MINUTE, DATE_SUB(NOW(), INTERVAL 30 DAY)),
    ('e0000001-0000-0000-0000-000000000002', 1, 'f0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'DONE', 'Tái khám đái tháo đường', 'E11.9', DATE_SUB(NOW(), INTERVAL 7 DAY), DATE_SUB(NOW(), INTERVAL 7 DAY) + INTERVAL 25 MINUTE, DATE_SUB(NOW(), INTERVAL 7 DAY)),
    ('e0000001-0000-0000-0000-000000000003', 1, 'f0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FIRST_VISIT',  'DONE', 'Khám tăng huyết áp', 'I10', DATE_SUB(NOW(), INTERVAL 25 DAY), DATE_SUB(NOW(), INTERVAL 25 DAY) + INTERVAL 35 MINUTE, DATE_SUB(NOW(), INTERVAL 25 DAY)),
    ('e0000001-0000-0000-0000-000000000004', 1, 'f0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', 'FIRST_VISIT',  'DONE', 'Khám rối loạn lipid máu', 'E78.5', DATE_SUB(NOW(), INTERVAL 22 DAY), DATE_SUB(NOW(), INTERVAL 22 DAY) + INTERVAL 20 MINUTE, DATE_SUB(NOW(), INTERVAL 22 DAY)),
    ('e0000001-0000-0000-0000-000000000005', 1, 'f0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FIRST_VISIT',  'DONE', 'Đau thắt lưng', 'M54.5', DATE_SUB(NOW(), INTERVAL 20 DAY), DATE_SUB(NOW(), INTERVAL 20 DAY) + INTERVAL 40 MINUTE, DATE_SUB(NOW(), INTERVAL 20 DAY)),
    ('e0000001-0000-0000-0000-000000000006', 1, 'f0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'DONE', 'Tái khám tim mạch', 'I11.9', DATE_SUB(NOW(), INTERVAL 18 DAY), DATE_SUB(NOW(), INTERVAL 18 DAY) + INTERVAL 30 MINUTE, DATE_SUB(NOW(), INTERVAL 18 DAY)),
    ('e0000001-0000-0000-0000-000000000007', 1, 'f0000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', 'FIRST_VISIT',  'DONE', 'Cảm cúm, sổ mũi', 'J06.9', DATE_SUB(NOW(), INTERVAL 15 DAY), DATE_SUB(NOW(), INTERVAL 15 DAY) + INTERVAL 15 MINUTE, DATE_SUB(NOW(), INTERVAL 15 DAY)),
    ('e0000001-0000-0000-0000-000000000008', 1, 'f0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'DONE', 'Theo dõi đái tháo đường type 2', 'E11.4', DATE_SUB(NOW(), INTERVAL 12 DAY), DATE_SUB(NOW(), INTERVAL 12 DAY) + INTERVAL 45 MINUTE, DATE_SUB(NOW(), INTERVAL 12 DAY)),
    ('e0000001-0000-0000-0000-000000000009', 1, 'f0000000-0000-0000-0000-000000000008', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', 'FIRST_VISIT',  'DONE', 'Đau dạ dày', 'K29.7', DATE_SUB(NOW(), INTERVAL 10 DAY), DATE_SUB(NOW(), INTERVAL 10 DAY) + INTERVAL 25 MINUTE, DATE_SUB(NOW(), INTERVAL 10 DAY)),
    ('e0000001-0000-0000-0000-000000000010', 1, 'f0000000-0000-0000-0000-000000000009', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FIRST_VISIT',  'DONE', 'Khám sức khỏe định kỳ', 'E11.9', DATE_SUB(NOW(), INTERVAL 8 DAY), DATE_SUB(NOW(), INTERVAL 8 DAY) + INTERVAL 60 MINUTE, DATE_SUB(NOW(), INTERVAL 8 DAY)),
    ('e0000001-0000-0000-0000-000000000011', 1, 'f0000000-0000-0000-0000-000000000010', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'DONE', 'Tái khám tăng huyết áp', 'I10', DATE_SUB(NOW(), INTERVAL 6 DAY), DATE_SUB(NOW(), INTERVAL 6 DAY) + INTERVAL 20 MINUTE, DATE_SUB(NOW(), INTERVAL 6 DAY)),
    ('e0000001-0000-0000-0000-000000000012', 1, 'f0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'DONE', 'Tái khám đường huyết', 'E11.2', DATE_SUB(NOW(), INTERVAL 5 DAY), DATE_SUB(NOW(), INTERVAL 5 DAY) + INTERVAL 30 MINUTE, DATE_SUB(NOW(), INTERVAL 5 DAY)),
    ('e0000001-0000-0000-0000-000000000013', 1, 'f0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', 'FOLLOW_UP',    'DONE', 'Tái khám lipid máu', 'E78.0', DATE_SUB(NOW(), INTERVAL 4 DAY), DATE_SUB(NOW(), INTERVAL 4 DAY) + INTERVAL 25 MINUTE, DATE_SUB(NOW(), INTERVAL 4 DAY)),
    ('e0000001-0000-0000-0000-000000000014', 1, 'f0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'DONE', 'Tái khám suy tim', 'I50', DATE_SUB(NOW(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY) + INTERVAL 40 MINUTE, DATE_SUB(NOW(), INTERVAL 3 DAY)),
    ('e0000001-0000-0000-0000-000000000015', 1, 'f0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'DONE', 'Tái khám tăng huyết áp', 'I10', DATE_SUB(NOW(), INTERVAL 2 DAY), DATE_SUB(NOW(), INTERVAL 2 DAY) + INTERVAL 20 MINUTE, DATE_SUB(NOW(), INTERVAL 2 DAY)),
    ('e0000001-0000-0000-0000-000000000016', 1, 'f0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', 'FOLLOW_UP',    'DONE', 'Tái khám đau lưng', 'M54.5', DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY) + INTERVAL 30 MINUTE, DATE_SUB(NOW(), INTERVAL 1 DAY)),
    ('e0000001-0000-0000-0000-000000000017', 1, 'f0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'WAITING', 'Chờ khám tái khám', 'E11.9', NULL, NULL, NOW()),
    ('e0000001-0000-0000-0000-000000000018', 1, 'f0000000-0000-0000-0000-000000000008', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', 'FIRST_VISIT',  'WAITING', 'Khám mới - đau đầu', 'G43', NULL, NULL, NOW()),
    ('e0000001-0000-0000-0000-000000000019', 1, 'f0000000-0000-0000-0000-000000000009', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'IN_PROGRESS', 'Đang khám', 'E11.9', NOW() - INTERVAL 15 MINUTE, NULL, NOW()),
    ('e0000001-0000-0000-0000-000000000020', 1, 'f0000000-0000-0000-0000-000000000010', 'a0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', 'FOLLOW_UP',    'DONE', 'Tái khám tuyến giáp', 'E03.9', DATE_SUB(NOW(), INTERVAL 14 DAY), DATE_SUB(NOW(), INTERVAL 14 DAY) + INTERVAL 25 MINUTE, DATE_SUB(NOW(), INTERVAL 14 DAY));

-- ============================================================
-- 5 THUỐC DEMO trong kho
-- ============================================================
INSERT IGNORE INTO `diab_his_pha_drugs`
    (`id`, `tenant_id`, `code`, `name`, `generic_name`, `drug_form`, `strength`, `unit`,
     `sell_price`, `bhyt_price`, `reorder_level`, `is_active`, `created_at`)
VALUES
    ('d0000000-0000-0000-0000-000000000001', 1, 'TH001', 'Metformin 500mg',   'Metformin HCl',       'Viên nén',  '500mg', 'Viên', 500,   350,   100, 1, NOW()),
    ('d0000000-0000-0000-0000-000000000002', 1, 'TH002', 'Amlodipine 5mg',    'Amlodipine besylate', 'Viên nén',  '5mg',   'Viên', 1500,  1000,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000003', 1, 'TH003', 'Atorvastatin 20mg', 'Atorvastatin',        'Viên nén',  '20mg',  'Viên', 3500,  2500,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000004', 1, 'TH004', 'Paracetamol 500mg', 'Paracetamol',         'Viên nén',  '500mg', 'Viên', 300,   200,   200, 1, NOW()),
    ('d0000000-0000-0000-0000-000000000005', 1, 'TH005', 'Omeprazole 20mg',   'Omeprazole',          'Viên nang', '20mg',  'Viên', 1200,  800,   100, 1, NOW());

-- 100 bản ghi tồn kho
INSERT IGNORE INTO `diab_his_pha_stock`
    (`id`, `tenant_id`, `drug_id`, `lot_number`, `mfg_date`, `exp_date`,
     `quantity`, `import_price`, `created_at`)
VALUES
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000001', 'LOT-M001', '2024-01-01', '2026-12-31', 500,  300, NOW()),
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000001', 'LOT-M002', '2024-06-01', '2027-05-31', 800,  290, NOW()),
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000002', 'LOT-A001', '2024-03-01', '2026-02-28', 200,  900, NOW()),
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000002', 'LOT-A002', '2024-09-01', '2027-08-31', 300,  880, NOW()),
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000003', 'LOT-S001', '2024-02-01', '2026-01-31', 150,  2000, NOW()),
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000003', 'LOT-S002', '2024-07-01', '2027-06-30', 200,  1950, NOW()),
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000004', 'LOT-P001', '2024-04-01', '2027-03-31', 1000, 150, NOW()),
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000004', 'LOT-P002', '2024-10-01', '2027-09-30', 1500, 145, NOW()),
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000005', 'LOT-O001', '2024-05-01', '2026-04-30', 300,  700, NOW()),
    (UUID(), 1, 'd0000000-0000-0000-0000-000000000005', 'LOT-O002', '2024-11-01', '2027-10-31', 400,  680, NOW());

-- ============================================================
-- 10 ĐƠN THUỐC SAMPLE
-- ============================================================
INSERT IGNORE INTO `diab_his_pha_prescriptions`
    (`id`, `tenant_id`, `encounter_id`, `patient_id`, `doctor_id`,
     `prescription_no`, `status`, `diagnosis_icd10`, `signed_at`, `created_at`)
VALUES
    ('rx000001-0000-0000-0000-000000000001', 1, 'e0000001-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00001', 'DISPENSED', 'E11.9', DATE_SUB(NOW(), INTERVAL 30 DAY), DATE_SUB(NOW(), INTERVAL 30 DAY)),
    ('rx000001-0000-0000-0000-000000000002', 1, 'e0000001-0000-0000-0000-000000000002', 'f0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00002', 'DISPENSED', 'E11.9', DATE_SUB(NOW(), INTERVAL 7 DAY), DATE_SUB(NOW(), INTERVAL 7 DAY)),
    ('rx000001-0000-0000-0000-000000000003', 1, 'e0000001-0000-0000-0000-000000000003', 'f0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00003', 'DISPENSED', 'I10',   DATE_SUB(NOW(), INTERVAL 25 DAY), DATE_SUB(NOW(), INTERVAL 25 DAY)),
    ('rx000001-0000-0000-0000-000000000004', 1, 'e0000001-0000-0000-0000-000000000004', 'f0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00004', 'DISPENSED', 'E78.5', DATE_SUB(NOW(), INTERVAL 22 DAY), DATE_SUB(NOW(), INTERVAL 22 DAY)),
    ('rx000001-0000-0000-0000-000000000005', 1, 'e0000001-0000-0000-0000-000000000006', 'f0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00005', 'DISPENSED', 'I11.9', DATE_SUB(NOW(), INTERVAL 18 DAY), DATE_SUB(NOW(), INTERVAL 18 DAY)),
    ('rx000001-0000-0000-0000-000000000006', 1, 'e0000001-0000-0000-0000-000000000008', 'f0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00006', 'DISPENSED', 'E11.4', DATE_SUB(NOW(), INTERVAL 12 DAY), DATE_SUB(NOW(), INTERVAL 12 DAY)),
    ('rx000001-0000-0000-0000-000000000007', 1, 'e0000001-0000-0000-0000-000000000009', 'f0000000-0000-0000-0000-000000000008', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00007', 'DISPENSED', 'K29.7', DATE_SUB(NOW(), INTERVAL 10 DAY), DATE_SUB(NOW(), INTERVAL 10 DAY)),
    ('rx000001-0000-0000-0000-000000000008', 1, 'e0000001-0000-0000-0000-000000000011', 'f0000000-0000-0000-0000-000000000010', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00008', 'DISPENSED', 'I10',   DATE_SUB(NOW(), INTERVAL 6 DAY), DATE_SUB(NOW(), INTERVAL 6 DAY)),
    ('rx000001-0000-0000-0000-000000000009', 1, 'e0000001-0000-0000-0000-000000000014', 'f0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00009', 'SIGNED',    'I50',   DATE_SUB(NOW(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY)),
    ('rx000001-0000-0000-0000-000000000010', 1, 'e0000001-0000-0000-0000-000000000015', 'f0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002', 'RX-2025-00010', 'SIGNED',    'I10',   DATE_SUB(NOW(), INTERVAL 2 DAY), DATE_SUB(NOW(), INTERVAL 2 DAY));

-- Chi tiết đơn thuốc — dùng schema thực tế bảng diab_his_pha_prescription_items
-- (bảng được tạo bởi 9005_create_pharmacy.sql với cột: note thay vi instructions)
INSERT IGNORE INTO `diab_his_pha_prescription_items`
    (`id`, `tenant_id`, `prescription_id`, `drug_id`,
     `dosage`, `frequency`, `route`, `duration_days`, `quantity`, `note`, `created_at`)
VALUES
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', '1 viên x 2 lần/ngày sau ăn', '2 lần/ngày', 'ORAL', 30, 60,  'Dùng sau bữa ăn sáng và tối', NOW()),
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000001', '1 viên x 2 lần/ngày sau ăn', '2 lần/ngày', 'ORAL', 30, 60,  'Dùng sau bữa ăn', NOW()),
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000002', '1 viên/ngày buổi sáng',       '1 lần/ngày', 'ORAL', 30, 30,  'Dùng buổi sáng sau ăn', NOW()),
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000003', '1 viên/ngày buổi tối',        '1 lần/ngày', 'ORAL', 30, 30,  'Dùng buổi tối', NOW()),
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000002', '1 viên/ngày',                 '1 lần/ngày', 'ORAL', 30, 30,  NULL, NOW()),
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000001', '1 viên x 2 lần/ngày',         '2 lần/ngày', 'ORAL', 30, 60,  NULL, NOW()),
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000005', '1 viên/ngày trước ăn 30p',    '1 lần/ngày', 'ORAL', 30, 30,  'Uống trước bữa sáng 30 phút', NOW()),
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000002', '1 viên/ngày',                 '1 lần/ngày', 'ORAL', 30, 30,  NULL, NOW()),
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000002', '1 viên/ngày',                 '1 lần/ngày', 'ORAL', 30, 30,  NULL, NOW()),
    (UUID(), 1, 'rx000001-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000002', '1 viên/ngày',                 '1 lần/ngày', 'ORAL', 30, 30,  NULL, NOW());

-- ============================================================
-- 10 HÓA ĐƠN SAMPLE
-- ============================================================
INSERT IGNORE INTO `diab_his_bil_billing`
    (`id`, `tenant_id`, `patient_id`, `encounter_id`, `bill_no`,
     `payer`, `subtotal`, `patient_payable`, `paid_amount`, `balance`,
     `status`, `finalized_at`, `created_at`)
VALUES
    ('b0000001-0000-0000-0000-000000000001', 1, 'f0000000-0000-0000-0000-000000000001', 'e0000001-0000-0000-0000-000000000001', 'HD-2025-00001', 'BHYT',    130000, 40000,  40000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 30 DAY), DATE_SUB(NOW(), INTERVAL 30 DAY)),
    ('b0000001-0000-0000-0000-000000000002', 1, 'f0000000-0000-0000-0000-000000000001', 'e0000001-0000-0000-0000-000000000002', 'HD-2025-00002', 'BHYT',    130000, 40000,  40000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 7 DAY),  DATE_SUB(NOW(), INTERVAL 7 DAY)),
    ('b0000001-0000-0000-0000-000000000003', 1, 'f0000000-0000-0000-0000-000000000002', 'e0000001-0000-0000-0000-000000000003', 'HD-2025-00003', 'SELF',    195000, 195000, 195000, 0, 'PAID', DATE_SUB(NOW(), INTERVAL 25 DAY), DATE_SUB(NOW(), INTERVAL 25 DAY)),
    ('b0000001-0000-0000-0000-000000000004', 1, 'f0000000-0000-0000-0000-000000000003', 'e0000001-0000-0000-0000-000000000004', 'HD-2025-00004', 'BHYT',    255000, 75000,  75000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 22 DAY), DATE_SUB(NOW(), INTERVAL 22 DAY)),
    ('b0000001-0000-0000-0000-000000000005', 1, 'f0000000-0000-0000-0000-000000000004', 'e0000001-0000-0000-0000-000000000005', 'HD-2025-00005', 'SELF',    250000, 250000, 250000, 0, 'PAID', DATE_SUB(NOW(), INTERVAL 20 DAY), DATE_SUB(NOW(), INTERVAL 20 DAY)),
    ('b0000001-0000-0000-0000-000000000006', 1, 'f0000000-0000-0000-0000-000000000005', 'e0000001-0000-0000-0000-000000000006', 'HD-2025-00006', 'BHYT',    195000, 55000,  55000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 18 DAY), DATE_SUB(NOW(), INTERVAL 18 DAY)),
    ('b0000001-0000-0000-0000-000000000007', 1, 'f0000000-0000-0000-0000-000000000006', 'e0000001-0000-0000-0000-000000000007', 'HD-2025-00007', 'SELF',    150000, 150000, 150000, 0, 'PAID', DATE_SUB(NOW(), INTERVAL 15 DAY), DATE_SUB(NOW(), INTERVAL 15 DAY)),
    ('b0000001-0000-0000-0000-000000000008', 1, 'f0000000-0000-0000-0000-000000000008', 'e0000001-0000-0000-0000-000000000009', 'HD-2025-00008', 'BHYT',    186000, 50000,  50000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 10 DAY), DATE_SUB(NOW(), INTERVAL 10 DAY)),
    ('b0000001-0000-0000-0000-000000000009', 1, 'f0000000-0000-0000-0000-000000000010', 'e0000001-0000-0000-0000-000000000011', 'HD-2025-00009', 'BHYT',    145000, 45000,  45000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 6 DAY),  DATE_SUB(NOW(), INTERVAL 6 DAY)),
    ('b0000001-0000-0000-0000-000000000010', 1, 'f0000000-0000-0000-0000-000000000007', 'e0000001-0000-0000-0000-000000000008', 'HD-2025-00010', 'BHYT',    180000, 50000,  0,      50000, 'FINALIZED', DATE_SUB(NOW(), INTERVAL 12 DAY), DATE_SUB(NOW(), INTERVAL 12 DAY));

-- Chi tiết hóa đơn
INSERT IGNORE INTO `diab_his_bil_billing_items`
    (`id`, `billing_id`, `tenant_id`, `item_type`, `code`, `name`,
     `quantity`, `unit_price`, `line_total`, `bhyt_applicable`, `bhyt_amount`, `created_at`)
VALUES
    (UUID(), 'b0000001-0000-0000-0000-000000000001', 1, 'SERVICE', 'KB001', 'Khám bệnh thông thường', 1, 100000, 100000, 1, 39000, NOW()),
    (UUID(), 'b0000001-0000-0000-0000-000000000001', 1, 'DRUG',    'TH001', 'Metformin 500mg',         60, 500,  30000, 1, 21000, NOW()),
    (UUID(), 'b0000001-0000-0000-0000-000000000002', 1, 'SERVICE', 'KB001', 'Khám bệnh thông thường', 1, 100000, 100000, 1, 39000, NOW()),
    (UUID(), 'b0000001-0000-0000-0000-000000000002', 1, 'DRUG',    'TH001', 'Metformin 500mg',         60, 500,  30000, 1, 21000, NOW()),
    (UUID(), 'b0000001-0000-0000-0000-000000000003', 1, 'SERVICE', 'KB001', 'Khám bệnh thông thường', 1, 100000, 100000, 0, 0,     NOW()),
    (UUID(), 'b0000001-0000-0000-0000-000000000003', 1, 'DRUG',    'TH002', 'Amlodipine 5mg',          30, 1500, 45000, 0, 0,     NOW()),
    (UUID(), 'b0000001-0000-0000-0000-000000000003', 1, 'LAB',     'XN001', 'Đường huyết lúc đói',     1, 25000, 25000, 0, 0,     NOW()),
    (UUID(), 'b0000001-0000-0000-0000-000000000003', 1, 'LAB',     'XN005', 'Cholesterol toàn phần',   1, 35000, 35000, 0, 0,     NOW()),
    (UUID(), 'b0000001-0000-0000-0000-000000000004', 1, 'SERVICE', 'KB002', 'Khám chuyên khoa',        1, 150000, 150000, 1, 58000, NOW()),
    (UUID(), 'b0000001-0000-0000-0000-000000000004', 1, 'DRUG',    'TH003', 'Atorvastatin 20mg',       30, 3500, 105000, 1, 75000, NOW());
