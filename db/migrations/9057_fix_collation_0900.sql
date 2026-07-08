-- ============================================================
-- Migration: 9057_fix_collation_0900
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta: Chuan hoa collation. Nhieu bang diab_his_* duoc tao voi
--   utf8mb4_unicode_ci (theo template migration cu) trong khi cac bang loi
--   (pat/enc/pha/sec...) la utf8mb4_0900_ai_ci. Khi JOIN cheo hai nhom nay tren
--   cot CHAR(36)/VARCHAR -> loi 1267 "Illegal mix of collations" (vd bao cao
--   revenue/by-doctor, cashier/daily-summary bi 500).
--   Migration nay CONVERT moi bang diab_his_* con unicode_ci sang 0900_ai_ci.
--   KHONG dung toi bang hangfire_* (utf8mb3, do Hangfire tu quan ly).
-- Idempotent: YES (chi convert bang chua phai 0900; chay lai = no-op).
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS fix_collation_0900;
DELIMITER $$
CREATE PROCEDURE fix_collation_0900()
BEGIN
    DECLARE done INT DEFAULT 0;
    DECLARE tname VARCHAR(200);
    DECLARE cur CURSOR FOR
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = DATABASE()
          AND table_type = 'BASE TABLE'
          AND table_name LIKE 'diab_his_%'
          AND table_collation NOT LIKE 'utf8mb4_0900%';
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

    -- Tat FK check trong luc convert de tranh loi rang buoc collation tam thoi.
    SET FOREIGN_KEY_CHECKS = 0;

    OPEN cur;
    read_loop: LOOP
        FETCH cur INTO tname;
        IF done = 1 THEN LEAVE read_loop; END IF;
        SET @ddl = CONCAT('ALTER TABLE `', tname,
                          '` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci');
        PREPARE stmt FROM @ddl;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END LOOP;
    CLOSE cur;

    SET FOREIGN_KEY_CHECKS = 1;
END$$
DELIMITER ;

CALL fix_collation_0900();
DROP PROCEDURE IF EXISTS fix_collation_0900;
