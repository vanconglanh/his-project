-- ============================================================
-- Migration: 9009_create_legacy_views
-- Engine: MySQL 8.0+
-- Generated: 2026-05-29
-- Mo ta: Tao VIEW alias cho cac bang legacy khong co prefix diab_his_
--        De backward-compat voi code Dapper dung ten bang cu
-- Idempotent: YES (CREATE OR REPLACE VIEW)
-- ============================================================
SET NAMES utf8mb4;

-- Pharmacy
CREATE OR REPLACE VIEW pha_drug_master       AS SELECT * FROM diab_his_pha_drugs;
CREATE OR REPLACE VIEW pha_prescriptions     AS SELECT * FROM diab_his_pha_prescriptions;
CREATE OR REPLACE VIEW pha_prescription_items AS SELECT * FROM diab_his_pha_prescription_items;
CREATE OR REPLACE VIEW pha_stocks            AS SELECT * FROM diab_his_pha_stock;

-- Patient
CREATE OR REPLACE VIEW pat_patients          AS SELECT * FROM diab_his_pat_patients;

-- Billing
CREATE OR REPLACE VIEW bil_billing           AS SELECT * FROM diab_his_bil_billing;

-- Security
CREATE OR REPLACE VIEW sec_users             AS SELECT * FROM diab_his_sec_users;
CREATE OR REPLACE VIEW sec_user_roles        AS SELECT * FROM diab_his_sec_user_roles;
CREATE OR REPLACE VIEW sec_audit_logs        AS SELECT * FROM diab_his_sec_audit_logs;
