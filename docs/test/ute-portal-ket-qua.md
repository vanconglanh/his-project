# Kết quả THỰC THI UTC (UTE / 単体テスト実施結果) — Portal bệnh nhân

> Spec case: [utc-portal-benh-nhan.md](utc-portal-benh-nhan.md). Quy ước: [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md).
> **Môi trường thực thi:** deploy CỤC BỘ (staging-like) — backend `.NET` (127.0.0.1:5080) + `portal-client` Next.js (localhost:3333) + **MySQL thật `prodiab_his`**. Test **UI thật** bằng trình duyệt Chromium (Playwright), thiết bị mobile 390×844.
> **Tài khoản test:** bệnh nhân `BN00001 — Trần Văn Bình` (SĐT `0912111001`, PIN `246810`), phòng khám tenant 1 (subdomain `diabetis`). Ngày: 2026-07-08.
> Evidence ảnh (đợt UTE gốc): thư mục [ute-shots/portal/](ute-shots/portal/) (15 ảnh, đặt tên theo luồng).
> **CẬP NHẬT 22/08/2026 — đã chụp lại evidence UI:** bộ `ute-shots/DIAB-*.png` + `ute-shots/scan/*.png` được chụp lại trên **build production** của `portal-client` để khớp layout hiện tại (4 thẻ lớn + panel “Tiện ích” 4 icon + bottom-nav 5 tab). Chi tiết: [mục 4](#4-chụp-lại-evidence-ui-22082026).

---

## 0. Lưu ý contract (đọc trước)
- **API `snake_case`** (`JsonNamingPolicy.SnakeCaseLower`). `portal-client/lib/api.ts` tự chuyển 2 chiều (request camel→snake, response snake→camel) — đã verify hoạt động (mọi field render đúng).
- **Tenant theo subdomain** (không JWT lúc login): request ẩn danh resolve `Host`→`sys_tenants.subdomain`; dev gửi header `X-Portal-Subdomain=diabetis`. Production dùng subdomain thật (same-origin).
- **Đăng nhập KHÔNG mật khẩu**: mã kích hoạt (lễ tân cấp) → PIN 6 số → phiên 30 ngày.
- Web Push: RFC 8291/8292 (đã verify test vector chính thức — xem `WebPushCryptoTests`, 556/556 unit test PASS).

---

## 1. Tóm tắt kết quả (TỔNG QUAN)

| Nhóm chức năng | Case chính | 判定 | Evidence |
|---|---|---|---|
| Đăng nhập PIN (happy path) | SĐT + PIN đúng → vào `/` | **OK** | 01, 02, 03 |
| Đăng nhập PIN (sai PIN) | PIN sai → báo lỗi, không vào | **OK** | 15 |
| Kích hoạt tài khoản | màn 3 bước (SĐT+mã→PIN→xong) | **OK** (render) | 14 |
| Tenant resolve subdomain | `tenant-info` trả đúng phòng khám + VAPID | **OK** | (API 200) |
| Trang chủ | tên BN + lịch hẹn sắp tới + 4 thẻ + bottom-nav *(UI bản 08/07/2026)* | **OK** | 03 |
| Hàng đợi + nhắc tới lượt | số 001, phòng, đang gọi, còn X người, banner "Sắp tới lượt" | **OK** | 04 |
| Đặt lịch (slot bác sĩ) | Bước 1/3 chọn BS (từ `booking/doctors`) | **OK** | 06 |
| Lịch hẹn (danh sách) | list lịch hẹn | **OK** (⚠️ D-PORTAL-01) | 05 |
| Lịch sử khám + chi tiết | list + chi tiết (chẩn đoán/kết luận/lời dặn/thuốc) | **OK** (⚠️ D-PORTAL-02,03) | 07, 08 |
| Đơn thuốc | list đơn + tải PDF | **OK** (⚠️ D-PORTAL-01) | 09 |
| Kết quả XN (chỉ VERIFIED) | "Đường huyết lúc đói 12.4 mmol/L (HIGH)" + PDF | **OK** | 10 |
| Nhắc uống thuốc | lịch theo buổi + bật/tắt | **OK** (render) | 11 |
| Hồ sơ cá nhân | thông tin BN + BHYT + đăng xuất | **OK** | 12 |
| Cài đặt thông báo | toggle push/email + hướng dẫn A2HS iOS | **OK** | 13 |

> ⚠️ **Trang chủ đã đổi sau đợt UTE này.** UI hiện tại (từ commit `d36ec26`): **4 thẻ lớn** (Hàng đợi / Đặt lịch / Kết quả / Hồ sơ) **+ panel “Tiện ích”** gồm 4 icon (Đơn thuốc / Lịch sử khám / Nhắc thuốc / Sức khoẻ) **+ bottom-nav 5 tab** (Trang chủ / Hàng đợi / **Sức khoẻ** / Đặt lịch / Hồ sơ). Evidence đúng layout hiện tại: `DIAB-portal-home.png`, `DIAB-04-tienich.png`, `DIAB-03-footer.png` — xem [mục 4](#4-chụp-lại-evidence-ui-22082026).

**Cách ly dữ liệu (bảo mật):** mọi endpoint `/me/*` lọc `tenant_id + patient_id` từ token — verify bằng API (login-pin SĐT chưa đăng ký → 404 `PORTAL_PHONE_NOT_REGISTERED`; token của BN chỉ thấy dữ liệu của chính BN đó). **OK.**

**Tiêu chí PASS:** các luồng mức Cao (đăng nhập, hàng đợi, kết quả, cách ly tenant/BN) **OK**. Còn 3 defect mức Thấp (giao diện/seed-data), không chặn.

---

## 2. Defect phát hiện khi UTE (đã fix ngay trong đợt test)

| ID | Mức | Màn | Mô tả | Root cause | Trạng thái |
|---|---|---|---|---|---|
| **D-PORTAL-01** | Thấp | Lịch hẹn / Đơn thuốc (list) | React cảnh báo *"two children with the same key `00000000-0000-...`"* → nhiều item cùng key rỗng | Bản ghi seed có `uuid`/`id` NULL → BE trả `Guid.Empty`; FE dùng `id` làm React key → trùng | ✅ **ĐÃ FIX** — FE key `${id}-${idx}` (appointments/prescriptions); re-test console **sạch** |
| **D-PORTAL-02** | Thấp | Chi tiết khám / Đặt lịch / Trang chủ… | Tên bác sĩ hiển thị **"BS. BS. Nguyễn Văn An"** (lặp tiền tố "BS.") | `sec_users.full_name` đã chứa "BS. "; UI thêm "BS. " lần nữa (7 chỗ) | ✅ **ĐÃ FIX** — bỏ prefix "BS." literal ở FE (6 file); re-test hiển thị "BS. Nguyễn Văn An" |
| **D-PORTAL-03** | Info | Chi tiết khám | Lượt khám seed (e0000001-…-012) hiển thị thưa (không chẩn đoán/kết luận/lời dặn/thuốc) | Dữ liệu seed lượt khám này rỗng EMR/đơn — KHÔNG phải lỗi code (query đã verify đúng với lượt có dữ liệu) | ⚪ Không phải bug — cần seed lượt có đơn+chẩn đoán để evidence "lời dặn" đầy đủ |

> Sau fix D-PORTAL-01/02: chạy lại evidence các màn liên quan → **CONSOLE_ERRORS: none**. Ghi chú: 1 request **400** trong đợt đầu là **case âm cố ý** (sai PIN — case No.15), đúng kỳ vọng.

## 2b. Defect THIẾT KẾ (đối chiếu 3 tầng trong UTC) — dev đã fix + re-verify

Tài liệu [utc-portal-benh-nhan.md](utc-portal-benh-nhan.md) phát hiện 12 GAP qua đối chiếu FE/BE/DB. Đã fix 3 GAP mức Cao/TB-Cao (bảo mật + đúng đắn), verify runtime:

| GAP | Mức | Mô tả | Fix | Verify |
|---|---|---|---|---|
| **GAP-6** | Cao | Reset-PIN OTP **không đếm lần sai / không khoá** → brute-force OTP 6 số | Thêm đếm `attempts` + khoá tài khoản 15' sau 5 lần sai (giống login OTP) | Compile + logic (mirror login lockout đã kiểm chứng) |
| **GAP-7** | Cao | Đặt lịch **không chặn thời điểm quá khứ** | Chặn `appointment_at <= now` → 400 `APPOINTMENT_IN_PAST` | ✅ Runtime: quá khứ → **400 IN_PAST** |
| **GAP-9** | TB-Cao | `doctor_id` null → **bỏ qua toàn bộ** validate slot/lịch/trùng | Bắt buộc chọn bác sĩ → 400 `APPOINTMENT_DOCTOR_REQUIRED` | ✅ Runtime: thiếu BS → **400 DOCTOR_REQUIRED**; happy (tương lai+BS+đúng slot) → **201** |

Còn lại (GAP-1..5, 8, 10..12 mức TB-Thấp: FE label/format validate, `DepartmentId` field chết, map frequency thiếu buổi…) — ghi nhận trong UTC để xử lý đợt sau, không chặn.

## 2c. Defect MỚI phát hiện khi chụp lại evidence (22/08/2026) — **chưa fix**

> Phát hiện khi chạy lại toàn bộ màn `portal-client` trên build production để chụp evidence mới (xem [mục 4](#4-chụp-lại-evidence-ui-22082026)). Đều mức **Thấp/Info**, không chặn phát hành.

| ID | Mức | Màn | Mô tả | Root cause | Giao cho |
|---|---|---|---|---|---|
| **D-PORTAL-04** | Thấp | Bottom-nav (màn Nhắc thuốc) | Ở `/medications`, tab **Hồ sơ** bị sáng (`aria-current="page"`) dù không ở màn Hồ sơ | `components/BottomNav.tsx` dùng `pathname.startsWith(href)`; `"/medications".startsWith("/me")` → `true` | frontend |
| **D-PORTAL-05** | Thấp | Cài đặt thông báo | Nút gạt “Thông báo đẩy (push)” khi BẬT bị **lòi núm ra ngoài rãnh ~2px** (trông như bị cắt) | `<label class="relative inline-flex h-8 w-14 …">` thiếu `shrink-0`; mô tả dài 2 dòng ép flex item co từ 63px → 56.5px, núm `absolute` theo `rem` không co → tràn | frontend |
| **D-PORTAL-06** | Info | Toàn app | `GET /favicon.ico` → **404** → 1 lỗi console trên mọi trang | Thiếu `portal-client/public/favicon.ico` | frontend |

**Gợi ý fix:**
- **D-PORTAL-04** — `portal-client/components/BottomNav.tsx`: đổi `pathname.startsWith(href)` thành `pathname === href || pathname.startsWith(href + "/")`.
- **D-PORTAL-05** — thêm `shrink-0` vào `<label>` của toggle trong `app/settings/notifications/page.tsx` và `app/medications/page.tsx`.
- **D-PORTAL-06** — thêm `portal-client/public/favicon.ico` (hoặc `app/icon.png`) để hết 404.

---

## 3. Evidence

### 3a. Evidence TỪNG STEP có khoanh focus — [ute-shots/portal-steps/](ute-shots/portal-steps/) (CHUẨN)
Mỗi ảnh = 1 step, **banner xanh** `[Mã case] 観点 · 期待: <kết quả mong đợi>`, **khoanh đỏ (#ef4444)** vùng cần confirm — đúng chuẩn `ute-evidence.spec.ts`. Manifest: [manifest.jsonl](ute-shots/portal-steps/manifest.jsonl).

| # | Mã case | 観点 · Step | Vùng khoanh focus |
|---|---|---|---|
| 01 | ACT-A01 | Load màn kích hoạt | Form kích hoạt |
| 02 | LOGIN-A01 | 初期表示 load đăng nhập | Tiêu đề "Cổng bệnh nhân" |
| 03 | LOGIN-L01 | 異常系 sai PIN | Lỗi "Mã PIN không đúng" |
| 04 | LOGIN-B01 | Nhập SĐT | Ô số điện thoại |
| 05 | LOGIN-B02 | Nhập PIN 6 số | Bàn phím/chấm PIN |
| 06 | HOME-A02 | Trang chủ sau đăng nhập | "Xin chào + tên BN" |
| 07 | HOME-A03 | Thẻ Hàng đợi | Thẻ Hàng đợi |
| 08 | QUEUE-A01 | Số thứ tự của tôi | Card số 001 |
| 09 | QUEUE-E01 | Nhắc sắp tới lượt | Banner "Sắp tới lượt" |
| 10 | BOOK-A01 | Đặt lịch bước 1 | Danh sách bác sĩ |
| 11 | LAB-A01 | Kết quả XN VERIFIED | Giá trị 12.4 mmol/L |
| 12 | RX-A01 | Đơn thuốc | Nút tải PDF |
| 13 | MED-A01 | Nhắc uống thuốc | Lịch theo buổi |
| 14 | ME-A01 | Hồ sơ cá nhân | Tên bệnh nhân |
| 15 | NOTI-A01 | Cài đặt thông báo | Toggle push/email |

### 3b. Evidence toàn màn (bổ trợ) — ute-shots/portal/
| Ảnh | Nội dung |
|---|---|
| 01-dang-nhap | Màn đăng nhập (SĐT + NumPad PIN) |
| 02-nhap-sdt-pin | Đã nhập SĐT + PIN |
| 03-trang-chu | Trang chủ (tên BN, lịch hẹn, 4 thẻ) |
| 04-hang-doi-so-thu-tu | Hàng đợi: số 001, phòng, đang gọi, banner sắp tới lượt |
| 05-lich-hen-danh-sach | Danh sách lịch hẹn |
| 06-dat-lich-chon-bac-si | Đặt lịch Bước 1/3 — chọn bác sĩ |
| 07-lich-su-kham | Lịch sử khám |
| 08-ket-qua-kham-loi-dan | Chi tiết kết quả khám |
| 09-don-thuoc | Danh sách đơn thuốc |
| 10-ket-qua-xet-nghiem | Kết quả XN (VERIFIED, đường huyết 12.4 HIGH) |
| 11-nhac-uong-thuoc | Nhắc uống thuốc theo buổi |
| 12-ho-so-ca-nhan | Hồ sơ cá nhân |
| 13-cai-dat-thong-bao | Cài đặt thông báo (push/email + A2HS) |
| 14-kich-hoat-tai-khoan | Màn kích hoạt tài khoản |
| 15-sai-pin-bao-loi | Case âm: sai PIN → báo lỗi |

---

## 4. Chụp lại evidence UI (22/08/2026)

### 4.1 Lý do chụp lại
QC chặn track `portal-client` vì bộ 20 ảnh thêm ở commit `7576e99` **không khớp code hiện tại**:

| Vấn đề | Ảnh liên quan | Bằng chứng |
|---|---|---|
| Chụp ở **dev mode** trên bản build **CŨ**: bottom-nav chỉ **4 tab** (thiếu **Sức khoẻ**), Trang chủ **chưa có panel “Tiện ích”**, có **dev indicator** của Next.js góc trái dưới | `DIAB-portal-home`, `DIAB-portal-queue`, `DIAB-mint-appt`, `DIAB-mint-me`, `scan/*` | Tab “Sức khoẻ” thêm từ `70fff75`, panel “Tiện ích” từ `d36ec26` — cả hai đều **trước** commit chứa ảnh |
| **Ảnh trắng hoàn toàn** 7 KB, trùng md5 `710ab445…` | `DIAB-02-home`, `DIAB-03-footer` | Ảnh hợp lệ khác 90–250 KB |
| Chụp tính năng **đã gỡ** (dải “Chỉ số sức khoẻ” trên Trang chủ, gỡ ở `ca66b39`) | `DIAB-trends-home` | Đã **xoá file** — tính năng nay nằm ở tab Sức khoẻ (`DIAB-health.png`) |
| Không tài liệu nào tham chiếu 20 ảnh | tất cả | Mục 4 này bổ sung tham chiếu |

### 4.2 Môi trường chụp lại
| Hạng mục | Giá trị |
|---|---|
| Bản build | `portal-client` **production** (`next build` + `next start`, Next.js 16.2.6) — **không** dev mode, **không** dev indicator |
| Địa chỉ | `http://localhost:3010` |
| Trình duyệt | Playwright 1.60 + **Google Chrome** hệ thống (`channel: "chrome"`) |
| Thiết bị | mobile **390×844** CSS, `deviceScaleFactor: 2` → ảnh rộng **780 px** (đồng bộ quy ước ảnh cũ) |
| Phiên đăng nhập | cookie `portal-token` (proxy `portal-client/proxy.ts` chỉ kiểm tra **có/không** cookie, không verify JWT) |
| Nguồn dữ liệu | ⚠️ **API stub cục bộ** `http://localhost:5099/api/portal/v1` |

> **⚠️ Ghi rõ nguồn dữ liệu (đọc kỹ trước khi dùng làm bằng chứng):**
> Máy chạy đợt kiểm thử này **không có Docker/MySQL** → không dựng được backend `.NET` + DB `prodiab_his` thật; deploy production `https://hisapp.diab.com.vn` **không truy cập được** từ máy này (curl timeout 15s).
> Vì vậy dữ liệu hiển thị đến từ **API stub cục bộ**, lặp lại đúng bộ seed đã dùng ở UTE 08/07/2026 (BN00001 — Trần Văn Bình, số thứ tự 001, BS. Nguyễn Văn An).
> ⇒ Bộ ảnh 22/08/2026 **chứng minh LAYOUT/UI hiện tại** của `portal-client` (cấu trúc màn, điều hướng, nhãn tiếng Việt, badge trạng thái).
> ⇒ **KHÔNG** dùng để chứng minh backend/DB/cách ly tenant — phần đó vẫn dựa vào bộ `portal/` + `portal-steps/` chạy trên **DB thật** ngày 08/07/2026.

### 4.3 Danh sách ảnh đã chụp lại

| Ảnh | Màn | Kích thước | Nội dung xác nhận |
|---|---|---|---|
| `DIAB-portal-home.png` | Trang chủ (đầy đủ) | 780×2212 | **4 thẻ lớn** + **panel “Tiện ích” 4 icon** + bottom-nav **5 tab** |
| `DIAB-02-home.png` | Trang chủ (khung 390×844) | 780×1688 | Trang chủ như trên điện thoại thật (panel Tiện ích nằm dưới nếp gấp) |
| `DIAB-03-footer.png` | Bottom-nav (cắt cận) | 780×156 | Đủ **5 tab**: Trang chủ · Hàng đợi · **Sức khoẻ** · Đặt lịch · Hồ sơ |
| `DIAB-04-tienich.png` | Panel “Tiện ích” (cắt cận) | 708×422 | Đúng **4 icon**: Đơn thuốc · Lịch sử khám · Nhắc thuốc · Sức khoẻ |
| `DIAB-01-login.png` | Đăng nhập | 780×1688 | “Cổng bệnh nhân” + SĐT + NumPad PIN |
| `DIAB-portal-queue.png` = `scan/queue.png` | Hàng đợi | 780×1688 | Số **001**, phòng, đang gọi **000**, còn 1 người, banner “Sắp tới lượt của bạn!” |
| `DIAB-health.png` | Sức khoẻ | 780×2984 | Lưới 7 chỉ số + biểu đồ + Lần trước/Gần nhất/Trung bình |
| `DIAB-mint-appt.png` = `scan/appointments.png` | Lịch hẹn | 780×1704 | Badge tiếng Việt “Đã xác nhận” / “Đã khám” (đã hết `PENDING`/`CONFIRMED` thô) |
| `DIAB-mint-me.png` = `scan/me.png` | Hồ sơ | 780×2882 | Thông tin BN + số thẻ BHYT + Đăng xuất |
| `scan/appt-new.png` | Đặt lịch — Bước 1/3 | 780×1704 | Danh sách bác sĩ |
| `scan/encounters.png` | Lịch sử khám | 780×1704 | 2 lượt khám + chẩn đoán ICD |
| `scan/prescriptions.png` | Đơn thuốc | 780×1704 | Đơn `DT000031` + nút “Tải PDF đơn thuốc” |
| `scan/lab.png` | Kết quả XN/CLS | 780×1704 | 2 kết quả “Đã duyệt” + nút “Tải PDF kết quả” |
| `scan/medications.png` | Nhắc uống thuốc | 780×1728 | Nhóm theo buổi Sáng/Tối + toggle bật/tắt |
| `scan/settings.png` | Cài đặt thông báo | 780×1720 | Toggle push/email + “Bật thông báo trên thiết bị này” |

**Đã xoá:** `DIAB-trends-home.png` (chụp tính năng đã gỡ khỏi Trang chủ).

### 4.4 Kiểm chứng tự động kèm theo (chạy cùng lúc chụp ảnh)

| Kiểm chứng | Kết quả |
|---|---|
| Panel “Tiện ích” tồn tại trên Trang chủ | ✅ 1 panel, đúng **4** liên kết: Đơn thuốc \| Lịch sử khám \| Nhắc thuốc \| Sức khoẻ |
| Số thẻ lớn trên Trang chủ | ✅ **4** thẻ: Hàng đợi (Số 001) \| Đặt lịch \| Kết quả \| Hồ sơ |
| Bottom-nav trên **12/12** màn cần đăng nhập | ✅ đều **5 tab**: Trang chủ \| Hàng đợi \| Sức khoẻ \| Đặt lịch \| Hồ sơ |
| Chặn truy cập khi **chưa đăng nhập** (không cookie `portal-token`) | ✅ `GET /` → redirect `**/login?redirect=%2F**` |
| Lỗi console / lỗi JS | ✅ **0** trên mọi màn sau đăng nhập (chỉ còn 404 `/favicon.ico` — xem D-PORTAL-06) |
| HTTP ≥ 400 khi tải màn | ✅ none |
| Hiệu năng tải Trang chủ (`goto` → thấy panel “Tiện ích”) | ✅ **1015 ms** (bao gồm cold start route lần đầu) |
| Tab đang chọn (`aria-current="page"`) đúng route | ⚠️ sai ở `/medications` → xem **D-PORTAL-04** |

### 4.5 Lưu ý về evidence cũ (giữ nguyên, không ghi đè)
- **`ute-shots/portal/` (15 ảnh) và `ute-shots/portal-steps/` (15 ảnh)**: là hồ sơ UTE ngày **08/07/2026** chạy trên **DB thật** → **giữ nguyên làm bằng chứng lịch sử**. UI trong các ảnh này là bản 08/07/2026 (**theme xanh dương**, bottom-nav **4 tab**, Trang chủ chưa có panel “Tiện ích”) — **không phản ánh UI hiện tại**.
- **`PROD-diab-home.png`, `PROD-diab-login.png`**: ảnh bản **deploy production** `https://hisapp.diab.com.vn` ngày 08/07/2026 — cũng là **layout cũ (bottom-nav 4 tab)**. Không chụp lại được (domain không truy cập được từ máy kiểm thử).
  ⚠️ **Việc cần làm:** production đang chạy bản **cũ hơn** code trên `main` → cần **redeploy** rồi chụp lại `PROD-diab-*` thì evidence production mới khớp code.
