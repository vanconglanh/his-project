# Bộ câu hỏi Onboarding — Khởi tạo 1 phòng khám chạy được

> Mục tiêu: khách/vận hành trả lời bộ câu hỏi này → generator sinh ra bộ deploy hoàn chỉnh
> (`.env` + `docker-compose.yml` + `nginx.conf` + `seed.sql`) cho **1 phòng khám = 1 stack Docker + 1 DB riêng**.

> ### 🎯 Phạm vi MVP đợt 1 (đã chốt 08/07/2026)
> Wizard đợt đầu bao gồm **đầy đủ nhóm A–M + E6**, **NGOẠI TRỪ** (hoãn giai đoạn sau):
> - ❌ **ĐTQG liên thông + chữ ký số** (H2, I2, I8, E6.6 scan, E6.7 chữ ký số) — MVP chỉ kê đơn nội bộ + in PDF.
> - ❌ **Đặt lịch online / patient self-booking** (H8, J3–J5) — Phần J chỉ giữ `slot_duration` + buffer nội bộ.
> - ✅ Vẫn thu thập CCHN cơ bản của bác sĩ (E6.1–E6.5) để in trên đơn thuốc + chuẩn bị hồ sơ sau này.
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
| A6 | Ngôn ngữ mặc định | `default_locale` | enum `vi`\|`en` | 🟢 | `vi` | env/settings |
| A7 | Đơn vị tiền tệ | `currency` | string | 🟢 | `VND` | settings |

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

### E6 — Hồ sơ hành nghề BÁC SĨ (chỉ với nhân sự có vai trò `bac_si`)

Theo các HIS quốc tế (SimplePractice/Cliniko/DrChrono) và **TT 27/2021** (đơn thuốc điện tử phải ký số bằng mã định danh của bác sĩ có CCHN hợp lệ), profile bác sĩ nên **tách riêng** khỏi tài khoản user.
⚠️ **Hiện `diab_his_sec_users` CHƯA có các cột này** → cần migration + bảng mới `his_provider_profile` (1-1 với user role bác sĩ) + `his_provider_schedule`.

| # | Trường | Key | Mức độ | Ghi chú |
|---|--------|-----|--------|---------|
| E6.1 | Số chứng chỉ hành nghề (CCHN) | `doctor.so_cchn` | 🟡 (🔴 nếu ĐTQG/BHYT) | Định danh hành nghề QG (tương đương NPI). In trên đơn thuốc |
| E6.2 | Nơi cấp CCHN | `doctor.noi_cap_cchn` | 🟡 | Sở Y tế cấp — đối soát khi đăng ký ĐTQG |
| E6.3 | Ngày cấp CCHN | `doctor.ngay_cap_cchn` | 🟡 | |
| E6.4 | Phạm vi hoạt động chuyên môn | `doctor.pham_vi_hanh_nghe` | 🟢 | Giới hạn dịch vụ bác sĩ được thực hiện |
| E6.5 | Chuyên khoa | `doctor.chuyen_khoa` | 🟡 | Booking + KPI bác sĩ + gợi ý CDSS |
| E6.6 | Scan CCHN (PDF) | `doctor.scan_cchn_file` | 🟡 (🔴 nếu ĐTQG) | ĐTQG yêu cầu đính kèm khi đăng ký CSKCB (lưu MinIO) |
| E6.7 | Chữ ký số / chứng thư số | `doctor.chu_ky_so_config` | 🟢 (🔴 nếu kê đơn điện tử liên thông) | TT 27/2021 — cấu hình USB token/HSM (mã hóa AES-GCM). **Hạng mục phức tạp — cân nhắc để giai đoạn sau** |
| E6.8 | Lịch làm việc riêng | `doctor.schedule` | 🟢 | Override giờ chung cơ sở (bảng `his_provider_schedule`): theo ngày, giờ mở/đóng, break |
| E6.9 | Dịch vụ được phép thực hiện | `doctor.allowed_services` | 🟢 | Gán provider ↔ dịch vụ để hiện đúng khi đặt lịch |
| E6.10 | Phí khám riêng (override giá chung) | `doctor.custom_fee` | 🟢 | Bác sĩ senior/junior khác phí cùng 1 dịch vụ |

> **Tối thiểu**: chỉ cần admin (E1–E4) là chạy được; thêm nhân sự sau qua màn Quản lý người dùng
> (có sẵn InviteUserForm / AssignRolesForm). Nhưng để "khám được ngay" nên khai tối thiểu **1 bác sĩ**.

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
| H8 | Đặt lịch online (bệnh nhân tự đặt)? | `module_online_booking` | bool | 🟢 | false | seed flags, nav |
| H9 | Nhắc lịch qua SMS? | `module_sms_reminder` | bool | 🟢 | false | seed flags + Phần I |
| H10 | Nhắc lịch/thông báo qua Zalo OA? | `module_zalo_oa` | bool | 🟢 | false | seed flags + Phần I |

---

## Phần I — Tích hợp (chỉ hỏi khi module tương ứng BẬT)

| # | Câu hỏi | Key | Điều kiện | Mức độ | Dùng cho |
|---|---------|-----|-----------|--------|----------|
| I1 | ĐTQG: `co_so_kham_chua_benh_id` | `dtqg_cskcb_id` | H2=true | 🔴 (nếu bật) | env/seed |
| I2 | ĐTQG: API token | `dtqg_token` | H2=true | 🔴 (nếu bật) | env `DonThuocQG__ApiKey` (mã hóa) |
| I3 | BHYT: tài khoản cổng giám định (user/pass) | `bhyt_gd_credentials` | H1=true | 🟡 | seed (mã hóa) |
| I4 | HĐĐT: nhà cung cấp | `einvoice_provider` | H4=true | 🔴 (nếu bật) | enum MISA/VNPT/EFY |
| I5 | HĐĐT: credentials + mẫu số/ký hiệu | `einvoice_config` | H4=true | 🔴 (nếu bật) | seed (mã hóa) |
| I6 | SMS gateway (nhắc lịch/OTP portal) | `sms_config` | H9=true | 🟢 | provider + key (SpeedSMS/Viettel/ESMS) |
| I7 | Zalo OA (app_id, secret, template nhắc lịch) | `zalo_config` | H10=true | 🟢 | app_id + secret |
| I8 | **ĐTQG — hồ sơ pháp lý đăng ký CSKCB** (bắt buộc để đăng ký tài khoản donthuocquocgia.vn) | | H2=true | | |
| I8a | Scan giấy phép hoạt động khám chữa bệnh (PDF) | `dtqg_kcb_license_file` | 🔴 (nếu ĐTQG) | Bắt buộc khi đăng ký CSKCB |
| I8b | Loại hình cơ sở | `facility_type` | 🔴 (nếu ĐTQG) | Phòng khám đa khoa/chuyên khoa... |
| I8c | Mã hợp đồng BHYT | `bhyt_contract_code` | 🟢 | Nếu có ký BHYT |
| I8d | Scan CCHN từng bác sĩ (PDF) | dùng chung `doctor.scan_cchn_file` (E6.6) | 🔴 (nếu ĐTQG) | Đính kèm hồ sơ đăng ký |

---

## Phần J — Cấu hình lịch hẹn (Appointment Settings) 🟢

| # | Câu hỏi | Key | Kiểu | Mặc định | Ghi chú |
|---|---------|-----|------|----------|---------|
| J1 | Thời lượng 1 lượt khám (phút) | `slot_duration_minutes` | int | 15 | Dựng lưới lịch (Cliniko/Halaxy bắt buộc) |
| J2 | Đệm trước/sau lượt khám (phút) | `buffer_before/after` | int | 0 | Tránh chồng lịch |
| J3 | Cho đặt trước tối đa / tối thiểu | `max_advance_days` / `min_advance_hours` | int | 30 / 1 | |
| J4 | Chính sách hủy hẹn (giờ báo trước) + phí no-show | `cancellation_hours` / `no_show_fee` | int / money | 24 / 0 | |
| J5 | Bật đặt lịch online + bác sĩ hiển thị công khai | `allow_online_booking` | bool | false | Gắn H8 |

> Ghi chú: giờ làm việc **riêng theo bác sĩ** nằm ở E6.8 (`his_provider_schedule`); giờ chung cơ sở ở Phần F3.

---

## Phần K — Tài chính / Thuế / Thanh toán 🟡

| # | Câu hỏi | Key | Kiểu | Mức độ | Mặc định | Ghi chú |
|---|---------|-----|------|--------|----------|---------|
| K1 | VAT mặc định (%) | `vat_rate_default` | enum 0/5/8/10 | 🟢 | 0 (dịch vụ y tế thường miễn) | per-clinic |
| K2 | Ký hiệu / mẫu số hóa đơn điện tử (NĐ 123/2020) | `invoice_symbol` | string | 🟡 (🔴 nếu HĐĐT) | — | Bắt buộc khi khởi tạo HĐĐT |
| K3 | Số hóa đơn bắt đầu | `invoice_start_number` | int | 🟡 | 1 | |
| K4 | Tài khoản ngân hàng nhận tiền (QR chuyển khoản) | `bank_account` | object `{name,number,bank,vietqr}` | 🟢 | rỗng | Thu ngân xuất VietQR |
| K5 | Phương thức thanh toán khởi tạo | `payment_methods` | multi | 🟢 | tiền mặt + chuyển khoản | (trùng D3 — gộp) |

---

## Phần L — Vận hành & Bảo mật 🟢

| # | Câu hỏi | Key | Kiểu | Mặc định | Ghi chú |
|---|---------|-----|------|----------|---------|
| L1 | Chính sách mật khẩu (độ dài/độ phức tạp/hạn đổi) | `password_policy` | object | độ dài ≥8 | Áp khi tạo tài khoản nhân sự |
| L2 | Thời gian lưu hồ sơ bệnh án (năm) | `data_retention_years` | int | theo quy định BYT | Compliance |
| L3 | Lịch backup DB/MinIO | `backup_schedule` | cron/enum | hằng đêm | ops/scripts/backup-nightly.sh |
| L4 | Định dạng ngày | `date_format` | string | `dd/MM/yyyy` | |
| L5 | Kênh nhắc lịch/thông báo bật | `notification_channels` | multi email/SMS/Zalo | email | Gắn G/H9/H10 |

---

## Phần M — Kho dược khởi tạo (chỉ khi H3 = bật kho dược) 🟡

| # | Câu hỏi | Key | Kiểu | Mặc định | Ghi chú |
|---|---------|-----|------|----------|---------|
| M1 | Đơn vị tính seed sẵn | `drug_units` | list | viên, vỉ, hộp, chai, ống, gói... | Cần trước khi nhập kho |
| M2 | Nhà cung cấp mặc định | `default_supplier` | object | rỗng | Giảm nhập liệu lần đầu |
| M3 | Ngưỡng cảnh báo tồn thấp / cận HSD (ngày) | `low_stock_threshold` / `expiry_alert_days` | int | 10 / 90 | |
| M4 | Import danh mục thuốc mẫu (theo DMT BYT)? | `seed_drug_catalog` | bool | false | "Bỏ qua, tự nhập sau" được |

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

---

## Schema/bảng cần BỔ SUNG để chứa hết câu hỏi (chưa có trong DB hiện tại)

Từ đối chiếu các HIS khác — các mục dưới **chưa có cột/bảng**, cần migration nếu đưa vào wizard:
1. `his_provider_profile` (1-1 user role bác sĩ): `so_cchn`, `noi_cap_cchn`, `ngay_cap_cchn`, `pham_vi_hanh_nghe`, `chuyen_khoa`, `scan_cchn_file_url` (MinIO), `chu_ky_so_config` (JSON, mã hóa AES-GCM). → Phần E6.
2. `his_provider_schedule` (tenant_id, provider_id, clinic_id, day_of_week, start/end, break). → E6.8.
3. `his_appointment_setting` (tenant_id, slot_duration, buffer_before/after, cancellation_hours, allow_online_booking). → Phần J.
4. Cột thêm cho `diab_his_sys_tenants` hoặc bảng cấu hình: `invoice_symbol`, `invoice_start_number`, `vat_rate_default`, `bank_account_info` (JSON), `medical_practice_license_url`, `facility_type`. → Phần K + I8.
5. Feature flags per-deployment: `online_booking`, `sms_reminder`, `zalo_oa` (ngoài các flag đã có). → H8–H10.

> Phần lớn là **giai đoạn sau MVP**. Wizard đợt 1 chỉ cần nhóm A–I mức 🔴/🟡; J/K/L/M + E6 để đợt 2.

## Câu hỏi cần bạn quyết (phạm vi MVP)

1. **Đặt lịch online cho bệnh nhân** (patient portal self-booking) có làm ở đợt đầu không? Nếu không → Phần J tối giản (chỉ `slot_duration` nội bộ).
2. **Chữ ký số/HSM cho kê đơn điện tử** (E6.7) — làm ngay hay để sau? (phức tạp, cần chứng thư số CA công cộng).
3. **Mẫu in hóa đơn/đơn thuốc** — đã có sẵn trong Report Engine (dùng chung), hay cần cho phòng khám chọn template lúc onboarding?
