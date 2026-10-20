-- ============================================================
-- Data remediation (PRODUCTION prodiab_his) - 2026-10-20
-- Muc dich: don dep hau qua bug "Data truncated for column 'id'" + chong gui nhac
--   hang loat cho benh nhan that (du lieu migrate tu SUNS) khi HIS CHUA go-live.
--
-- BOI CANH:
--   - 50 recall (diab_his_cli_followup_recall) bi notify_status='FAILED' do bug INSERT
--     thong bao (khong phai that bai gui that su). Reset ve NULL (trang thai "chua gui"
--     trung tinh) de nhan vien thay dung, khong hieu nham la da gui that bai.
--   - ~11-12 lich hen tuong lai (diab_his_sch_appointments) nguon legacy SUNS (HenTaiKham),
--     created_by IS NULL, batch migrate, reminder_sent_at IS NULL. Danh dau reminder_sent_at
--     = NOW() de AppointmentReminderNotifyJob KHONG gui Zalo/SMS (phong kham van chay SUNS
--     tu nhac -> tranh nhan trung). Khi go-live, lich hen MOI (created_by != NULL) van duoc
--     nhac binh thuong.
--
-- AN TOAN:
--   - Chi dong toi 2 bang nghiep vu, khong dong SUNS/diab_his khac.
--   - BACKUP DB TRUOC khi chay (xem runbook deploy).
--   - Idempotent: chay lai nhieu lan khong doi ket qua (guard WHERE ... IS NULL / = 'FAILED').
--   - KHONG xoa du lieu (chi UPDATE cot trang thai/moc gio).
-- ============================================================
SET NAMES utf8mb4;

-- 1) Reset recall bi FAILED oan do bug INSERT thong bao -> NULL (chua gui).
--    Giu nguyen status (PENDING/CONTACTED...) va notified_at (dang NULL).
UPDATE diab_his_cli_followup_recall
   SET notify_status = NULL,
       updated_at    = NOW(3)
 WHERE notify_status = 'FAILED';

-- 2) Danh dau lich hen legacy (migrate, chua co nguoi tao) la "da nhac" de khong gui ngoai.
--    Dieu kien xac dinh legacy: created_by IS NULL (batch migrate SUNS), chua gui nhac,
--    con hieu luc va o tuong lai.
UPDATE diab_his_sch_appointments
   SET reminder_sent_at = NOW(),               -- chong AppointmentReminderNotifyJob gui lai
       updated_at       = NOW()
 WHERE reminder_sent_at IS NULL
   AND created_by IS NULL                       -- nguon migrate (khong phai le tan tao)
   AND deleted_at IS NULL
   AND status IN ('PENDING','CONFIRMED')
   AND appointment_at > NOW();

-- Kiem tra sau khi chay (chay tay, khong bat buoc):
--   SELECT notify_status, COUNT(*) FROM diab_his_cli_followup_recall GROUP BY notify_status;
--   SELECT COUNT(*) FROM diab_his_sch_appointments
--     WHERE created_by IS NULL AND appointment_at > NOW() AND deleted_at IS NULL
--       AND reminder_sent_at IS NULL;   -- ky vong: 0
