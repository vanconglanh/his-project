-- =============================================================================
-- 9198_fix_missing_cls_round_perms.sql
-- Fix bug that: bac_si va ke_toan KHONG co bat ky quyen cls_round.* nao, du
-- migration 9139_reconcile_role_permissions.sql (grant cho bac_si: create/
-- read/submit/pay/waive) va 9141_rbac_standard_alignment.sql PHAN A1 (grant
-- cho ke_toan: read/pay/waive) DA co lenh INSERT ... SELECT id FROM
-- diab_his_sec_permissions WHERE code IN (...) - NHUNG catalog permission
-- cls_round.* CHI duoc tao boi 9144_seed_cls_round_permissions.sql, chay SAU
-- ca 9139 lan 9141. Tai thoi diem 9139/9141 chay, cac dong code IN (...) do
-- khong khop id nao trong bang permissions -> INSERT 0 dong, am tham that
-- bai. 9144 sau do chi tu grant het cho admin, khong quay lai grant cho
-- bac_si/ke_toan nhu 2 migration truoc DA CO Y DINH.
--
-- HAU QUA THAT: bac si khong tao duoc "Dot chi dinh can lam sang" (POST/GET
-- /api/v1/encounters/{id}/cls-rounds -> 403 "Ban khong co quyen thuc hien
-- thao tac nay") -> khong co ket qua XN/CDHA nao duoc tao trong he thong ->
-- chan luon man hinh Ky thuat vien (phu thuoc du lieu tu cls-rounds). Phat
-- hien khi QC chup anh tai lieu huong dan luong kham benh tren his.diab.vn
-- (2026-09-02), ngay sau khi fix bug branch.read tuong tu (9197).
--
-- FIX: chi INSERT IGNORE dung cac quyen con thieu, dung theo Y DINH DA GHI RO
-- trong comment cua 9139 (bac_si: create/read/submit/pay/waive) va 9141 PHAN
-- A1 (ke_toan: read/pay/waive) - KHONG tu y mo rong them quyen nao khac
-- ngoai nhung gi da duoc quyet dinh nghiep vu ghi lai truoc do.
--
-- Idempotent: YES.
-- =============================================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _grant_perm_9198;
DELIMITER $$
CREATE PROCEDURE _grant_perm_9198(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
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

-- Bac si — dung y dinh goc cua 9139
CALL _grant_perm_9198('bac_si', 'cls_round.create');
CALL _grant_perm_9198('bac_si', 'cls_round.read');
CALL _grant_perm_9198('bac_si', 'cls_round.submit');
CALL _grant_perm_9198('bac_si', 'cls_round.pay');
CALL _grant_perm_9198('bac_si', 'cls_round.waive');

-- Ke toan — dung y dinh goc cua 9141 PHAN A1
CALL _grant_perm_9198('ke_toan', 'cls_round.read');
CALL _grant_perm_9198('ke_toan', 'cls_round.pay');
CALL _grant_perm_9198('ke_toan', 'cls_round.waive');

DROP PROCEDURE IF EXISTS _grant_perm_9198;
