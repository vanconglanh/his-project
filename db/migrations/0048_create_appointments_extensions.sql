-- Sprint 10 / EPIC 8: Extend diab_his_sch_appointments for Public API partner tracking
-- MySQL 8

-- FIX: cot/index co the da ton tai (base/migration khac) -> dung helper idempotent
CALL add_col_if_missing('diab_his_sch_appointments', 'source_partner_id', "BINARY(16) NULL COMMENT 'FK to diab_his_api_partners.id (BINARY UUID)'");
CALL add_col_if_missing('diab_his_sch_appointments', 'partner_reference', "VARCHAR(100) NULL COMMENT 'Reference ID from partner system'");

CALL add_index_if_missing('diab_his_sch_appointments', 'idx_appt_partner',   '(source_partner_id)');
CALL add_index_if_missing('diab_his_sch_appointments', 'idx_appt_tenant_at', '(tenant_id, appointment_at)');
