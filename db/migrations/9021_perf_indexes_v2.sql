-- ============================================================
-- Migration: 9021_perf_indexes_v2
-- Engine: MySQL 8.0+, InnoDB
-- Generated: 2026-05-31
-- Mo ta: Them composite indexes cho billing/services/suppliers/cashier_shifts
--        Phuc vu cac query hot: BHYT export, danh sach dich vu, NCC, ca thu ngan
-- Idempotent: YES (dung stored proc add_index_if_missing tu 0000_helpers.sql)
-- Luu y: Chay 0000_helpers.sql truoc de co stored proc
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------------
-- diab_his_bil_billing
-- Query BHYT export: WHERE tenant_id AND payer IN ('BHYT','MIXED') AND finalized_at BETWEEN ? AND ?
-- Cot xac nhan ton tai: tenant_id, payer, created_at (0041_billing_extensions.sql)
-- finalized_at co the NULL nen dung created_at thay the neu finalized_at chua co index
-- ------------------------------------------------------------
CALL add_index_if_missing(
    'diab_his_bil_billing',
    'idx_billing_period_payer',
    '(tenant_id, payer, created_at)'
);

-- ------------------------------------------------------------
-- diab_his_pha_suppliers
-- Query list filter: WHERE tenant_id AND is_active = ? (khong co cot status, dung is_active)
-- Cot xac nhan ton tai: tenant_id, is_active
-- ------------------------------------------------------------
CALL add_index_if_missing(
    'diab_his_pha_suppliers',
    'idx_suppliers_tenant_active',
    '(tenant_id, is_active)'
);

-- ------------------------------------------------------------
-- diab_his_bil_services
-- Query list: WHERE tenant_id AND is_active = ? [AND category = ?]
-- Cot xac nhan ton tai: tenant_id, is_active (0040_service_catalog.sql / 9006b)
-- ------------------------------------------------------------
CALL add_index_if_missing(
    'diab_his_bil_services',
    'idx_services_tenant_active',
    '(tenant_id, is_active)'
);

-- ------------------------------------------------------------
-- diab_his_bil_cashier_shifts
-- Query GetCurrentShift: WHERE tenant_id AND cashier_user_id AND status = 'OPEN'
-- Cot xac nhan ton tai: tenant_id, cashier_user_id, status (0043_cashier_shifts.sql)
-- ------------------------------------------------------------
CALL add_index_if_missing(
    'diab_his_bil_cashier_shifts',
    'idx_cashier_shift_user_open',
    '(tenant_id, cashier_user_id, status)'
);

SET FOREIGN_KEY_CHECKS = 1;
