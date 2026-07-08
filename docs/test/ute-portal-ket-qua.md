# Kết quả THỰC THI UTC (UTE / 単体テスト実施結果) — Portal bệnh nhân

> Spec case: [utc-portal-benh-nhan.md](utc-portal-benh-nhan.md). Quy ước: [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md).
> **Môi trường thực thi:** deploy CỤC BỘ (staging-like) — backend `.NET` (127.0.0.1:5080) + `portal-client` Next.js (localhost:3333) + **MySQL thật `prodiab_his`**. Test **UI thật** bằng trình duyệt Chromium (Playwright), thiết bị mobile 390×844.
> **Tài khoản test:** bệnh nhân `BN00001 — Trần Văn Bình` (SĐT `0912111001`, PIN `246810`), phòng khám tenant 1 (subdomain `diabetis`). Ngày: 2026-07-08.
> Evidence ảnh: thư mục [ute-shots/portal/](ute-shots/portal/) (15 ảnh, đặt tên theo luồng).

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
| Trang chủ | tên BN + lịch hẹn sắp tới + 4 thẻ + bottom-nav | **OK** | 03 |
| Hàng đợi + nhắc tới lượt | số 001, phòng, đang gọi, còn X người, banner "Sắp tới lượt" | **OK** | 04 |
| Đặt lịch (slot bác sĩ) | Bước 1/3 chọn BS (từ `booking/doctors`) | **OK** | 06 |
| Lịch hẹn (danh sách) | list lịch hẹn | **OK** (⚠️ D-PORTAL-01) | 05 |
| Lịch sử khám + chi tiết | list + chi tiết (chẩn đoán/kết luận/lời dặn/thuốc) | **OK** (⚠️ D-PORTAL-02,03) | 07, 08 |
| Đơn thuốc | list đơn + tải PDF | **OK** (⚠️ D-PORTAL-01) | 09 |
| Kết quả XN (chỉ VERIFIED) | "Đường huyết lúc đói 12.4 mmol/L (HIGH)" + PDF | **OK** | 10 |
| Nhắc uống thuốc | lịch theo buổi + bật/tắt | **OK** (render) | 11 |
| Hồ sơ cá nhân | thông tin BN + BHYT + đăng xuất | **OK** | 12 |
| Cài đặt thông báo | toggle push/email + hướng dẫn A2HS iOS | **OK** | 13 |

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
