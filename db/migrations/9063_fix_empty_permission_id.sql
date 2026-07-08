-- ============================================================
-- Migration: 9063_fix_empty_permission_id
-- Engine: MySQL 8.0+
-- Muc dich: sua du lieu hong lam GET /users/me + /admin/roles tra 500.
--   Bang diab_his_sec_permissions co dong id='' (rong) do seed 0052
--   INSERT qua view sec_permissions chi voi (code, description), khong set id;
--   tren prod DEFAULT (UUID()) khong ap qua view -> id thanh ''.
--   Prod dung GuidFormat=None -> EF Core `new Guid("")` -> FormatException -> 500.
--   Fix: backfill id=UUID() cho moi dong permission id rong/khong hop le,
--   dong bo permission_id trong diab_his_sec_role_permissions, va backfill
--   resource/action con trong (best-effort tach tu code "resource.action").
-- Idempotent: YES (chi dung khi con dong hong; chay lai khong tac dong).
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_empty_perm_id;
DELIMITER $$
CREATE PROCEDURE _fix_empty_perm_id()
BEGIN
    DECLARE done INT DEFAULT 0;
    DECLARE bad_code VARCHAR(100);
    DECLARE new_id CHAR(36);
    DECLARE cur CURSOR FOR
        SELECT code FROM diab_his_sec_permissions
        WHERE id IS NULL OR id = '' OR CHAR_LENGTH(id) <> 36;
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

    OPEN cur;
    fix_loop: LOOP
        FETCH cur INTO bad_code;
        IF done = 1 THEN
            LEAVE fix_loop;
        END IF;

        SET new_id = UUID();
        SET FOREIGN_KEY_CHECKS = 0;

        -- Dong bo role_permissions dang tro toi permission_id hong (map theo code)
        UPDATE diab_his_sec_role_permissions rp
        JOIN diab_his_sec_permissions p
             ON (p.id IS NULL OR p.id = '' OR CHAR_LENGTH(p.id) <> 36)
            AND p.code = bad_code
        SET rp.permission_id = new_id
        WHERE rp.permission_id IS NULL OR rp.permission_id = ''
              OR CHAR_LENGTH(rp.permission_id) <> 36;

        -- Backfill id + resource/action (tach tu code "resource.action" neu con trong)
        UPDATE diab_his_sec_permissions
        SET id = new_id,
            resource = CASE WHEN resource IS NULL OR resource = ''
                            THEN SUBSTRING_INDEX(bad_code, '.', 1) ELSE resource END,
            action   = CASE WHEN action IS NULL OR action = ''
                            THEN SUBSTRING_INDEX(bad_code, '.', -1) ELSE action END
        WHERE code = bad_code
          AND (id IS NULL OR id = '' OR CHAR_LENGTH(id) <> 36);

        SET FOREIGN_KEY_CHECKS = 1;
    END LOOP;
    CLOSE cur;
END$$
DELIMITER ;

CALL _fix_empty_perm_id();
DROP PROCEDURE IF EXISTS _fix_empty_perm_id;
