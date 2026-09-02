-- ============================================================
-- Seed: diab_test_tenant_demo_extra
-- Phiên bản: 1.0  Ngày: 2026-09-02
-- Mục đích: Mở rộng tenant_id=2 với data đa trạng thái cho demo & QA.
--           Chạy SAU diab_test_tenant.sql (bảng & 15 bệnh nhân cũ đã có).
--
-- Nội dung:
--   +10 bệnh nhân mới (BNT00016-BNT00025) → tổng 25 BN
--   Hàng đợi hôm nay: 3 WAITING + 1 IN_PROGRESS (queue tickets)
--   Encounters hôm nay: WAITING / IN_PROGRESS / DONE × nhiều BN
--   Encounters lịch sử: 2 ca tháng trước (cho báo cáo)
--   Đơn thuốc: SIGNED (chưa cấp phát) + DISPENSED (đã cấp phát)
--   Hoá đơn: PAID / PARTIAL_PAID (còn nợ) / FINALIZED (chưa thanh toán)
--   CLS: lab order ordered (chưa có kết quả) + done (có kết quả)
--   Recall: OVERDUE_VISIT + OVERDUE_HBA1C
--   Gói dịch vụ: 1 gói SẮP HẾT HẠN (10 ngày) + 1 gói còn hạn dài
--   Chỉ số HbA1c: tốt / khá / kém (3 BN ĐTĐ)
--
-- Idempotent: INSERT IGNORE (PK cố định)
-- Cleanup:    DELETE FROM <bảng> WHERE id IN (...) — xem cuối file
-- Mật khẩu:  Tất cả user từ seed gốc dùng chung admin123
-- KHÔNG chạy trên production.
-- ============================================================
SET NAMES utf8mb4;
SET @today      = CURDATE();                                 -- 2026-09-02
SET @today_dt   = NOW();
SET @last_month = DATE_SUB(CURDATE(), INTERVAL 35 DAY);     -- khoảng 2026-07-28

-- ================================================================
-- 1. BỆNH NHÂN MỚI (BNT00016 – BNT00025)
--    Prefix 'f2000000-0000-0000-0000-0000000000' (16 ký tự cuối: 2 chữ số)
-- ================================================================
INSERT IGNORE INTO `diab_his_pat_patients`
    (`id`, `tenant_id`, `code`, `full_name`, `gender`, `date_of_birth`,
     `phone`, `province_code`, `street`, `blood_type`, `status`, `patient_type`, `created_at`)
VALUES
-- BNT00016: BHYT, hàng đợi chờ khám hôm nay
('f2000000-0000-0000-0000-000000000016', 2, 'BNT00016', N'Phan Thị Quỳnh',   'FEMALE', '1971-03-12', '0932000016', '79', '16 Demo, Q.Tân Bình', 'A+',  'ACTIVE', 'BHYT',    NOW()),
-- BNT00017: SERVICE, hàng đợi chờ khám hôm nay
('f2000000-0000-0000-0000-000000000017', 2, 'BNT00017', N'Lưu Văn Sơn',      'MALE',   '1988-07-04', '0932000017', '79', '17 Demo, Q.12',       'O+',  'ACTIVE', 'SERVICE', NOW()),
-- BNT00018: SERVICE, ĐTĐ kiểm soát TỐT (HbA1c 6.2%)
('f2000000-0000-0000-0000-000000000018', 2, 'BNT00018', N'Nguyễn Thị Thanh', 'FEMALE', '1965-11-20', '0932000018', '79', '18 Demo, Q.Bình Thạnh','B+',  'ACTIVE', 'SERVICE', NOW()),
-- BNT00019: BHYT, ĐTĐ kiểm soát KÉM (HbA1c 10.1%) → recall OVERDUE_HBA1C
('f2000000-0000-0000-0000-000000000019', 2, 'BNT00019', N'Trương Văn Toàn',  'MALE',   '1952-05-09', '0932000019', '79', '19 Demo, Q.Gò Vấp',   'O-',  'ACTIVE', 'BHYT',    NOW()),
-- BNT00020: SERVICE, ĐTĐ kiểm soát KHÁ (HbA1c 7.8%)
('f2000000-0000-0000-0000-000000000020', 2, 'BNT00020', N'Võ Thị Uyên',      'FEMALE', '1959-09-15', '0932000020', '79', '20 Demo, Q.Phú Nhuận', 'AB+', 'ACTIVE', 'SERVICE', NOW()),
-- BNT00021: SERVICE, có gói dịch vụ SẮP HẾT HẠN → demo report P-01
('f2000000-0000-0000-0000-000000000021', 2, 'BNT00021', N'Đinh Văn Vinh',    'MALE',   '1976-02-28', '0932000021', '79', '21 Demo, Q.7',         'A-',  'ACTIVE', 'SERVICE', NOW()),
-- BNT00022: SERVICE, có gói dịch vụ còn hạn dài
('f2000000-0000-0000-0000-000000000022', 2, 'BNT00022', N'Hà Thị Xuân',      'FEMALE', '1983-06-18', '0932000022', '79', '22 Demo, Q.3',         'B-',  'ACTIVE', 'SERVICE', NOW()),
-- BNT00023: BHYT, cần tái khám → recall OVERDUE_VISIT
('f2000000-0000-0000-0000-000000000023', 2, 'BNT00023', N'Bùi Văn Yên',      'MALE',   '1961-12-03', '0932000023', '79', '23 Demo, Q.Bình Tân',  'O+',  'ACTIVE', 'BHYT',    NOW()),
-- BNT00024: BHYT, lịch sử khám tháng trước (cho báo cáo)
('f2000000-0000-0000-0000-000000000024', 2, 'BNT00024', N'Cao Thị Zoan',     'FEMALE', '1943-04-07', '0932000024', '79', '24 Demo, Q.1',         'A+',  'ACTIVE', 'BHYT',    NOW()),
-- BNT00025: SERVICE, lịch sử khám 2 tháng trước (cho báo cáo)
('f2000000-0000-0000-0000-000000000025', 2, 'BNT00025', N'Lê Văn Anh',       'MALE',   '1993-08-21', '0932000025', '79', '25 Demo, Q.2',         'B+',  'ACTIVE', 'SERVICE', NOW());

-- ================================================================
-- 2. HÀNG ĐỢI HÔM NAY (diab_his_rcp_queue_tickets)
--    ticket_no format: T<YYYYMMDD>-NNN
--    Dải UUID: q2000000-0000-0000-0000-000000000001..
-- ================================================================
INSERT IGNORE INTO `diab_his_rcp_queue_tickets`
    (`id`, `tenant_id`, `patient_id`, `room_id`, `doctor_id`,
     `ticket_no`, `ticket_date`, `status`, `priority`,
     `reason_for_visit`, `checked_in_at`, `created_by`)
VALUES
-- T001: BNT00016 chờ khám phòng 1
('q2000000-0000-0000-0000-000000000001', 2,
 'f2000000-0000-0000-0000-000000000016',
 'c2000000-0000-0000-0000-000000000001',
 'a2000000-0000-0000-0000-000000000003',
 '001', @today, 'WAITING', 'NORMAL',
 N'Tái khám tiểu đường, kiểm tra HbA1c',
 TIMESTAMP(@today, '07:30:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- T002: BNT00017 chờ khám phòng 2
('q2000000-0000-0000-0000-000000000002', 2,
 'f2000000-0000-0000-0000-000000000017',
 'c2000000-0000-0000-0000-000000000002',
 'a2000000-0000-0000-0000-000000000004',
 '002', @today, 'WAITING', 'NORMAL',
 N'Khám lần đầu, đau đầu và chóng mặt',
 TIMESTAMP(@today, '07:45:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- T003: BNT00001 đang khám (IN_PROGRESS) phòng 1
('q2000000-0000-0000-0000-000000000003', 2,
 'f2000000-0000-0000-0000-000000000001',
 'c2000000-0000-0000-0000-000000000001',
 'a2000000-0000-0000-0000-000000000003',
 '003', @today, 'IN_PROGRESS', 'NORMAL',
 N'Khám định kỳ ĐTĐ type 2',
 TIMESTAMP(@today, '07:15:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- T004: BNT00003 đã xong (DONE) sáng sớm
('q2000000-0000-0000-0000-000000000004', 2,
 'f2000000-0000-0000-0000-000000000003',
 'c2000000-0000-0000-0000-000000000001',
 'a2000000-0000-0000-0000-000000000003',
 '004', @today, 'DONE', 'NORMAL',
 N'Tăng huyết áp, khó thở',
 TIMESTAMP(@today, '06:50:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- T005: BNT00005 đã xong (DONE) — BHYT
('q2000000-0000-0000-0000-000000000005', 2,
 'f2000000-0000-0000-0000-000000000005',
 'c2000000-0000-0000-0000-000000000002',
 'a2000000-0000-0000-0000-000000000004',
 '005', @today, 'DONE', 'NORMAL',
 N'Khám tổng quát',
 TIMESTAMP(@today, '07:00:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- T006: BNT00018 đã xong (DONE) — ĐTĐ tốt
('q2000000-0000-0000-0000-000000000006', 2,
 'f2000000-0000-0000-0000-000000000018',
 'c2000000-0000-0000-0000-000000000001',
 'a2000000-0000-0000-0000-000000000003',
 '006', @today, 'DONE', 'NORMAL',
 N'Tái khám ĐTĐ định kỳ 3 tháng',
 TIMESTAMP(@today, '06:40:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- T007: BNT00019 — ĐTĐ kém, đã xong
('q2000000-0000-0000-0000-000000000007', 2,
 'f2000000-0000-0000-0000-000000000019',
 'c2000000-0000-0000-0000-000000000002',
 'a2000000-0000-0000-0000-000000000004',
 '007', @today, 'DONE', 'HIGH',
 N'HbA1c tăng cao, cần điều chỉnh insulin',
 TIMESTAMP(@today, '07:05:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- T008: BNT00007 đang khám (IN_PROGRESS) phòng 2
('q2000000-0000-0000-0000-000000000008', 2,
 'f2000000-0000-0000-0000-000000000007',
 'c2000000-0000-0000-0000-000000000002',
 'a2000000-0000-0000-0000-000000000004',
 '008', @today, 'IN_PROGRESS', 'NORMAL',
 N'Khám mắt do ĐTĐ',
 TIMESTAMP(@today, '07:50:00'),
 'a2000000-0000-0000-0000-000000000002');

-- ================================================================
-- 3. ENCOUNTERS HÔM NAY
--    Dải UUID: e2000000-0000-0000-0000-000000000001..
-- ================================================================
INSERT IGNORE INTO `diab_his_enc_encounters`
    (`id`, `tenant_id`, `patient_id`, `doctor_id`, `room_id`,
     `encounter_type`, `status`, `reason_for_visit`,
     `primary_icd10`, `encounter_no`,
     `started_at`, `finished_at`, `created_by`)
VALUES
-- E01: BNT00001 — WAITING (bác sĩ chưa mở file)
('e2000000-0000-0000-0000-000000000001', 2,
 'f2000000-0000-0000-0000-000000000001',
 'a2000000-0000-0000-0000-000000000003',
 'c2000000-0000-0000-0000-000000000001',
 'FOLLOW_UP', 'WAITING',
 N'Tái khám ĐTĐ type 2',
 'E11.9', 'ENC20260902-001',
 TIMESTAMP(@today, '08:00:00'), NULL,
 'a2000000-0000-0000-0000-000000000002'),

-- E02: BNT00003 — DONE (đã khám xong hôm nay)
('e2000000-0000-0000-0000-000000000002', 2,
 'f2000000-0000-0000-0000-000000000003',
 'a2000000-0000-0000-0000-000000000003',
 'c2000000-0000-0000-0000-000000000001',
 'FOLLOW_UP', 'DONE',
 N'Tăng huyết áp, khó thở',
 'I10', 'ENC20260902-002',
 TIMESTAMP(@today, '07:00:00'), TIMESTAMP(@today, '07:40:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- E03: BNT00005 — DONE (đã khám xong, BHYT)
('e2000000-0000-0000-0000-000000000003', 2,
 'f2000000-0000-0000-0000-000000000005',
 'a2000000-0000-0000-0000-000000000004',
 'c2000000-0000-0000-0000-000000000002',
 'FIRST_VISIT', 'DONE',
 N'Khám tổng quát lần đầu',
 'Z00.0', 'ENC20260902-003',
 TIMESTAMP(@today, '07:10:00'), TIMESTAMP(@today, '07:50:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- E04: BNT00007 — IN_PROGRESS (đang khám)
('e2000000-0000-0000-0000-000000000004', 2,
 'f2000000-0000-0000-0000-000000000007',
 'a2000000-0000-0000-0000-000000000004',
 'c2000000-0000-0000-0000-000000000002',
 'FOLLOW_UP', 'IN_PROGRESS',
 N'Biến chứng mắt do ĐTĐ',
 'E11.3', 'ENC20260902-004',
 TIMESTAMP(@today, '08:00:00'), NULL,
 'a2000000-0000-0000-0000-000000000002'),

-- E05: BNT00018 — DONE (ĐTĐ kiểm soát tốt)
('e2000000-0000-0000-0000-000000000005', 2,
 'f2000000-0000-0000-0000-000000000018',
 'a2000000-0000-0000-0000-000000000003',
 'c2000000-0000-0000-0000-000000000001',
 'FOLLOW_UP', 'DONE',
 N'Tái khám ĐTĐ định kỳ 3 tháng',
 'E11.9', 'ENC20260902-005',
 TIMESTAMP(@today, '06:50:00'), TIMESTAMP(@today, '07:30:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- E06: BNT00019 — DONE (ĐTĐ kiểm soát kém, HbA1c 10.1%)
('e2000000-0000-0000-0000-000000000006', 2,
 'f2000000-0000-0000-0000-000000000019',
 'a2000000-0000-0000-0000-000000000004',
 'c2000000-0000-0000-0000-000000000002',
 'FOLLOW_UP', 'DONE',
 N'HbA1c tăng cao, điều chỉnh liều insulin',
 'E11.9', 'ENC20260902-006',
 TIMESTAMP(@today, '07:15:00'), TIMESTAMP(@today, '08:00:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- E07: BNT00008 — DONE (có CLS, kết quả bất thường chưa duyệt)
('e2000000-0000-0000-0000-000000000007', 2,
 'f2000000-0000-0000-0000-000000000008',
 'a2000000-0000-0000-0000-000000000003',
 'c2000000-0000-0000-0000-000000000001',
 'FOLLOW_UP', 'DONE',
 N'Đau ngực, khó thở — chỉ định XN Tim mạch',
 'I20.9', 'ENC20260902-007',
 TIMESTAMP(@today, '06:30:00'), TIMESTAMP(@today, '07:10:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- E08: BNT00004 — DONE (đơn thuốc chưa cấp phát, hoá đơn còn nợ)
('e2000000-0000-0000-0000-000000000008', 2,
 'f2000000-0000-0000-0000-000000000004',
 'a2000000-0000-0000-0000-000000000003',
 'c2000000-0000-0000-0000-000000000001',
 'FOLLOW_UP', 'DONE',
 N'Rối loạn mỡ máu, tái khám',
 'E78.5', 'ENC20260902-008',
 TIMESTAMP(@today, '06:45:00'), TIMESTAMP(@today, '07:20:00'),
 'a2000000-0000-0000-0000-000000000002');

-- ================================================================
-- 4. ENCOUNTERS LỊCH SỬ (tháng trước — cho báo cáo)
-- ================================================================
INSERT IGNORE INTO `diab_his_enc_encounters`
    (`id`, `tenant_id`, `patient_id`, `doctor_id`, `room_id`,
     `encounter_type`, `status`, `reason_for_visit`,
     `primary_icd10`, `encounter_no`,
     `started_at`, `finished_at`, `created_by`)
VALUES
-- E09: BNT00024 — lịch sử tháng trước
('e2000000-0000-0000-0000-000000000009', 2,
 'f2000000-0000-0000-0000-000000000024',
 'a2000000-0000-0000-0000-000000000003',
 'c2000000-0000-0000-0000-000000000001',
 'FIRST_VISIT', 'DONE',
 N'Khám mới, đái tháo đường type 2',
 'E11.9', 'ENC20260728-001',
 TIMESTAMP(@last_month, '09:00:00'), TIMESTAMP(@last_month, '09:45:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- E10: BNT00020 — lịch sử tháng trước (HbA1c khá)
('e2000000-0000-0000-0000-000000000010', 2,
 'f2000000-0000-0000-0000-000000000020',
 'a2000000-0000-0000-0000-000000000004',
 'c2000000-0000-0000-0000-000000000002',
 'FOLLOW_UP', 'DONE',
 N'Tái khám ĐTĐ, kiểm tra HbA1c định kỳ',
 'E11.9', 'ENC20260728-002',
 TIMESTAMP(@last_month, '10:00:00'), TIMESTAMP(@last_month, '10:40:00'),
 'a2000000-0000-0000-0000-000000000002'),

-- E11: BNT00025 — lịch sử 2 tháng trước
('e2000000-0000-0000-0000-000000000011', 2,
 'f2000000-0000-0000-0000-000000000025',
 'a2000000-0000-0000-0000-000000000003',
 'c2000000-0000-0000-0000-000000000001',
 'FIRST_VISIT', 'DONE',
 N'Khám lần đầu, mệt mỏi và khát nhiều',
 'E11.9', 'ENC20260702-001',
 DATE_SUB(TIMESTAMP(@last_month, '08:30:00'), INTERVAL 35 DAY),
 DATE_SUB(TIMESTAMP(@last_month, '09:10:00'), INTERVAL 35 DAY),
 'a2000000-0000-0000-0000-000000000002');

-- ================================================================
-- 5. ĐƠN THUỐC (pha_prescriptions + pha_prescription_items)
--    Dải UUID đơn thuốc: rx000000-... ; item: ri000000-...
-- ================================================================

-- RX01: BNT00003/E02 — DISPENSED (đã cấp phát)
INSERT IGNORE INTO `pha_prescriptions`
    (`id`, `tenant_id`, `encounter_id`, `patient_id`, `doctor_id`,
     `prescription_no`, `status`, `diagnosis_icd10`,
     `signed_at`, `dispensed_at`, `created_by`)
VALUES
('rx000000-0000-0000-0000-000000000001', 2,
 'e2000000-0000-0000-0000-000000000002',
 'f2000000-0000-0000-0000-000000000003',
 'a2000000-0000-0000-0000-000000000003',
 'RX20260902-001', 'DISPENSED', 'I10',
 TIMESTAMP(@today, '07:35:00'), TIMESTAMP(@today, '08:30:00'),
 NULL);

INSERT IGNORE INTO `pha_prescription_items`
    (`id`, `tenant_id`, `prescription_id`, `drug_id`,
     `dosage`, `frequency`, `route`, `duration_days`, `quantity`, `instructions`, `created_by`)
VALUES
('ri000000-0000-0000-0000-000000000001', 2,
 'rx000000-0000-0000-0000-000000000001',
 'd2000000-0000-0000-0000-000000000002',
 N'5mg', N'1 lần/ngày', 'ORAL', 30, 30.00,
 N'Uống vào buổi sáng sau ăn', NULL),
('ri000000-0000-0000-0000-000000000002', 2,
 'rx000000-0000-0000-0000-000000000001',
 'd2000000-0000-0000-0000-000000000003',
 N'20mg', N'1 lần/ngày', 'ORAL', 30, 30.00,
 N'Uống buổi tối', NULL);

-- RX02: BNT00004/E08 — SIGNED (chưa cấp phát → demo hàng đợi dược)
INSERT IGNORE INTO `pha_prescriptions`
    (`id`, `tenant_id`, `encounter_id`, `patient_id`, `doctor_id`,
     `prescription_no`, `status`, `diagnosis_icd10`,
     `signed_at`, `dispensed_at`, `created_by`)
VALUES
('rx000000-0000-0000-0000-000000000002', 2,
 'e2000000-0000-0000-0000-000000000008',
 'f2000000-0000-0000-0000-000000000004',
 'a2000000-0000-0000-0000-000000000003',
 'RX20260902-002', 'SIGNED', 'E78.5',
 TIMESTAMP(@today, '07:15:00'), NULL,
 NULL);

INSERT IGNORE INTO `pha_prescription_items`
    (`id`, `tenant_id`, `prescription_id`, `drug_id`,
     `dosage`, `frequency`, `route`, `duration_days`, `quantity`, `instructions`, `created_by`)
VALUES
('ri000000-0000-0000-0000-000000000003', 2,
 'rx000000-0000-0000-0000-000000000002',
 'd2000000-0000-0000-0000-000000000003',
 N'20mg', N'1 lần/ngày buổi tối', 'ORAL', 60, 60.00,
 N'Tránh ăn bưởi khi dùng thuốc', NULL),
('ri000000-0000-0000-0000-000000000004', 2,
 'rx000000-0000-0000-0000-000000000002',
 'd2000000-0000-0000-0000-000000000008',
 N'600mg', N'2 lần/ngày', 'ORAL', 60, 120.00,
 N'Uống trước ăn 30 phút', NULL);

-- RX03: BNT00018/E05 — DISPENSED (ĐTĐ kiểm soát tốt)
INSERT IGNORE INTO `pha_prescriptions`
    (`id`, `tenant_id`, `encounter_id`, `patient_id`, `doctor_id`,
     `prescription_no`, `status`, `diagnosis_icd10`,
     `signed_at`, `dispensed_at`, `created_by`)
VALUES
('rx000000-0000-0000-0000-000000000003', 2,
 'e2000000-0000-0000-0000-000000000005',
 'f2000000-0000-0000-0000-000000000018',
 'a2000000-0000-0000-0000-000000000003',
 'RX20260902-003', 'DISPENSED', 'E11.9',
 TIMESTAMP(@today, '07:25:00'), TIMESTAMP(@today, '08:10:00'),
 NULL);

INSERT IGNORE INTO `pha_prescription_items`
    (`id`, `tenant_id`, `prescription_id`, `drug_id`,
     `dosage`, `frequency`, `route`, `duration_days`, `quantity`, `instructions`, `created_by`)
VALUES
('ri000000-0000-0000-0000-000000000005', 2,
 'rx000000-0000-0000-0000-000000000003',
 'd2000000-0000-0000-0000-000000000001',
 N'500mg', N'2 lần/ngày', 'ORAL', 90, 180.00,
 N'Uống trong bữa ăn hoặc ngay sau ăn', NULL);

-- RX04: BNT00019/E06 — SIGNED (ĐTĐ kém, insulin chưa cấp phát)
INSERT IGNORE INTO `pha_prescriptions`
    (`id`, `tenant_id`, `encounter_id`, `patient_id`, `doctor_id`,
     `prescription_no`, `status`, `diagnosis_icd10`,
     `signed_at`, `dispensed_at`, `created_by`)
VALUES
('rx000000-0000-0000-0000-000000000004', 2,
 'e2000000-0000-0000-0000-000000000006',
 'f2000000-0000-0000-0000-000000000019',
 'a2000000-0000-0000-0000-000000000004',
 'RX20260902-004', 'SIGNED', 'E11.9',
 TIMESTAMP(@today, '07:55:00'), NULL,
 NULL);

INSERT IGNORE INTO `pha_prescription_items`
    (`id`, `tenant_id`, `prescription_id`, `drug_id`,
     `dosage`, `frequency`, `route`, `duration_days`, `quantity`, `instructions`, `created_by`)
VALUES
('ri000000-0000-0000-0000-000000000006', 2,
 'rx000000-0000-0000-0000-000000000004',
 'd2000000-0000-0000-0000-000000000007',
 N'20UI', N'1 lần/ngày buổi tối', 'SC', 30, 1.00,
 N'Tiêm dưới da vùng bụng, xoay vị trí tiêm', NULL),
('ri000000-0000-0000-0000-000000000007', 2,
 'rx000000-0000-0000-0000-000000000004',
 'd2000000-0000-0000-0000-000000000001',
 N'1000mg', N'2 lần/ngày', 'ORAL', 30, 60.00,
 N'Uống trong bữa ăn', NULL);

-- RX05: BNT00024 — lịch sử tháng trước, DISPENSED
INSERT IGNORE INTO `pha_prescriptions`
    (`id`, `tenant_id`, `encounter_id`, `patient_id`, `doctor_id`,
     `prescription_no`, `status`, `diagnosis_icd10`,
     `signed_at`, `dispensed_at`, `created_by`)
VALUES
('rx000000-0000-0000-0000-000000000005', 2,
 'e2000000-0000-0000-0000-000000000009',
 'f2000000-0000-0000-0000-000000000024',
 'a2000000-0000-0000-0000-000000000003',
 'RX20260728-001', 'DISPENSED', 'E11.9',
 TIMESTAMP(@last_month, '09:40:00'), TIMESTAMP(@last_month, '10:30:00'),
 NULL);

INSERT IGNORE INTO `pha_prescription_items`
    (`id`, `tenant_id`, `prescription_id`, `drug_id`,
     `dosage`, `frequency`, `route`, `duration_days`, `quantity`, `instructions`, `created_by`)
VALUES
('ri000000-0000-0000-0000-000000000008', 2,
 'rx000000-0000-0000-0000-000000000005',
 'd2000000-0000-0000-0000-000000000001',
 N'500mg', N'2 lần/ngày', 'ORAL', 30, 60.00,
 N'Uống trong bữa ăn', NULL);

-- ================================================================
-- 6. HOÁ ĐƠN (diab_his_bil_billing + items + payments)
--    Dải UUID: bx000000-... , bi000000-... , bp000000-...
-- ================================================================

-- BILL01: BNT00003/E02 — PAID (đã thanh toán đủ, SERVICE)
INSERT IGNORE INTO `diab_his_bil_billing`
    (`id`, `tenant_id`, `patient_id`, `encounter_id`, `bill_no`,
     `payer`, `subtotal`, `vat_total`, `discount_amount`,
     `bhyt_amount`, `patient_payable`, `paid_amount`, `balance`,
     `status`, `finalized_at`, `created_by`)
VALUES
('bx000000-0000-0000-0000-000000000001', 2,
 'f2000000-0000-0000-0000-000000000003',
 'e2000000-0000-0000-0000-000000000002',
 'BILL20260902-001', 'SELF',
 195000.00, 0.00, 0.00, 0.00, 195000.00, 195000.00, 0.00,
 'PAID', TIMESTAMP(@today, '08:45:00'),
 'a2000000-0000-0000-0000-000000000006');

INSERT IGNORE INTO `diab_his_bil_billing_items`
    (`id`, `billing_id`, `tenant_id`, `item_type`, `code`, `name`,
     `quantity`, `unit_price`, `vat_rate`, `discount_percent`,
     `line_total`, `bhyt_applicable`, `bhyt_amount`)
VALUES
('bi000000-0000-0000-0000-000000000001',
 'bx000000-0000-0000-0000-000000000001', 2,
 'SERVICE', 'DV001', N'Khám nội tổng quát',
 1.000, 150000.00, 0, 0.00, 150000.00, 0, 0.00),
('bi000000-0000-0000-0000-000000000002',
 'bx000000-0000-0000-0000-000000000001', 2,
 'DRUG', 'TH002', N'Amlodipine 5mg x 30 viên',
 30.000, 1500.00, 0, 0.00, 45000.00, 0, 0.00);

INSERT IGNORE INTO `diab_his_bil_payments`
    (`id`, `tenant_id`, `billing_id`, `amount`, `method`,
     `status`, `paid_at`, `paid_by`)
VALUES
('bp000000-0000-0000-0000-000000000001', 2,
 'bx000000-0000-0000-0000-000000000001',
 195000.00, 'CASH', 'PENDING',
 TIMESTAMP(@today, '08:45:00'),
 'a2000000-0000-0000-0000-000000000006');

-- BILL02: BNT00004/E08 — PARTIAL_PAID (đặt cọc, còn nợ 90k)
INSERT IGNORE INTO `diab_his_bil_billing`
    (`id`, `tenant_id`, `patient_id`, `encounter_id`, `bill_no`,
     `payer`, `subtotal`, `vat_total`, `discount_amount`,
     `bhyt_amount`, `patient_payable`, `paid_amount`, `balance`,
     `status`, `finalized_at`, `created_by`)
VALUES
('bx000000-0000-0000-0000-000000000002', 2,
 'f2000000-0000-0000-0000-000000000004',
 'e2000000-0000-0000-0000-000000000008',
 'BILL20260902-002', 'SELF',
 240000.00, 0.00, 0.00, 0.00, 240000.00, 150000.00, 90000.00,
 'PARTIAL_PAID', TIMESTAMP(@today, '07:30:00'),
 'a2000000-0000-0000-0000-000000000006');

INSERT IGNORE INTO `diab_his_bil_billing_items`
    (`id`, `billing_id`, `tenant_id`, `item_type`, `code`, `name`,
     `quantity`, `unit_price`, `vat_rate`, `discount_percent`,
     `line_total`, `bhyt_applicable`, `bhyt_amount`)
VALUES
('bi000000-0000-0000-0000-000000000003',
 'bx000000-0000-0000-0000-000000000002', 2,
 'SERVICE', 'DV001', N'Khám nội tổng quát',
 1.000, 150000.00, 0, 0.00, 150000.00, 0, 0.00),
('bi000000-0000-0000-0000-000000000004',
 'bx000000-0000-0000-0000-000000000002', 2,
 'DRUG', 'TH003', N'Atorvastatin 20mg x 60 viên',
 60.000, 1500.00, 0, 0.00, 90000.00, 0, 0.00);

INSERT IGNORE INTO `diab_his_bil_payments`
    (`id`, `tenant_id`, `billing_id`, `amount`, `method`,
     `status`, `paid_at`, `paid_by`)
VALUES
('bp000000-0000-0000-0000-000000000002', 2,
 'bx000000-0000-0000-0000-000000000002',
 150000.00, 'CASH', 'PENDING',
 TIMESTAMP(@today, '07:30:00'),
 'a2000000-0000-0000-0000-000000000006');

-- BILL03: BNT00005/E03 — FINALIZED, BHYT, chưa thanh toán (balance = patient_payable)
INSERT IGNORE INTO `diab_his_bil_billing`
    (`id`, `tenant_id`, `patient_id`, `encounter_id`, `bill_no`,
     `payer`, `subtotal`, `vat_total`, `discount_amount`,
     `bhyt_amount`, `patient_payable`, `paid_amount`, `balance`,
     `status`, `finalized_at`, `created_by`)
VALUES
('bx000000-0000-0000-0000-000000000003', 2,
 'f2000000-0000-0000-0000-000000000005',
 'e2000000-0000-0000-0000-000000000003',
 'BILL20260902-003', 'BHYT',
 200000.00, 0.00, 0.00, 160000.00, 40000.00, 0.00, 40000.00,
 'FINALIZED', TIMESTAMP(@today, '08:00:00'),
 'a2000000-0000-0000-0000-000000000006');

INSERT IGNORE INTO `diab_his_bil_billing_items`
    (`id`, `billing_id`, `tenant_id`, `item_type`, `code`, `name`,
     `quantity`, `unit_price`, `vat_rate`, `discount_percent`,
     `line_total`, `bhyt_applicable`, `bhyt_amount`)
VALUES
('bi000000-0000-0000-0000-000000000005',
 'bx000000-0000-0000-0000-000000000003', 2,
 'SERVICE', 'DV002', N'Khám nội tiết BHYT',
 1.000, 200000.00, 0, 0.00, 200000.00, 1, 160000.00);

-- BILL04: BNT00018/E05 — PAID (ĐTĐ kiểm soát tốt)
INSERT IGNORE INTO `diab_his_bil_billing`
    (`id`, `tenant_id`, `patient_id`, `encounter_id`, `bill_no`,
     `payer`, `subtotal`, `vat_total`, `discount_amount`,
     `bhyt_amount`, `patient_payable`, `paid_amount`, `balance`,
     `status`, `finalized_at`, `created_by`)
VALUES
('bx000000-0000-0000-0000-000000000004', 2,
 'f2000000-0000-0000-0000-000000000018',
 'e2000000-0000-0000-0000-000000000005',
 'BILL20260902-004', 'SELF',
 240000.00, 0.00, 0.00, 0.00, 240000.00, 240000.00, 0.00,
 'PAID', TIMESTAMP(@today, '08:15:00'),
 'a2000000-0000-0000-0000-000000000006');

INSERT IGNORE INTO `diab_his_bil_billing_items`
    (`id`, `billing_id`, `tenant_id`, `item_type`, `code`, `name`,
     `quantity`, `unit_price`, `vat_rate`, `discount_percent`,
     `line_total`, `bhyt_applicable`, `bhyt_amount`)
VALUES
('bi000000-0000-0000-0000-000000000006',
 'bx000000-0000-0000-0000-000000000004', 2,
 'SERVICE', 'DV001', N'Khám nội tổng quát',
 1.000, 150000.00, 0, 0.00, 150000.00, 0, 0.00),
('bi000000-0000-0000-0000-000000000007',
 'bx000000-0000-0000-0000-000000000004', 2,
 'DRUG', 'TH001', N'Metformin 500mg x 180 viên',
 180.000, 500.00, 0, 0.00, 90000.00, 0, 0.00);

INSERT IGNORE INTO `diab_his_bil_payments`
    (`id`, `tenant_id`, `billing_id`, `amount`, `method`,
     `status`, `paid_at`, `paid_by`)
VALUES
('bp000000-0000-0000-0000-000000000003', 2,
 'bx000000-0000-0000-0000-000000000004',
 240000.00, 'BANK_TRANSFER', 'PENDING',
 TIMESTAMP(@today, '08:15:00'),
 'a2000000-0000-0000-0000-000000000006');

-- BILL05: BNT00024 — lịch sử, PAID
INSERT IGNORE INTO `diab_his_bil_billing`
    (`id`, `tenant_id`, `patient_id`, `encounter_id`, `bill_no`,
     `payer`, `subtotal`, `vat_total`, `discount_amount`,
     `bhyt_amount`, `patient_payable`, `paid_amount`, `balance`,
     `status`, `finalized_at`, `created_by`)
VALUES
('bx000000-0000-0000-0000-000000000005', 2,
 'f2000000-0000-0000-0000-000000000024',
 'e2000000-0000-0000-0000-000000000009',
 'BILL20260728-001', 'BHYT',
 180000.00, 0.00, 0.00, 144000.00, 36000.00, 36000.00, 0.00,
 'PAID', TIMESTAMP(@last_month, '10:45:00'),
 'a2000000-0000-0000-0000-000000000006');

INSERT IGNORE INTO `diab_his_bil_billing_items`
    (`id`, `billing_id`, `tenant_id`, `item_type`, `code`, `name`,
     `quantity`, `unit_price`, `vat_rate`, `discount_percent`,
     `line_total`, `bhyt_applicable`, `bhyt_amount`)
VALUES
('bi000000-0000-0000-0000-000000000008',
 'bx000000-0000-0000-0000-000000000005', 2,
 'SERVICE', 'DV002', N'Khám nội tiết BHYT',
 1.000, 180000.00, 0, 0.00, 180000.00, 1, 144000.00);

-- ================================================================
-- 7. CLS — LAB ORDERS (diab_his_cli_lab_orders)
--    status values: ordered | sample_taken | processing | done | cancelled
--    Dải UUID: lo000000-...
-- ================================================================
INSERT IGNORE INTO `diab_his_cli_lab_orders`
    (`id`, `tenant_id`, `encounter_id`, `test_code`, `test_name`,
     `sample_type`, `priority`, `status`, `ordered_at`, `ordered_by`, `created_by`)
VALUES
-- LO01: BNT00005/E03 — HbA1c, ordered (chưa có kết quả)
('lo000000-0000-0000-0000-000000000001', 2,
 'e2000000-0000-0000-0000-000000000003',
 'HBA1C', N'HbA1c (Glycated Hemoglobin)',
 N'Máu tĩnh mạch', 'NORMAL', 'ordered',
 TIMESTAMP(@today, '07:50:00'),
 'a2000000-0000-0000-0000-000000000004',
 'a2000000-0000-0000-0000-000000000004'),

-- LO02: BNT00008/E07 — Troponin I, done (kết quả BẤT THƯỜNG, chưa duyệt)
('lo000000-0000-0000-0000-000000000002', 2,
 'e2000000-0000-0000-0000-000000000007',
 'TropI', N'Troponin I tim mạch',
 N'Máu tĩnh mạch', 'STAT', 'done',
 TIMESTAMP(@today, '06:35:00'),
 'a2000000-0000-0000-0000-000000000003',
 'a2000000-0000-0000-0000-000000000003'),

-- LO03: BNT00008/E07 — CK-MB, done
('lo000000-0000-0000-0000-000000000003', 2,
 'e2000000-0000-0000-0000-000000000007',
 'CKMB', N'CK-MB (Creatine Kinase-MB)',
 N'Máu tĩnh mạch', 'STAT', 'done',
 TIMESTAMP(@today, '06:35:00'),
 'a2000000-0000-0000-0000-000000000003',
 'a2000000-0000-0000-0000-000000000003'),

-- LO04: BNT00019/E06 — HbA1c, done (10.1%)
('lo000000-0000-0000-0000-000000000004', 2,
 'e2000000-0000-0000-0000-000000000006',
 'HBA1C', N'HbA1c (Glycated Hemoglobin)',
 N'Máu tĩnh mạch', 'NORMAL', 'done',
 TIMESTAMP(@today, '07:20:00'),
 'a2000000-0000-0000-0000-000000000004',
 'a2000000-0000-0000-0000-000000000004'),

-- LO05: BNT00018/E05 — HbA1c, done (6.2%)
('lo000000-0000-0000-0000-000000000005', 2,
 'e2000000-0000-0000-0000-000000000005',
 'HBA1C', N'HbA1c (Glycated Hemoglobin)',
 N'Máu tĩnh mạch', 'NORMAL', 'done',
 TIMESTAMP(@today, '06:55:00'),
 'a2000000-0000-0000-0000-000000000003',
 'a2000000-0000-0000-0000-000000000003'),

-- LO06: BNT00001/E01 — FBS + HbA1c ordered hôm nay
('lo000000-0000-0000-0000-000000000006', 2,
 'e2000000-0000-0000-0000-000000000001',
 'FBS', N'Đường huyết lúc đói (FBS)',
 N'Máu mao mạch', 'NORMAL', 'ordered',
 TIMESTAMP(@today, '08:05:00'),
 'a2000000-0000-0000-0000-000000000003',
 'a2000000-0000-0000-0000-000000000003');

-- ================================================================
-- 8. CHỈ SỐ HbA1c (diab_his_cli_indicator_reading)
--    indicator_type = 'HBA1C'; value = %; unit = '%'
--    Dải UUID: ir000000-...
-- ================================================================
INSERT IGNORE INTO `diab_his_cli_indicator_reading`
    (`id`, `tenant_id`, `patient_id`, `encounter_id`,
     `indicator_type`, `value`, `unit`, `source`, `source_ref_id`,
     `recorded_at`, `recorded_by`)
VALUES
-- BNT00018: HbA1c 6.2% — kiểm soát TỐT (< 7%)
('ir000000-0000-0000-0000-000000000001', 2,
 'f2000000-0000-0000-0000-000000000018',
 'e2000000-0000-0000-0000-000000000005',
 'HBA1C', 6.2000, '%', 'lab_order',
 'lo000000-0000-0000-0000-000000000005',
 TIMESTAMP(@today, '07:00:00'),
 'a2000000-0000-0000-0000-000000000007'),

-- BNT00019: HbA1c 10.1% — kiểm soát KÉM (> 9%)
('ir000000-0000-0000-0000-000000000002', 2,
 'f2000000-0000-0000-0000-000000000019',
 'e2000000-0000-0000-0000-000000000006',
 'HBA1C', 10.1000, '%', 'lab_order',
 'lo000000-0000-0000-0000-000000000004',
 TIMESTAMP(@today, '07:30:00'),
 'a2000000-0000-0000-0000-000000000007'),

-- BNT00020: HbA1c 7.8% — kiểm soát KHÁ (7-9%) - lịch sử tháng trước
('ir000000-0000-0000-0000-000000000003', 2,
 'f2000000-0000-0000-0000-000000000020',
 'e2000000-0000-0000-0000-000000000010',
 'HBA1C', 7.8000, '%', 'manual', NULL,
 TIMESTAMP(@last_month, '10:30:00'),
 'a2000000-0000-0000-0000-000000000007'),

-- BNT00024: HbA1c 8.9% — khá/kém (vừa chẩn đoán lần đầu)
('ir000000-0000-0000-0000-000000000004', 2,
 'f2000000-0000-0000-0000-000000000024',
 'e2000000-0000-0000-0000-000000000009',
 'HBA1C', 8.9000, '%', 'manual', NULL,
 TIMESTAMP(@last_month, '09:30:00'),
 'a2000000-0000-0000-0000-000000000007'),

-- BNT00003: HbA1c 7.1% — kiểm soát khá (lịch sử hôm nay)
('ir000000-0000-0000-0000-000000000005', 2,
 'f2000000-0000-0000-0000-000000000003',
 'e2000000-0000-0000-0000-000000000002',
 'HBA1C', 7.1000, '%', 'manual', NULL,
 TIMESTAMP(@today, '07:05:00'),
 'a2000000-0000-0000-0000-000000000007');

-- ================================================================
-- 9. GÓI DỊCH VỤ (diab_his_pkg_service_packages + subscriptions)
--    Dải UUID pkg: pk000000-... ; sub: ps000000-...
-- ================================================================

-- Seed 1 gói "Quản lý ĐTĐ toàn diện 1 năm" cho tenant 2
INSERT IGNORE INTO `diab_his_pkg_service_packages`
    (`id`, `tenant_id`, `code`, `name`, `description`,
     `duration_days`, `list_price`, `vat_rate`, `is_active`,
     `valid_from`, `created_by`)
VALUES
('pk000000-0000-0000-0000-000000000001', 2,
 'PKG-DTD-1Y', N'Quản lý ĐTĐ toàn diện 1 năm',
 N'Gói theo dõi đái tháo đường 1 năm: 4 lần khám + 4 lần HbA1c + tư vấn dinh dưỡng',
 365, 3600000.00, 0, 1,
 DATE_SUB(@today, INTERVAL 180 DAY),
 'a2000000-0000-0000-0000-000000000001');

-- SUB01: BNT00021 — gói SẮP HẾT HẠN (còn 10 ngày)
INSERT IGNORE INTO `diab_his_pkg_subscriptions`
    (`id`, `tenant_id`, `patient_id`, `package_id`,
     `subscription_no`, `package_code_snapshot`, `package_name_snapshot`,
     `purchase_date`, `effective_date`, `expiry_date`,
     `duration_days_snapshot`, `total_price`, `amount_paid`,
     `payment_status`, `status`, `activated_at`, `created_by`)
VALUES
('ps000000-0000-0000-0000-000000000001', 2,
 'f2000000-0000-0000-0000-000000000021',
 'pk000000-0000-0000-0000-000000000001',
 'SUB-20250902-001', 'PKG-DTD-1Y', N'Quản lý ĐTĐ toàn diện 1 năm',
 DATE_SUB(@today, INTERVAL 355 DAY),
 DATE_SUB(@today, INTERVAL 355 DAY),
 DATE_ADD(@today, INTERVAL 10 DAY),
 365, 3600000.00, 3600000.00,
 'paid_full', 'active',
 DATE_SUB(TIMESTAMP(@today, '09:00:00'), INTERVAL 355 DAY),
 'a2000000-0000-0000-0000-000000000006');

-- SUB02: BNT00022 — gói còn hạn dài (còn 200 ngày)
INSERT IGNORE INTO `diab_his_pkg_subscriptions`
    (`id`, `tenant_id`, `patient_id`, `package_id`,
     `subscription_no`, `package_code_snapshot`, `package_name_snapshot`,
     `purchase_date`, `effective_date`, `expiry_date`,
     `duration_days_snapshot`, `total_price`, `amount_paid`,
     `payment_status`, `status`, `activated_at`, `created_by`)
VALUES
('ps000000-0000-0000-0000-000000000002', 2,
 'f2000000-0000-0000-0000-000000000022',
 'pk000000-0000-0000-0000-000000000001',
 'SUB-20260302-002', 'PKG-DTD-1Y', N'Quản lý ĐTĐ toàn diện 1 năm',
 DATE_SUB(@today, INTERVAL 165 DAY),
 DATE_SUB(@today, INTERVAL 165 DAY),
 DATE_ADD(@today, INTERVAL 200 DAY),
 365, 3600000.00, 1800000.00,
 'deposit_paid', 'active',
 DATE_SUB(TIMESTAMP(@today, '09:00:00'), INTERVAL 165 DAY),
 'a2000000-0000-0000-0000-000000000006');

-- ================================================================
-- 10. RECALL — TÁI KHÁM / NHẮC ĐO HbA1c
--     recall_type: OVERDUE_VISIT | OVERDUE_HBA1C | RISK_ESCALATION
--     status:      PENDING | CONTACTED | SCHEDULED | DONE | DISMISSED
--     Dải UUID: rc000000-...
-- ================================================================
INSERT IGNORE INTO `diab_his_cli_followup_recall`
    (`id`, `tenant_id`, `patient_id`,
     `recall_type`, `due_date`, `reason_json`, `priority`, `status`,
     `channel`, `note`, `created_at`)
VALUES
-- RC01: BNT00019 — HbA1c 10.1%, nhắc đo lại sau 3 tháng (OVERDUE_HBA1C)
('rc000000-0000-0000-0000-000000000001', 2,
 'f2000000-0000-0000-0000-000000000019',
 'OVERDUE_HBA1C',
 DATE_ADD(@today, INTERVAL 90 DAY),
 '{"trigger":"HbA1c=10.1%","threshold":9.0,"encounter_id":"e2000000-0000-0000-0000-000000000006"}',
 'HIGH', 'PENDING', 'PHONE',
 N'HbA1c 10.1% vượt ngưỡng 9% — cần đo lại sau 3 tháng điều chỉnh insulin',
 NOW()),

-- RC02: BNT00023 — chưa tái khám quá 90 ngày (OVERDUE_VISIT)
('rc000000-0000-0000-0000-000000000002', 2,
 'f2000000-0000-0000-0000-000000000023',
 'OVERDUE_VISIT',
 DATE_SUB(@today, INTERVAL 10 DAY),
 '{"last_visit":"2026-05-15","days_overdue":110,"recommended_interval_days":90}',
 'NORMAL', 'PENDING', 'SMS',
 N'BN chưa tái khám 110 ngày (khuyến cáo mỗi 90 ngày)',
 NOW()),

-- RC03: BNT00021 — gói dịch vụ sắp hết hạn (OVERDUE_VISIT kết hợp)
('rc000000-0000-0000-0000-000000000003', 2,
 'f2000000-0000-0000-0000-000000000021',
 'OVERDUE_VISIT',
 DATE_ADD(@today, INTERVAL 7 DAY),
 CONCAT('{"trigger":"subscription_expiry","subscription_id":"ps000000-0000-0000-0000-000000000001","expiry_date":"', DATE_FORMAT(DATE_ADD(@today, INTERVAL 10 DAY), '%Y-%m-%d'), '"}'),
 'HIGH', 'PENDING', 'PHONE',
 N'Gói dịch vụ sắp hết hạn trong 10 ngày — tư vấn gia hạn',
 NOW()),

-- RC04: BNT00024 — vừa chẩn đoán ĐTĐ, nhắc đo HbA1c sau 3 tháng
('rc000000-0000-0000-0000-000000000004', 2,
 'f2000000-0000-0000-0000-000000000024',
 'OVERDUE_HBA1C',
 DATE_ADD(@last_month, INTERVAL 90 DAY),
 '{"trigger":"new_diagnosis","first_encounter_id":"e2000000-0000-0000-0000-000000000009"}',
 'NORMAL', 'PENDING', 'ZALO',
 N'Bệnh nhân mới chẩn đoán ĐTĐ — nhắc đo HbA1c sau 3 tháng theo dõi',
 NOW()),

-- RC05: BNT00020 — CONTACTED (đã liên hệ, đang chờ lịch hẹn)
('rc000000-0000-0000-0000-000000000005', 2,
 'f2000000-0000-0000-0000-000000000020',
 'OVERDUE_VISIT',
 DATE_SUB(@today, INTERVAL 5 DAY),
 '{"last_visit":"2026-07-28","days_overdue":35}',
 'NORMAL', 'CONTACTED', 'PHONE',
 N'Đã gọi điện, BN hẹn tái khám tuần sau',
 NOW());

-- ================================================================
-- KIỂM CHỨNG NHANH (chạy thủ công sau khi seed):
--
-- SELECT 'patients', COUNT(*) FROM diab_his_pat_patients     WHERE tenant_id=2
-- UNION ALL
-- SELECT 'enc_today', COUNT(*) FROM diab_his_enc_encounters  WHERE tenant_id=2 AND DATE(created_at)=CURDATE()
-- UNION ALL
-- SELECT 'queue_today', COUNT(*) FROM diab_his_rcp_queue_tickets WHERE tenant_id=2 AND ticket_date=CURDATE()
-- UNION ALL
-- SELECT 'prescriptions', COUNT(*) FROM pha_prescriptions     WHERE tenant_id=2
-- UNION ALL
-- SELECT 'bill_paid', COUNT(*) FROM diab_his_bil_billing     WHERE tenant_id=2 AND status='PAID'
-- UNION ALL
-- SELECT 'bill_partial', COUNT(*) FROM diab_his_bil_billing  WHERE tenant_id=2 AND status='PARTIAL_PAID'
-- UNION ALL
-- SELECT 'bill_finalized', COUNT(*) FROM diab_his_bil_billing WHERE tenant_id=2 AND status='FINALIZED'
-- UNION ALL
-- SELECT 'lab_ordered', COUNT(*) FROM diab_his_cli_lab_orders WHERE tenant_id=2 AND status='ordered'
-- UNION ALL
-- SELECT 'lab_done', COUNT(*) FROM diab_his_cli_lab_orders   WHERE tenant_id=2 AND status='done'
-- UNION ALL
-- SELECT 'hba1c_readings', COUNT(*) FROM diab_his_cli_indicator_reading WHERE tenant_id=2 AND indicator_type='HBA1C'
-- UNION ALL
-- SELECT 'pkg_subs', COUNT(*) FROM diab_his_pkg_subscriptions WHERE tenant_id=2
-- UNION ALL
-- SELECT 'pkg_expiring', COUNT(*) FROM diab_his_pkg_subscriptions WHERE tenant_id=2 AND expiry_date<=DATE_ADD(CURDATE(),INTERVAL 30 DAY)
-- UNION ALL
-- SELECT 'recalls', COUNT(*) FROM diab_his_cli_followup_recall WHERE tenant_id=2;
--
-- CLEANUP (xoá toàn bộ data seed này):
--   DELETE FROM diab_his_cli_followup_recall       WHERE tenant_id=2 AND id LIKE 'rc%';
--   DELETE FROM diab_his_pkg_subscriptions         WHERE tenant_id=2 AND id LIKE 'ps%';
--   DELETE FROM diab_his_pkg_service_packages      WHERE tenant_id=2 AND id LIKE 'pk%';
--   DELETE FROM diab_his_cli_indicator_reading     WHERE tenant_id=2 AND id LIKE 'ir%';
--   DELETE FROM diab_his_cli_lab_orders            WHERE tenant_id=2 AND id LIKE 'lo%';
--   DELETE FROM diab_his_bil_payments              WHERE tenant_id=2 AND id LIKE 'bp%';
--   DELETE FROM diab_his_bil_billing_items         WHERE tenant_id=2 AND id LIKE 'bi%';
--   DELETE FROM diab_his_bil_billing               WHERE tenant_id=2 AND id LIKE 'bx%';
--   DELETE FROM pha_prescription_items             WHERE tenant_id=2 AND id LIKE 'ri%';
--   DELETE FROM pha_prescriptions                  WHERE tenant_id=2 AND id LIKE 'rx%';
--   DELETE FROM diab_his_rcp_queue_tickets         WHERE tenant_id=2 AND id LIKE 'q2%';
--   DELETE FROM diab_his_enc_encounters            WHERE tenant_id=2 AND id LIKE 'e2%';
--   DELETE FROM diab_his_pat_patients              WHERE tenant_id=2 AND code LIKE 'BNT001%' AND code > 'BNT00015';
-- ================================================================
