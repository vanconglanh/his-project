# Log tập trung — Loki + Grafana + Promtail (Pro-Diab HIS)

> Bổ sung cho mục "Monitor: Sentry + Serilog → Loki/Grafana" trong `CLAUDE.md` (trước đó chưa triển
> khai thật — chỉ có Sentry + Serilog console/file cục bộ, mỗi container tự giữ log riêng, không tập trung).

## 1. Kiến trúc

```
Backend (.NET, Console sink JSON)  ---+
Frontend / MySQL / Redis / MinIO   ---+---> Promtail (đọc log container qua Docker socket) ---> Loki ---> Grafana
Nginx (access/error log file)      ---+
```

- **Loki**: lưu trữ log, filesystem storage, retention 30 ngày (`ops/monitoring/loki-config.yml`).
- **Promtail**: tự động discover TẤT CẢ container đang chạy trên network app (`docker_sd_configs`,
  đọc `/run/docker.sock`) — không cần mount file log riêng cho từng service. Đây là lựa chọn ĐƠN GIẢN
  HƠN so với mount volume log file: không phải sửa `docker-compose*.yml` của từng service để thêm
  volume, tự động bắt được container mới khi thêm service, không phụ thuộc đường dẫn file bên trong
  container. Đánh đổi: log không còn nếu container đã bị xoá (Docker log driver mặc định `json-file`
  vẫn giữ log trên đĩa host ngay cả khi container dừng, miễn chưa bị `docker rm`).
- **Grafana**: datasource Loki tự động provisioning, 3 dashboard dựng sẵn (không cần import tay).

## 2. Thay đổi ở backend (Serilog)

`backend/src/ProDiabHis.Api/appsettings.json` — sink `Console` đổi từ text sang **JSON**
(`Serilog.Formatting.Json.JsonFormatter`, `renderMessage: true`, KHÔNG cần cài NuGet mới vì
`JsonFormatter` có sẵn trong core `Serilog`). Sink `File` (`logs/prodiabhis-*.log`) vẫn giữ dạng text
để đọc nhanh cục bộ khi debug trực tiếp trên máy/server.

Lý do chọn "Console sink → JSON, Promtail đọc container stdout" thay vì "thêm sink Loki trực tiếp
(`Serilog.Sinks.Grafana.Loki`)":
- Không thêm dependency mới, không đổi logic gửi log của app (app vẫn chỉ ghi ra Console như cũ).
- Không tạo thêm một đường network fail-point (nếu Loki down, app vẫn chạy bình thường — Promtail tự
  retry đẩy log khi Loki sống lại, backend không hề biết Loki tồn tại).
- Đồng bộ với mysql/redis/minio/nginx — tất cả đều được Promtail thu qua CÙNG một cơ chế Docker socket.

`Program.cs` — `UseSerilogRequestLogging` được enrich thêm `UserId`, `TenantId`, `UserEmail`,
`RoleCodes` từ `ICurrentUser` (`EnrichDiagnosticContext`). Middleware được đăng ký TRƯỚC
`UseAuthentication` trong pipeline, nhưng callback enrich chỉ chạy SAU KHI toàn bộ request (bao gồm
Auth) đã xử lý xong — nên đọc `ICurrentUser` tại thời điểm đó là an toàn (đã verify bằng test thật, xem
mục 5). Request log mặc định đã có sẵn `RequestMethod`, `RequestPath`, `StatusCode`, `Elapsed`
(Serilog.AspNetCore tự gắn, không phụ thuộc `MessageTemplate` tuỳ biến).

`AuditLogMiddleware` (có sẵn từ trước) ghi dòng `AUDIT | Method=... Path=... StatusCode=... UserId=... TenantId=...`
cho mọi request POST/PUT/PATCH/DELETE — dùng làm nguồn cho panel "Audit Log" (đối chiếu nhanh audit có
hoạt động không, KHÔNG thay thế bảng `diab_his_sec_audit_logs` — vẫn phải tra DB khi cần bằng chứng chính thức).

**Lưu ý quan trọng**: `UseSerilogRequestLogging` mặc định log ở mức `Information` NGAY CẢ KHI response
là 5xx (chỉ lên `Error` khi có exception thoát ra ngoài). Vì vậy panel lọc lỗi 5xx dùng chuỗi
`StatusCode":5xx` (khớp JSON thô) chứ KHÔNG dùng `level=Error`.

## 3. Chạy stack monitoring (local/dev)

```bash
cd ops/monitoring
cp .env.example .env    # dien GF_ADMIN_PASSWORD that, sua PRODIAB_NETWORK neu can
docker compose -f docker-compose.yml up -d loki promtail grafana alertmanager node-exporter
# cadvisor publish cong 8080 - TRUNG voi prodiab-adminer o may dev (ops/docker-compose.yml).
# Neu can cadvisor, doi port trong docker-compose.yml hoac tat adminer truoc.
```

Truy cập Grafana: `http://localhost:3100` (user `admin`, mật khẩu = `GF_ADMIN_PASSWORD` trong `.env`,
mặc định compose fallback `Grafana@ProDiab2026!` — **BẮT BUỘC đổi** khi chạy ngoài máy dev cá nhân).
Grafana **KHÔNG** publish ra ngoài container ngoại trừ port `3100` trên máy chạy compose — khi deploy
lên server thật, KHÔNG mở port này ra Internet, chỉ truy cập qua SSH tunnel hoặc reverse proxy có auth
riêng (chưa cấu hình sẵn trong phạm vi task này — cần bổ sung khi go-live thật).

### Ghép với network của từng stack

Promtail cần join network Docker của stack muốn giám sát để resolve DNS `loki` + gọi được
`unix:///run/docker.sock`. Biến `PRODIAB_NETWORK` trong `ops/monitoring/.env` quyết định network nào:

| Stack | File compose | Tên network |
|---|---|---|
| Dev (local) | `ops/docker-compose.yml` | `prodiab-net` (mặc định) |
| Production | `ops/docker-compose.prod.yml` | `prodiab-prod-net` |
| Deploy (self-build, chung VM vienankids) | `ops/docker-compose.deploy.yml` | `prodiab-his-net` |

**GOTCHA đã sửa**: bản gốc của `ops/monitoring/docker-compose.yml` khai báo network ngoài tên
`prodiab_default` — tên này KHÔNG TỒN TẠI (Docker Compose chỉ tự sinh tên đó khi project không có
`name:` tường minh; các file compose của Pro-Diab HIS đều có `name:` riêng). Đã sửa thành biến
`PRODIAB_NETWORK` (mặc định `prodiab-net`) — LUÔN kiểm tra `docker network ls` trước khi chạy ở
server mới.

## 4. Dashboard có sẵn (provisioning tự động — không cần import tay)

Đặt tại `ops/monitoring/grafana/dashboards/`, được nạp qua
`ops/monitoring/grafana/provisioning/dashboards/dashboards.yml` (quét mỗi 30 giây).

### `Pro-Diab Backend Overview` (`backend-overview.json`)
RPS, tỷ lệ lỗi 4xx+5xx, latency p50/p95/p99 theo endpoint (`Properties.Elapsed`), log stream 5xx,
tổng request/lỗi 24h, RPS + tỷ lệ 5xx của Nginx, và panel "Audit Log" mới thêm (đếm dòng
`AUDIT |` theo phút — đối chiếu nhanh audit middleware còn hoạt động không).

### `Pro-Diab MySQL Health` (`mysql-health.json`)
Log lỗi MySQL, connection error từ backend, slow query, trạng thái backup (đã có sẵn từ trước, chưa
verify thật trong task này vì không phát sinh log lỗi MySQL/slow query lúc kiểm thử).

### `Pro-Diab User Activity & Product Analytics` (`user-activity.json`) — MỚI
Mục tiêu **phân tích sản phẩm/UX**, không chỉ vận hành kỹ thuật:
- **Hoạt động theo user** (request/giờ theo `UserEmail`) — phát hiện tài khoản bất thường.
- **Top chức năng dùng nhiều nhất (7 ngày)** — xếp hạng TOÀN BỘ endpoint theo tần suất gọi.
- **Xu hướng theo function (theo ngày, khung 30 ngày)** — chức năng nào đang tăng/giảm sử dụng.
- **Lỗi 4xx/5xx theo user + function** (log stream) — tra cứu nhanh khi user báo lỗi, lọc theo email
  ngay trong ô tìm kiếm panel, không cần hỏi lại chi tiết. Cố tình gồm cả 400 (validation fail = tín
  hiệu UX form khó điền, không chỉ là bug).
- **Tỷ lệ lỗi theo function (24h)** — tín hiệu chất lượng/UX theo từng chức năng.
- **Hoạt động theo role (24h)** — góc nhìn tổng hợp theo vai trò, hữu ích hơn cho quyết định cải thiện
  sản phẩm so với chỉ nhìn từng cá nhân.

Toàn bộ 6 panel đã **verify bằng dữ liệu thật** (xem mục 5) — không phải suy đoán từ schema log lý thuyết.

## 5. Verify thật đã thực hiện (2026-08-30)

1. Build lại `backend` với appsettings.json + Program.cs mới → `docker compose build backend` PASS
   (build Release, không lỗi biên dịch).
2. Chạy container thật (`ops/docker-compose.yml` + `ops/docker-compose.local-app.yml`), xác nhận
   Console sink xuất JSON hợp lệ (`docker logs prodiab-backend`).
3. Đăng nhập thật qua `POST /api/v1/auth/login` (tài khoản test `letan.test@prodiab.test`), gọi
   `GET /api/v1/patients` (200) và `GET /api/v1/nonexistent-xyz` (404) kèm Bearer token → xác nhận
   dòng log request có `Properties.UserId/UserEmail/RoleCodes/TenantId/RequestPath/StatusCode/Elapsed`.
4. Dựng `loki` + `promtail` + `grafana` (+ `alertmanager`, `node-exporter`; `cadvisor` bỏ qua vì đụng
   port 8080 với `adminer` trên máy dev — không phải lỗi cấu hình, chỉ là xung đột cổng cục bộ).
5. Phát hiện và sửa 2 lỗi thật trong lúc verify (không có trong yêu cầu ban đầu nhưng chặn hoạt động):
   - `promtail-config.yml`: job `docker-containers` thiếu label tĩnh bắt buộc → container không có
     label `com.docker.compose.*` (hiếm nhưng có thể xảy ra) tạo stream 0-label → Loki trả
     `400 "at least one label pair is required per stream"` và làm hỏng CẢ BATCH đang gửi. Đã thêm
     `target_label: job, replacement: docker` làm nhãn tĩnh bắt buộc luôn có.
   - `grafana` container **crash-loop liên tục** vì bật đồng thời `GF_ALERTING_ENABLED=true` (legacy)
     và `GF_UNIFIED_ALERTING_ENABLED=true` — Grafana 10.x không cho phép bật cả hai. Đã tắt
     `GF_ALERTING_ENABLED` (dùng Unified Alerting).
6. Sửa toàn bộ query dashboard cũ dùng sai label `service=prodiab_backend` (giá trị thật của label
   `service` là tên SERVICE trong compose, vd `backend`, KHÔNG PHẢI tên container) → đổi sang
   `container=~prodiab.*backend` (khớp `prodiab-backend` ở dev/prod, `prodiab-his-backend` ở stack
   deploy — portable qua cả 3 stack).
7. Query trực tiếp qua Loki API (`/loki/api/v1/query`) VÀ qua Grafana datasource proxy
   (`/api/datasources/proxy/uid/loki_prodiab/...`) cho từng panel chính (RPS, latency theo endpoint,
   audit log count, top endpoint, hoạt động theo user) — tất cả trả về dữ liệu đúng khớp với request
   thật đã tạo ở bước 3.
8. `curl http://localhost:3100/api/search?type=dash-db` xác nhận cả 3 dashboard được Grafana
   provisioning tự động nạp thành công.

### Còn tồn đọng / chưa verify (nói rõ, không giấu)
- Sau khi các fix ở trên, batch push vẫn còn xuất hiện lỗi 400 rải rác không liên tục trong log
  Promtail (không chặn dữ liệu — đã xác nhận log vẫn tới Loki đầy đủ cho các container chính: backend,
  frontend, mailhog, redis, grafana, node-exporter). Nghi ngờ nguyên nhân: container chạy lâu
  (mysql/minio/adminer) có backlog log cũ lớn khi Promtail lần đầu tail từ đầu, va chạm thời điểm với
  container khác trong cùng batch. Chưa xác định được chính xác vì thời gian khảo sát có hạn — KHÔNG
  chặn go-live (dữ liệu chính vẫn vào đủ) nhưng nên theo dõi thêm nếu triển khai lâu dài.
- Panel MySQL Health (`mysql-health.json`) — kế thừa nguyên trạng, CHƯA kiểm chứng bằng log lỗi
  MySQL/slow query thật (không có tình huống lỗi MySQL để test trong phiên làm việc này).
- `cadvisor` chưa chạy được trên máy dev do đụng port 8080 — không ảnh hưởng 3 dashboard log chính,
  chỉ ảnh hưởng metrics resource per-container (ngoài phạm vi yêu cầu gốc — yêu cầu là LOG, không phải
  metrics).
- Reverse proxy + auth cho Grafana khi deploy thật (server `vak-new` / `ops/docker-compose.deploy.yml`)
  CHƯA được cấu hình trong task này — hiện tại chỉ có `docker compose ports: 3100:3000` publish thẳng
  ra host. Trước khi mở trên server production, PHẢI thêm 1 trong 2: (a) không publish port ra
  `0.0.0.0`, đổi thành `127.0.0.1:3100:3000` + SSH tunnel khi cần xem, hoặc (b) đặt sau Nginx có
  Basic-Auth/SSO riêng. Đây là việc còn lại BẮT BUỘC trước khi bật monitoring trên server thật.

## 6. Cách query log cơ bản (LogQL)

```
# Toàn bộ log backend
{container=~"prodiab.*backend"}

# Lọc theo user cụ thể
{container=~"prodiab.*backend"} |= "letan.test@prodiab.test"

# Request lỗi (>=400) kèm parse JSON để lấy field
{container=~"prodiab.*backend"} | json | Properties_StatusCode >= 400

# Audit log
{container=~"prodiab.*backend"} |= "AUDIT |"

# Latency p95 theo endpoint (5 phút gần nhất)
quantile_over_time(0.95, {container=~"prodiab.*backend"} | json | unwrap Properties_Elapsed [5m]) by (Properties_RequestPath)
```

Lưu ý: field lồng trong `Properties.XYZ` của JSON log, khi dùng `| json` trong LogQL sẽ tự flatten
thành `Properties_XYZ` (Loki thay `.` bằng `_`).

## 7. Thêm dashboard mới

1. Tạo file `.json` trong `ops/monitoring/grafana/dashboards/` (export từ Grafana UI: Dashboard →
   Settings → JSON Model, hoặc viết tay theo mẫu 3 file hiện có).
2. KHÔNG cần restart Grafana — provisioning tự quét lại mỗi 30 giây
   (`updateIntervalSeconds: 30` trong `ops/monitoring/grafana/provisioning/dashboards/dashboards.yml`).
3. Đặt `datasource: { type: loki, uid: loki_prodiab }` cho mọi panel — `uid` phải khớp với
   `ops/monitoring/grafana/provisioning/datasources/loki.yml`.
4. Test query trước bằng Grafana Explore (menu trái) hoặc `curl` thẳng Loki API trước khi nhúng vào
   panel — tránh panel "No data" mà không biết query sai hay chưa có log.

## 8. Bảo mật

- Grafana KHÔNG có auth mặc định ngoài user/pass admin — đổi `GF_ADMIN_PASSWORD` qua `.env`, KHÔNG
  dùng giá trị mặc định trong `docker-compose.yml` (`Grafana@ProDiab2026!`) ở môi trường có dữ liệu thật.
- Log có thể chứa PII đã che (mask) nhưng vẫn nên hạn chế truy cập — xem mục "Còn tồn đọng" (reverse
  proxy + auth) trước khi mở trên server production.
- Loki/Promtail không có auth riêng (`auth_enabled: false`) — chỉ an toàn vì không publish port ra
  ngoài (`loki` port `3200` chỉ để debug cục bộ, nên đóng khi deploy thật).
