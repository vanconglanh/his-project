-- ============================================================
-- Migration: 9084_add_branch_id_columns
-- Muc dich: them cot branch_id INT NULL + index (tenant_id, branch_id)
--   cho toan bo bang van hanh (Nhom A). Nullable trong giai doan migrate.
-- LUU Y: pat_patients va cac bang Nhom C KHONG duoc them cot nay.
-- Idempotent: YES (add_branch_col kiem tra bang + cot ton tai, bo qua bang
--   chua ton tai trong moi truong nay)
-- ============================================================
SET NAMES utf8mb4;

-- Security / audit
CALL add_branch_col('diab_his_sec_audit_logs');

-- Scheduling / reception
CALL add_branch_col('diab_his_sch_appointments');
CALL add_branch_col('diab_his_sch_doctor_schedules');
CALL add_branch_col('diab_his_sch_schedule_blocks');
CALL add_branch_col('diab_his_rcp_queue_tickets');

-- Encounter
CALL add_branch_col('diab_his_enc_encounters');

-- CLS
CALL add_branch_col('diab_his_lab_orders');
CALL add_branch_col('diab_his_rad_orders');
CALL add_branch_col('diab_his_lab_results');
CALL add_branch_col('diab_his_rad_results');
CALL add_branch_col('diab_his_cls_uploads');
CALL add_branch_col('diab_his_fil_cls_uploads');

-- Pharmacy
CALL add_branch_col('diab_his_pha_prescriptions');
CALL add_branch_col('diab_his_pha_dispenses');
CALL add_branch_col('diab_his_pha_dispense_records');
CALL add_branch_col('diab_his_pha_stock');
CALL add_branch_col('diab_his_pha_stock_movements');
CALL add_branch_col('diab_his_pha_purchase_orders');
CALL add_branch_col('diab_his_pha_grn');
CALL add_branch_col('diab_his_pha_stocktakes');
CALL add_branch_col('pha_warehouses');

-- Billing
CALL add_branch_col('diab_his_bil_billing');
CALL add_branch_col('diab_his_bil_payments');
CALL add_branch_col('diab_his_bil_einvoices');
CALL add_branch_col('diab_his_bil_cashier_shifts');
CALL add_branch_col('diab_his_bil_counters');
CALL add_branch_col('diab_his_bil_cash_out');

-- Integration
CALL add_branch_col('diab_his_int_bhyt_exports');
CALL add_branch_col('diab_his_int_bhyt_reconcile_uploads');
CALL add_branch_col('diab_his_int_dtqg_credentials');
CALL add_branch_col('diab_his_int_dtqg_submissions');

-- Clinical follow-up
CALL add_branch_col('diab_his_cli_followup_recall');

-- Report cache
CALL add_branch_col('diab_his_rep_daily_revenue_cache');
CALL add_branch_col('diab_his_rep_doctor_kpi_cache');
CALL add_branch_col('diab_his_rep_top_drugs_cache');
CALL add_branch_col('diab_his_rep_inventory_value_cache');
CALL add_branch_col('diab_his_rep_diabetes_cohort_cache');

-- DTQG credentials: 1 credential / branch thay vi 1 / tenant
-- Ten index UNIQUE(tenant_id) khai bao inline o 9011 -> MySQL dat ten la 'tenant_id'
CALL drop_index_if_exists('diab_his_int_dtqg_credentials', 'tenant_id');
CALL add_index_if_missing('diab_his_int_dtqg_credentials',
     'uq_dtqg_cred_tenant_branch', '(`tenant_id`, `branch_id`)');
