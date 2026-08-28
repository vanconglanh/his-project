# Tổng hợp phiên làm việc — Bổ sung chức năng theo SRS Phòng khám Nội/Nội tiết

> Ngày thực hiện: 2026-08-25 → 2026-08-28
> Nguồn yêu cầu: `SRS-HIS-phong-kham-noi-noi-tiet_V2.docx`
> Branch: `sys_phong_kham_noi` (base từ `claude/his-system-functions-review-aaf6ef`)
> Commit chính: `8fa4336`

---

## 1. Bối cảnh

Đối chiếu SRS (phòng khám chuyên khoa Nội/Nội tiết, đa chi nhánh, không BHYT) với codebase Pro-Diab HIS hiện tại (multi-tenant, có BHYT). Phát hiện gap lớn nhất: hệ thống dùng model **Tenant = 1 phòng khám**, không có khái niệm **Branch (chi nhánh)** — không đáp ứng được yêu cầu đa chi nhánh dùng chung 1 DB của SRS.

Toàn bộ việc dưới đây được làm theo quy trình: **architect thiết kế trước (ra file `docs/erd/*.md`) → xác nhận quyết định nghiệp vụ với PO (qua AskUserQuestion) → backend triển khai theo đúng thiết kế → build + review tĩnh**.

---

## 2. Danh sách chức năng đã triển khai (backend)

### P0 — Bắt buộc, chặn go-live

| # | Chức năng | Migration | File thiết kế |
|---|---|---|---|
| 1 | **Đa chi nhánh (Branch)** — tách khỏi Tenant, mỗi Tenant có N Branch dùng chung DB, Patient toàn cục xuyên chi nhánh | `9080`-`9087` | [branch-multi-chi-nhanh.md](erd/branch-multi-chi-nhanh.md) |
| 2 | **FR-101** — Dedup bệnh nhân theo CCCD (hash SHA-256) hoặc SĐT+tên+ngày sinh; bắt buộc thông tin người giám hộ nếu bệnh nhân <72 tháng tuổi | `9088` | — |
| 3 | **FR-302/402** — Kiến trúc adapter ký số độc lập nhà cung cấp (`IDigitalSignatureProvider`), thay thế mock cứng, sẵn sàng cắm VNPT SmartCA khi có sandbox thật | `9089` | — |

### P1 — Ưu tiên cao

| # | Chức năng | Migration | File thiết kế |
|---|---|---|---|
| 4 | **FR-311** — Annotation ảnh lâm sàng (layer JSON đè lên ảnh, không sửa ảnh gốc) | `9090` | — |
| 5 | **FR-511/512** — Cảnh báo SLA xét nghiệm trễ hạn + đối soát công nợ/hoa hồng đối tác XN theo kỳ | `9091` | — |
| 6 | **FR-1201–1206** — Hệ thống **Gói dịch vụ & định mức** đầy đủ: template gói, bán gói (thanh toán đủ/cọc ≥50%), thu nốt, tự động trừ định mức tại 3 điểm (check-in/CLS/kê đơn), huỷ gói + hoàn tiền theo tỷ lệ chưa dùng, cảnh báo sắp hết hạn/định mức/công nợ | `9092`-`9095` | [goi-dich-vu-dinh-muc.md](erd/goi-dich-vu-dinh-muc.md) |
| 7 | **FR-801–803** — Tích hợp **Telehealth với Docosan** (hệ thống thật của công ty): đặt lịch tư vấn từ xa, lấy link video, đồng bộ trạng thái qua polling job (Docosan không có webhook đối tác) | `9096`-`9097` | [telehealth-docosan.md](erd/telehealth-docosan.md) |

### P2 — Ưu tiên thấp

| # | Chức năng | Migration |
|---|---|---|
| 8 | **FR-711** — Khung tích hợp thiết bị CGM/đo đường huyết (interface `ICgmDeviceProvider`, adapter mẫu Dexcom OAuth2) | `9098` |

---

## 3. Quyết định nghiệp vụ đã chốt với PO trong phiên

1. Phạm vi Branch: làm **đầy đủ** theo SRS (không chỉ nền tảng) — gắn `branch_id` vào User/Appointment/MedicalRecord/Invoice/InventoryItem, thêm quyền `branch.cross_view`.
2. Điều chuyển kho liên chi nhánh: **chưa làm** trong đợt này.
3. Bảng giá dịch vụ + ký hiệu hoá đơn điện tử: **dùng chung toàn tenant**, không tách theo chi nhánh.
4. Role Kế toán: mặc định **có** quyền `branch.cross_view`.
5. Đặt lịch qua Portal: bệnh nhân **bắt buộc chọn chi nhánh**.
6. Gói dịch vụ dùng **xuyên chi nhánh** (mua ở A, dùng được ở B).
7. Trừ định mức thuốc tại **thời điểm kê đơn** (không phải lúc cấp phát).
8. Huỷ gói dịch vụ: **hoàn tiền theo tỷ lệ định mức chưa dùng** (tính theo đơn giá hiện tại tại thời điểm huỷ).
9. Telehealth: bệnh nhân **tự đặt lịch qua Portal** (không phải lễ tân đặt hộ) → HIS phải lưu/mã hoá Bearer token Docosan của từng bệnh nhân.
10. Mapping dịch vụ `telemedicine` với Docosan: **nhập tay** lúc đầu (chưa có API tra cứu tự động từ Docosan).

---

## 4. Phát hiện kỹ thuật đáng chú ý trong quá trình làm

- **Docosan không có endpoint riêng cho "khám từ xa"** — phân biệt hoàn toàn qua `service_type='telemedicine'` gắn trong danh sách dịch vụ khi gọi `POST api/payment/create-order-partner`. Gọi nhầm endpoint khác sẽ tạo lịch khám thường, không sinh link video, mà **không báo lỗi gì** — rủi ro cao nếu code không đọc kỹ source Docosan (`E:\git\diab\docosan\Docosan-API`).
- **Docosan không hỗ trợ webhook cho đối tác** (chỉ có ZaloPay/Stripe) → bắt buộc dùng polling job thay vì đồng bộ realtime.
- Hệ thống có **2 bảng lab-order song song** (`diab_his_lab_orders` không ai ghi vào, `diab_his_cli_lab_orders` mới là bảng thật) — lỗi kiến trúc có sẵn từ trước, được phát hiện và né đúng khi làm FR-511/512.
- `ClinicalIndicator` như mô tả trong SRS **không tồn tại thật** trong code; chỉ số theo dõi hiện nằm ở `VitalSigns` nhưng bảng đó bắt buộc gắn 1 lần khám — không phù hợp cho dữ liệu CGM đo liên tục, nên đã tạo bảng riêng thay vì ép vào bảng cũ.
- Vá kèm 1 lỗ hổng có sẵn từ trước: `EInvoice` **thiếu hoàn toàn tenant query filter** trong `AppDbContext` gốc — đã bổ sung khi thêm branch filter.
- Review tĩnh phát hiện và vá 1 lỗi mức Cao ở `9095_create_sys_settings.sql`: `UNIQUE KEY` chứa cột `tenant_id NULL` khiến MySQL không coi 2 NULL là trùng nhau → `ON DUPLICATE KEY UPDATE` không có tác dụng, mỗi lần chạy lại migration sẽ tạo dữ liệu rác nhân bản. Đã sửa bằng cột sinh (`tenant_scope` = COALESCE(tenant_id, 0)) làm khoá unique thật.

---

## 5. Kết quả kiểm thử

- `dotnet build` toàn bộ solution (Domain/Application/Infrastructure/Api + UnitTests): **0 Warning, 0 Error**.
- Review tĩnh toàn bộ 19 migration mới (`9080`-`9098`): 1 lỗi mức Cao đã vá, 3 lỗi mức Thấp (1 đã vá, 2 còn lại không chặn — ghi chú ở mục 6).
- **Chưa chạy migration thật lên MySQL** — môi trường agent không có Docker/MySQL khả dụng. Cần chạy tay theo script trong `ops/docker-compose.yml` trước khi merge vào `dev`.
- **Chưa chạy `dotnet test` thật** — môi trường agent chỉ có .NET runtime 3.1.32/10.0.5, thiếu runtime 8.0.x. Chỉ verify được bằng compile.

---

## 6. Việc còn thiếu (chưa làm trong phiên này — liệt kê trung thực)

### Chưa làm ở tất cả các module trên
- **Toàn bộ frontend** (Branch switcher, quản lý chi nhánh, form giám hộ + cảnh báo trùng bệnh nhân, gói dịch vụ, canvas annotation ảnh, Portal Telehealth, link CGM...) — chưa có 1 dòng UI nào.
- Integration test cho các luồng mới (chỉ có vài unit test edge-case).

### Nợ kỹ thuật trong từng module
| Module | Còn thiếu |
|---|---|
| Đa chi nhánh | ~10 Dapper handler (dispensing, DTQG credentials/submissions, BHYT export create/list) và phần lớn report descriptor (mới có `bil_cash_out`) chưa lọc theo branch |
| Ký số CA | Chỉ là khung/adapter, chưa nối VNPT SmartCA thật; bảng `sec_digital_signatures` chưa được insert record thật |
| Gói dịch vụ | `pkg_usage_logs.billing_id` chưa ghi ngược 2 chiều; job cảnh báo chưa override tham số theo từng tenant; `ReverseAsync` đã chặn hoàn sai khi billing PAID/đã cấp phát |
| Telehealth-Docosan | Chưa có API CRUD mapping bác sĩ/phòng khám (mới có đọc); TTL token Docosan là giả định 24h, chưa xác nhận với Docosan |
| CGM | Chưa có API xem trend/biểu đồ; chưa tự động refresh token khi hết hạn |

### Việc bị hoãn có chủ đích (đã quyết định, không phải quên)
- Điều chuyển kho liên chi nhánh (stock transfer)
- Bảng giá / ký hiệu hoá đơn riêng theo chi nhánh
- FR-112: Zalo OA nhắc lịch (mới có SMS)
- FR-1211/1212: chính sách định mức dư khi gói hết hạn, báo cáo doanh thu gói tổng hợp toàn hệ thống
- FR-804: giới hạn ICD-10 được phép tư vấn từ xa (chưa đối chiếu danh mục TT 30/2023/TT-BYT)

---

## 7. Danh sách file thiết kế & migration

**Thiết kế (docs/erd/):**
- `branch-multi-chi-nhanh.md`
- `goi-dich-vu-dinh-muc.md`
- `telehealth-docosan.md`

**Migration (db/migrations/):** `9080` → `9098` (19 file, idempotent, MySQL 8.0)

**Đề xuất bước tiếp theo:**
1. Chạy thử migration `9080`-`9098` 2 lần liên tiếp trên MySQL thật (test idempotency) trước khi merge vào `dev`.
2. Chạy `dotnet test` thật trên máy có .NET runtime 8.0.
3. Bắt đầu frontend — đề xuất ưu tiên Branch switcher trước vì các màn hình khác đều phụ thuộc context chi nhánh.
4. Liên hệ VNPT (ký số) và Docosan (xác nhận TTL token, API mapping bác sĩ) để gỡ các TODO còn lại.

---

*Tài liệu này được tổng hợp tự động cuối phiên làm việc, phản ánh đúng trạng thái code tại commit `8fa4336` trên branch `sys_phong_kham_noi`.*
