# Bộ câu hỏi Onboarding — Khởi tạo 1 phòng khám chạy được

> Mục tiêu: khách/vận hành trả lời bộ câu hỏi này → generator sinh ra bộ deploy hoàn chỉnh
> (`.env` + `docker-compose.yml` + `nginx.conf` + `seed.sql`) cho **1 phòng khám = 1 stack Docker + 1 DB riêng**.
>
> Quy ước cột **Mức độ**:
> - 🔴 **Bắt buộc để chạy** — thiếu là hệ thống không lên / không đăng nhập / không khám được.
> - 🟡 **Nên có** — chạy được nếu bỏ, nhưng thiếu trải nghiệm (branding, email mời...).
> - 🟢 **Tùy chọn / cấu hình sau** — có thể để mặc định, chỉnh trong màn Settings sau khi deploy.
>
> Cột **Dùng cho**: `env` = biến môi trường deploy · `seed` = seed.sql dữ liệu phòng khám · `nginx` = cấu hình domain/proxy · `build` = build-arg frontend.

---

## Phần A — Định danh & hạ tầng (người vận hành)

| # | Câu hỏi | Key | Kiểu | Mức độ | Mặc định | Dùng cho |
|---|---------|-----|------|--------|----------|----------|
| A1 | Mã phòng khám (slug, chữ thường + số, vd `abc`) — dùng đặt tên DB/container/network | `clinic_code` | slug `^[a-z0-9-]{3,20}$` | 🔴 | — | env, seed, tên tài nguyên |
| A2 | Tên miền truy cập (vd `abc.diab.com.vn` hoặc `phongkhamabc.vn`) | `domain` | domain | 🔴 | — | nginx, env `APP_PUBLIC_URL`, build, CORS |
| A3 | Cổng nội bộ nginx publish (để nhiều clinic cùng 1 VM không đụng nhau) | `nginx_port` | int | 🟡 | tự cấp (8090, 8091...) | compose, nginx host ngoài |
| A4 | Múi giờ | `timezone` | string | 🟢 | `Asia/Ho_Chi_Minh` | env |
| A5 | Email nhận cảnh báo kỹ thuật / Sentry DSN (nếu có) | `sentry_dsn` | string | 🟢 | rỗng | env |

> **Tự sinh — KHÔNG hỏi** (generator tạo ngẫu nhiên/suy ra từ `clinic_code`):
> `DB_NAME` (`prodiab_<code>`), `DB_USER`, `DB_PASSWORD`, `DB_ROOT_PASSWORD`, `REDIS_PASSWORD`,
> `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`, `JWT_SECRET` (≥32 ký tự), `ENCRYPTION_MASTER_KEY`
> (base64 32 bytes — **bắt buộc**, mã hóa CMND/BHYT/bệnh án), container-name prefix, network name, volume paths.

---

## Phần B — Thông tin phòng khám (letterhead + BHYT)

| # | Câu hỏi | Key | Kiểu | Mức độ | Mặc định | Dùng cho |
|---|---------|-----|------|--------|----------|----------|
| B1 | Tên phòng khám | `clinic_name` | string | 🔴 | — | seed (tenant.name), branding |
| B2 | Tên công ty / pháp nhân | `company_name` | string | 🟡 | = clinic_name | seed (letterhead) |
| B3 | Mã CSKCB (BYT cấp) | `cskcb_code` | string | 🟡 (🔴 nếu bật BHYT) | rỗng | seed, BHYT |
| B4 | Mã số thuế | `tax_code` | string | 🟢 | rỗng | seed |
| B5 | Địa chỉ | `address` | string | 🟡 | rỗng | seed, in giấy tờ |
| B6 | Điện thoại | `phone` | string | 🟡 | rỗng | seed, in giấy tờ |
| B7 | Email liên hệ | `email` | email | 🟡 | rỗng | seed |
| B8 | Website | `website` | string | 🟢 | rỗng | seed |
| B9 | Slogan (in trên giấy tờ) | `slogan` | string | 🟢 | rỗng | seed |

---

## Phần C — Thương hiệu (branding)

| # | Câu hỏi | Key | Kiểu | Mức độ | Mặc định | Dùng cho |
|---|---------|-----|------|--------|----------|----------|
| C1 | Logo (file PNG/SVG hoặc URL) | `logo` | file/url | 🟡 | logo diaB mặc định | seed (logo_url), sidebar, PDF |
| C2 | Màu chủ đạo (hex) | `primary_color` | hex | 🟢 | `#01645A` | branding UI + PDF |
| C3 | Tên hiển thị trên app (topbar/sidebar) | `app_display_name` | string | 🟢 | = clinic_name | build `NEXT_PUBLIC_APP_NAME` |

---

## Phần D — Chuyên khoa & danh mục dịch vụ

| # | Câu hỏi | Key | Kiểu | Mức độ | Mặc định | Dùng cho |
|---|---------|-----|------|--------|----------|----------|
| D1 | Chuyên khoa chính (chọn preset) | `specialty_preset` | enum: `noi_tong_quat` \| `noi_tiet_dtd` \| `san_phu` \| `nhi` \| `da_lieu` \| `da_khoa` | 🟡 | `da_khoa` | seed (bộ dịch vụ/ICD/mẫu mặc định) |
| D2 | Danh mục dịch vụ khám + giá | `services` | list `{code,name,price,vat,bhyt_code}` hoặc "dùng preset" hoặc file Excel | 🟡 | theo preset | seed (bil_services) |
| D3 | Phương thức thanh toán chấp nhận | `payment_methods` | multi: tiền mặt / chuyển khoản / QR VietQR / Momo / VNPay / thẻ | 🟢 | tiền mặt + chuyển khoản | seed/settings |

---

## Phần E — Tài khoản quản trị đầu tiên

| # | Câu hỏi | Key | Kiểu | Mức độ | Mặc định | Dùng cho |
|---|---------|-----|------|--------|----------|----------|
| E1 | Họ tên admin | `admin_full_name` | string | 🔴 | — | seed (user) |
| E2 | Email admin (đăng nhập / nhận lời mời) | `admin_email` | email | 🔴 | — | seed (user) |
| E3 | Cách đặt mật khẩu | `admin_password_mode` | enum: `set_now` \| `invite_email` | 🔴 | `invite_email` | seed |
| E4 | Mật khẩu admin (nếu chọn `set_now`) | `admin_password` | password | 🟡 (nếu E3=set_now) | — | seed (băm bcrypt) |

> Nếu `invite_email`: cần Phần G (SMTP) hoạt động để gửi link kích hoạt. Nếu môi trường chưa có SMTP,
> nên chọn `set_now` để đăng nhập được ngay.

### E5 — Danh sách nhân sự (người dùng) — thêm bao nhiêu tuỳ ý

Mỗi nhân sự là 1 tài khoản đăng nhập. Lặp lại bộ trường dưới cho từng người (`staff[]`).
Vai trò lấy từ danh mục hệ thống: **admin, bac_si (bác sĩ), le_tan (lễ tân), duoc_si (dược sĩ), ke_toan (kế toán), ky_thuat_vien (KTV)**. 1 người có thể giữ nhiều vai trò.

| # | Trường | Key | Kiểu | Mức độ | Mặc định | Dùng cho |
|---|--------|-----|------|--------|----------|----------|
| E5.1 | Họ tên | `staff[].full_name` | string | 🔴 (mỗi nhân sự) | — | seed (user) |
| E5.2 | Email (đăng nhập) | `staff[].email` | email | 🔴 | — | seed (user, unique/tenant) |
| E5.3 | Vai trò (chọn ≥1) | `staff[].roles` | multi-enum (6 vai trò trên) | 🔴 | — | seed (user_roles) |
| E5.4 | Số điện thoại | `staff[].phone` | string | 🟡 | rỗng | seed (user.phone) |
| E5.5 | Cách đặt mật khẩu | `staff[].password_mode` | enum `set_now` \| `invite_email` | 🔴 | `invite_email` | seed |
| E5.6 | Mật khẩu (nếu `set_now`) | `staff[].password` | password | 🟡 | — | seed (băm bcrypt) |
| E5.7 | Phòng khám phụ trách (cho bác sĩ) | `staff[].room` | ref phòng (Phần F1) | 🟢 | rỗng | seed (phân phòng) |

**Cần cho BÁC SĨ nếu bật kê đơn ĐTQG / BHYT** (⚠️ hiện **chưa có cột** trong bảng user — cần bổ sung migration nếu dùng):

| # | Trường | Key | Mức độ | Ghi chú |
|---|--------|-----|--------|---------|
| E5.8 | Số chứng chỉ hành nghề (CCHN) | `staff[].practice_cert_no` | 🟡 (🔴 nếu kê đơn ĐTQG/BHYT) | In trên đơn thuốc (TT 27/2021); **cần thêm cột `practice_cert_no` vào `diab_his_sec_users`** |
| E5.9 | Mã bác sĩ / chức danh | `staff[].doctor_code` | 🟢 | Định danh nội bộ; cần thêm cột nếu muốn lưu |
| E5.10 | Chuyên khoa của bác sĩ | `staff[].specialty` | 🟢 | Hiển thị/thống kê KPI bác sĩ |

> **Tối thiểu**: có thể chỉ tạo admin (Phần E1–E4) rồi mời/ thêm nhân sự sau qua màn Quản lý người dùng
> (đã có sẵn: InviteUserForm / AssignRolesForm). Nhưng để "khám được ngay" nên khai tối thiểu **1 bác sĩ**.

---

## Phần F — Cơ sở, phòng & giờ làm việc

| # | Câu hỏi | Key | Kiểu | Mức độ | Mặc định | Dùng cho |
|---|---------|-----|------|--------|----------|----------|
| F1 | Số phòng khám bệnh | `exam_rooms` | int | 🟡 | 1 | seed (rooms) |
| F2 | Số quầy tiếp đón / thu ngân | `reception_counters` | int | 🟡 | 1 | seed (counters) |
| F3 | Giờ làm việc theo ngày trong tuần | `working_hours` | object (T2–CN: giờ mở/đóng) | 🟢 | 07:30–17:00, nghỉ CN | seed/settings (đặt lịch) |

---

## Phần G — Email (SMTP) — cho lời mời & thông báo

| # | Câu hỏi | Key | Kiểu | Mức độ | Mặc định | Dùng cho |
|---|---------|-----|------|--------|----------|----------|
| G1 | SMTP host | `smtp_host` | string | 🟡 (🔴 nếu admin dùng invite_email) | rỗng | env `Smtp__Host` |
| G2 | SMTP port | `smtp_port` | int | 🟡 | 587 | env |
| G3 | SMTP user | `smtp_user` | string | 🟡 | rỗng | env |
| G4 | SMTP password | `smtp_pass` | password | 🟡 | rỗng | env |
| G5 | Email gửi đi (From) | `smtp_from` | email | 🟡 | `no-reply@<domain>` | env `Smtp__FromEmail` |
| G6 | Bật SSL/TLS | `smtp_ssl` | bool | 🟢 | true | env |

---

## Phần H — Module bật/tắt (feature flags)

| # | Câu hỏi | Key | Kiểu | Mức độ | Mặc định | Dùng cho |
|---|---------|-----|------|--------|----------|----------|
| H1 | Dùng BHYT (giám định, export XML 4210)? | `module_bhyt` | bool | 🟡 | false | seed flags, nav |
| H2 | Đẩy Đơn thuốc Quốc gia (ĐTQG)? | `module_dtqg` | bool | 🟡 | false | env + seed flags |
| H3 | Quản lý kho dược (nhập/xuất/tồn/lô/HSD)? | `module_pharmacy` | bool | 🟡 | true | seed flags, nav |
| H4 | Hóa đơn điện tử? | `module_einvoice` | bool | 🟡 | false | seed flags |
| H5 | CDSS (cảnh báo tương tác thuốc, dị ứng...)? | `module_cdss` | bool | 🟢 | true | seed flags |
| H6 | Cổng bệnh nhân (patient portal)? | `module_patient_portal` | bool | 🟢 | false | seed flags |
| H7 | Tích hợp Xét nghiệm/CĐHA ngoài? | `module_lab_integration` | bool | 🟢 | false | seed flags |

---

## Phần I — Tích hợp (chỉ hỏi khi module tương ứng BẬT)

| # | Câu hỏi | Key | Điều kiện | Mức độ | Dùng cho |
|---|---------|-----|-----------|--------|----------|
| I1 | ĐTQG: `co_so_kham_chua_benh_id` | `dtqg_cskcb_id` | H2=true | 🔴 (nếu bật) | env/seed |
| I2 | ĐTQG: API token | `dtqg_token` | H2=true | 🔴 (nếu bật) | env `DonThuocQG__ApiKey` (mã hóa) |
| I3 | BHYT: tài khoản cổng giám định (user/pass) | `bhyt_gd_credentials` | H1=true | 🟡 | seed (mã hóa) |
| I4 | HĐĐT: nhà cung cấp | `einvoice_provider` | H4=true | 🔴 (nếu bật) | enum MISA/VNPT/EFY |
| I5 | HĐĐT: credentials + mẫu số/ký hiệu | `einvoice_config` | H4=true | 🔴 (nếu bật) | seed (mã hóa) |
| I6 | SMS gateway (nhắc lịch/OTP portal) | `sms_config` | tùy | 🟢 | provider + key |

---

## Tập TỐI THIỂU để "chạy được" (đăng nhập + khám 1 bệnh nhân + in đơn)

Chỉ cần 8 câu 🔴 sau là ra được hệ thống chạy + đăng nhập được (các phần còn lại để mặc định/cấu hình sau):

1. `clinic_code` (A1)
2. `domain` (A2)
3. `clinic_name` (B1)
4. `admin_full_name` (E1)
5. `admin_email` (E2)
6. `admin_password_mode` (E3) — chọn `set_now` để login ngay
7. `admin_password` (E4) — nếu set_now
8. (Chuyên khoa D1 để có sẵn danh mục dịch vụ — nếu bỏ thì tự thêm dịch vụ sau)

Mọi secret (DB/JWT/Encryption/Minio/Redis) generator **tự sinh**, không cần hỏi.

---

## Ghi chú kỹ thuật cho generator (dựa trên hạ tầng hiện có)

- Base template: `ops/docker-compose.deploy.yml` (self-build, coexist nhiều stack) + `ops/nginx/deploy-prodiab.conf` + `ops/.env.example`.
- **Bắt buộc bổ sung** so với `.env.example`: `ENCRYPTION_MASTER_KEY` (base64 32B), `APP_PUBLIC_URL`.
- Frontend `NEXT_PUBLIC_API_BASE_URL` bake lúc build → mỗi domain phải **build lại image** (hoặc chuyển sang runtime-config nếu muốn tránh rebuild).
- Stack deploy hiện **thiếu bước migration** → generator phải thêm (service migrator kiểu dev, hoặc chạy `ops/scripts/apply-migrations.sh` / `prodiab-migrate --new-db` sau khi `up`).
- Mỗi clinic cần **unique**: container-name prefix, network name, nginx publish port, volume paths.
- Thống nhất tên biến frontend (`NEXT_PUBLIC_API_BASE_URL`) — hiện prod.yml và deploy.yml dùng tên khác nhau.
