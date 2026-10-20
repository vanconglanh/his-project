# Hướng dẫn deploy PRODUCTION — fix "Data truncated" + cổng tắt nhắc bệnh nhân

> Trạng thái: **CHƯA deploy production** (theo yêu cầu, chỉ staging đã làm). Tài liệu này để chạy sau.
> Nguyên tắc an toàn: **BẬT cổng tắt `PatientNotifications__Enabled=false` TRƯỚC**, rồi mới áp migration 9222.
> Lý do thứ tự: nếu áp 9222 trước khi deploy code có cổng tắt, backend CŨ sẽ INSERT thông báo thành công
> và có thể GỬI email/push thật cho bệnh nhân (kênh gửi chạy trước INSERT trong `PatientNotifyService`).

Branch/commit: `test/lark-full-stack` @ `c1fc628` (fix nằm trong commit này).
Server: VNG, SSH `ssh -p 234 -i D:/diab/_Azure/diab-server.pem stackops@192.168.1.4`.
Stack: `/srv/apps/pro/his` (compose `docker-compose.yml`, image `prodiab-his-backend:${PRO_HIS_TAG}`).

## 0. Dữ liệu production đã khảo sát (2026-10-20, read-only)
- `diab_his_nti_notifications`: shape legacy `id INT` (bảng RỖNG 0 row) → nguồn bug.
- `diab_his_cli_followup_recall`: 50 recall `notify_status=FAILED` (PENDING 49 + CONTACTED 1), sinh 03:30 bởi `ChronicCareRecallJob` từ bệnh nhân tiểu đường legacy.
- `diab_his_sch_appointments`: 11 lịch hẹn tương lai (2026-10-20..2027-03-25), đều legacy (`created_by IS NULL`, batch migrate 2026-10-17 04:01:09, `reminder_sent_at IS NULL`).

## 1. Build image (nếu chưa có trên server)
```bash
# Local, từ HEAD test/lark-full-stack (đã có commit fix):
TS=$(date +%Y%m%d-%H%M)       # vd prod-20261020-1112
docker build -t prodiab-his-backend:prod-$TS ./backend
docker save -o his-backend-prod-$TS.tar prodiab-his-backend:prod-$TS
scp -P 234 -i D:/diab/_Azure/diab-server.pem his-backend-prod-$TS.tar stackops@192.168.1.4:/tmp/
ssh ... "docker load -i /tmp/his-backend-prod-$TS.tar"
```
> Image `prod-20261020-1112` đã build local trong phiên này (chưa transfer lên server).

## 2. Backup DB production
```bash
ssh ... "/srv/apps/pro/his/backup-pro-his.sh"   # -> /home/stackops/backups/pro-his/*.sql.gz
```

## 3. Bật cổng tắt + deploy backend MỚI (gate chặn gửi)
```bash
cd /srv/apps/pro/his
cp .env .env.bak.before-notify-$(date +%Y%m%d-%H%M)
# 3a. Thêm cổng tắt vào backend.env (KHÔNG đụng backend.sso.env / JWT / Encryption keys)
grep -q '^PatientNotifications__Enabled=' backend.env || echo 'PatientNotifications__Enabled=false' >> backend.env
# 3b. Lưu tag hiện tại để rollback
#     PRO_HIS_TAG hiện = prod-20261017-1605 (image id 1df2596...). Đặt alias prev:
docker tag prodiab-his-backend:prod-20261017-1605 prodiab-his-backend:prev-20261017-1605
# 3c. Đổi PRO_HIS_TAG -> tag mới trong .env
sed -i 's/^PRO_HIS_TAG=.*/PRO_HIS_TAG=prod-'"$TS"'/' .env
# 3d. Recreate CHỈ backend (env_file tự nạp lại backend.env + backend.sso.env)
docker compose up -d --no-deps --force-recreate backend
docker restart pro-his-nginx
```

## 4. Áp migration 9222 + data remediation (sau khi gate đã active)
```bash
scp ... db/migrations/9222_fix_nti_notifications_id_charid.sql stackops@192.168.1.4:/tmp/
scp ... docs/migration/20261020_prod_notify_legacy_remediation.sql stackops@192.168.1.4:/tmp/
PW=$(grep '^PRO_HIS_DB_PASS=' /srv/apps/pro/his/.env | cut -d= -f2-)
# 4a. migration schema (idempotent)
docker exec -i -e MYSQL_PWD=$PW pro-his-db mysql --default-character-set=utf8mb4 -uprodiab prodiab_his < /tmp/9222_fix_nti_notifications_id_charid.sql
docker exec -e MYSQL_PWD=$PW pro-his-db mysql -uprodiab prodiab_his -e \
  "INSERT INTO _schema_migrations(filename,applied_at) SELECT '9222_fix_nti_notifications_id_charid.sql',NOW() WHERE NOT EXISTS (SELECT 1 FROM _schema_migrations WHERE filename='9222_fix_nti_notifications_id_charid.sql');"
# 4b. data remediation (idempotent): reset recall FAILED->NULL + đánh dấu legacy appt reminder_sent_at
docker exec -i -e MYSQL_PWD=$PW pro-his-db mysql --default-character-set=utf8mb4 -uprodiab prodiab_his < /tmp/20261020_prod_notify_legacy_remediation.sql
```

## 5. Verify
```bash
docker exec pro-his-backend sh -c 'curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/healthz'   # 200
curl -s -o /dev/null -w "%{http_code}" https://his.diab.com.vn/healthz                                      # 200
docker inspect pro-his-backend --format '{{range .Config.Env}}{{println .}}{{end}}' | grep -E 'Sso__Enabled|PatientNotifications'  # Sso__Enabled=true, PatientNotifications__Enabled=false
# SSO giữ phiên: không mất session (chỉ recreate backend, giữ JWT secret/issuer/audience + backend.sso.env)
# Theo dõi log 1-2 chu kỳ job (recall 08:00, appointment reminder mỗi giờ):
docker logs pro-his-backend --since 2h 2>&1 | grep -iE 'truncat|PatientNotifications dang TAT|Data truncated'
#  -> KỲ VỌNG: KHÔNG còn "Data truncated"; thấy log info "PatientNotifications dang TAT - bo qua..."
```

## 6. Rollback (nếu cần)
- Backend: `sed -i 's/^PRO_HIS_TAG=.*/PRO_HIS_TAG=prod-20261017-1605/' .env && docker compose up -d --no-deps --force-recreate backend && docker restart pro-his-nginx`.
- DB: migration 9222 là additive/an toàn (bảng rỗng), **không cần rollback**. Nếu vẫn muốn, restore từ backup bước 2.

## 7. Bật lại nhắc khi GO-LIVE
1. Sửa `backend.env`: `PatientNotifications__Enabled=true` (hoặc xoá dòng này để về mặc định true).
2. `docker compose up -d --no-deps --force-recreate backend && docker restart pro-his-nginx`.
3. Lưu ý sau khi bật:
   - Recall đang `notify_status=NULL` (PENDING) sẽ được gửi ở lần job kế tiếp (08:00).
   - Lịch hẹn legacy đã đặt `reminder_sent_at` nên **sẽ KHÔNG bị nhắc lại** (tránh nhắn trùng với SUNS). Chỉ lịch hẹn MỚI (do lễ tân tạo, `created_by != NULL`) được nhắc.
   - Nếu muốn bật nhắc cho một số recall ngay, kiểm tra danh sách worklist recall trước khi go-live.
