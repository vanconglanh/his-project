-- =============================================================================
-- Seed: e2e_billing_finalized.sql
-- Mục đích: Tạo dữ liệu billing FINALIZED/PARTIAL_PAID cho E2E test (R7+)
--
-- Vấn đề gốc: BillingsTab (Cashier) query với from_date=TODAY.
--   Backend filter: WHERE DATE(b.created_at) >= @from
--   Seed cũ có created_at từ 2026-05-16..05-30 → bị lọc ra khi chạy test ngày mới.
--   Script này dùng NOW() để đảm bảo row luôn hiển thị trong ngày chạy test.
--
-- Cách áp dụng:
--   docker exec -i prodiab-mysql mysql -u prodiab -pprodiab_dev_2026 prodiab_his \
--     < db/seeds/e2e_billing_finalized.sql
--
-- Idempotent: dùng INSERT IGNORE với UUID cố định.
--   Lần 2 chạy: không lỗi duplicate, nhưng created_at giữ nguyên lần đầu.
--   Nếu cần reset ngày: DELETE WHERE id IN (...) rồi chạy lại.
--
-- UUID dành riêng cho E2E seed (prefix e2e để tránh conflict):
--   Billing FINALIZED #1 : e2ebill1-0000-0000-0000-000000000001
--   Billing FINALIZED #2 : e2ebill2-0000-0000-0000-000000000001
--   Billing PARTIAL_PAID : e2ebill3-0000-0000-0000-000000000001
--   Encounter IN_PROGRESS: e2eenc01-0000-0000-0000-000000000001
--
-- Tenant: 1 (default seed tenant)
-- Patient: f0000000-0000-0000-0000-000000000005 (có sẵn trong DB)
-- Encounter hiện tại IN_PROGRESS: e0000001-0000-0000-0000-000000000019
-- Admin user: a0000000-0000-0000-0000-000000000001
-- =============================================================================

SET NAMES utf8mb4;

-- -----------------------------------------------------------------------------
-- 1. Billing FINALIZED #1 — 850,000 VND, đã finalize hôm nay
-- -----------------------------------------------------------------------------
INSERT IGNORE INTO diab_his_bil_billing (
    id,
    tenant_id,
    patient_id,
    encounter_id,
    bill_no,
    payer,
    subtotal,
    vat_total,
    discount_amount,
    bhyt_amount,
    patient_payable,
    paid_amount,
    balance,
    status,
    finalized_at,
    created_at,
    created_by,
    updated_at,
    updated_by
) VALUES (
    'e2ebill1-0000-0000-0000-000000000001',
    1,
    'f0000000-0000-0000-0000-000000000005',
    'e0000001-0000-0000-0000-000000000001',
    CONCAT('E2E-', DATE_FORMAT(NOW(), '%Y%m%d'), '-001'),
    'SELF',
    850000.00,
    0.00,
    0.00,
    0.00,
    850000.00,
    850000.00,
    0.00,
    'FINALIZED',
    NOW(),
    NOW(),
    'a0000000-0000-0000-0000-000000000001',
    NOW(),
    'a0000000-0000-0000-0000-000000000001'
);

-- -----------------------------------------------------------------------------
-- 2. Billing FINALIZED #2 — 1,200,000 VND, BHYT một phần
-- -----------------------------------------------------------------------------
INSERT IGNORE INTO diab_his_bil_billing (
    id,
    tenant_id,
    patient_id,
    encounter_id,
    bill_no,
    payer,
    subtotal,
    vat_total,
    discount_amount,
    bhyt_amount,
    patient_payable,
    paid_amount,
    balance,
    status,
    finalized_at,
    created_at,
    created_by,
    updated_at,
    updated_by
) VALUES (
    'e2ebill2-0000-0000-0000-000000000001',
    1,
    'f0000000-0000-0000-0000-000000000006',
    'e0000001-0000-0000-0000-000000000002',
    CONCAT('E2E-', DATE_FORMAT(NOW(), '%Y%m%d'), '-002'),
    'BHYT',
    1200000.00,
    0.00,
    0.00,
    960000.00,
    240000.00,
    240000.00,
    0.00,
    'FINALIZED',
    NOW(),
    NOW(),
    'a0000000-0000-0000-0000-000000000001',
    NOW(),
    'a0000000-0000-0000-0000-000000000001'
);

-- -----------------------------------------------------------------------------
-- 3. Billing PARTIAL_PAID — 500,000 VND, đã thu 200,000, còn thiếu 300,000
-- -----------------------------------------------------------------------------
INSERT IGNORE INTO diab_his_bil_billing (
    id,
    tenant_id,
    patient_id,
    encounter_id,
    bill_no,
    payer,
    subtotal,
    vat_total,
    discount_amount,
    bhyt_amount,
    patient_payable,
    paid_amount,
    balance,
    status,
    finalized_at,
    created_at,
    created_by,
    updated_at,
    updated_by
) VALUES (
    'e2ebill3-0000-0000-0000-000000000001',
    1,
    'f0000000-0000-0000-0000-000000000007',
    'e0000001-0000-0000-0000-000000000012',
    CONCAT('E2E-', DATE_FORMAT(NOW(), '%Y%m%d'), '-003'),
    'SELF',
    500000.00,
    0.00,
    0.00,
    0.00,
    500000.00,
    200000.00,
    300000.00,
    'PARTIAL_PAID',
    NULL,
    NOW(),
    'a0000000-0000-0000-0000-000000000001',
    NOW(),
    'a0000000-0000-0000-0000-000000000001'
);

-- -----------------------------------------------------------------------------
-- 4. Encounter IN_PROGRESS (dự phòng nếu e0000001-...-000000000019 bị xoá)
--    Dùng patient f0000000-...-000000000009 (cùng patient với encounter gốc)
-- -----------------------------------------------------------------------------
INSERT IGNORE INTO diab_his_enc_encounters (
    id,
    tenant_id,
    patient_id,
    doctor_id,
    encounter_type,
    status,
    reason_for_visit,
    encounter_no,
    started_at,
    created_at,
    created_by,
    updated_at,
    updated_by
) VALUES (
    'e2eenc01-0000-0000-0000-000000000001',
    1,
    'f0000000-0000-0000-0000-000000000009',
    'a0000000-0000-0000-0000-000000000001',
    'FOLLOW_UP',
    'IN_PROGRESS',
    'Tái khám đái tháo đường (E2E seed)',
    CONCAT('E2E-ENC-', DATE_FORMAT(NOW(), '%Y%m%d')),
    NOW(),
    NOW(),
    'a0000000-0000-0000-0000-000000000001',
    NOW(),
    'a0000000-0000-0000-0000-000000000001'
);

-- -----------------------------------------------------------------------------
-- Verify
-- -----------------------------------------------------------------------------
SELECT 'billing_e2e_check' AS label,
       COUNT(*) AS total_rows,
       SUM(status = 'FINALIZED') AS finalized,
       SUM(status = 'PARTIAL_PAID') AS partial_paid
FROM diab_his_bil_billing
WHERE id IN (
    'e2ebill1-0000-0000-0000-000000000001',
    'e2ebill2-0000-0000-0000-000000000001',
    'e2ebill3-0000-0000-0000-000000000001'
);

SELECT 'encounter_e2e_check' AS label,
       COUNT(*) AS total_in_progress
FROM diab_his_enc_encounters
WHERE status = 'IN_PROGRESS'
  AND tenant_id = 1
  AND deleted_at IS NULL;
