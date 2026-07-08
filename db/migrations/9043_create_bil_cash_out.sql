-- ============================================================
-- Migration: 9043_create_bil_cash_out
-- Mo ta: Bang phieu chi tien mat (P0-1 Report Catalog gap analysis,
--   docs/prd/reports-catalog-prd.md §7.B P0-1) — chieu CHI cua So quy
--   tien mat. Chieu THU da co san o diab_his_bil_payments (method='CASH').
--   Descriptor bao cao "so-quy-tien-mat" (ReportRegistry) se UNION 2 nguon
--   nay + tinh Ton quy luy ke.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + INSERT ... WHERE NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

-- 1) Bang phieu chi tien mat -----------------------------------------------
CREATE TABLE IF NOT EXISTS diab_his_bil_cash_out (
    id          CHAR(36)       NOT NULL DEFAULT (UUID()),
    tenant_id   INT            NOT NULL,
    code        VARCHAR(30)    NULL COMMENT 'So phieu chi (vd PC-2026-001)',
    amount      DECIMAL(15,2)  NOT NULL,
    category    VARCHAR(50)    NULL COMMENT 'Loai chi (vd Van phong pham, Luong, Dien nuoc...)',
    reason      TEXT           NULL COMMENT 'Dien giai ly do chi',
    paid_to     VARCHAR(255)   NULL COMMENT 'Nguoi/don vi nhan tien',
    paid_at     DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    note        TEXT           NULL,
    created_at  DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by  CHAR(36)       NULL,
    updated_at  DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by  CHAR(36)       NULL,
    deleted_at  DATETIME(3)    NULL,
    deleted_by  CHAR(36)       NULL,
    PRIMARY KEY (id),
    INDEX idx_cashout_tenant_date (tenant_id, paid_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2) SEED DEMO (dev) — 4 phieu chi cho tenant_id=1 de co du lieu render
--    bao cao "so-quy-tien-mat" khi review (bil_payments hien 0 dong).
--    Day la du lieu DEMO, KHONG dung cho production/UAT that.
--    Idempotent qua WHERE NOT EXISTS theo (tenant_id, code).
INSERT INTO diab_his_bil_cash_out (id, tenant_id, code, amount, category, reason, paid_to, paid_at)
SELECT UUID(), 1, 'PC-2026-001', 500000,  'Van phong pham',          'Mua van phong pham', 'Cua hang VPP An Phat', '2026-07-02 09:00:00'
 WHERE NOT EXISTS (SELECT 1 FROM diab_his_bil_cash_out WHERE tenant_id = 1 AND code = 'PC-2026-001');

INSERT INTO diab_his_bil_cash_out (id, tenant_id, code, amount, category, reason, paid_to, paid_at)
SELECT UUID(), 1, 'PC-2026-002', 3000000, 'Luong',                   'Tam ung luong nhan vien', 'Nguyen Van A', '2026-07-05 14:30:00'
 WHERE NOT EXISTS (SELECT 1 FROM diab_his_bil_cash_out WHERE tenant_id = 1 AND code = 'PC-2026-002');

INSERT INTO diab_his_bil_cash_out (id, tenant_id, code, amount, category, reason, paid_to, paid_at)
SELECT UUID(), 1, 'PC-2026-003', 1200000, 'Dien nuoc',               'Tien dien nuoc thang 6', 'Cong ty Dien luc', '2026-07-06 10:15:00'
 WHERE NOT EXISTS (SELECT 1 FROM diab_his_bil_cash_out WHERE tenant_id = 1 AND code = 'PC-2026-003');

INSERT INTO diab_his_bil_cash_out (id, tenant_id, code, amount, category, reason, paid_to, paid_at)
SELECT UUID(), 1, 'PC-2026-004', 800000,  'Sua chua thiet bi',       'Sua may sieu am', 'Ky thuat Minh Duc', '2026-07-07 08:45:00'
 WHERE NOT EXISTS (SELECT 1 FROM diab_his_bil_cash_out WHERE tenant_id = 1 AND code = 'PC-2026-004');
