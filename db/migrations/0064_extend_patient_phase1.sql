-- ============================================================
-- Migration: 0064_extend_patient_phase1
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Story refs: PHASE1-SUNS-COMPLIANCE
-- Idempotent: YES
-- Mô tả: Thêm 6 cột Phase 1 vào pat_patients cho compliance BHYT + nghiệp vụ VN
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('pat_patients', 'id_card_issued_date', 'DATE NULL');
CALL add_col_if_missing('pat_patients', 'id_card_issued_place', 'VARCHAR(255) NULL');
CALL add_col_if_missing('pat_patients', 'nationality', "VARCHAR(50) NOT NULL DEFAULT 'VN'");
CALL add_col_if_missing('pat_patients', 'patient_type', "VARCHAR(20) NOT NULL DEFAULT 'SERVICE'");
CALL add_col_if_missing('pat_patients', 'marital_status', 'VARCHAR(20) NULL');
CALL add_col_if_missing('pat_patients', 'visit_type', "VARCHAR(20) NULL DEFAULT 'FIRST_VISIT'");
