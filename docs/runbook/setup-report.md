# Runbook — Setup hệ thống Báo cáo & Report Builder

> Đối tượng: DevOps / Admin triển khai. Hướng dẫn cấu hình đầy đủ module **Báo cáo (Report Engine)** + **Trình tạo báo cáo tự phục vụ (Report Builder P1–P3: dataset, dashboard, calc field, chia sẻ role, lịch gửi email)**.
> Hướng dẫn cho NGƯỜI DÙNG cuối: xem `docs/user-guide/08-bao-cao-bi.md`.

---

## 1. Tổng quan kiến trúc

| Thành phần | Vị trí |
|---|---|
| Report Engine (config-driven, ~43 báo cáo hệ thống) | `ProDiabHis.Infrastructure/Reports/ReportRegistry.cs` |
| Report Builder (user tự tạo) | `ProDiabHis.*/Reports/Engine/*` (DatasetRegistry, SafeQueryBuilder, ReportDefinitionStore...) |
| PDF/Excel | QuestPDF + ClosedXML, khung `ReportPdfCommon.cs` (letterhead diaB) |
| Lịch gửi email | Hangfire recurring job `ReportScheduleDispatchJob` + `IEmailSender` (MailKit/SMTP) |
| Endpoint | `ReportsController` — `/api/v1/reports/*` |

**Nguyên tắc bảo mật:** người dùng KHÔNG gõ SQL. Report Builder chỉ cho chọn field từ **Dataset whitelist**; backend sinh SQL tham số hoá, luôn ép `tenant_id`, chống injection.

---

## 2. Yêu cầu trước khi setup

- MySQL 8.0+ (DB `prodiab_his`), Redis 7, .NET 8 runtime.
- Đã chạy schema gốc + migrations nền (patient/encounter/billing/pharmacy...).
- Frontend Next.js build được (`npm run build`).

---

## 3. Áp Migrations

Chạy theo thứ tự số tăng dần (idempotent — chạy lại an toàn). Dùng script `ops/scripts/apply-migrations.sh` hoặc thủ công.

**Migrations dữ liệu nền cho báo cáo** (nếu chưa có):
- `9034_pat_add_patient_source.sql` — cột nguồn khách.
- `9035_create_bil_counters.sql` — quầy thu.
- `9041_tenants_add_slogan_website.sql` — slogan + website cho letterhead.

**Migrations Report Builder (P1–P3):**
| Migration | Nội dung |
|---|---|
| `9055_create_rep_definitions.sql` | Bảng `diab_his_rep_definitions` — báo cáo user tạo |
| `9056_seed_report_build_permission.sql` | Quyền `report.build` + gán role |
| `9058_create_rep_dashboards.sql` | Bảng `diab_his_rep_dashboards` — dashboard tùy biến |
| `9059_rep_definitions_add_shared_roles.sql` | Cột `shared_roles_json` — chia sẻ theo role |
| `9060_create_rep_schedules.sql` | Bảng `diab_his_rep_schedules` — lịch gửi email |

Áp thủ công (WSL/dev):
```bash
export MYSQL_PWD=<db_password>
for f in 9055 9056 9058 9059 9060; do
  mysql -h<host> -u<user> prodiab_his < db/migrations/${f}_*.sql
done
```
Kiểm tra: `SHOW TABLES LIKE 'diab_his_rep_%';` → thấy `rep_definitions`, `rep_dashboards`, `rep_schedules`.

---

## 4. Phân quyền (RBAC)

Permission liên quan báo cáo (đã seed qua migrations):

| Quyền | Ý nghĩa | Cấp cho role (mặc định) |
|---|---|---|
| `report.read` | Xem/tải báo cáo, xem dashboard | admin, bac_si, ke_toan, duoc_si... |
| `report.export` | Kết xuất PDF/Excel | như trên |
| `report.build` | **Tạo/sửa** báo cáo tự phục vụ + dashboard + lịch | admin, ke_toan, bac_si, duoc_si (power user) |

- Muốn thêm/bớt role được tạo báo cáo → sửa migration `9056` (bảng `diab_his_sec_role_permissions`) hoặc gán qua màn Quản trị > Phân quyền.
- **Lưu ý JWT:** tính năng chia sẻ theo role dựa vào claim `role_code` trong JWT. Đảm bảo user **đăng nhập lại** sau khi nâng cấp (token cũ có thể thiếu `role_code`).

---

## 5. Dataset (nguồn dữ liệu cho Report Builder)

4+2 dataset whitelist đã định nghĩa sẵn trong code (`Infrastructure/Reports/DatasetRegistry.cs`):
`thu-ngan` (Thu ngân) · `luot-kham` (Lượt khám) · `kho` (Kho dược) · `don-thuoc` (Đơn thuốc) · `cong-no` (Công nợ) · `cls` (Chỉ định CLS).

**Thêm dataset mới** = việc của DEV (không cấu hình runtime): thêm 1 `Dataset` vào `DatasetRegistry.cs` với base+joins (bake sẵn `COLLATE utf8mb4_unicode_ci` cho join chuỗi khác họ collation) + danh sách field (Dimension/Measure + `AllowedAggregations`). Sau đó rebuild + deploy. Không cần migration.

Kiểm tra: `GET /api/v1/reports/datasets` (kèm JWT có `report.build`) → trả về danh sách dataset + field.

---

## 6. Cấu hình gửi email cho Lịch báo cáo (P3.3)

Lịch gửi email dùng **Hangfire** (recurring job tự đăng ký khi app khởi động, cron `0 * * * *`) + `IEmailSender` (SMTP qua MailKit).

Cấu hình SMTP trong `appsettings.{Environment}.json` section `Smtp`:
```json
"Smtp": {
  "Host": "smtp.yourprovider.com",
  "Port": 587,
  "FromEmail": "no-reply@phongkham.vn",
  "FromName": "Phòng khám ...",
  "UseSsl": true,
  "User": "<smtp_user>",
  "Password": "<smtp_password>"
}
```
- **Dev/test:** dùng MailHog (`Host: localhost`, `Port: 1025`, `UseSsl: false`) để bắt email không gửi thật.
- File đính kèm (PDF/Excel) qua `IEmailSender.SendWithAttachmentAsync`.
- Job dispatch quét `diab_his_rep_schedules` (enabled + tới hạn theo frequency/hour/day) → export báo cáo theo kỳ (period) → gửi tới recipients → cập nhật `last_run_at`. Lỗi 1 lịch không làm chết job.
- **Hangfire dashboard:** truy cập theo cấu hình (super-admin filter) để theo dõi job `report-schedule-dispatch`.

---

## 7. Letterhead (đầu trang PDF)

Đầu trang báo cáo lấy từ bảng `diab_his_sys_tenants` (mỗi phòng khám tự cấu hình):
`name` (tên PK), `company_name`, `slogan`, `address`, `phone`, `website`, `email`/`email_support`, `logo_url`, `cskcb_code`.
- Cập nhật qua màn Quản trị > Phòng khám (Tenant) hoặc SQL.
- `logo_url` trỏ ảnh logo (hoặc để trống → dùng logo diaB bundled). Header nền teal `#01645A`, icon liên hệ dạng SVG.

---

## 8. Checklist verify sau setup

Chạy API (`ASPNETCORE_URLS`), lấy JWT admin, kiểm:

```bash
B=http://<host>/api/v1
TOKEN=$(curl -s -X POST "$B/auth/login" -H "Content-Type: application/json" \
  -d '{"email":"admin@...","password":"..."}' | jq -r .data.accessToken)

curl -s "$B/reports/catalog"    -H "Authorization: Bearer $TOKEN"   # danh mục báo cáo hệ thống
curl -s "$B/reports/datasets"   -H "Authorization: Bearer $TOKEN"   # 6 dataset (report.build)
curl -s "$B/reports/dashboards" -H "Authorization: Bearer $TOKEN"   # dashboard
curl -s "$B/reports/schedules"  -H "Authorization: Bearer $TOKEN"   # lịch
```
- FE: đăng nhập power user → menu **Trình tạo báo cáo / Bảng điều khiển / Lịch báo cáo** hiển thị. Vào `/reports/builder` → 6 nguồn dữ liệu hiện ra.
- Tạo thử 1 báo cáo (chọn dataset → cột → Xem trước → Lưu) → xuất hiện trong `/reports` nhóm "Báo cáo của phòng khám" → in PDF được.

---

## 9. Xử lý sự cố thường gặp

| Triệu chứng | Nguyên nhân / Cách xử lý |
|---|---|
| `/reports/datasets` 403 | User thiếu quyền `report.build` → gán role, đăng nhập lại |
| Menu Trình tạo báo cáo không hiện | Thiếu `report.build` hoặc token cũ (thiếu `role_code`) → đăng nhập lại |
| `REPORT_INVALID_DATE_RANGE` | Khoảng ngày > **366 ngày** → thu hẹp kỳ báo cáo |
| Báo cáo trả 0 dòng (không lỗi) | Chưa phát sinh nghiệp vụ trong kỳ (vd chưa có phiếu thu/biến động kho) — không phải lỗi |
| `Illegal mix of collations` | Join chuỗi giữa bảng `bil_*` (unicode_ci) và bảng gốc (0900_ai_ci) — dataset mới phải `COLLATE` trong SqlExpr |
| Formula calc field bị 400 | Chỉ cho phép field measure + số + `+ - * / ( )`; token/hàm lạ bị chặn (chống injection) |
| Email lịch không gửi | Kiểm cấu hình `Smtp`, Hangfire job `report-schedule-dispatch` đang chạy, recipients hợp lệ, schedule `enabled` |
| Chia sẻ theo role không hiệu lực | User chưa đăng nhập lại sau khi có claim `role_code`; kiểm `shared_roles` khớp role_code (bac_si/ke_toan...) |

---

## 10. Ghi chú vận hành

- **Giới hạn tài nguyên** (đã áp): số cột ≤20, filter ≤15, group ≤3, khoảng ngày ≤366, LIMIT data 5000 / preview 200, tối đa 12 widget/dashboard.
- **Multi-tenant:** mọi báo cáo/dashboard/lịch tự lọc theo phòng khám đăng nhập — không rò rỉ chéo tenant.
- Thêm báo cáo hệ thống mới (code-defined) = DEV thêm 1 descriptor vào `ReportRegistry.cs` (không cần migration). Thêm báo cáo tự phục vụ = người dùng tự làm qua UI.
