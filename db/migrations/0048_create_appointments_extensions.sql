-- Sprint 10 / EPIC 8: Extend diab_his_sch_appointments for Public API partner tracking
-- MySQL 8

ALTER TABLE diab_his_sch_appointments
    ADD COLUMN source_partner_id BINARY(16) NULL COMMENT 'FK to diab_his_api_partners.id (BINARY UUID)',
    ADD COLUMN partner_reference VARCHAR(100) NULL COMMENT 'Reference ID from partner system';

CREATE INDEX idx_appt_partner ON diab_his_sch_appointments (source_partner_id);
CREATE INDEX idx_appt_tenant_at ON diab_his_sch_appointments (tenant_id, appointment_at);
