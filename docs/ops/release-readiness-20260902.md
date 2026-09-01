# Release Readiness — Pro-Diab HIS — 02/09/2026

> Người thực hiện: DevOps (Chương). Base commit trước khi sửa: `e913ae8` (develop).
> Phạm vi: rà soát toàn diện + sửa các lỗi chặn release phát hiện trong quá trình verify. KHÔNG deploy lên server thật — chỉ chuẩn bị + báo cáo.

---

## 1. Trạng thái migrator — root cause + fix + evidence

### 1.1 Vấn đề ban đầu
`prodiab-migrator` restart-loop nhiều giờ liền. Log lặp lại:
```
ERROR 1146 (42S02) at line 17: Table 'prodiab_his.pat_pii_data' doesn't exist
```

### 1.2 2 task nền trước đó — trạng thái thật
Kiểm tra `git log -30` trên `develop`: không có commit nào liên quan `DrugsController.Import` hay `pat_pii_data`/migrator idempotency. Tìm thấy 2 worktree riêng biệt còn uncommitted, chưa merge:
- `.claude/worktrees/compassionate-sutherland-351efd` (base cũ, đã lạc hậu 7 commit so với develop) — có sửa `db/migrations/0000_helpers.sql` tương tự nhưng KHÔNG hoàn chỉnh (chưa xử lý case bảng đã bị `9000_drop_legacy` xoá vẫn còn raw SQL không qua helper), còn WIP dở dang.
- `.claude/worktrees/sad-antonelli-014074` (branch portal, xa base) — có sửa 1 dòng `DrugsController.cs` thêm `[Consumes("multipart/form-data")]`, đúng hướng nhưng chưa đủ (xem 1.4).

Kết luận: cả 2 task đều chưa xong và chưa mergeable trực tiếp (base lệch, nội dung dở dang). Đã tự sửa lại từ đầu trực tiếp trên `develop`, không cherry-pick nguyên trạng.

### 1.3 Root cause thật của restart-loop (2 lớp)
1. `add_col_if_missing`/`add_index_if_missing`/`add_unique_index_if_missing` (trong `db/migrations/0000_helpers.sql`) không kiểm tra bảng đích có tồn tại trước khi ALTER TABLE — khi migration 0002/0003/0019 chạy lần 2 (container restart) trên DB đã qua `9000_drop_legacy.sql` (xoá toàn bộ bảng legacy short-name để chuyển sang schema `diab_his_*`), các bảng như `pat_pii_data`, `sec_roles`... không còn tồn tại → lỗi 1146/1347 → script `set -e` dừng → container exit non-zero → Docker `restart: unless-stopped` khởi động lại → lặp vô hạn từ đầu.
2. Nguyên nhân sâu hơn (không chỉ riêng `pat_pii_data`): `ops/scripts/apply-migrations.sh` chạy lại TOÀN BỘ 215 file migration mỗi khi container restart, không có cơ chế theo dõi migration đã áp dụng. Bất kỳ container restart nào sau lần deploy đầu tiên (crash, host reboot, `docker compose up` lại) đều kích hoạt lại toàn chuỗi migration trên DB đã ở trạng thái khác (bảng legacy đã bị xoá, có thể đã là VIEW) → chắc chắn lỗi.

### 1.4 Các fix đã áp dụng (commit trên develop)
- `db/migrations/0000_helpers.sql`: thêm guard `TABLE_TYPE = 'BASE TABLE'` cho cả 3 stored procedure (`add_col_if_missing`, `add_index_if_missing`, `add_unique_index_if_missing`) — bỏ qua êm (log `[SKIP ...]`) nếu bảng đích không tồn tại hoặc đã là VIEW, thay vì lỗi cứng.
- `db/migrations/9000_drop_legacy.sql`: loại trừ `_schema_migrations` khỏi vòng lặp xoá bảng legacy (bảng tracking mới, xem dưới).
- `ops/scripts/apply-migrations.sh`: thêm bảng `_schema_migrations (filename PK, applied_at)` — mỗi file migration chỉ áp dụng 1 lần duy nhất, ghi nhận sau khi chạy thành công; lần chạy sau (restart) sẽ bỏ qua toàn bộ file đã áp dụng thay vì chạy lại. Đây là fix triệt để cho root cause restart-loop, không phụ thuộc vào việc từng file trong 215 file có idempotent tuyệt đối hay không.
- `backend/src/ProDiabHis.Api/Controllers/DrugsController.cs` + `BankStatementsController.cs`: bỏ `[FromForm]` khỏi tham số `IFormFile` (giữ `[FromForm]` cho các tham số form khác) + thêm `[Consumes("multipart/form-data")]` — đây là fix đúng theo khuyến nghị Swashbuckle (chỉ thêm `[Consumes]` không đủ, đã verify thật bằng lỗi 500 lặp lại ở `BankStatementsController` sau khi fix `DrugsController`).
- `backend/src/ProDiabHis.Api/Program.cs`: thêm `GET /healthz` — phát hiện gap R-6 (xem mục 2).

### 1.5 Evidence chạy sạch từ DB rỗng (đã test thật, không suy đoán)
- Xoá volume dev-only `prodiab-dev_mysql-data` (định nghĩa trong `ops/docker-compose.yml`, project `prodiab-dev`, tách biệt hoàn toàn khỏi mọi volume có dữ liệu thật/production) — đã xác nhận rõ ràng đây là volume an toàn trước khi xoá.
- Lần chạy 1 (DB trống hoàn toàn): `docker compose up -d migrator` → log 215/215 file `Applying migration`, 0 dòng ERROR, container `Exited (0)`.
- Lần chạy 2 (giả lập restart ngay sau đó, DB đã có dữ liệu + đã qua `9000_drop_legacy`): 215/215 file báo `Bo qua (da apply truoc do)`, 0 dòng ERROR, container `Exited (0)` trong vài giây.
- Lần chạy 3 (giả lập restart lần nữa): kết quả giống lần 2 — ổn định, không restart-loop.
- Khởi động lại `backend`/`frontend` trên DB sạch này → smoke test full (mục 4).

### 1.6 Giới hạn đã biết (ghi nhận minh bạch)
- `_schema_migrations` track theo tên file, không theo nội dung/checksum. Nếu sau này sửa nội dung 1 file migration đã từng chạy trên server thật (đặc biệt `0000_helpers.sql`), file đó sẽ không tự chạy lại vì đã có record. Đây là hành vi chuẩn của migration runner, nhưng cần lưu ý: mọi thay đổi kiểu hạ tầng như 2 fix trong `0000_helpers.sql`/`9000_drop_legacy.sql` hôm nay chỉ có hiệu lực đầy đủ trên deploy đầu tiên (DB trống) — không áp dụng ngược cho DB nào đã từng chạy migrator cũ (chưa có, vì server thật chưa go-live).
- Bảng `pat_pii_data` (và ~48 bảng legacy short-name khác) bị `9000_drop_legacy.sql` xoá theo đúng thiết kế có chủ đích (chuyển sang schema `diab_his_*`) — đã grep toàn bộ `backend/src`, xác nhận không có code nào còn tham chiếu `pat_pii_data`. Không phải bug, không cần khôi phục.

---

## 2. Đối chiếu Release Checklist R-1 → R-11 (trạng thái thật)

| # | Việc | Trạng thái thật (02/09/2026) |
|---|---|---|
| R-1 | Sinh mới secret prod (JWT/Encryption/DB) | ⚠️ Chưa làm (đúng như thiết kế — đây là việc làm tại thời điểm deploy thật). `.env`/`appsettings.Development.json` hiện dùng giá trị dev (`root_dev`...). Chưa thấy script sinh secret prod riêng trong repo — cần tạo/ghi rõ quy trình khi deploy thật (xem mục 5). |
| R-2 | Backup 2 khoá mã hoá (MasterKey, BlindIndexKey) | ⚠️ Chưa làm — phụ thuộc R-1, chỉ thực hiện được khi có secret prod thật. |
| R-3 | `Minio:PublicEndpoint` domain thật | ⚠️ Chưa cấu hình — hiện là giá trị dev/localhost. Việc set domain thật thuộc bước deploy. |
| R-4 | HTTPS/TLS + HSTS/CSP | ⚠️ Chưa xác nhận trên server thật (chưa có server thật). `ops/docker-compose.prod.yml` có service `nginx` sẵn, cần cấu hình cert lúc deploy. |
| R-5 | Backup MySQL + restore thử | ⚠️ Chưa làm — cần server thật để test restore trên môi trường riêng. |
| R-6 | Docker healthcheck cho service chính | ✅ Đã đủ + vá 1 gap phát hiện hôm nay. `docker-compose.prod.yml` đã có healthcheck mysql/redis/minio/backend/frontend/nginx. Phát hiện: endpoint `/healthz` backend gọi trong healthcheck chưa từng tồn tại (404) → đã thêm `MapGet("/healthz")`, verify thật 200. |
| R-7 | `backfill-bidx` chạy được | ✅ Xác nhận lệnh tồn tại trong `Program.cs` (`args[0] == "backfill-bidx"`), build thành công, sẵn sàng chạy khi có server thật. Chưa chạy trên data thật (chưa có). |
| R-8 | `.env` monitoring SMTP thật | ⚠️ Chưa điền — việc làm tại thời điểm deploy. |
| R-9 | Reverse proxy + auth cho Grafana | ⚠️ Chưa xác nhận đã cấu hình — cần kiểm tra `ops/monitoring/docker-compose.yml` lúc deploy thật lên domain public. |
| R-10 | OCR build + verify Linux thật | ✅ Giữ nguyên như audit 30/08 — đã build lại Docker image backend hôm nay (không cache liên quan), build thành công, không phát sinh vấn đề mới với Tesseract/libleptonica. |
| R-11 | Hangfire nghe đủ queue default/bhyt/ocr | ⚠️ Code không đổi so với audit 30/08 (đã re-verify bằng build lại solution) — vẫn cấu hình đúng. Việc kiểm tồn đọng job (`hangfire_JobQueue`) là thao tác PHẢI làm trên server thật sau deploy, dữ liệu dev không đại diện. |

Tổng kết R-1→R-11: không có mục nào thiếu code. R-6/R-10 đã đủ. R-7 sẵn sàng chạy. Còn lại R-1,2,3,4,5,8,9,11 là hành động vận hành PHẢI làm tại thời điểm deploy thật (secret, DNS/TLS, backup thật, SMTP, Grafana auth, kiểm queue) — đúng bản chất của các mục này, không phải thiếu sót code.

---

## 3. Kết quả build/test/tsc cuối cùng

| Bước | Lệnh | Kết quả |
|---|---|---|
| Backend build | `dotnet build backend/src/ProDiabHis.Api/ProDiabHis.Api.csproj -c Release` | Build succeeded, 0 Error(s), 8 warning có sẵn từ trước (nullable/unused field, không liên quan thay đổi hôm nay) |
| Unit tests | `dotnet test backend/tests/ProDiabHis.UnitTests` | Passed: 965/965, 0 Failed |
| Architecture tests | `dotnet test backend/tests/ProDiabHis.ArchitectureTests` | Passed: 7/7, 0 Failed |
| Integration tests | `dotnet test backend/tests/ProDiabHis.IntegrationTests` (Testcontainers MySQL riêng, không đụng DB dev) | Passed: 1193/1193, 0 Failed, 2m16s |
| Tổng backend test | | 965 + 7 + 1193 = 2165/2165 pass — khớp baseline "2165+" nêu trong nhiệm vụ |
| Frontend typecheck | `npx tsc --noEmit` (thư mục frontend/) | 0 lỗi |
| Docker build backend | `docker compose build backend` (Dockerfile riêng, context repo root theo đúng quy ước) | Build thành công, không cache liên quan đến layer code |
| Docker build frontend | `docker compose build frontend` | Build thành công |

---

## 4. Kết quả smoke test trên DB sạch (migrate từ volume rỗng)

Toàn bộ chạy sau khi: xoá `prodiab-dev_mysql-data` → `docker compose up -d mysql` (healthy) → `docker compose up -d migrator` (215/215 OK, exit 0) → build lại + start backend/frontend từ image mới.

| Kiểm tra | Kết quả |
|---|---|
| `GET /healthz` | 200 `{"status":"ok"}` (mới thêm — trước đó 404) |
| `GET /swagger/v1/swagger.json` | 200 (trước đó 500 do `BankStatementsController.Import`/`DrugsController.Import` — đã fix, xem 1.4) |
| `GET /swagger/index.html` | 200 |
| `POST /api/v1/auth/login` (`bacsi.test@prodiab.test` / `Test@123`, seed từ `9137_seed_test_login_users.sql`) | 200, trả `accessToken` + đầy đủ `permissions` theo role `bac_si` |
| `GET /api/v1/patients` (kèm Bearer token) | 200 |
| `POST /api/v1/drugs/import` (multipart thật, không quyền `drug.import`) | 403 `PERMISSION_DENIED` — xác nhận model binding multipart hoạt động đúng (không còn lỗi binding/500), request đến đúng handler và bị chặn bởi RBAC như thiết kế |
| `GET /` (frontend Next.js) | 307 (redirect — hành vi bình thường của middleware auth Next.js khi chưa có session cookie) |

Không phát hiện lỗi 5xx nào trong toàn bộ smoke test.

---

## 5. Việc phải làm thủ công tại thời điểm deploy thật lên server thật

Không tự thực hiện các việc dưới đây — đây là checklist cho user/devops khi deploy thật:

1. R-1: Sinh mới toàn bộ secret prod: `JWT_SECRET`, `Encryption:MasterKey`, `Encryption:BlindIndexKey`, mật khẩu MySQL root/app/MinIO/Redis — không tái sử dụng giá trị dev (`root_dev`...) hiện có trong `docker-compose.yml`/`appsettings.Development.json`.
2. R-2: Backup 2 khoá mã hoá (`MasterKey`, `BlindIndexKey`) vào vault/password manager riêng cho hạ tầng, tách khỏi backup DB thường.
3. R-3: Set `Minio:PublicEndpoint` = domain MinIO thật trên server.
4. R-4: Cấu hình HTTPS/TLS (Let’s Encrypt hoặc cert mua), xác nhận Nginx enforce HTTPS + HSTS/CSP header (service `nginx` đã có sẵn trong `ops/docker-compose.prod.yml`, cần điền cert thật).
5. R-5: Chạy backup MySQL + restore thử trên môi trường riêng, xác nhận dữ liệu khôi phục đúng.
6. R-7: Sau khi migrate DB đích lần đầu, chạy 1 lần: `dotnet run --project backend/src/ProDiabHis.Api -- backfill-bidx [tenantId]` để backfill blind-index CCCD/SĐT cho dữ liệu cũ (nếu import dữ liệu có sẵn).
7. R-8: Điền SMTP thật vào `ops/monitoring/.env` để Alertmanager gửi được email cảnh báo.
8. R-9: Đặt reverse proxy + xác thực trước Grafana (hiện public thẳng port nếu không cấu hình).
9. R-11: Sau khi hệ thống chạy một thời gian có nghiệp vụ BHYT, kiểm bảng `hangfire_JobQueue` xác nhận không tồn đọng job ở queue `bhyt`/`ocr`.
10. Xác nhận DNS trỏ đúng domain thật (`his.atds.com.vn`/tương tự) trước khi mở cho user.
11. Set `NEXT_PUBLIC_TEST_LOGIN_PANEL` = false/không set trên build production (panel đăng nhập nhanh với tài khoản test `Test@123` chỉ dùng dev/QC — xem `9137_seed_test_login_users.sql`, KHÔNG được bật trên production).
12. Kiểm tra biến build-arg `NEXT_PUBLIC_API_BASE_URL` khi build frontend image cho server thật (đã từng là gotcha ghi trong memory deploy).

---

## 6. Ghi chú bổ sung (không chặn release, đã rà soát theo yêu cầu)

- 7 file evidence binary (`docs/qc/evidence-*`) đổi ngoài ý muốn nhiều lần trong phiên làm việc (do các lần chạy test/QC ghi đè, size byte không đổi) — đã `git checkout --` revert về bản gốc committed, working tree sạch. Không commit lại các file này.
- TODO/FIXME: rà `backend/src/ProDiabHis.Infrastructure/Reports/ReportRegistry.cs` có nhiều comment `TODO schema: ...` — đây là các ghi chú có chủ đích, đã review trong commit `e913ae8` ("HOAN TAT"), giải thích rõ khi thiếu bảng nghiệp vụ thì report trả về tập rỗng an toàn theo tenant thay vì lỗi 500. Không phải TODO bỏ dở nghiêm trọng, không chặn release.
- File `docs/qc/ute-his-core-20260829-retest.md` (untracked, có sẵn từ đầu phiên) không thuộc phạm vi nhiệm vụ này — không động vào.
- Còn 2 worktree WIP chưa merge (`compassionate-sutherland-351efd`, `sad-antonelli-014074`) chứa các thay đổi KHÔNG liên quan migrator/DrugsController (portal Home UI, master-data admin UI...) — không đụng tới, ngoài phạm vi.

---

## 7. Kết luận

SẴN SÀNG RELEASE (về mặt code/build/test) — với điều kiện toàn bộ checklist mục 5 (R-1,2,3,4,5,8,9,11 + DNS/build-arg/test-login-panel) được thực hiện đầy đủ TRƯỚC khi mở cho user thật trên server thật, theo đúng nguyên tắc "deploy là hành động khó đảo ngược, cần xác nhận riêng".

Lý do đủ điều kiện:
- Migrator không còn restart-loop, đã verify 3 lần chạy liên tiếp trên DB sạch (bao gồm mô phỏng restart) — 0 lỗi.
- 2 lỗi 500 thật (Swagger do `DrugsController`/`BankStatementsController.Import`) đã fix và verify bằng gọi API thật, không suy đoán.
- 1 gap thật phát hiện thêm (thiếu endpoint `/healthz` dù compose healthcheck đã cấu hình gọi nó) đã vá và verify.
- Build backend + frontend 0 lỗi, Docker image build sạch từ đầu.
- 2165/2165 test pass (965 unit + 7 architecture + 1193 integration), tsc 0 lỗi.
- Smoke test đầy đủ trên DB hoàn toàn mới (login, patients, swagger, healthz, multipart upload binding) đều đúng như kỳ vọng, không có lỗi 5xx.

Chưa sẵn sàng ở khía cạnh hạ tầng thật (bình thường, vì chưa deploy thật lần nào) — toàn bộ liệt kê rõ trong mục 5, không phải thiếu code.
