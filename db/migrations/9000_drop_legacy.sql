-- ============================================================
-- Migration: 9000_drop_legacy
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tác giả: Pro-Diab Team
-- Mô tả: Xóa toàn bộ bảng legacy không có prefix diab_his_
--        (trừ hangfire_*) để chuẩn bị cho Clean Slate schema.
-- Idempotent: YES (xóa nếu tồn tại, bỏ qua nếu không có)
-- ============================================================
SET NAMES utf8mb4;

-- Tắt kiểm tra khóa ngoại để xóa không bị ràng buộc
SET FOREIGN_KEY_CHECKS = 0;

-- Tạo stored procedure để drop từng bảng legacy
DROP PROCEDURE IF EXISTS drop_legacy_tables;

DELIMITER $$
CREATE PROCEDURE drop_legacy_tables()
BEGIN
    DECLARE v_done    INT DEFAULT FALSE;
    DECLARE v_tbl     VARCHAR(64);
    DECLARE v_sql     TEXT;
    DECLARE v_db      VARCHAR(64);

    DECLARE cur_legacy CURSOR FOR
        SELECT TABLE_NAME
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME NOT LIKE 'diab\_his\_%'
          AND TABLE_NAME NOT LIKE 'hangfire\_%';

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = TRUE;

    OPEN cur_legacy;

    read_loop: LOOP
        FETCH cur_legacy INTO v_tbl;
        IF v_done THEN
            LEAVE read_loop;
        END IF;

        SET v_sql = CONCAT('DROP TABLE IF EXISTS `', v_tbl, '`');
        SET @__ddl = v_sql;
        PREPARE __stmt FROM @__ddl;
        EXECUTE __stmt;
        DEALLOCATE PREPARE __stmt;
    END LOOP;

    CLOSE cur_legacy;
END$$
DELIMITER ;

-- Gọi procedure để xóa tất cả bảng legacy
CALL drop_legacy_tables();

-- Dọn dẹp procedure sau khi dùng xong
DROP PROCEDURE IF EXISTS drop_legacy_tables;

-- Bật lại kiểm tra khóa ngoại
SET FOREIGN_KEY_CHECKS = 1;
