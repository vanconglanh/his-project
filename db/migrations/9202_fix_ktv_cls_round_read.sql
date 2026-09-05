-- =============================================================================
-- 9202_fix_ktv_cls_round_read.sql
-- Fix not con sot cua 9198: role ky_thuat_vien KHONG co quyen cls_round.read,
-- du 9139_reconcile_role_permissions.sql PHAN "KY THUAT VIEN" DA liet ke
-- 'cls_round.read' trong danh sach curated.
--
-- NGUYEN NHAN GIONG HET 9198: catalog permission cls_round.* chi duoc tao boi
-- 9144_seed_cls_round_permissions.sql, chay SAU 9139. Tai thoi diem 9139 chay,
-- dong "INSERT ... SELECT id FROM permissions WHERE code IN (...)" khong khop
-- id nao -> INSERT 0 dong, am tham that bai. 9198 sau do da vet lai cho bac_si
-- va ke_toan NHUNG BO SOT ky_thuat_vien.
--
-- HAU QUA: KTV mo man hinh ket qua CLS khong doc duoc thong tin dot chi dinh
-- (GET /api/v1/cls-rounds/{id} -> 403), tuy hien tai luong nhap ket qua qua
-- /lab-results van chay duoc nen chua lo ra ngay. Phat hien khi QC verify lai
-- ma tran phan quyen sau dot fix 8 bug (2026-09-05).
--
-- FIX: chi INSERT dung 1 quyen con thieu, theo dung Y DINH DA GHI trong 9139.
-- KHONG mo rong them quyen nao khac.
--
-- Idempotent: YES (kiem tra NOT EXISTS truoc khi insert).
-- =============================================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _grant_perm_9202;
DELIMITER $$
CREATE PROCEDURE _grant_perm_9202(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
BEGIN
    DECLARE v_role_id CHAR(36);
    DECLARE v_perm_id CHAR(36);
    SELECT id INTO v_role_id FROM diab_his_sec_roles WHERE code = p_role_code AND tenant_id IS NULL LIMIT 1;
    SELECT id INTO v_perm_id FROM diab_his_sec_permissions WHERE code = p_perm_code LIMIT 1;
    IF v_role_id IS NOT NULL AND v_perm_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM diab_his_sec_role_permissions
                        WHERE role_id = v_role_id AND permission_id = v_perm_id) THEN
        INSERT INTO diab_his_sec_role_permissions (role_id, permission_id) VALUES (v_role_id, v_perm_id);
    END IF;
END$$
DELIMITER ;

-- Ky thuat vien — dung y dinh goc cua 9139
CALL _grant_perm_9202('ky_thuat_vien', 'cls_round.read');

-- Vet lai luon cho bac_si / ke_toan de migration nay tu du tren moi truong dung moi
-- (neu 9198 chua chay). Da co thi NOT EXISTS bo qua.
CALL _grant_perm_9202('bac_si', 'cls_round.create');
CALL _grant_perm_9202('bac_si', 'cls_round.read');
CALL _grant_perm_9202('bac_si', 'cls_round.submit');
CALL _grant_perm_9202('bac_si', 'cls_round.pay');
CALL _grant_perm_9202('bac_si', 'cls_round.waive');
CALL _grant_perm_9202('ke_toan', 'cls_round.read');
CALL _grant_perm_9202('ke_toan', 'cls_round.pay');
CALL _grant_perm_9202('ke_toan', 'cls_round.waive');

DROP PROCEDURE IF EXISTS _grant_perm_9202;
