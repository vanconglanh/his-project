-- ============================================================
-- Migration: 9090_create_ticket_reassignments
-- Engine: MySQL 8.0+, InnoDB, utf8mb4_0900_ai_ci
-- Story refs: G05 — Dieu phoi kham (doi bac si / doi phong / chuyen phong giua ca)
-- Mo ta: Lich su dieu phoi luot kham. GIU NGUYEN ticket_no, KHONG huy-tao-lai ve.
--        Bo sung cot reassign_count + finished_by_doctor_id tren ve hang doi
--        (finished_by_doctor_id = chot cong bac si, moi ve quy ve dung 1 BS).
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + add_col_if_missing/add_index_if_missing
--                  + DROP TRIGGER IF EXISTS truoc khi tao)
-- Phu thuoc: 0000_helpers.sql, 0022_create_reception_queue.sql, 9003_create_encounter.sql
-- ============================================================
SET NAMES utf8mb4;

-- ---------- 1. Bang lich su dieu phoi ----------
CREATE TABLE IF NOT EXISTS `diab_his_rcp_ticket_reassignments` (
    `id`                      CHAR(36)     NOT NULL DEFAULT (UUID())  COMMENT 'UUID khoa chinh',
    `tenant_id`               INT          NOT NULL                   COMMENT 'ID tenant (bat buoc filter moi query)',
    `ticket_id`               CHAR(36)     NOT NULL                   COMMENT 'FK -> diab_his_rcp_queue_tickets.id',
    `encounter_id`            CHAR(36)     NULL                       COMMENT 'FK -> diab_his_enc_encounters.id (NULL neu chua admit)',
    `from_doctor_id`          CHAR(36)     NULL                       COMMENT 'Bac si truoc khi doi (NULL = chua phan cong)',
    `to_doctor_id`            CHAR(36)     NULL                       COMMENT 'Bac si sau khi doi',
    `from_room_id`            CHAR(36)     NULL                       COMMENT 'Phong truoc khi doi',
    `to_room_id`              CHAR(36)     NULL                       COMMENT 'Phong sau khi doi',
    `change_type`             VARCHAR(10)  NOT NULL                   COMMENT 'DOCTOR|ROOM|BOTH',
    `ticket_status_at_change` VARCHAR(20)  NOT NULL                   COMMENT 'Trang thai ve luc doi (WAITING|CALLED|IN_PROGRESS|WAITING_CLS)',
    `reason`                  TEXT         NOT NULL                   COMMENT 'Ly do dieu phoi — BAT BUOC',
    `schedule_warning_flag`   TINYINT(1)   NOT NULL DEFAULT 0         COMMENT '1 = BS dich khong truc / bi block khung gio nay (canh bao, khong chan)',
    `warning_message`         TEXT         NULL                       COMMENT 'Noi dung canh bao hien thi cho nguoi dieu phoi',
    `acknowledged_warning`    TINYINT(1)   NOT NULL DEFAULT 0         COMMENT '1 = nguoi dieu phoi da xac nhan doc canh bao',
    `changed_at`              DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) COMMENT 'Thoi diem dieu phoi (UTC)',
    `changed_by`              CHAR(36)     NULL                       COMMENT 'FK -> diab_his_sec_users.id',
    `created_at`              DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_by`              CHAR(36)     NULL,
    `updated_at`              DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    `updated_by`              CHAR(36)     NULL,
    `deleted_at`              DATETIME(3)  NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_reassign_tenant_ticket` (`tenant_id`, `ticket_id`, `changed_at`),
    INDEX `idx_reassign_tenant_enc`    (`tenant_id`, `encounter_id`),
    INDEX `idx_reassign_to_doctor`     (`tenant_id`, `to_doctor_id`, `changed_at`),
    INDEX `idx_reassign_from_doctor`   (`tenant_id`, `from_doctor_id`, `changed_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lich su dieu phoi luot kham (doi bac si / doi phong), giu nguyen ticket_no';

-- ---------- 2. Cot bo tro tren ve hang doi ----------
CALL add_col_if_missing('diab_his_rcp_queue_tickets', 'reassign_count',
     'INT NOT NULL DEFAULT 0 COMMENT ''So lan da dieu phoi ve nay''');
CALL add_col_if_missing('diab_his_rcp_queue_tickets', 'finished_by_doctor_id',
     'CHAR(36) NULL COMMENT ''Bac si ket thuc ca (chot cong) — set 1 lan khi ve -> DONE''');

CALL add_index_if_missing('diab_his_rcp_queue_tickets', 'idx_rcp_finished_doctor',
     '(`tenant_id`, `finished_by_doctor_id`, `ticket_date`)');

-- ---------- 3. Backfill du lieu lich su ----------
-- Ve DONE truoc khi co tinh nang dieu phoi: BS ket thuc ca = BS dang gan tren ve.
UPDATE `diab_his_rcp_queue_tickets`
   SET `finished_by_doctor_id` = `doctor_id`
 WHERE `status` = 'DONE'
   AND `finished_by_doctor_id` IS NULL
   AND `doctor_id` IS NOT NULL;

-- ---------- 4. Trigger chot cong (an toan cho moi duong ghi) ----------
-- Ve co the chuyen DONE tu nhieu luong (man Tiep don, dong ca kham dong bo nguoc).
-- Trigger bao dam finished_by_doctor_id luon duoc set 1 lan duy nhat, khong ghi de.
DROP TRIGGER IF EXISTS `trg_rcp_ticket_finished_doctor`;
DELIMITER $$
CREATE TRIGGER `trg_rcp_ticket_finished_doctor`
BEFORE UPDATE ON `diab_his_rcp_queue_tickets`
FOR EACH ROW
BEGIN
    IF NEW.`status` = 'DONE' AND NEW.`finished_by_doctor_id` IS NULL THEN
        SET NEW.`finished_by_doctor_id` = NEW.`doctor_id`;
    END IF;
END$$
DELIMITER ;

-- Rollback:
--   DROP TRIGGER IF EXISTS trg_rcp_ticket_finished_doctor;
--   DROP TABLE IF EXISTS diab_his_rcp_ticket_reassignments;
--   ALTER TABLE diab_his_rcp_queue_tickets DROP COLUMN reassign_count, DROP COLUMN finished_by_doctor_id;
