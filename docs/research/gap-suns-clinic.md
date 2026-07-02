# GAP Analysis — SUNS Clinic Service - Advance vs Pro-Diab Backlog

> So sánh feature list **SUNS Clinic Advance** (HIS 1 phòng khám đang dùng thực tế) với 88 user story hiện có trong [requirement-backlog.md](requirement-backlog.md).
> Mục tiêu: phát hiện feature SUNS có mà Pro-Diab backlog chưa cover → bổ sung.
>
> Ký hiệu: ✅ đã cover đầy đủ / ⚠️ cover một phần / ❌ thiếu

---

## 1. 11 chức năng chính SUNS — đối chiếu module

| # | Feature SUNS | Story tương ứng (Pro-Diab) | Trạng thái | Đề xuất bổ sung |
|---|---|---|---|---|
| 1 | Tiếp nhận bệnh nhân | US-R01..R07, US-PT01..PT06 | ✅ | — |
| 2 | Thu ngân | US-C01..C08 | ✅ | — |
| 3 | Khám chữa bệnh (EMR) | US-E01..E09 | ✅ | — |
| 4 | Cận lâm sàng (CĐHA) | US-L01..L07 | ✅ | — |
| 5 | Khoa xét nghiệm (LIS) | US-L01..L07 | ⚠️ | Thiếu “danh mục đơn vị gửi mẫu / lab thứ 3” (mục III) |
| 6 | Kho – Quản lý kho dược | US-PH01..PH12 | ✅ | — |
| 7 | Cấp phát thuốc | US-PH06 | ✅ | — |
| 8 | Quản lý toa gửi cổng Dược QG | US-P01..P10 | ✅ | — |
| 9 | Báo cáo | US-RP01..RP06 | ✅ | — |
| 10 | Web portal tra cứu kết quả khám (EMR portal cho BN) | US-PT08 (COULD) | ⚠️ | Nâng độ ưu tiên + thêm story portal tra cứu kết quả CLS bằng mã/OTP |
| 11 | Quản trị hệ thống | US-T01..T04, US-U01..U07 | ✅ | — |

---

## 2. Customize / New Feature — đối chiếu chi tiết

### I. Form nhận bệnh (Đăng ký khám)

| Feature SUNS | Story Pro-Diab | Trạng thái | Đề xuất |
|---|---|---|---|
| Textarea “Ghi chú” hiển thị xuyên phòng ban | (không có) | ❌ | Thêm story: ghi chú tiếp đón sticky cross-module |
| Upload ảnh đại diện BN | (không có) | ❌ | Thêm story: avatar BN |
| Button “Upload kết quả CLS” từ DS đăng ký (popup chọn loại hồ sơ + file PNG/JPEG) | (không có) | ❌ | Thêm story: lễ tân upload file CLS bên ngoài vào hồ sơ |
| Danh sách file CLS upload (ngày, loại, hình, người upload) | (không có) | ❌ | Thêm story: list/view/download file CLS đã upload |
| BS xem file CLS upload trong EMR (tab “Kết quả CLS file Upload”) | US-L04 (upload từ KTV) | ⚠️ | Thêm story: EMR có tab riêng xem file CLS từ nguồn lễ tân/ngoài |

### II. API cho bên thứ 3

| Feature SUNS | Story Pro-Diab | Trạng thái | Đề xuất |
|---|---|---|---|
| API đăng ký bệnh nhân | (không có public API) | ❌ | Thêm story API public + API key |
| API đặt lịch khám | US-R03 (internal) | ⚠️ | Thêm story: expose API đặt lịch ra ngoài |
| API danh mục gói khám | (không có) | ❌ | Thêm story: gói khám + API tra cứu |
| API danh mục dịch vụ (thuốc, vật tư, CLS) | (không có public API) | ❌ | Thêm story API danh mục |
| API tra cứu nhật ký khám | US-PT04 (UI only) | ⚠️ | Thêm story: API lịch sử khám cho bên ngoài |

### III. API kết nối lab bên ngoài (LIS thứ 3)

| Feature SUNS | Story Pro-Diab | Trạng thái | Đề xuất |
|---|---|---|---|
| Gửi chỉ định XN sang lab thứ 3 | (không có) | ❌ | Thêm story: send order lab ngoài |
| Nhận kết quả XN từ lab thứ 3 | US-L06 (HL7/ASTM máy XN, COULD) | ⚠️ | Thêm story: webhook/API nhận kết quả từ lab partner |
| Danh mục đơn vị gửi mẫu | (không có) | ❌ | Thêm story: master data lab partner |

### IV. Push Notification trình duyệt

| Feature SUNS | Story Pro-Diab | Trạng thái | Đề xuất |
|---|---|---|---|
| Push notification khi có đăng ký từ API | US-L05 (notify CLS) | ⚠️ | Thêm story: framework push notify trình duyệt (Web Push) |
| Cấu hình giao diện thông báo per user (vị trí, âm thanh) | (không có) | ❌ | Thêm story: user setting notification |
| Ẩn cột BHYT/Sổ khám/VIP/Miễn CK/Dân tộc/Nghề nghiệp theo cấu hình phòng khám | (không có) | ❌ | Thêm story: cấu hình hiển thị cột theo tenant |

### V. Form Điều dưỡng (mới)

| Feature SUNS | Story Pro-Diab | Trạng thái | Đề xuất |
|---|---|---|---|
| Form Điều dưỡng riêng: tìm kiếm + lọc ngày + lịch sử khám + nhập sinh hiệu | US-E02 (BS nhập sinh hiệu) | ❌ | Thêm story: vai trò/form điều dưỡng độc lập |
| Sinh hiệu lưu nhiều record/lần khám | US-E02 (1 record/encounter) | ⚠️ | Thêm story: bảng `his_vital_sign` 1-N với encounter |
| Xem nhật ký sinh hiệu | (không có) | ❌ | Thêm story: timeline sinh hiệu trong EMR |

### VI. EMR bổ sung

| Feature SUNS | Story Pro-Diab | Trạng thái | Đề xuất |
|---|---|---|---|
| Sinh hiệu mặc định = record mới nhất từ điều dưỡng | — | ❌ | Phụ thuộc story sinh hiệu N record |
| Tab “Xem kết quả CLS upload” | — | ❌ | (đã ghi ở mục I) |

### VII. Thu ngân

| Feature SUNS | Story Pro-Diab | Trạng thái | Đề xuất |
|---|---|---|---|
| Thanh toán Visa/Master | US-C03 (QR/VNPay/Momo) | ⚠️ | Thêm story: gateway thẻ quốc tế (Stripe/Onepay) |
| Tạo mã QR thanh toán động (số tiền tự bind) | US-C03 | ⚠️ | Thêm story: QR động VietQR per hóa đơn |

### Tiện ích hosting (SaaS bundle)

| Feature SUNS | Story Pro-Diab | Trạng thái | Đề xuất |
|---|---|---|---|
| Subdomain riêng `tenphongkham.suns.com.vn` | (không có) | ❌ | Thêm story: tenant subdomain + wildcard DNS |
| SSL tự động | (không có) | ❌ | Thêm story: Let’s Encrypt wildcard / cert-manager |
| Quota storage 20GB | (không có) | ❌ | Thêm story: storage quota + cảnh báo |
| Gia hạn license tự động | (không có) | ❌ | Thêm story: subscription/billing auto-renew |

---

## 3. Tổng kết GAP

- **Đã cover (✅):** 8/11 chức năng chính + nhiều feature trùng.
- **Cover một phần (⚠️):** 9 feature cần nâng cấp story.
- **Thiếu (❌):** 17 feature cần bổ sung story mới.
- **Tổng story bổ sung:** 22 story mới (xem `requirement-backlog.md` Module 12).

### 3 gap quan trọng nhất

1. **Sinh hiệu nhiều record + vai trò Điều dưỡng riêng** — pattern phổ biến tại phòng khám VN, backlog hiện chỉ cho BS nhập 1 lần.
2. **Public API cho bên thứ 3 (App/Web/Cam AI/Lab)** — SUNS đã có, Pro-Diab chưa thiết kế cổng API + API key + rate-limit per partner.
3. **Upload file CLS từ lễ tân (ngoài) + tab xem trong EMR** — luồng phổ biến khi BN mang kết quả từ phòng khám khác đến.
