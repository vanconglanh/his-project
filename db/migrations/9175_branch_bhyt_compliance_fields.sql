-- ============================================================================
-- 9175_branch_bhyt_compliance_fields.sql
-- Dot 5: BHYT/DTQG tuan thu theo chi nhanh (BR-100..108, US-7.1)
-- Can cu: docs/prd/dinh-nghia-nghiep-vu-da-chi-nhanh-20260829.md muc 7.
-- Idempotent: YES (add_col_if_missing). Can 0000_helpers.sql + 9150 da chay truoc.
-- ============================================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_sys_branches', 'hospital_rank', 'VARCHAR(50) NULL COMMENT "Hang benh vien/phong kham - BR-102"');
CALL add_col_if_missing('diab_his_sys_branches', 'kcb_tuyen', 'VARCHAR(50) NULL COMMENT "Tuyen KCB (TW/TINH/HUYEN/XA) - BR-102"');
CALL add_col_if_missing('diab_his_sys_branches', 'bhyt_contract_code', 'VARCHAR(100) NULL COMMENT "Ma hop dong BHYT - BR-102"');
CALL add_col_if_missing('diab_his_sys_branches', 'bhyt_contract_valid_from', 'DATE NULL');
CALL add_col_if_missing('diab_his_sys_branches', 'bhyt_contract_valid_to', 'DATE NULL');
CALL add_col_if_missing('diab_his_sys_branches', 'bhyt_enabled', 'TINYINT(1) NOT NULL DEFAULT 0 COMMENT "Chi nhanh co ap dung BHYT - BR-107"');
CALL add_col_if_missing('diab_his_sys_branches', 'dtqg_enabled', 'TINYINT(1) NOT NULL DEFAULT 0 COMMENT "Chi nhanh co ap dung Don thuoc Quoc gia - BR-107"');

CALL add_index_if_missing('diab_his_sys_branches', 'idx_branches_bhyt_enabled', '(`tenant_id`, `bhyt_enabled`)');

-- Chi nhanh tao moi mac dinh DRAFT (BR-110). Chi nhanh cu dang ACTIVE/SUSPENDED giu nguyen.
-- (khong can UPDATE gi them o day, gia tri default cua status da xu ly o 9150)
