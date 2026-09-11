-- =============================================================================
-- 9204_seed_report_date_range_permission.sql
-- Nguon: Lark Bug-Feedback (bang DiaB His(Web)), ticket BM-01 - Ma tran phan
-- quyen Le tan/CC: "Khong duoc phep xem Bao cao *thang* / *quy* (ly do: bao
-- mat so lieu cong ty)". Role le_tan hien co report.read nen van goi duoc
-- toan bo GET /api/v1/reports/* voi tham so from/to bat ky (khong gioi han
-- pham vi ngay) -> can 1 permission rieng danh dau "duoc xem bao cao voi
-- khoang ngay mo rong (>1 ngay / khong phai hom nay)".
--
-- Permission moi: report.date_range.extended
-- Cap cho: admin, bac_si, ke_toan, duoc_si (cac role dang dung report.read
-- cho nghiep vu can xem theo ky dai hon 1 ngay). KHONG cap cho le_tan - dung
-- yeu cau ticket BM-01. Enforce thuc te o
-- backend/src/ProDiabHis.Api/Filters/ReportDateRangeGuardFilter.cs (ap dung
-- tren ReportsController) - migration nay chi seed du lieu permission.
--
-- Idempotent: YES (INSERT IGNORE + check ton tai truoc khi grant).
-- =============================================================================
SET NAMES utf8mb4;

INSERT INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'report.date_range.extended', 'report', 'date_range.extended',
       'Xem bao cao/dashboard voi khoang ngay mo rong (khac hom nay), vd theo thang/quy', NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions WHERE code = 'report.date_range.extended');

DROP PROCEDURE IF EXISTS _grant_perm_9204;
DELIMITER $$
CREATE PROCEDURE _grant_perm_9204(IN p_role_code VARCHAR(50))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = 'report.date_range.extended' LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions
                        WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;

CALL _grant_perm_9204('bac_si');
CALL _grant_perm_9204('ke_toan');
CALL _grant_perm_9204('duoc_si');
-- admin bypass qua is_super_admin claim (xem 9139) nhung van grant du lieu cho dung logic hien thi menu.
CALL _grant_perm_9204('admin');

DROP PROCEDURE IF EXISTS _grant_perm_9204;
