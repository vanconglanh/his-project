# 単体テスト仕様書 (UTC) — Màn hình **Portal bệnh nhân** (Patient Portal / app portal-client)

> Quy ước & catalog test viewpoint (12 nhóm 観点): xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md).
> File này bám **code thật** 3 tầng:
> - **FE** portal-client/: app/login/page.tsx, app/activate/page.tsx, app/reset-pin/page.tsx, components/NumPad.tsx, lib/api.ts (snake-camel), proxy.ts (guard cookie token).
> - **BE** PatientPortalController.cs (/api/portal/v1/*) + PortalAuthHandlers.cs, PortalHandlers.cs, PortalMeHandlers.cs, PortalBookingHandlers.cs, PortalMedReminderHandlers.cs, PortalNotifyPrefHandlers.cs, PortalTenantResolveHandlers.cs, PublicApiDtos.cs.
> - **DB** db/migrations/9070_recreate_portal_accounts_char36.sql (+ 9071-9075).

| Mục | Nội dung |
|---|---|
| 機能ID | PORTAL-BN-001 |
| Màn hình | Cổng bệnh nhân: Đăng nhập PIN · Kích hoạt tài khoản · Quên/Đặt lại PIN · Trang chủ · Hàng đợi · Đặt lịch · Lượt khám · Đơn thuốc · Kết quả XN · Nhắc thuốc · Hồ sơ · Cài đặt thông báo |
| Route FE | /login, /activate, /reset-pin, /(protected)/ (home), /queue, /appointments/new, /encounters/[id], /prescriptions, /lab-results, /medications, /me, /settings/notifications |
| API base | /api/portal/v1/* (ẩn danh: tenant-info, auth/activate, auth/login-pin, auth/forgot-pin, auth/reset-pin; đã đăng nhập: me, me/encounters(/{id}), me/prescriptions, me/lab-results, me/queue, me/appointments GET/POST/DELETE, me/med-reminders*, me/notification-preferences, me/push-subscriptions, booking/doctors, booking/slots) + ReceptionController patients/{id}/portal-activation (lễ tân cấp mã) |
| Bảng DB | diab_his_pat_portal_accounts (9070) · _portal_otp_log · _portal_sessions · diab_his_sch_appointments (9071) · _doctor_schedules (9073) · _schedule_blocks · diab_his_ptl_med_reminders (9075) · diab_his_nti_web_push_subs · read: pat_patients, enc_encounters, enc_diagnoses, pha_prescriptions(_items), pha_drugs, lab_results, rad_orders/results, rcp_queue_tickets, sys_tenants, sec_users/roles |
| PK / Unique | portal_accounts.id CHAR(36); UNIQUE (tenant_id, phone); sessions.jti UNIQUE; appointments PK id INT + uuid CHAR36 |
| Permission | **KHÔNG dùng JWT nội bộ**. Auth ẩn danh: resolve tenant qua **subdomain** (Host to sys_tenants.subdomain; dev header X-Portal-Subdomain / ?clinic=). Sau đăng nhập: JWT scheme **PortalBearer** chứa patient_id + tenant_id. Không có role — phạm vi = 1 bệnh nhân. |
| Đặc thù đăng nhập | **Không mật khẩu**: lễ tân cấp **mã kích hoạt 8 ký tự** (alphabet bỏ O/0/I/1, hash BCrypt, hạn **72h**) to bệnh nhân đặt **PIN 6 số** (BCrypt) to phiên **30 ngày**. Login lại: **SĐT + PIN**. Sai PIN **5 lần to khoá 15 phút** (PORTAL_ACCOUNT_LOCKED 429). Quên PIN to **OTP 6 số gửi EMAIL** (hạn 10 phút) to reset PIN. |
| Đặc thù tenant | Cách ly: mọi query /me* lọc **đồng thời tenant_id + patient_id**. Không resolve được subdomain to PORTAL_TENANT_UNRESOLVED 400. |
| Envelope / naming | Success {data, [meta]}; lỗi {error:{code,message}}. API **snake_case** (JsonNamingPolicy.SnakeCaseLower); FE lib/api.ts tự chuyển camel-snake. |

---

## 1. Field matrix (bám 3 tầng)

### 1.1 Form Đăng nhập PIN — POST /auth/login-pin (PortalPinLoginRequest)
| Field (FE) | Nhãn | Control | FE required/rule | BE (DTO / rule) | DB liên quan | GAP |
|---|---|---|---|---|---|---|
| phone | Số điện thoại | input tel numeric | disable khi phone.length < 9; **không regex** | string Phone; **không validate format**; khoá (tenant_id, phone) | phone VARCHAR(20) NOT NULL | GAP-1 FE chỉ check <9, BE không validate to SĐT rác vẫn query |
| pin | Mã PIN | NumPad maxLength=6 | disable khi pin.length < 4 | string Pin; **không tiền-validate** (chỉ so hash) | pin_hash VARCHAR(100) | GAP-2 FE cho submit PIN 4-5 số nhưng BE lưu hash **PIN 6 số** to 4-5 số luôn PORTAL_PIN_INVALID |
| (tenant) | — | — | resolve tại proxy/Host | ResolveTenantIdAsync(Host, X-Portal-Subdomain, ?clinic) to >0 | sys_tenants.subdomain ACTIVE | Không resolve to 400 PORTAL_TENANT_UNRESOLVED |

### 1.2 Form Kích hoạt — POST /auth/activate (PortalActivateRequest) — 3 bước
| Field (FE) | Nhãn | Control | FE required/rule | BE (DTO / rule) | DB | GAP |
|---|---|---|---|---|---|---|
| phone | Số điện thoại | input tel | B1 disable khi < 9 | string Phone khoá (tenant_id, phone) | phone VARCHAR(20) | như GAP-1 |
| activation_code | Mã kích hoạt | input text | B1 disable khi length < 4 | .Trim().ToUpperInvariant() rồi BCrypt.Verify vs activation_code_hash; check activation_expires_at >= now | activation_code_hash VARCHAR(100), activation_expires_at DATETIME | GAP-3 mã sinh **8 ký tự** nhưng FE chỉ chặn <4 to mã 4-7 ký tự submit được (BE trả PORTAL_ACTIVATION_INVALID) |
| pin | PIN mới | NumPad maxLength=6, label **"Đặt mã PIN (4-6 số)"** | B2 disable khi pin.length < 4 | PortalPinRules.IsValidPin: **đúng 6 chữ số** | pin_hash VARCHAR(100) | GAP-2/4 Label FE nói "4-6 số" nhưng BE bắt buộc **đúng 6 số** |

### 1.3 Form Đặt lại PIN — POST /auth/forgot-pin + POST /auth/reset-pin
| Field (FE) | Nhãn | Control | FE rule | BE rule | DB | GAP |
|---|---|---|---|---|---|---|
| phone | Số điện thoại | input tel | disable < 9 | forgot: tìm email (account.email hoặc patient.email); **luôn trả 202** | email VARCHAR(100) | anti-enumeration OK |
| otp | Mã xác nhận | input numeric, placeholder **"Nhập mã đã gửi qua SMS"** | disable otp.length < 4 | so otp_hash (purpose=RESET_PIN, verified_at IS NULL, mới nhất); hết hạn 10' to 410 | otp_log.otp_hash, expires_at | GAP-5 OTP gửi **EMAIL** nhưng FE ghi "SMS"; GAP-6 reset **không đếm sai / không khoá** OTP |
| new_pin | PIN mới | NumPad | disable < 4 | IsValidPin đúng 6 số | pin_hash | như GAP-2 |

### 1.4 Form Đặt lịch — POST /me/appointments (PortalAppointmentCreateRequest)
| Field | Nhãn | Control | FE rule | BE rule | DB | GAP |
|---|---|---|---|---|---|---|
| appointment_at | Thời gian hẹn | chọn slot từ booking/slots | chọn slot Available | DateTime; **KHÔNG check tương lai**; nếu có schedule to phải trong doctor_schedules & không schedule_blocks; check trùng +-30' status<>CANCELLED | appointment_at DATETIME | GAP-7 không chặn **quá khứ**; GAP-8 slot ngoài lịch/bị block ném APPOINTMENT_SLOT_TAKEN (sai ngữ nghĩa) |
| doctor_id | Bác sĩ | chọn từ booking/doctors | — | Guid? **optional**; nếu null to **bỏ qua toàn bộ** validate slot/trùng, tạo doctor_ref=NULL | doctor_ref CHAR(36) NULL | GAP-9 doctor_id null to đặt lịch tuỳ ý |
| department_id | Khoa | — | — | Guid? nhận nhưng **không dùng/không lưu** | — | GAP-10 field DTO chết |
| note | Ghi chú | textarea | — | string? to cột note | note | encoding tiếng Việt |
| — | mã hẹn | server sinh | — | LH{yyyyMMdd}{6 hex upper}; status=PENDING, source=APP | appointment_code, uuid | |

### 1.5 Form Cài đặt thông báo — PUT /me/notification-preferences + POST /me/push-subscriptions
| Field | Control | FE | BE | DB | GAP |
|---|---|---|---|---|---|
| push | toggle | bool | UpdatePortalNotifyPreferencesRequest.Push | notify_prefs_json JSON {push,email} | default {true,true} khi null/parse lỗi |
| email | toggle | bool | .Email | notify_prefs_json | |
| endpoint/p256dh/auth | web push (RFC 8291) | từ ServiceWorker | PortalPushSubscribeRequest; **không validate** | nti_web_push_subs (user_id/id BINARY(16) qua UUID_TO_BIN, patient_id CHAR36) | GAP-11 không validate; kiểu id trộn BINARY(16)+CHAR36 |

---

## 2. Test cases

判定: để trống — tester điền OK/NG/N/A/保留 khi chạy UTE.

### A — 初期表示 (Load ban đầu)
| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| A01 | Màn login | Render | subdomain hợp lệ | Mở /login | — | Tiêu đề "Cổng bệnh nhân"; ô SĐT; NumPad; nút "Đăng nhập" **disabled**; link Quên PIN + Kích hoạt | | |
| A02 | tenant-info | Load tenant | subdomain hợp lệ | GET /tenant-info | Host phongkham-a.diab.com.vn | 200 {data:{tenant_id,name,logo_url,vapid_public_key}} | | |
| A03 | tenant-info lỗi | Subdomain rỗng | Host localhost/www/IP | GET /tenant-info | — | 400 PORTAL_TENANT_UNRESOLVED | | |
| A04 | Màn activate | Render 3 bước | — | Mở /activate | — | "Bước 1/3"; ô SĐT + Mã kích hoạt; nút Tiếp tục disabled | | |
| A05 | Nút login disable | Trạng thái nút | — | Không nhập gì | phone/pin rỗng | Nút disabled (phone<9 hoặc pin<4) | | |
| A06 | Guard route | Chưa đăng nhập | không có cookie token | Mở /(protected)/ | — | proxy.ts redirect /login?redirect=... | | |
| A07 | Home sau login | Render menu | đã đăng nhập | Mở / | — | Tên BN + thẻ điều hướng (Hàng đợi, Đặt lịch, Đơn thuốc, Kết quả, Nhắc thuốc, Hồ sơ) | | |

### B — 項目表示・設定
| No | 中項目 | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| B01 | NumPad nhập PIN | Control số | bấm 1-6 | 6 chấm; maxLength=6 chặn số thứ 7 | | |
| B02 | NumPad xoá | Sửa | nhập 6 số to Xóa | Xoá số cuối, còn 5 chấm | | |
| B03 | me/profile | Load hồ sơ | mở /me | Mã BN, họ tên có dấu, giới tính, ngày sinh dd/MM/yyyy, SĐT, địa chỉ, **BHYT masked** | | card_no_masked |
| B04 | Encounter list | Load lượt khám | mở lượt khám | mã, ngày khám, bác sĩ, lý do, chẩn đoán (PRIMARY trước), trạng thái | | |
| B05 | snake-camel | Mapping | response patient_code | FE hiển thị patientCode đúng | | |

### C — 入力チェック
| No | 中項目 | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| C01 | PIN đúng 6 số | Hợp lệ | pin=123456 | IsValidPin=true to đặt PIN OK | | |
| C02 | PIN 5 số | 境界値 | pin=12345 | Kỳ vọng FE chặn; thực tế cho submit to BE PORTAL_PIN_INVALID | | GAP-2 |
| C03 | PIN 4 số | 境界値 | pin=1234 | FE cho bấm (label 4-6 số) to BE PORTAL_PIN_INVALID | | GAP-4 |
| C04 | PIN chứa chữ | Kiểu | pin=12a456 | NumPad chặn; API trực tiếp to BE PORTAL_PIN_INVALID (char.IsDigit) | | |
| C05 | SĐT ký tự chữ | Regex | phone=abcdefghi | FE cho submit; BE không thấy account to PORTAL_PHONE_NOT_REGISTERED 404 | | GAP-1 |
| C06 | Mã kích hoạt 7 ký tự | Độ dài | code=ABCDEFG | BE PORTAL_ACTIVATION_INVALID | | GAP-3 |
| C07 | Mã kích hoạt thường/hoa | Chuẩn hoá | code=abcdEfgh | BE Trim+ToUpper to verify; đúng thì pass | | |
| C08 | OTP reset < 6 số | Độ dài | otp=123 | FE disable <4; BE so hash sai to PORTAL_OTP_INVALID | | |
| C09 | new_pin reset 5 số | 境界値 | new_pin=12345 | BE PORTAL_PIN_INVALID | | GAP-2 |
| C10 | API bỏ FE PIN rỗng | Ko tiền-validate | POST login-pin {phone,pin:""} | So hash rỗng to PORTAL_PIN_INVALID (không crash) | | |

### D — 境界値
| No | 中項目 | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| D01 | Hạn mã 72h | Biên | dùng mã lúc 71h59' | Activate OK | | |
| D02 | Mã hết hạn | Biên | mã lúc 72h01' | PORTAL_ACTIVATION_INVALID | | activation_expires_at<now |
| D03 | OTP reset 10' | Biên | otp lúc 10'01" | 410 PORTAL_OTP_EXPIRED | | |
| D04 | Khoá PIN ngưỡng 5 | Biên | sai lần 1-4 rồi 5 | 1-4: PIN_INVALID 400; **lần 5: 429 ACCOUNT_LOCKED** (locked_until=now+15') | | |
| D05 | Login trong 15' khoá | Biên | sau khi khoá | ACCOUNT_LOCKED 429 dù PIN đúng | | locked_until>now |
| D06 | Hết 15' mở khoá | Biên | sau 15'01" PIN đúng | Login OK; failed_attempts=0, locked_until=NULL | | |
| D07 | Phân trang encounters | Biên | ?page=1&page_size=20 | <=20/total; meta.total khớp COUNT | | |
| D08 | Huỷ lịch 2h | Biên | hẹn còn 2h00'/1h59' | >=2h huỷ OK 204; <2h APPOINTMENT_CANCEL_TOO_LATE 400 | | |

### E — 業務ルール
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| E01 | activate to PIN to login | E2E | Lễ tân cấp mã to activate to logout to login-pin | Token 30 ngày; activated_at set; activation_code_hash=NULL; login lại OK | | |
| E02 | Login chưa kích hoạt | Trạng thái | account có, pin_hash=NULL | PORTAL_NOT_ACTIVATED 400 | | |
| E03 | SĐT chưa đăng ký | Không tồn tại | login-pin SĐT lạ | PORTAL_PHONE_NOT_REGISTERED 404 | | |
| E04 | Cấp lại mã | Upsert | lễ tân cấp mã lần 2 | UPDATE code mới, failed_attempts=0, locked_until=NULL; mã cũ vô hiệu | | |
| E05 | Đặt lịch trùng slot | Trùng | 2 BN cùng BS +-30' | 1 OK, 1 APPOINTMENT_SLOT_TAKEN 409 | | bỏ qua CANCELLED |
| E06 | Đặt lịch ngoài giờ | Business | giờ ngoài start..end | Ném APPOINTMENT_SLOT_TAKEN (sai ngữ nghĩa) | | GAP-8 |
| E07 | Đặt lịch ngày block | Business | schedule_blocks phủ slot | APPOINTMENT_SLOT_TAKEN | | |
| E08 | Slot khả dụng | Sinh slot | GET booking/slots | slot theo slot_minutes; block/đã đặt to available=false | | |
| E09 | BS không có lịch | Fallback | booking/doctors | Trả user role BacSi đang active | | |
| E10 | Lab chỉ VERIFIED | Lọc | có PENDING + VERIFIED | /me/lab-results chỉ trả VERIFIED | | |
| E11 | Encounter detail đủ | Ghép | mở /encounters/{id} | chẩn đoán + kết luận CĐHA (join rad_orders) + lời dặn BS + thuốc | | |
| E12 | Nhắc thuốc từ đơn | Map buổi | freq "3 lần" | SANG/TRUA/TOI (07:00/11:30/19:00); end_date=today+duration; enabled=1 | | |
| E13 | freq "2 lần" | Map | freq 2 lần | SANG+TOI | | |
| E14 | freq lạ | Default | freq "4 lần"/rỗng | Default SANG+TOI (thiếu CHIEU/buổi 4) | | GAP-12 |
| E15 | Toggle nhắc thuốc | Bật/tắt | PUT med-reminders/{id} enabled=false | enabled=0; 204; BN khác to MED_REMINDER_NOT_FOUND | | |
| E16 | Prefs mặc định | Default | notify_prefs_json=NULL | GET trả {push:true,email:true} | | |
| E17 | Queue trạng thái | Nghiệp vụ | có ticket hôm nay WAITING | ticket_no, room_name, số chờ trước, ước tính phút | | |
| E18 | Queue rỗng | Empty | không ticket hôm nay | Trả null (FE empty state) | | |

### F — DB登録
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| F01 | Tạo account cấp mã | INSERT | lễ tân cấp mã BN chưa có account | 1 row portal_accounts; activation_code_hash **BCrypt**; tenant_id server | | |
| F02 | Đặt PIN | UPDATE | activate OK | pin_hash BCrypt; activated_at set; activation_code_hash/expires_at=NULL | | |
| F03 | Ghi session | INSERT | sau login | 1 row portal_sessions jti UNIQUE, expires_at=now+30 ngày | | |
| F04 | OTP reset ghi log | INSERT | forgot-pin (có email) | 1 row otp_log purpose=RESET_PIN, otp_hash BCrypt, hạn 10' | | |
| F05 | Tạo lịch hẹn | INSERT | đặt lịch hợp lệ | 1 row sch_appointments status=PENDING, source=APP, uuid CHAR36, patient_ref=BN | | |
| F06 | tenant_id server gán | Multi-tenant | mọi INSERT | tenant_id = resolve/JWT, KHÔNG từ client body | | |
| F07 | Push subscription | INSERT | POST push-subscriptions | Row nti_web_push_subs; ON DUPLICATE KEY update key; patient_id=BN | | |
| F08 | Nhắc thuốc ghi | INSERT | E12 | N row ptl_med_reminders theo số buổi | | |

### G — 登録後再表示
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| G01 | Lịch mới hiện list | GET me/appointments sau F05 | Lịch mới, status=PENDING, đúng BS | | |
| G02 | Slot đã đặt biến mất | booking/slots sau F05 | Slot vừa đặt available=false | | |
| G03 | Prefs round-trip | PUT rồi GET | {push,email} đúng giá trị lưu | | |
| G04 | Nhắc thuốc hiện list | GET sau E12 | Reminder mới, sắp start_date desc, remind_time asc | | |

### H — 更新・削除
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| H01 | Huỷ lịch (soft) | DELETE me/appointments/{id} >=2h | status=CANCELLED, updated_at đổi; 204 (không xoá cứng) | | |
| H02 | Huỷ lịch <2h | luật thời gian | APPOINTMENT_CANCEL_TOO_LATE 400 | | |
| H03 | Huỷ lịch lạ / BN khác | Cách ly | DELETE uuid lạ | APPOINTMENT_NOT_FOUND 404 (lọc patient_ref+tenant_id) | | |
| H04 | Logout thu hồi phiên | Revoke | POST auth/logout | jti revoke; token cũ 401 ở /me | | |
| H05 | Huỷ push subscription | Delete | DELETE me/push-subscriptions {endpoint} | Xoá theo endpoint+patient_id+tenant_id; 204 | | |

### I — 権限・テナント
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| I01 | Thiếu token | Authorization | GET /me không Bearer | 401 (PortalBearer challenge) | | |
| I02 | Token nội bộ dùng portal | Sai scheme | JWT staff gọi /me | 401 (scheme riêng, thiếu patient_id) | | |
| I03 | Cách ly BN cùng tenant | Multi-tenant | BN A xem me/encounters/{id} của BN B | 404 ENCOUNTER_NOT_FOUND (lọc patient_id) | | |
| I04 | Cách ly đơn thuốc | Multi-tenant | BN A xem me/prescriptions/{idB}/pdf | 404 (lọc patient_id) | | |
| I05 | Cách ly tenant | Cross-tenant | token tenant A, dữ liệu tenant B | Không trả (lọc tenant_id) | | |
| I06 | Resolve tenant sai | Tenant | login-pin Host không map | 400 PORTAL_TENANT_UNRESOLVED | | |
| I07 | Tenant INACTIVE | Tenant | subdomain status<>ACTIVE | resolve 0 to PORTAL_TENANT_UNRESOLVED | | |
| I08 | Cùng SĐT khác tenant | Unique | SĐT X ở tenant A và B | 2 account độc lập (UNIQUE(tenant_id,phone)) | | |
| I09 | Nhắc thuốc BN khác | Cách ly | PUT med-reminders/{idB} | MED_REMINDER_NOT_FOUND | | |
| I10 | Queue BN khác | Cách ly | GET me/queue | Chỉ ticket của chính BN | | |

### J — セキュリティ
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| J01 | Hash PIN | Không plaintext | xem pin_hash | BCrypt, không PIN thô | | |
| J02 | Hash mã/OTP | Không plaintext | activation_code_hash, otp_hash | BCrypt | | |
| J03 | BHYT không lộ | Masking | GET /me | bhyt_number = card_no_masked | | |
| J04 | Brute-force PIN | Rate/lock | thử PIN sai liên tục | Khoá sau 5 lần (D04) | | |
| J05 | Brute-force OTP reset | Rate/lock | thử OTP reset sai nhiều lần | Kỳ vọng khoá/đếm; thực tế **không giới hạn** to dò 6 số trong 10' | | GAP-6 (Cao) |
| J06 | Anti-enum forgot-pin | Ẩn tồn tại | SĐT không có/không email | Luôn 202 | | |
| J07 | SQL injection | Param hoá | phone SQLi | Dapper param; không bypass; 404/401 | | |
| J08 | XSS ghi chú lịch | Chống script | note script tag | Lưu literal; FE render text | | |
| J09 | Token replay sau logout | Revoke | token đã logout | 401 (jti revoked) | | H04 |
| J10 | Rò rỉ PDF đơn thuốc | Cách ly | me/prescriptions/{idB}/pdf | 404 | | I04 |

### K — 文字コード
| No | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| K01 | Tên BN có dấu | Nguyễn Thị Bích Hằng | /me hiển thị đủ dấu (utf8mb4) | | |
| K02 | Tên thuốc/chẩn đoán có dấu | Đái tháo đường típ 2 | Encounter detail đúng | | |
| K03 | Ghi chú lịch + emoji | Khám định kỳ (emoji) | Lưu & hiển thị nguyên vẹn | | |
| K04 | Message lỗi tiếng Việt | login sai | error.message có dấu đầy đủ | | |
| K05 | Buổi nhắc thuốc | SANG/TRUA/CHIEU/TOI | FE map Sáng/Trưa/Chiều/Tối | | |

### L — UI/UX・異常系
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| L01 | Nút disable khi gửi | Chống double-submit | bấm Đăng nhập nhiều lần | isPending disable, nhãn "Đang đăng nhập..." | | |
| L02 | Hiển thị lỗi envelope | Error UI | login PIN sai | Banner đỏ hiện error.message | | |
| L03 | OTP placeholder sai kênh | Nhất quán | màn reset PIN | Placeholder "SMS" nhưng OTP gửi email | | GAP-5 |
| L04 | Label PIN sai spec | Nhất quán | activate B2 | Label "4-6 số" mâu thuẫn BE (đúng 6 số) | | GAP-4 |
| L05 | Mất mạng khi submit | Timeout | ngắt mạng rồi login | Lỗi fallback; không treo | | |
| L06 | Quay lại không lưu | Cancel | activate B2 to Quay lại B1 | Không gọi API, giữ dữ liệu B1 | | |
| L07 | Empty state | UX | không đơn thuốc/kết quả | EmptyState tiếng Việt, không lỗi | | |
| L08 | Đặt lịch quá khứ | Luồng lỗi | appointment_at hôm qua | Kỳ vọng chặn; thực tế BE không validate to tạo lịch quá khứ | | GAP-7 |
| L09 | Đặt lịch không chọn BS | Luồng lỗi | doctor_id=null | BE bỏ qua kiểm tra, tạo doctor_ref=NULL | | GAP-9 |
| L10 | Phiên hết hạn | Re-auth | token 30 ngày hết hạn | 401 to proxy.ts đẩy /login | | |

---

## 3. 境界値一覧
| Đối tượng | min-1 | hợp lệ | max+1 | Ghi chú |
|---|---|---|---|---|
| PIN | 5 số NG | **đúng 6 số** | 7 số (NumPad chặn) | FE chỉ chặn <4 to GAP-2/4 |
| Mã kích hoạt | <8 NG | 8 ký tự | — | FE chỉ chặn <4 to GAP-3 |
| Hạn mã kích hoạt | — | <=72h | >72h INVALID | |
| OTP reset | sai/hết hạn | 6 số, <=10' | >10' 410 | không giới hạn số lần to GAP-6 |
| Sai PIN | 1-4 lần PIN_INVALID | — | >=5 LOCKED 15' | |
| Huỷ lịch | <2h NG | >=2h | — | |

## 4. Defect candidates (đối chiếu 3 tầng)
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| GAP-6 | **Cao** | reset-pin verify OTP **không đếm số lần sai, không khoá** (khác PortalVerifyOtp login có attempts+lock). OTP 6 số, hạn 10' to brute-force khả thi | PortalAuthHandlers.cs PortalResetPinHandler |
| GAP-7 | **Cao** | Đặt lịch **không validate appointment_at là tương lai** to tạo lịch quá khứ qua API | PortalHandlers.cs CreatePortalAppointmentHandler |
| GAP-9 | TB-Cao | doctor_id optional: null to **bỏ qua toàn bộ** kiểm tra slot/trùng, tạo doctor_ref=NULL tuỳ ý | cùng handler, khối if (DoctorId.HasValue) |
| GAP-8 | TB | Slot ngoài giờ/bị block ném APPOINTMENT_SLOT_TAKEN ("đã được đặt") sai ngữ nghĩa; nên code riêng SLOT_UNAVAILABLE | CreatePortalAppointmentHandler |
| GAP-2 | TB | FE cho submit PIN 4-5 số (disable chỉ <4) trong khi BE bắt buộc đúng 6 số to luôn PIN_INVALID, UX xấu | login/activate/reset-pin page.tsx |
| GAP-4 | TB | Label FE "Đặt mã PIN (4-6 số)" mâu thuẫn spec BE (đúng 6 số) | activate/page.tsx:93 |
| GAP-5 | TB | OTP quên PIN gửi email nhưng FE ghi "gửi qua SMS" | login/page.tsx:171, reset-pin |
| GAP-3 | Thấp | FE không chặn đúng độ dài mã kích hoạt (8), chỉ <4 | activate/page.tsx:78 |
| GAP-1 | Thấp | SĐT không validate format ở FE (chỉ <9) lẫn BE to SĐT rác vẫn query | login/page.tsx, handler auth |
| GAP-10 | Thấp | PortalAppointmentCreateRequest.DepartmentId nhận nhưng không dùng/không lưu | PublicApiDtos.cs:213 |
| GAP-11 | Thấp | Push-subscribe không validate endpoint/keys; nti_web_push_subs trộn user_id BINARY(16) + patient_id CHAR36 | PortalNotifyPrefHandlers.cs |
| GAP-12 | Thấp | MapFrequencyToSlots chỉ phủ 1/2/3 lần; freq lạ to default 2 buổi (bỏ CHIEU/buổi 4) | PortalMedReminderHandlers.cs |
| NOTE | Ghi chú | Timezone: Cancel dùng appointment_at - UtcNow; queue dùng DateTime.Today (giờ server) so ticket_date to rủi ro lệch nếu server không +7 | PortalHandlers/PortalMeHandlers |

## 5. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A 初期表示 | 7 | | | |
| B 項目表示 | 5 | | | |
| C 入力チェック | 10 | | | |
| D 境界値 | 8 | | | |
| E 業務ルール | 18 | | | |
| F DB登録 | 8 | | | |
| G 登録後再表示 | 4 | | | |
| H 更新・削除 | 5 | | | |
| I 権限・テナント | 10 | | | |
| J セキュリティ | 10 | | | |
| K 文字コード | 5 | | | |
| L UI/UX・異常系 | 10 | | | |
| **TỔNG** | **100** | | | |
