-- =============================================================================
-- 9197_fix_missing_branch_read.sql
-- Fix bug that: 4/5 role non-admin (le_tan, bac_si, duoc_si, ky_thuat_vien) bi
-- mat quyen "branch.read" sau khi migration 9139_reconcile_role_permissions.sql
-- XOA TOAN BO role_permissions cu (co branch.read tu 9086) roi seed lai danh
-- sach "curated" nhung QUEN dua branch.read vao (chi ke_toan co branch.read +
-- branch.cross_view trong danh sach curated cua 9139).
--
-- HAU QUA THAT: BranchSwitcher.tsx goi GET /api/v1/branches (yeu cau quyen
-- branch.read) de lay danh sach chi nhanh + tu dong chon chi nhanh mac dinh.
-- Voi 4 role tren, API tra 403 -> danh sach branches rong -> activeBranchId
-- KHONG BAO GIO duoc set -> moi thao tac yeu cau branch_id (vd tiep don benh
-- nhan) deu bi chan boi loi "Vui long chon chi nhanh truoc khi tiep don" MA
-- KHONG CO CACH NAO chon duoc chi nhanh tren UI (chi hien label tinh).
-- Phat hien khi QC chup anh tai lieu huong dan luong tiep don tren
-- his.diab.vn (2026-09-02).
--
-- FIX: grant "branch.read" cho 4 role con thieu. KHONG dung DELETE+INSERT lai
-- toan bo (rui ro cao, de sot quyen khac) - chi INSERT IGNORE dung 1 quyen
-- con thieu, giu nguyen moi quyen khac da co.
--
-- Idempotent: YES (INSERT IGNORE + kiem tra NOT EXISTS qua stored procedure).
-- =============================================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _grant_branch_read_9197;
DELIMITER $$
CREATE PROCEDURE _grant_branch_read_9197(IN p_role_code VARCHAR(50))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = 'branch.read' LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions
                        WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;

CALL _grant_branch_read_9197('le_tan');
CALL _grant_branch_read_9197('bac_si');
CALL _grant_branch_read_9197('duoc_si');
CALL _grant_branch_read_9197('ky_thuat_vien');
-- ke_toan da co branch.read tu 9139, khong can grant lai (INSERT IGNORE an toan neu chay lai)
CALL _grant_branch_read_9197('ke_toan');

DROP PROCEDURE IF EXISTS _grant_branch_read_9197;
