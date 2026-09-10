-- =============================================================================
-- 9205_seed_cls_round_cancel_bacsi.sql
-- Nguon: Lark Bug-Feedback (BM-15) - "[BUG] phan quyen khi huy/xoa chi dinh".
-- Bac si bam "Huy dot" tren dot chi dinh CHINH MINH tao nhung he thong bao
-- "Ban khong co quyen thuc hien thao tac nay" (403 PERMISSION_DENIED).
--
-- ROOT CAUSE: permission cls_round.cancel (tao boi 9144_seed_cls_round_permissions.sql)
-- CHI duoc grant cho role admin. Cac migration truoc do (9139_reconcile_role_permissions,
-- 9198_fix_missing_cls_round_perms) chi grant cho bac_si: create/read/submit/pay/waive -
-- KHONG co cancel, du logic chuyen trang thai (ClsRoundStatus.CanTransition trong
-- ClsOrderRound.cs) da cho phep huy tu OPEN/SUBMITTED/IN_PROGRESS (chua "chot" =
-- COMPLETED) tu truoc. Day la thieu sot seed, khong phai chu y nghiep vu.
--
-- FIX: grant cls_round.cancel cho bac_si (nguoi tao dot, duoc huy khi dot CHUA hoan
-- tat/CHUA thanh toan - xem guard bo sung trong CancelClsRoundCommandHandler).
-- Truong hop dot DA THANH TOAN can hoan tien thi thuoc quy trinh Thu ngan/ke_toan xu ly
-- rieng (billing refund) - KHONG cap cls_round.cancel cho ke_toan trong migration nay vi
-- chua co UI/flow hoan tien tuong ung, tranh mo quyen "chet" (grant nhung khong co man
-- hinh nao dung toi).
--
-- Idempotent: YES.
-- =============================================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _grant_perm_9205;
DELIMITER $$
CREATE PROCEDURE _grant_perm_9205(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
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

CALL _grant_perm_9205('bac_si', 'cls_round.cancel');

DROP PROCEDURE IF EXISTS _grant_perm_9205;

-- Rollback:
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_roles r ON r.id = rp.role_id
--     JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
--    WHERE r.code = 'bac_si' AND r.tenant_id IS NULL AND p.code = 'cls_round.cancel';
