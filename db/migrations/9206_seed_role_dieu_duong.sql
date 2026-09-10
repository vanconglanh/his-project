-- =============================================================================
-- 9206_seed_role_dieu_duong.sql
-- Nguon: Lark Bug-Feedback (BM-05) - "Role Dieu duong/KTV - Flow/phan quyen de xuat".
-- PO xac nhan (2026-09-10): tao ROLE MOI "Dieu duong" (rieng biet voi ky_thuat_vien),
-- quyen NEN giong ky_thuat_vien roi bo sung theo yeu cau rieng cua Dieu duong:
--   - Xem hang doi tiep don (reception.read) de theo doi + goi benh nhan
--   - Dua benh nhan vao kham (encounter.create, encounter.start)
--   - Ghi sinh hieu (vital_sign.read/write) - tinh nang nay DA CO SAN trong he thong
--     (trang /nurse, VitalSignsController) nhung truoc gio khong role nao dung toi vi
--     permission "nursing.read" dung o FE (nav-items.ts) chua tung duoc seed vao catalog.
--
-- Idempotent: YES (INSERT IGNORE + check ton tai truoc khi grant).
-- =============================================================================
SET NAMES utf8mb4;

-- Permission "nursing.read" duoc FE (nav-items.ts) tham chieu de hien nav "Dieu duong"
-- nhung chua tung ton tai trong catalog - bo sung.
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'nursing.read', 'nursing', 'read',
       'Xem trang Dieu duong (hang doi tiep don + ghi sinh hieu)', NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions WHERE code = 'nursing.read');

-- Role moi: Dieu duong
INSERT IGNORE INTO diab_his_sec_roles
    (id, code, name, description, role_type, tenant_id, is_active, created_at)
VALUES
    ('00000000-0000-0000-0000-000000000007', 'dieu_duong', 'Điều dưỡng',
     'Theo doi hang doi tiep don, dua benh nhan vao kham, ghi sinh hieu, ho tro CLS',
     'SYSTEM', NULL, 1, NOW());

DROP PROCEDURE IF EXISTS _grant_perm_9206;
DELIMITER $$
CREATE PROCEDURE _grant_perm_9206(IN p_perm_code VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = 'dieu_duong' AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm_code LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions
                        WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;

-- Quyen NEN: dung y het ky_thuat_vien (xem 9139_reconcile_role_permissions.sql PHAN KTV).
CALL _grant_perm_9206('patient.read');
CALL _grant_perm_9206('encounter.read');
CALL _grant_perm_9206('lab_order.read');
CALL _grant_perm_9206('lab_order.update');
CALL _grant_perm_9206('rad_order.read');
CALL _grant_perm_9206('rad_order.update');
CALL _grant_perm_9206('lab_result.read');
CALL _grant_perm_9206('lab_result.write');
CALL _grant_perm_9206('lab_result.verify');
CALL _grant_perm_9206('lab_result.import');
CALL _grant_perm_9206('rad_result.read');
CALL _grant_perm_9206('rad_result.write');
CALL _grant_perm_9206('rad_result.verify');
CALL _grant_perm_9206('cls_upload.create');
CALL _grant_perm_9206('cls_upload.read');
CALL _grant_perm_9206('cls_upload.delete');
CALL _grant_perm_9206('cls_round.read');
CALL _grant_perm_9206('icd10.read');
CALL _grant_perm_9206('file.upload');
CALL _grant_perm_9206('file.delete');
CALL _grant_perm_9206('file_annotation.read');
CALL _grant_perm_9206('file_annotation.write');
CALL _grant_perm_9206('file_annotation.delete');
CALL _grant_perm_9206('dashboard.read');
CALL _grant_perm_9206('notification.read');

-- Quyen BO SUNG rieng cho Dieu duong (theo BM-05):
CALL _grant_perm_9206('reception.read');       -- xem "Bang hang doi" tai man Tiep don
CALL _grant_perm_9206('encounter.create');     -- tao luot kham khi "Dua vao kham"
CALL _grant_perm_9206('encounter.start');      -- bat dau kham (chuyen WAITING -> IN_PROGRESS)
CALL _grant_perm_9206('vital_sign.read');      -- xem lai sinh hieu da ghi
CALL _grant_perm_9206('vital_sign.write');     -- "Ghi sinh hieu" (da co san UI: VitalSignsForm)
CALL _grant_perm_9206('nursing.read');         -- hien nav "Dieu duong" (/nurse)

DROP PROCEDURE IF EXISTS _grant_perm_9206;

-- Rollback:
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_roles r ON r.id = rp.role_id
--    WHERE r.code = 'dieu_duong' AND r.tenant_id IS NULL;
--   DELETE FROM diab_his_sec_roles WHERE code = 'dieu_duong' AND tenant_id IS NULL;
--   DELETE FROM diab_his_sec_permissions WHERE code = 'nursing.read';
