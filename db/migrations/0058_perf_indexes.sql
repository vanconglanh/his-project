-- Migration 0058: Performance Indexes
-- Sprint 12 EPIC 10 Hardening
-- MySQL 8 — missing composite indexes for hot query paths

-- cli_visits: list by tenant + status + time (reception dashboard)
CREATE INDEX IF NOT EXISTS `idx_cli_visits_tenant_status_time`
    ON `cli_visits` (`tenant_id`, `status`, `started_at` DESC);

-- bil_billing: cashier queries by tenant + status + created
CREATE INDEX IF NOT EXISTS `idx_bil_billing_tenant_status_time`
    ON `bil_billing` (`tenant_id`, `status`, `created_at` DESC);

-- pha_prescriptions: doctor workload, by tenant + doctor + date
CREATE INDEX IF NOT EXISTS `idx_pha_presc_tenant_doctor_time`
    ON `pha_prescriptions` (`tenant_id`, `doctor_id`, `prescribed_at` DESC);

-- sec_audit_logs: time-series read by admin (already added in 0056 but repeat safe)
CREATE INDEX IF NOT EXISTS `idx_audit_tenant_time`
    ON `sec_audit_logs` (`tenant_id`, `created_at` DESC);
