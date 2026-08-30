# Go-live readiness — Pro-Diab HIS (audit 2026-08-30, nhánh `develop`)

## KẾT LUẬN (cập nhật 2026-08-30 sau fix): 2 P0 ĐÃ FIX ✅ — còn P1 #3 + P0 vận hành #4

**2 P0 blocker (#1 thực thi 2FA, #2 backfill `id_number_bidx`) đã được sửa và verify thật** (chi tiết cách verify ở mục 3). Còn lại **P1 #3 (Minio PublicEndpoint rỗng)** và **P0 vận hành #4 (checklist sinh secret khi deploy)** CHƯA làm — vẫn cần trước khi go-live thật lên server production, nhưng **không chặn tiếp tục dev/test**.

> Lịch sử: bản audit gốc kết luận 🟡 BLOCK vì 2 lỗi bảo mật/dữ liệu. Hệ thống đủ chức năng vận hành; 2 lỗi đó nay đã khắc phục.

Tin tốt trước: **12/12 hạng mục chọn ngẫu nhiên để tự kiểm đều khớp 100% với báo cáo trước đó.** Các agent báo cáo trung thực ở đợt này — không lặp lại sai sót của vòng PO review trước (báo migration "chưa chạy" trong khi thực tế đã áp).

## 1. Đối chiếu ngẫu nhiên 12 hạng mục ✅ Done (bằng chứng thật, tự query DB/grep code — không tin lời báo cáo cũ)

| Hạng mục | Cách kiểm | Kết quả |
|---|---|---|
| C-1/C-2 DROP bảng chết | `information_schema` DB thật | ✅ 2 bảng đã biến mất; `cli_lab_orders` (18 dòng)/`cli_rad_orders` (1) còn sống |
| C-1/C-2 không còn tham chiếu | grep toàn repo | ✅ Chỉ còn trong comment, migration lịch sử (9004/9020/9084/9085) và test canh gác `DroppedLegacyTablesGuardTests`. Replay DB trắng vẫn an toàn (9171 `DROP IF EXISTS` chạy sau 9004) |
| L-1 master data thuốc | Query cột | ✅ `route varchar(30)`, `bhyt_code varchar(50)` |
| L-3 EMR snapshot v2 | Query cột | ✅ `emr_templates.structured_json`, `emr_versions.{template_id,structured_values_json,schema_snapshot_json}` |
| M-1 giá/ẩn hiện chi nhánh | Query | ✅ `service_branch_prices.is_active` + bảng `pha_drug_branch_prices` |
| E/Đợt4 công nợ nội bộ (9174) | Query | ✅ `bil_inter_branch_debts` |
| E/Đợt5 BHYT per chi nhánh (9175/9176) | Query | ✅ đủ `hospital_rank/kcb_tuyen/bhyt_contract_*/bhyt_enabled/dtqg_enabled/cskcb_code/status` + `clinic_internal_referrals` |
| J-1 InBody (9173) | Query | ✅ `cli_inbody_report` + `cli_indicator_reading` |
| H-8 ICD telehealth (9170) | Query | ✅ `tel_allowed_icd10` |
| RBAC 10 quyền mới | Query `sec_permissions` | ✅ đủ 10/10 |
| **F/Đợt2 thu hồi quyền SoD** | Query `role_permissions` | ✅ trả về **rỗng** — `bac_si` hết `cls_round.pay/waive`, `duoc_si` hết `stock.adjust`/`report.build` |
| H-3 report lọc chi nhánh | Đọc `ReportRegistry.cs` | ✅ `BranchSql.Condition(...)` phủ toàn bộ descriptor (20 file dùng `BranchSql`) |

## 2. Build & Test — số liệu THẬT đo hôm nay

- `dotnet build`: **0 error**, 13 warning (nullable + field không dùng, lành tính)
- `dotnet test`: **902 pass / 0 FAIL / 0 SKIP** (Unit 891 + Arch 6 + Integration 5) — khớp chính xác số TASKLIST công bố; đáng chú ý **0 test bị skip**
- `npx tsc --noEmit`: **sạch**, exit 0

## 3. Phát hiện MỚI (chưa báo cáo nào nêu trước đây)

### ✅ #1 — P0 Blocker (bảo mật): 2FA không được thực thi ở backend — ĐÃ FIX (2026-08-30)

**Đã sửa:** login bước 1 (email+password đúng) với user `TwoFaEnabled=true` KHÔNG còn cấp `AccessToken` đầy đủ — chỉ cấp `mfaPendingToken` (aud=`mfa-pending`, TTL 5 phút). Thêm endpoint `POST /api/v1/auth/2fa/verify` nhận `mfaPendingToken` + mã TOTP 6 số (hoặc recovery code), verify đúng mới cấp `AccessToken`/`RefreshToken` đầy đủ; sai mã → `AUTH_MFA_INVALID_CODE`, có rate-limit chống brute-force (`AUTH_MFA_TOO_MANY_ATTEMPTS`, 5 lần/5 phút qua `IRateLimiter`). Role bắt buộc 2FA (`Security:MandatoryMfaRoles`) mà chưa bật → CHẶN token đầy đủ, chỉ cấp `mfaSetupToken` (aud=`mfa-setup`) dùng được duy nhất cho `me/2fa/setup`/`enable`. Token tạm có `aud` khác nên bị scheme Bearer mặc định từ chối ở mọi API nghiệp vụ. Frontend: thêm màn nhập TOTP trong luồng login.
**Fix phụ (chặn 2FA hoạt động):** cột `two_fa_recovery_codes` kiểu JSON không lưu được ciphertext → migration `9186` đổi sang TEXT (trước đó `me/2fa/enable` luôn 500).
**Verify thật:** API test 5 bước (bacsi.test bật 2FA → chặn/verify sai/verify đúng; letan.test không 2FA vẫn login thường; qc.admin role bắt buộc → mfaSetupToken) + browser test end-to-end. Evidence: `docs/qc/evidence-p0-golive-fix-20260830/P0-1-2fa-api-test.md` + `P0-1-2fa-browser-test.md`. `dotnet test` 901 pass.

<details><summary>Mô tả lỗi gốc (đã khắc phục)</summary>
`backend/src/ProDiabHis.Application/Auth/LoginCommandHandler.cs:111-140` cấp token đầy đủ rồi chỉ trả một lá cờ:
```csharp
var mfaSetupRequired = isMandatoryMfaRole && !user.TwoFaEnabled;
// khong chan, van tra AccessToken day du + MfaSetupRequired: true
```
Hai lỗ hổng:
- **(a)** Role bắt buộc 2FA chưa bật → vẫn nhận token hợp lệ; việc ép thiết lập **chỉ ở frontend** → bypass được bằng gọi thẳng API.
- **(b)** Nghiêm trọng hơn: **kể cả `TwoFaEnabled = true`, login vẫn KHÔNG hỏi mã TOTP.** grep Controller/Auth chỉ có `me/2fa/setup|enable|disable` — **không có endpoint verify lúc đăng nhập**. 2FA hiện chỉ mang tính trang trí; mật khẩu lộ = mất tài khoản admin.

Vi phạm trực tiếp `CLAUDE.md`: *"RBAC enforce ở backend (không chỉ frontend hide)"*. TASKLIST H-10 có ghi nhận gap này nhưng vẫn đánh ✅ Done và coi là "ngoài phạm vi". Về QC, hạng mục bảo mật không thực thi được thì không tính là Done.

</details>

### ✅ #2 — P0 Blocker (dữ liệu): tra cứu theo CCCD trượt toàn bộ hồ sơ cũ — ĐÃ FIX (2026-08-30)

**Đã sửa:** viết console command chạy-một-lần `dotnet run --project backend/src/ProDiabHis.Api -- backfill-bidx` (tái dùng `PiiBackfillService`, idempotent, mọi tenant), giải mã `*_enc` → tính lại blind index → ghi `*_bidx`.
**Bug ẩn phát hiện khi verify:** `id_number_enc`/`card_no_enc` lưu RAW (không tiền tố `enc:v1:`), backfill cũ dùng `_pii.Unprotect` → trả nguyên ciphertext → bidx = hash(ciphertext) ≠ hash(CCCD thật) → tìm vẫn trượt dù cột bidx đã có giá trị. Đã sửa `PiiBackfillService.DecryptEnc()` marker-aware; bổ sung backfill `phone_bidx` từ `phone_enc` (plaintext đã bị NULL sau khi mã hoá).
**Runtime key:** `Encryption:BlindIndexKey` trước đó KHÔNG có trong runtime (chỉ ở `.env.example`) → dù backfill xong search vẫn hỏng. Đã set khoá cố định cho dev (appsettings.Development.json, gitignored) + tài liệu vận hành yêu cầu set + backup khoá cho mọi môi trường.
**Verify thật:** DB `bidx_ok = enc_total = 20`; blind index tự tính khớp bidx đã lưu; **gọi `GET /patients/search?q=048172044001` (CCCD bệnh nhân cũ) qua API trả đúng bệnh nhân BNT01000020**. Evidence: `docs/qc/evidence-p0-golive-fix-20260830/P0-2-backfill-bidx.md`. Docs vận hành: `docs/ops/backfill-id-number-bidx.md`. Test hồi quy pass.

<details><summary>Mô tả lỗi gốc (đã khắc phục)</summary>
Hai cột dùng cho hai mục đích khác nhau:
- Kiểm trùng CCCD (I-1) dùng `IdNumberHash` → **20/20 dòng có** ✅
- Tìm kiếm bệnh nhân (`PatientQueryHandler.cs:73`) dùng `IdNumberBidx` → **0/20 dòng có** ❌

```
SELECT SUM(id_number_hash<>''), SUM(id_number_bidx<>'') FROM diab_his_pat_patients;
-- ket qua that: 20 , 0
```
Hậu quả: lễ tân gõ đúng CCCD bệnh nhân cũ → "không tìm thấy" → tạo trùng hồ sơ. Nguyên nhân: `IdNumberBidx` thêm sau, code create/update có ghi cho bản ghi MỚI (`PatientCommandHandler.cs:125,217`) nhưng **thiếu migration backfill dữ liệu cũ**. Lỗi im lặng — không exception, test không bắt được.

</details>

### ⚠️ #3 — P1 (triển khai): `MINIO_PUBLIC_ENDPOINT` không có mặc định
Có mặt đúng trong file prod (`ops/docker-compose.prod.yml:161`, `ops/docker-compose.deploy.yml:67`) — claim này đúng. Nhưng dùng `${MINIO_PUBLIC_ENDPOINT}` **không fallback** và `.env.example:25` để trống. Quên set → chuỗi rỗng; `DependencyInjection.cs:142` dùng `?? minioEndpoint` **chỉ bắt null, không bắt chuỗi rỗng** → presigned URL hỏng, không xem được file CLS/ảnh.

### ⚠️ #4 — P2 (nợ schema): nghi có bảng EMR chết
DB tồn tại song song `diab_his_cli_emr_content` **và** `diab_his_cli_emr_contents`, cộng `diab_his_enc_emr_contents`. Cùng loại vấn đề với C-1/C-2 vừa dọn. Cần điều tra, **không drop vội**.

## 4. Các trục rủi ro đã ĐẠT (có bằng chứng)

- **Mã hoá PII**: DB thật cho `id_number_enc = x095vQLKE+UwSjj4Buiw3Z...` (ciphertext), `id_number_masked = 04********01`. Không có CCCD plaintext.
- **Khoá mã hoá**: `.env.example` để **trống** `ENCRYPTION_MASTER_KEY`/`BLIND_INDEX_KEY`; `AesGcmEncryptor` **fail-fast** khi thiếu hoặc ≠32 byte → không thể chạy prod bằng khoá rỗng/dev.
- **Secrets không commit**: `git ls-files ops/.env` rỗng, `.gitignore:35` chặn.
- **Audit VIEW (P0-01, hạn 31/12/2026)**: đang ghi THẬT — VIEW/Patient 33, VIEW/Encounter 22, VIEW/Prescription 7, mới nhất `2026-08-30 08:05:59`.
- **Chữ ký EMR v2 an toàn ngược**: `EmrSignPayload.cs` — bản ghi cũ cả 2 cột NULL → `BuildV1`, giữ nguyên đường verify cũ. Không breaking change.
- **RBAC**: `docs/prd/rbac-doi-chieu-chuan-20260829.md` **không còn mục 🔲 nào**.

## 5. BẮT BUỘC làm trước go-live

| Ưu tiên | Việc | Mức | Giao | Ước tính |
|---|---|---|---|---|
| 1 | Thực thi 2FA backend: token tạm `mfa_pending` → verify TOTP → mới cấp token đầy đủ; chặn role bắt buộc 2FA chưa bật | P0 | backend+frontend | 1–1,5 ngày |
| 2 | Migration backfill `id_number_bidx` (idempotent) + test hồi quy "tìm BN theo CCCD" | P0 | backend | 0,5 ngày |
| 3 | Guard fail-fast khi `Minio:PublicEndpoint` rỗng; điền mẫu `.env.example` | P1 | devops | 1 giờ |
| 4 | Checklist deploy: sinh MỚI `JWT_SECRET`, 2 khoá mã hoá, mật khẩu MySQL/MinIO — tuyệt đối không dùng lại giá trị dev | P0 vận hành | devops | 30 phút |

## 5.1 Bổ sung sau audit (2026-08-30, không phải P0 gốc) — Log tập trung Loki/Grafana

Mục "Monitor: Sentry + Serilog → Loki/Grafana" trong `CLAUDE.md` trước đây CHƯA triển khai (chỉ có
Sentry + Serilog console/file cục bộ, không tập trung). Đã bổ sung trong phiên làm việc này:
- Backend: Console sink Serilog đổi sang JSON (`Serilog.Formatting.Json.JsonFormatter`), enrich thêm
  `UserId/TenantId/UserEmail/RoleCodes` vào mọi dòng request log qua `EnrichDiagnosticContext`.
- Stack `ops/monitoring/` (Loki + Promtail + Grafana, đã có sẵn từ trước nhưng CHƯA BAO GIỜ chạy thử —
  phát hiện và sửa 2 lỗi chặn hoạt động hoàn toàn: Promtail tạo stream 0-label làm Loki từ chối cả
  batch; Grafana crash-loop do bật đồng thời legacy + unified alerting) — đã verify end-to-end bằng
  request thật (login → gọi API → xác nhận log tới Loki → query đúng qua Grafana).
- 3 dashboard: Backend Overview (đã sửa label sai + latency query sai), MySQL Health (kế thừa, chưa
  verify vì không có tình huống lỗi MySQL để test), **User Activity & Product Analytics (mới)** —
  hoạt động theo user/role, top chức năng dùng nhiều nhất, xu hướng theo thời gian, lỗi 4xx/5xx theo
  user+function.
- Còn thiếu trước khi bật trên server production: reverse proxy + auth cho Grafana (hiện publish
  thẳng port `3100` ra host, an toàn cho dev nhưng KHÔNG được mở vậy trên server thật).
- Chi tiết đầy đủ (kiến trúc, cách verify, LogQL mẫu, cách thêm dashboard): xem
  `docs/ops/log-monitoring-loki-grafana.md`.

Đây KHÔNG phải P0 chặn go-live (hệ thống vẫn chạy được không cần Loki/Grafana), nhưng ảnh hưởng trực
tiếp khả năng vận hành/hỗ trợ sau go-live (điều tra sự cố, đối chiếu audit, phân tích UX) nên ghi nhận
ở đây để BO/DevOps biết và lên kế hoạch bật trên server thật kèm phần bảo mật còn thiếu.

## 5.2 Bổ sung sau audit (2026-08-30) — Fix Alertmanager crash-loop, sẵn sàng cảnh báo email

`prodiab_alertmanager` bị crash-loop từ trước (nguyên nhân: `ops/monitoring/alertmanager-config.yml`
dùng cú pháp `${SMTP_HOST:-...}` kiểu docker-compose interpolation, nhưng Alertmanager tự đọc config
của chính nó, KHÔNG chạy qua docker-compose — biến không được thay thế → lỗi parse YAML → crash-loop).

Đã sửa trong phiên này:
- File config đổi thành template (`ops/monitoring/alertmanager-config.template.yml`), được 1 service
  init nhỏ (`alertmanager-config`, image alpine + `envsubst`) render thành `alertmanager.yml` thật
  trước khi Alertmanager khởi động (pattern chuẩn cho Alertmanager, vì bản thân nó không hỗ trợ env
  interpolation trong file config).
- Container **KHÔNG còn crash-loop** ngay cả khi CHƯA điền SMTP thật (mọi biến có default hợp lệ cú
  pháp YAML) — verify thật: `docker compose up -d`, `Up ... (healthy)` ổn định > 2 phút, không restart.
- Thêm rule cảnh báo LogQL thật `HTTP5xxRateHigh` (tỷ lệ log 5xx/tổng request backend > 1% trong 5
  phút) chạy qua Loki ruler (`ops/monitoring/loki-rules/fake/prodiab-alerts.yaml`), route sang email.
- Template email tiếng Việt có dấu (tên dịch vụ, mức độ, nội dung, thời điểm, link Grafana).
- Verify thật luồng gửi email: trỏ SMTP tới MailHog cục bộ (đã có sẵn trong stack dev,
  `ops/docker-compose.yml`), bắn 1 alert giả `HTTP5xxRateHigh` qua API Alertmanager, xác nhận email
  THẬT đã tới MailHog (`http://localhost:8025`) với nội dung tiếng Việt đúng.
- **Chưa test**: rule tự động FIRE từ log 5xx thật phát sinh tự nhiên (chỉ test bằng cách bắn alert
  giả trực tiếp vào API Alertmanager) — không chặn go-live, nhưng nên quan sát thêm khi có traffic thật.

**BO cần làm gì để bật gửi email thật khi deploy** (chỉ sửa `.env`, KHÔNG cần đụng code/YAML):
1. Copy `ops/monitoring/.env.example` → `ops/monitoring/.env`.
2. Điền `SMTP_HOST`, `SMTP_PORT`, `SMTP_FROM`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_REQUIRE_TLS=true`,
   `OPS_ALERT_EMAIL` (email nhận cảnh báo) — có comment hướng dẫn ngay trong `.env.example`.
3. `docker compose -f ops/monitoring/docker-compose.yml up -d alertmanager-config alertmanager`.

Chi tiết đầy đủ: `docs/ops/log-monitoring-loki-grafana.md` mục 8 "Cấu hình cảnh báo qua email".

## 6. Chấp nhận được sau go-live

M-5 snapshot giá thuốc (P2, dịch vụ đã có snapshot); dọn bảng EMR trùng (P2); D-3 refactor 77 file token màu (P2); P2-08 ScopeMode (P2, branch filter đã đủ an toàn); K-4 tour trang còn lại (P2); tách audit cross-branch attempt (P2); xoá `user.read` sau khi FE chuyển `doctors/lookup` (P2); 13 warning build (P3).

## 7. Phụ thuộc bên ngoài — BO cần lên kế hoạch

| Phụ thuộc | Trạng thái | Ảnh hưởng |
|---|---|---|
| **API lộ trình diaB** (L-2) | ⛔ diaB chưa có endpoint | HIS chạy `NullExternalPathwayProvider` → luôn `200/NOT_CONFIGURED/milestones=[]`, **không lỗi, không chặn vận hành**. Màn lộ trình trống. Cắm sau chỉ cần đăng ký DI |
| **Webhook ngân hàng** (H-9 QR VietQR) | Chưa có | Thu ngân phải bấm "Xác nhận đã thanh toán" **thủ công** → rủi ro nhầm/gian lận nội bộ. Ngắn hạn: đối soát cuối ngày |
| **eSMS / Zalo ZNS** (H-1) | Chờ credential thật | Nhắc lịch hẹn không gửi được; nhập qua UI `/admin/notification-channels`, không cần deploy lại |
| **ĐTQG + Giám định BHYT** | Cần `cskcb_code` + token thật/chi nhánh | Guard BR-108 đã chặn khi thiếu → **an toàn, không phát hành XML sai** |
| **7 câu hỏi BO chưa xác nhận** (mục E) | Chạy theo phương án đề xuất | Đặc biệt Q5 (ngưỡng duyệt kho 5tr) và Q7 (bác sĩ xem bệnh án chi nhánh khác) — nên chốt trước khi có dữ liệu thật |
| **Giả định SoD chưa BO duyệt** (F/Đợt2) | Tự quyết theo least-privilege | Giả định bác sĩ KHÔNG thu tiền CLS tại chỗ; có người kiểm kê tách khỏi dược sĩ. Nếu thực tế khác → nhân viên bị 403. Rollback có sẵn trong `9146` |

## Phán quyết

**BLOCK** cho tới khi xong mục 1 và 2 (assign: backend + frontend, có hỗ trợ devops cho mục 3–4). Sau đó → **APPROVE**. Tổng ước tính **1,5–2 ngày công**.

Nhấn mạnh với BO: 2 lỗi này không làm giảm giá trị công sức đã bỏ ra — phần thân hệ thống đã kiểm chứng là chắc chắn và báo cáo trung thực. Đây là 2 điểm rất cụ thể, sửa nhanh, và may mắn được phát hiện **trước** khi có dữ liệu bệnh nhân thật.

### File liên quan
- `backend/src/ProDiabHis.Application/Auth/LoginCommandHandler.cs`
- `backend/src/ProDiabHis.Application/Patients/PatientQueryHandler.cs`
- `backend/src/ProDiabHis.Application/Patients/PatientCommandHandler.cs`
- `backend/src/ProDiabHis.Infrastructure/DependencyInjection.cs`
- `ops/docker-compose.prod.yml`
- `ops/.env.example`
