-- =============================================================================
-- 9209_seed_ktv_dieuduong_admit_permission.sql
-- Nguon: Lark Bug-Feedback (BM-08) - "Nut Dua vao kham hien thi khong nhat quan
-- theo role - hanh vi sai khac nhau giua Le tan, KTV, Bac si".
-- Bug that: role ky_thuat_vien va dieu_duong CHUA tung duoc grant
-- 'reception.queue.manage' (permission BE dung cho endpoint POST
-- /reception/queue/{id}/admit), nen bam "Dua vao kham" luon bi 403 "Khong co
-- quyen thuc hien thao tac nay" du UI van hien nut binh thuong.
-- PO xac nhan: dung nghiep vu la KTV/Dieu duong moi la nguoi don benh nhan vao
-- do sinh hieu (khong phai Le tan) => bat quyen nay cho ca 2 role, dong thoi
-- FE se an han nut voi Le tan (xem TicketCard.tsx/ReceptionQueueBoard.tsx).
-- Idempotent: YES (thu tuc dung IF NOT EXISTS check truoc khi INSERT).
-- =============================================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _grant_perm_9209;
DELIMITER $$
CREATE PROCEDURE _grant_perm_9209(IN p_role_code VARCHAR(50), IN p_perm_code VARCHAR(100))
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

-- KTV: bo sung quyen "Dua vao kham" (truoc gio bam vao bi chan dung nhung UI van hien nut).
CALL _grant_perm_9209('ky_thuat_vien', 'reception.queue.manage');
CALL _grant_perm_9209('ky_thuat_vien', 'encounter.create');
CALL _grant_perm_9209('ky_thuat_vien', 'encounter.start');
CALL _grant_perm_9209('ky_thuat_vien', 'reception.read');

-- Dieu duong (9206): thieu 'reception.queue.manage' nen "Dua vao kham" cung dang
-- bi 403 tuong tu KTV du da co encounter.create/encounter.start - bo sung not.
CALL _grant_perm_9209('dieu_duong', 'reception.queue.manage');

DROP PROCEDURE IF EXISTS _grant_perm_9209;

-- Rollback:
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_roles r ON r.id = rp.role_id
--     JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
--    WHERE r.code IN ('ky_thuat_vien','dieu_duong') AND p.code = 'reception.queue.manage';
--   DELETE rp FROM diab_his_sec_role_permissions rp
--     JOIN diab_his_sec_roles r ON r.id = rp.role_id
--     JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
--    WHERE r.code = 'ky_thuat_vien' AND p.code IN ('encounter.create','encounter.start','reception.read');
