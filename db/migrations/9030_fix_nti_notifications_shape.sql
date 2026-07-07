-- ============================================================
-- Migration: 9030_fix_nti_notifications_shape
-- Mo ta: Bang diab_his_nti_notifications tren deploy dang shape legacy tu
--   migration 0009 (id INT, recipient_user_id INT, type varchar(50)) do IF NOT
--   EXISTS khien cac ban re-define sau (0050) bi skip. Code (Dapper handlers +
--   NotificationConfiguration) dung `user_id CHAR(36)` (GUID chuoi) va `id CHAR(36)`.
--   -> Query `WHERE user_id = @UserId` bao "Unknown column 'user_id'" 500 tren
--   moi trang (chuong thong bao poll unread-count + inbox).
--   Bang rong (0 row) nen recreate an toan, khong mat du lieu.
-- Idempotent: YES (chi recreate khi thieu cot `user_id` VA bang dang rong)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS _fix_nti_notifications_9030;
DELIMITER $$
CREATE PROCEDURE _fix_nti_notifications_9030()
BEGIN
    DECLARE has_user_id INT DEFAULT 0;
    DECLARE row_cnt INT DEFAULT 0;

    SELECT COUNT(*) INTO has_user_id FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'diab_his_nti_notifications'
       AND COLUMN_NAME = 'user_id';

    IF has_user_id = 0 THEN
        SELECT COUNT(*) INTO row_cnt FROM diab_his_nti_notifications;
        IF row_cnt = 0 THEN
            DROP TABLE diab_his_nti_notifications;
            CREATE TABLE diab_his_nti_notifications (
                id          CHAR(36)     NOT NULL,
                tenant_id   INT          NOT NULL,
                user_id     CHAR(36)     NOT NULL,
                type        VARCHAR(100) NOT NULL,
                title       VARCHAR(300) NOT NULL,
                body        TEXT         NOT NULL,
                data_json   JSON         NULL,
                read_at     DATETIME     NULL,
                created_at  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                INDEX idx_nti_user   (tenant_id, user_id, created_at),
                INDEX idx_nti_unread (tenant_id, user_id, read_at)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
        END IF;
    END IF;
END$$
DELIMITER ;
CALL _fix_nti_notifications_9030();
DROP PROCEDURE IF EXISTS _fix_nti_notifications_9030;
