-- ============================================================
-- Migration: 9222_fix_nti_notifications_id_charid
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mo ta:
--   Bang diab_his_nti_notifications tren prod/staging dang ket o shape LEGACY tu
--   migration 0009: `id INT AUTO_INCREMENT`, `recipient_user_id INT NOT NULL` (khong
--   default). Migration 9030 dang le recreate bang voi `id CHAR(36)` nhung guard
--   `has_user_id = 0` bi SAI vi cot `user_id` da duoc 9072 them vao -> 9030 skip, bang
--   giu nguyen `id INT`.
--
--   Code (PatientNotifyService.NotifyAsync + NotificationHandlers + NotificationConfiguration)
--   INSERT `id = Guid.NewGuid().ToString()` (chuoi 36 ky tu) vao cot `id INT`
--   -> MySqlException 1265 "Data truncated for column 'id' at row 1" -> RecallNotifyJob /
--   MedReminderJob / QueueTurnNotifyJob gui nhac benh nhan that bai, danh notify_status=FAILED.
--
--   Fix g-oc: dua shape ve dung ky vong cua code:
--     - id            INT  -> CHAR(36) NOT NULL DEFAULT (UUID())  (bo AUTO_INCREMENT)
--     - recipient_user_id INT NOT NULL -> INT NULL  (INSERT moi khong set cot legacy nay)
--     - type          VARCHAR(50)  -> VARCHAR(100)   (khop NotificationConfiguration)
--     - title         VARCHAR(255) -> VARCHAR(300)   (khop NotificationConfiguration)
--
--   An toan du lieu: prod bang rong (0 row). Staging ~5 row legacy USER notification; cac
--   gia tri id INT (vd "1".."7") se tro thanh chuoi tuong ung sau MODIFY, khong mat du lieu.
-- Idempotent: YES (kiem information_schema truoc moi ALTER)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_nti_9222;
DELIMITER $$
CREATE PROCEDURE _fix_nti_9222()
BEGIN
    DECLARE v_id_type       VARCHAR(64) DEFAULT NULL;
    DECLARE v_recip_null    VARCHAR(3)  DEFAULT NULL;
    DECLARE v_type_len      BIGINT      DEFAULT NULL;
    DECLARE v_title_len     BIGINT      DEFAULT NULL;
    DECLARE v_recip_exists  INT         DEFAULT 0;

    -- 1) id: INT -> CHAR(36)
    SELECT DATA_TYPE INTO v_id_type FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_nti_notifications' AND COLUMN_NAME = 'id';
    IF v_id_type IS NOT NULL AND v_id_type <> 'char' THEN
        ALTER TABLE diab_his_nti_notifications
            MODIFY COLUMN id CHAR(36) NOT NULL DEFAULT (UUID()) COMMENT 'Khoa chinh GUID (chuoi 36 ky tu)';
    END IF;

    -- 2) recipient_user_id: NOT NULL -> NULL (cot legacy, INSERT moi khong set)
    SELECT COUNT(*) INTO v_recip_exists FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_nti_notifications' AND COLUMN_NAME = 'recipient_user_id';
    IF v_recip_exists > 0 THEN
        SELECT IS_NULLABLE INTO v_recip_null FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_nti_notifications' AND COLUMN_NAME = 'recipient_user_id';
        IF v_recip_null = 'NO' THEN
            ALTER TABLE diab_his_nti_notifications
                MODIFY COLUMN recipient_user_id INT NULL COMMENT 'FK legacy -> sec_users.ID (khong con dung o INSERT moi)';
        END IF;
    END IF;

    -- 3) type: >= VARCHAR(100)
    SELECT CHARACTER_MAXIMUM_LENGTH INTO v_type_len FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_nti_notifications' AND COLUMN_NAME = 'type';
    IF v_type_len IS NOT NULL AND v_type_len < 100 THEN
        ALTER TABLE diab_his_nti_notifications MODIFY COLUMN `type` VARCHAR(100) NOT NULL;
    END IF;

    -- 4) title: >= VARCHAR(300)
    SELECT CHARACTER_MAXIMUM_LENGTH INTO v_title_len FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_nti_notifications' AND COLUMN_NAME = 'title';
    IF v_title_len IS NOT NULL AND v_title_len < 300 THEN
        ALTER TABLE diab_his_nti_notifications MODIFY COLUMN title VARCHAR(300) NOT NULL;
    END IF;
END$$
DELIMITER ;
CALL _fix_nti_9222();
DROP PROCEDURE IF EXISTS _fix_nti_9222;
