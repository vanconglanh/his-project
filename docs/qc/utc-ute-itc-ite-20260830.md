# UTC / UTE / ITC / ITE — Pro-Diab HIS
## Phạm vi: các tính năng triển khai trong phiên 2026-08-29 → 2026-08-30
## Ngày lập: 2026-08-30 · Nhánh: `develop` · Người lập: UTC/UTE Agent

---

## 0. Kết luận nhanh (đọc trước)

| Hạng mục | Số liệu |
|---|---|
| UTC (unit test case) mới | **48 case** trong 7 nhóm |
| UTE — chạy thật `dotnet test` | **86 PASS / 0 FAIL / 1 SKIP** (suite mới) · **833 PASS / 0 FAIL / 1 SKIP** (toàn bộ project) |
| ITC (integration test case) | **35 case** |
| ITE — chạy thật (API + browser Playwright) | **31 PASS / 2 FAIL / 2 KHÔNG THỰC THI ĐƯỢC** |
| Bug thật phát hiện | **2 Blocker/High + 3 Medium + 3 Low** |

**Cổng chất lượng: ❌ CONDITIONAL PASS** — phải vá **BUG-01** và **BUG-02** trước khi giao test tay / lên staging. Hai lỗi này đều nằm đúng vùng thay đổi của phiên (mục C rewrite CLS + E/Đợt2 guard chi nhánh) và đều **thất bại âm thầm** (không báo lỗi cho người dùng).

---

## 1. Môi trường kiểm thử

| Mục | Giá trị |
|---|---|
| Backend | http://localhost:5000 (container `prodiab-backend`, build từ code `develop` 2026-08-29) |
| Frontend | http://localhost:3000 (container `prodiab-frontend`, panel đăng nhập nhanh dev-only bật) |
| DB | MySQL 8.0 `prodiab_his` (container `prodiab-mysql`) |
| Compose | `ops/docker-compose.yml` + `ops/docker-compose.local-app.yml` |
| Tài khoản | `qc.admin@prodiab.test`, `letan.test@prodiab.test`, `duocsi.test@prodiab.test` — mật khẩu `Test@123` |
| Unit test | `backend/tests/ProDiabHis.UnitTests` (xUnit 2.9.3 + FluentAssertions + NSubstitute) |
| E2E | Playwright (Chromium headless, 1440×900, locale vi-VN) + Pillow để khoanh vùng ảnh |

### ⚠️ Lệch môi trường phải biết (env parity)
`Encryption__BlindIndexKey` **KHÔNG được cấu hình** trong `ops/docker-compose.local-app.yml`, trong khi
`ops/docker-compose.prod.yml:169` và `ops/docker-compose.deploy.yml:75` **CÓ** cấu hình.
Hệ quả: local log cảnh báo *"tra cuu benh nhan theo SDT/CMND/so the BHYT se KHONG hoat dong"*, và
**BUG-01 bị che hoàn toàn ở local nhưng đang sống ở staging/prod**. Để chứng minh, tôi đã tạm bật khoá này
bằng file override ngoài repo, chạy lại, ghi bằng chứng, rồi **khôi phục nguyên trạng** (đã xác nhận lại
bằng log container). Không sửa file nào trong repo cho việc này.

---

## 2. UTC — Unit Test Case

Toàn bộ test code mới nằm ở `backend/tests/ProDiabHis.UnitTests/Sprint20260830/`.

### 2.1 Nhóm H-14 — Gia hạn gói dịch vụ (`ExtendSubscriptionHandlerTests.cs`)

| ID | Loại | Mô tả | Kỳ vọng |
|---|---|---|---|
| UTC-H14-01 | Negative | `package_expiry_extension_days = 0` (mặc định = tắt) | `PACKAGE_EXTENSION_DISABLED` |
| UTC-H14-02 | Boundary | Setting âm (-1, -365) | Vẫn chặn, không lùi ngày hết hạn |
| UTC-H14-03 | Boundary | Setting = 1 (biên dương nhỏ nhất) | Vượt guard disabled, đi tiếp tới tra cứu gói |
| UTC-H14-04 | Negative | Gói không tồn tại / khác tenant | `PACKAGE_SUBSCRIPTION_NOT_FOUND` + message tiếng Việt |
| UTC-H14-05 | Contract | Đọc đúng key setting với default 0 | Không hardcode số ngày |
| UTC-H14-06 | Contract | Guard chặn thì không ghi audit | Không tạo rác audit |

### 2.2 Nhóm H-1 — Gửi nhắc lịch hẹn SMS/Zalo ZNS (`NotificationSenderTests.cs`)

| ID | Loại | Mô tả | Kỳ vọng |
|---|---|---|---|
| UTC-H01-01 | Negative | SĐT rỗng / toàn khoảng trắng | `NOTIFICATION_RECIPIENT_INVALID`, **không gọi provider** (không tốn tiền SMS) |
| UTC-H01-02 | Negative | Kênh chưa đăng ký sender | `NOTIFICATION_CHANNEL_UNSUPPORTED`, không ném exception |
| UTC-H01-03 | Negative | Kênh chưa cấu hình / đang tắt | `NOTIFICATION_CHANNEL_NOT_CONFIGURED` |
| UTC-H01-04 | Positive | Route đúng sender theo `Channel` | Zalo nhận, SMS không nhận; trả `ProviderMessageId` |
| UTC-H01-05 | Positive | `SendForTenantAsync` (job chạy nền) | Resolve credential theo tenant/branch, **không** dùng HTTP context |
| UTC-H01-06 | Contract | Gửi 2 lần | Đọc lại config 2 lần → đổi credential qua UI có hiệu lực ngay |
| UTC-H01-07 | Negative | Test kết nối khi chưa lưu cấu hình | Message hướng dẫn "Vui lòng lưu cấu hình trước khi test." |
| UTC-H01-08 | Negative | Test kết nối kênh không hỗ trợ | Trả lỗi **trước khi** chạm credential provider |

### 2.3 Nhóm E/Đợt3 — State machine điều chuyển kho (`StockTransferStateMachineTests.cs`)

| ID | Loại | Mô tả | Kỳ vọng |
|---|---|---|---|
| UTC-E3-01 | Negative | Phiếu không có dòng hàng | `STOCK_TRANSFER_EMPTY_ITEMS`, chặn trước khi chạm DB |
| UTC-E3-02 | Negative | Chi nhánh gửi = chi nhánh nhận (BR-55) | `STOCK_TRANSFER_SAME_BRANCH` |
| UTC-E3-03 | Negative | Chi nhánh không thuộc tenant (BR-54) | `BRANCH_ACCESS_DENIED` |
| UTC-E3-04 | Contract | 9 hằng trạng thái | Đúng chuỗi đã thống nhất với FE/báo cáo |
| UTC-E3-05..10 | Negative | Submit/Approve/Reject/Ship/Close/Cancel trên phiếu không tồn tại | `STOCK_TRANSFER_NOT_FOUND` (**không 500**) |
| UTC-E3-13 | Contract | Thứ tự guard khi approve | NotFound chặn trước khi đọc setting ngưỡng |

> Guard chuyển trạng thái và ngưỡng duyệt 5tr cần dữ liệu thật → phủ đầy đủ ở tầng **ITC/ITE** (mục 4.3).

### 2.4 Nhóm H-2 / E-Đợt2 — Guard tìm bệnh nhân xuyên chi nhánh (`CrossBranchPatientSearchGuardTests.cs`)

| ID | Loại | Mô tả | Kỳ vọng |
|---|---|---|---|
| UTC-H02-01 | Negative | Không quyền + tìm mờ theo tên | Chỉ thấy BN đã từng khám — **hiện SKIP vì BUG-01** |
| UTC-H02-02..04 | Positive | Có `patient.cross_branch_search` / `branch.group_view` / `cross_branch_view` | Thấy tất cả |
| UTC-H02-05 | Positive | Tìm chính xác SĐT 10 số (BR-33) | Mở khoá cross-branch dù không có quyền |
| UTC-H02-06 | Boundary | Chuỗi 9 chữ số | Không được coi là tìm chính xác |
| UTC-H02-07 | Positive | Admin (IgnoreBranchFilter) | Thấy tất cả, **không** ghi audit cross-branch |
| UTC-H02-08 | Security | User thường truy cập cross-branch | **Phải** ghi audit VIEW (dấu vết pháp lý) |
| UTC-H02-09 | Contract | Tìm không ra kết quả | Không ghi audit (tránh nhiễu log) |
| UTC-H02-10 | Security | Blind index CCCD cho chuỗi thuần chữ | **Chứng minh nguyên nhân gốc BUG-01** |

### 2.5 Nhóm H-9 — QR VietQR động (`DynamicBillingQrHandlerTests.cs`)

| ID | Loại | Mô tả | Kỳ vọng |
|---|---|---|---|
| UTC-H09-01 | Negative | Hoá đơn không tồn tại | `BILLING_NOT_FOUND` |
| UTC-H09-02 | Negative | Hoá đơn đã huỷ | `BILLING_VOID` |
| UTC-H09-03 | Boundary | Đã thu đủ | `BILLING_NO_AMOUNT_DUE` |
| UTC-H09-04 | Negative | Chưa cấu hình tài khoản nhận tiền | `BANK_ACCOUNT_NOT_CONFIGURED` + message tiếng Việt |
| UTC-H09-05 | Positive | Thu một phần (1.000.000, đã trả 400.000) | **Số tiền QR = 600.000** (số còn lại, không phải tổng) |
| UTC-H09-06 | Edge | `Balance` chưa tính (=0) | Tính lại = payable − paid = 200.000 |
| UTC-H09-07 | Edge | Chưa đặt tên tài khoản | Fallback `"PHONG KHAM"`, không ném lỗi |
| UTC-H09-08 | Security | Hoá đơn tenant khác | `BILLING_NOT_FOUND` (không lộ dữ liệu phòng khám khác) |

### 2.6 Nhóm E/Đợt3 — Giá override 3 tầng (`ServicePriceOverrideTests.cs`)

| ID | Loại | Mô tả | Kỳ vọng |
|---|---|---|---|
| UTC-E3P-01 | Security | Không có `service.price_override` | `FORBIDDEN`, không ghi DB |
| UTC-E3P-02 | Negative | Dịch vụ không tồn tại | `SERVICE_NOT_FOUND` |
| UTC-E3P-03 | Positive | Tạo override đầu tiên | Thành công, scope/branch đúng |
| UTC-E3P-04 | Negative | Khoảng hiệu lực giao nhau (BR-72) | `PRICE_OVERLAP` |
| UTC-E3P-05 | Boundary | Khoảng kế tiếp không giao (01/01 sau 31/12) | Cho phép |
| UTC-E3P-06 | Boundary | Giao đúng 1 ngày | **Vẫn bị chặn** |
| UTC-E3P-07 | Edge | Override vô hạn (`EffectiveTo = null`) | Chặn mọi khoảng sau đó |
| UTC-E3P-08 | Positive | Khác chi nhánh, cùng thời gian | Cho phép |
| UTC-E3P-09 | Contract | Scope GROUP | Không lưu lẫn `BranchId` |

### 2.7 Nhóm H-15 + H-10 + mục C

**`PackageReportDescriptorTests.cs`** (H-15, 9 case ×3 báo cáo): mỗi báo cáo `package-revenue` /
`package-utilization` / `package-outstanding-debt` phải có `tenant_id = @tenantId`, điều kiện lọc chi nhánh
(`@ignoreBranch`/`@branchId`), `deleted_at IS NULL`, `LIMIT`; tham số Dapper mang đúng ngữ cảnh chi nhánh;
mã báo cáo và `PdfTypeCode` duy nhất; báo cáo công nợ chỉ lấy `amount_due > 0 AND status <> 'cancelled'`.

**`MandatoryMfaConfigTests.cs`** (H-10, 5 case): parse `Security:MandatoryMfaRoles` ở cả dạng mảng JSON và
chuỗi CSV; **chuỗi rỗng phải fallback về `["admin"]`, tuyệt đối không trả danh sách rỗng** (danh sách rỗng =
không role nào bắt buộc 2FA = hạ thấp bảo mật âm thầm); CSV có dấu phẩy thừa không sinh phần tử rỗng.

**`DroppedLegacyTablesGuardTests.cs`** (mục C, 5 case — **chốt chống tái phát rủi ro cao nhất phiên**):
quét toàn bộ `backend/src` (.cs) và `frontend` (.ts/.tsx), bỏ comment, fail ngay nếu còn bất kỳ tham chiếu
nào tới 2 bảng đã DROP `diab_his_lab_orders` / `diab_his_rad_orders`; xác nhận EF map sang `diab_his_cli_*`;
xác nhận cổng thanh toán CLS đọc bảng còn sống; xác nhận migration `9171` tồn tại, DROP đúng 2 bảng chết và
**không** DROP nhầm bảng đang chứa dữ liệu.

---

## 3. UTE — Kết quả thực thi Unit Test

Lệnh: `dotnet test backend/tests/ProDiabHis.UnitTests/ProDiabHis.UnitTests.csproj`
Log đầy đủ: `docs/qc/evidence-utc-ute-20260830/ute-run-full.log` và `ute-run-sprint20260830.log`

| Nhóm UTC | Số test | PASS | FAIL | SKIP |
|---|---|---|---|---|
| H-14 gia hạn gói | 7 | 7 | 0 | 0 |
| H-1 gửi thông báo | 9 | 9 | 0 | 0 |
| E/Đợt3 điều chuyển kho | 10 | 10 | 0 | 0 |
| H-2 guard cross-branch | 12 | 11 | 0 | **1** (BUG-01) |
| H-9 QR động | 8 | 8 | 0 | 0 |
| E/Đợt3 giá override | 9 | 9 | 0 | 0 |
| H-15 báo cáo gói | 27 | 27 | 0 | 0 |
| H-10 cấu hình MFA | 5 | 5 | 0 | 0 |
| Mục C guard bảng chết | 5 | 5 | 0 | 0 |
| **Suite mới (Sprint20260830)** | **87** | **86** | **0** | **1** |
| **Toàn bộ project** | **834** | **833** | **0** | **1** |

> Test bị SKIP là `Search_KhongQuyen_TimTheoTen_ChiThayBenhNhanDaTungKham` — **không phải lỗi test code**,
> mà là bug sản phẩm BUG-01. Test giữ nguyên assert theo đúng spec, gắn `Skip` kèm mã bug + gợi ý sửa;
> **bỏ `Skip` ngay sau khi dev vá** để nó trở thành chốt chống tái phát vĩnh viễn.

---

## 4. ITC / ITE — Integration Test Case & Execution

Evidence: `docs/qc/evidence-itc-ite-20260830/` (ảnh khoanh vùng 🟦INPUT / 🟨ACTION / 🟩RESULT)
Script tái lập: `docs/qc/evidence-itc-ite-20260830/capture_evidence.py`

### 4.1 Luồng CLS sau khi DROP 2 bảng chết (mục C — rủi ro cao nhất)

| ID | Bước | Kỳ vọng | Kết quả |
|---|---|---|---|
| ITC-C-01 | Kiểm tra DB thật | 2 bảng chết không còn; `diab_his_cli_lab_orders` còn 17 dòng | ✅ PASS |
| ITC-C-02 | `GET /encounters/{id}/lab-orders` | 200, đọc từ bảng `cli_` | ✅ PASS (2 chỉ định ALT + CBC) |
| ITC-C-03 | `GET /cls-catalog/tests` | 200, danh mục XN/CĐHA | ✅ PASS |
| ITC-C-04 | `GET /lab-results` | 200, kết quả XN đọc được | ✅ PASS |
| ITC-C-05 | `GET /lab-orders/overdue` (job cảnh báo) | 200 | ✅ PASS |
| ITC-C-06 | `GET /encounters/{id}/lab-orders/pdf` — **in phiếu chỉ định XN** | PDF hợp lệ | ✅ PASS (124 KB, `PDF document, version 1.7`) |
| ITC-C-07 | `POST /encounters/{id}/rad-orders` — tạo chỉ định CĐHA | 201 + ghi DB | ✅ PASS (DB dump xác nhận, tiếng Việt có dấu đúng) |
| ITC-C-08 | `GET /encounters/{id}/rad-orders` — đọc lại | 200 + trả về chỉ định vừa tạo | ❌ **FAIL — HTTP 500 (BUG-02)** |
| ITC-C-09 | `GET /encounters/{id}/rad-orders/pdf` — in phiếu CĐHA | PDF hợp lệ | ✅ PASS (127 KB) |
| ITC-C-10 | Màn CLS trên browser thật, tab "Cận lâm sàng" | Hiện đủ XN + CĐHA | ❌ **FAIL — chỉ hiện 2 XN, CĐHA biến mất, KHÔNG có thông báo lỗi** |
| ITC-C-11 | `GET /encounters/{id}/cls-rounds` | 200 | ✅ PASS |

**Kết luận mục C:** phần rewrite JOIN sang bảng sống là **đúng** — đọc, ghi, in phiếu, báo cáo đều chạy trên
`diab_his_cli_*`, không còn tham chiếu bảng chết ở bất kỳ đâu trong code sống. Nhưng luồng CĐHA **chưa từng
được chạy thử với dữ liệu thật** (bảng `rad_orders` có 0 dòng), che mất BUG-02 cho tới khi tôi tạo chỉ định đầu tiên.

Evidence: `ITE-C_step1_man-CLS-danh-sach.png`, `ITE-C_step2_luot-kham-truoc-khi-mo-tab-CLS.png`,
`ITE-C_step3_tab-CLS-loi-tai-danh-sach-CDHA.png`

### 4.2 H-14 — Gia hạn gói dịch vụ

| ID | Bước | Kỳ vọng | Kết quả |
|---|---|---|---|
| ITC-H14-01 | Gia hạn gói đang `active` | Bị chặn `PACKAGE_NOT_EXPIRED` | ✅ PASS (HTTP 400) |
| ITC-H14-02 | Gia hạn gói `expired` còn 3/5 định mức | 200, `status → active`, HSD +30 ngày | ✅ PASS (20/08 → **28/09/2026** = max(HSD cũ, hôm nay) + 30) |
| ITC-H14-03 | DB dump sau gia hạn | `status=active`, `expiry_date=2026-09-28` | ✅ PASS |
| ITC-H14-04 | UI chi tiết bệnh nhân | Banner cảnh báo + nút "Gia hạn" | ✅ PASS |

Evidence: `ITE-H14_step1_chi-tiet-benh-nhan.png`, `ITE-H14_step2_nut-gia-han.png`

### 4.3 E/Đợt3 — State machine điều chuyển kho

Không có màn hình FE (xem GAP-01) → thực thi ở tầng API.

| ID | Bước | Kỳ vọng | Kết quả |
|---|---|---|---|
| ITC-E3-01 | Tạo phiếu rỗng | 422 `STOCK_TRANSFER_EMPTY_ITEMS` | ✅ PASS |
| ITC-E3-02 | Trùng chi nhánh gửi/nhận | 422 `STOCK_TRANSFER_SAME_BRANCH` | ✅ PASS |
| ITC-E3-03 | Tạo phiếu 120.000đ | 201, `status=DRAFT`, sinh mã `DC…` | ✅ PASS |
| ITC-E3-04 | Ship khi còn DRAFT (nhảy cóc) | 422 `INVALID_STATE` | ✅ PASS |
| ITC-E3-05 | Submit DRAFT | `PENDING_APPROVAL` | ✅ PASS |
| ITC-E3-06 | Submit lần 2 | 422 `INVALID_STATE` | ✅ PASS |
| ITC-E3-07 | Người tạo tự duyệt (BR-59) | 403 `SELF_APPROVAL_NOT_ALLOWED` | ✅ PASS |
| ITC-E3-08 | Người khác duyệt | `APPROVED` | ✅ PASS |
| ITC-E3-09 | Ship khi tồn kho không đủ | 422 `INSUFFICIENT_STOCK` | ✅ PASS (không tạo tồn ảo) |
| ITC-E3-10 | Cancel khi đang APPROVED | `CANCELLED` | ✅ PASS |
| ITC-E3-11 | **Ngưỡng BR-58**: phiếu 6.000.000đ > 5tr, dược sĩ (có `stock_transfer.approve`, không có `branch.group_view`) duyệt | 403 `APPROVAL_PERMISSION_REQUIRED` | ✅ PASS |
| ITC-E3-12 | Cùng phiếu 6.000.000đ, admin duyệt | `APPROVED` | ✅ PASS |
| ITC-E3-13 | Dược sĩ duyệt phiếu 40.000đ (dưới ngưỡng) | `APPROVED` | ✅ PASS |

State machine + ngưỡng duyệt hoạt động **chính xác 100%**. Đây là phần chắc chắn nhất của phiên.

### 4.4 H-9 — QR VietQR động

| ID | Bước | Kỳ vọng | Kết quả |
|---|---|---|---|
| ITC-H09-01 | `POST /billings/{id}/qr-dynamic` cho hoá đơn còn nợ 53.025đ | 200, payload EMVCo chứa đúng số tiền | ✅ PASS — tag `54` = `53025`, BIN `970436` có trong payload, ảnh base64 1152 bytes |
| ITC-H09-02 | Hoá đơn đã PAID | 409 `BILLING_NO_AMOUNT_DUE` | ✅ PASS |
| ITC-H09-03 | Hoá đơn không tồn tại | 404 `BILLING_NOT_FOUND` | ✅ PASS |
| ITC-H09-04 | Thu ngân bấm QR trên UI | Gọi `/qr-dynamic` | ⛔ **KHÔNG THỰC THI ĐƯỢC** — FE chưa gọi endpoint này (GAP-02) |

### 4.5 H-10 — Bắt buộc 2FA theo role

| ID | Bước | Kỳ vọng | Kết quả |
|---|---|---|---|
| ITC-H10-01 | Đăng nhập admin (role bắt buộc 2FA, chưa bật 2FA) | 200 + `mfaSetupRequired=true` + message tiếng Việt, **vẫn cấp token** | ✅ PASS |
| ITC-H10-02 | Đăng nhập lễ tân (role không bắt buộc) | 200 + `mfaSetupRequired=false` | ✅ PASS |
| ITC-H10-03 | Vào được hệ thống sau đăng nhập | Không bị khoá nhầm | ✅ PASS |

Evidence: `ITE-H10_step1_man-dang-nhap.png`, `ITE-H10_step2_dang-nhap-thanh-cong.png`

### 4.6 H-2 — Tìm bệnh nhân xuyên chi nhánh

| ID | Bước | Kỳ vọng | Kết quả |
|---|---|---|---|
| ITC-H02-01 | Lễ tân (không quyền) tìm mờ "Nguyễn", **local không có BlindIndexKey** | 0 kết quả | ✅ PASS |
| ITC-H02-02 | Cùng thao tác, **đã bật BlindIndexKey giống prod** | 0 kết quả | ❌ **FAIL — 2 kết quả (BUG-01)** |
| ITC-H02-03 | Tìm chính xác theo SĐT/CCCD | Mở khoá cross-branch | ⛔ **KHÔNG THỰC THI ĐƯỢC** ở local (thiếu BlindIndexKey) |

### 4.7 H-1 — Cấu hình kênh gửi thông báo

| ID | Bước | Kỳ vọng | Kết quả |
|---|---|---|---|
| ITC-H01-01 | Mở `/admin/notification-channels` | Màn cấu hình SMS/Zalo ZNS hiển thị | ✅ PASS |

Evidence: `ITE-H1_step1_man-cau-hinh-kenh.png`

---

## 5. Bug phát hiện

### ✅ BUG-01 — Guard tìm bệnh nhân xuyên chi nhánh bị vô hiệu hoàn toàn (High) — ĐÃ FIX 2026-08-30

- **Case ID**: UTC-H02-01, ITC-H02-02 · **Liên quan**: H-2 (FR-203), E/Đợt2 (BR-25, BR-33)
- **Severity**: **High** — rò rỉ dữ liệu bệnh nhân giữa các chi nhánh, vi phạm quy định bảo mật hồ sơ y tế
- **Ảnh hưởng**: **staging/prod** (nơi `Encryption__BlindIndexKey` được cấu hình). Local hiện đang che lỗi này.
- **File**: `backend/src/ProDiabHis.Application/Patients/PatientQueryHandler.cs` (`SearchPatientsQueryHandler`, ~dòng 63-107)
- **Steps to reproduce**:
  1. Đảm bảo `Encryption__BlindIndexKey` đã cấu hình (như prod).
  2. Đăng nhập `letan.test@prodiab.test` — role `le_tan`, **không** có `patient.cross_branch_search`, `cross_branch_view`, `branch.group_view`.
  3. `GET /api/v1/patients/search?q=Nguyễn`
- **Expected**: 0 kết quả (chỉ được thấy bệnh nhân đã từng khám tại chi nhánh mình).
- **Actual**: **2 bệnh nhân** trả về, gồm cả bệnh nhân chưa từng khám tại chi nhánh của lễ tân.
- **Bằng chứng đối chứng (cùng user, cùng câu tìm)**: chưa bật khoá → **0 kết quả** · đã bật khoá → **2 kết quả**.
- **Root cause**: điều kiện mở khoá guard là
  `isExactMatch = phoneBidx != null || idBidx != null || (digitsOnly.Length == 10 && ...)`.
  `idBidx = _pii.BlindIndex(q, PiiField.IdNumber)` đi qua `PiiNormalizer.NormalizeDigitsOrUpper`, hàm này
  **giữ lại mọi ký tự chữ-hoặc-số** (`IPiiProtector.cs:66-75`), nên với `q = "Nguyễn"` vẫn trả về chuỗi hash
  khác null → `isExactMatch = true` với **mọi chuỗi tìm kiếm không rỗng** → nhánh hạn chế
  `if (!hasCrossBranchSearch && !isExactMatch)` **không bao giờ chạy**.
  (Đã khẳng định độc lập bằng UTC-H02-10.)
- **Suggested fix area**: chỉ coi là "tìm chính xác theo giấy tờ" khi chuỗi tìm **thuần chữ số** và đúng độ dài
  CMND/CCCD/thẻ BHYT — không dùng `idBidx != null` làm dấu hiệu. Sau khi vá, **bỏ `Skip`** ở
  `CrossBranchPatientSearchGuardTests.Search_KhongQuyen_TimTheoTen_ChiThayBenhNhanDaTungKham`.
- **Đã fix**: `backend/src/ProDiabHis.Application/Patients/PatientQueryHandler.cs` — `isExactMatch` giờ chỉ dựa
  trên `digitsOnly.Length == trimmed.Length && (digitsOnly.Length == 10 || digitsOnly.Length == 12)` (SĐT 10 số
  hoặc CCCD 12 số, thuần chữ số), không còn dùng `idBidx != null`/`phoneBidx != null` làm điều kiện mở khoá.
  Đã bỏ `Skip` ở `Search_KhongQuyen_TimTheoTen_ChiThayBenhNhanDaTungKham` — chạy `dotnet test` xác nhận **834/834
  PASS, 0 SKIP** (toàn bộ suite, bao gồm 12/12 case nhóm H-2). UTC-H02-06 (9 chữ số) vẫn PASS đúng theo spec —
  không bị coi là tìm chính xác.

### ✅ BUG-02 — Danh sách chỉ định CĐHA trả HTTP 500, màn CLS mất dữ liệu âm thầm (Blocker) — ĐÃ FIX 2026-08-30

- **Case ID**: ITC-C-08, ITC-C-10 · **Liên quan**: mục C (rewrite CLS)
- **Severity**: **Blocker** — an toàn người bệnh: bác sĩ mở tab Cận lâm sàng **không thấy chỉ định CĐHA đã kê**,
  và **không có bất kỳ thông báo lỗi nào**, dễ dẫn tới chỉ định trùng hoặc bỏ sót phim chụp.
- **Platform**: API + Web (Chromium 1440×900)
- **File**: `backend/src/ProDiabHis.Application/CLS/ClsHandlers.cs:474` (`ListRadOrdersQueryHandler`)
- **Steps to reproduce**:
  1. `POST /api/v1/encounters/{id}/rad-orders` với 1 chỉ định bất kỳ → 201, DB ghi thành công.
  2. `GET /api/v1/encounters/{id}/rad-orders` → **HTTP 500**.
  3. Trên UI: mở lượt khám đó → tab "Cận lâm sàng" → chỉ hiện 2 XN, **CĐHA biến mất, không có báo lỗi**.
- **Expected**: 200 + danh sách chỉ định CĐHA.
- **Actual**: `RuntimeBinderException: Cannot convert type 'bool' to 'sbyte'`
- **Root cause**: cột `contrast` là `tinyint(1)`; MySqlConnector map `tinyint(1)` → **`bool`**, nhưng code ép
  `(bool)((sbyte)r.contrast == 1)`. Lỗi chỉ lộ khi bảng có dữ liệu — trước đó `diab_his_cli_rad_orders` có 0 dòng
  nên toàn bộ phiên trước không phát hiện được.
- **Suggested fix area**: dùng đúng pattern phòng thủ đã có sẵn trong repo ở
  `Icd10Handlers.cs:116` — `r.contrast is bool b ? b : (sbyte)r.contrast == 1`.
- **Ghi chú**: tôi **để lại** chỉ định CĐHA test trên encounter `6f750284-41c8-4625-9d41-587ba0c149a6` để dev
  tái lập ngay. Xoá dòng đó sẽ làm màn CLS "hết lỗi" một cách giả tạo.
- **Evidence**: `ITE-C_step3_tab-CLS-loi-tai-danh-sach-CDHA.png` + log container ở mục 4.1.
- **Đã fix**: `ClsHandlers.cs:474` đổi sang pattern `r.contrast is bool cb ? cb : (sbyte)r.contrast == 1` (giống
  `Icd10Handlers.cs:116`). Verify thật qua API: rebuild + redeploy container `prodiab-backend` từ code đã vá,
  gọi `GET /api/v1/encounters/6f750284-41c8-4625-9d41-587ba0c149a6/rad-orders` → **HTTP 200**, trả đúng 1 chỉ
  định CĐHA (`CT_ABD`, `contrast:false`) — dữ liệu test vẫn giữ nguyên trên encounter đó để tái lập/verify.

### ✅ BUG-03 — Cùng lỗi ép kiểu `tinyint(1)` ở các chỗ khác, đang tiềm ẩn (Medium) — ĐÃ FIX 2026-08-30

- **Severity**: Medium (chưa nổ vì bảng đang rỗng, sẽ nổ ngay khi có dữ liệu thật)
- **Vị trí đã sửa**: `DiabetesHandlers.cs:267` (`is_system`, bảng `diab_his_cli_diabetes_templates` — hiện 0 dòng),
  `EncryptionKeyStoreImpl.cs:116` (`is_active`), và thêm 1 chỗ phát hiện khi grep mở rộng toàn backend:
  `DtqgHandlers.cs:354` (`row.is_active == 1`, `row.last_test_ok == 1` trên `dynamic` — cùng lỗi RuntimeBinder
  khi cột đã map sẵn thành `bool`).
- **Bằng chứng**: `GET /api/v1/diabetes-templates` và `GET /api/v1/dtqg/credentials` hiện trả 200 sau khi vá
  (verify qua container đã rebuild); cột `is_system`/`is_active`/`last_test_ok` là `tinyint(1)`, mã nguồn giống
  hệt mẫu đã gây BUG-02.
- **Đã fix**: cả 3 chỗ đổi sang pattern `x is bool b ? b : (sbyte)x == 1` (hoặc `== 1` cho `dynamic` không ép
  sbyte). Đã grep rộng toàn backend `\(sbyte\)`, `\(bool\)\(`, `== 1\)` để xác nhận không còn chỗ nào khác dùng
  pattern ép kiểu sai này trên biến `dynamic` (các chỗ còn lại như `SupplierHandlers.cs` dùng `(bool)(x ?? true)`
  — an toàn vì `x` đã là `bool?`; `NotificationChannelHandlers.cs` dùng lớp `ChannelRow` có field `int`, Dapper tự
  convert bool→int khi map sang class có kiểu tường minh nên không lỗi).

### 🟠 GAP-01 — Điều chuyển kho không có giao diện người dùng (Medium)

Backend `E/Đợt3` hoàn chỉnh (11 endpoint, state machine đúng 100% theo ITE mục 4.3), nhưng
**toàn bộ frontend không có một tham chiếu nào** tới `stock-transfers` (đã grep toàn bộ `.ts`/`.tsx`).
Người dùng cuối chưa dùng được tính năng. Evidence: `ITE-E3_step1_man-duoc-khong-co-menu-dieu-chuyen.png`.

### 🟠 GAP-02 — QR động H-9 chưa được nối vào màn thu ngân (Medium)

Endpoint `POST /billings/{id}/qr-dynamic` chạy đúng (ITC-H09-01 PASS), nhưng
`components/domain/QrPaymentModal.tsx` vẫn dùng luồng QR cũ (có `qrId`, `expiresAt`, `provider`) — khác
hoàn toàn contract của endpoint mới. Không có file FE nào gọi `qr-dynamic`. FR-911 chưa tới tay thu ngân.

### 🟡 LOW-01 — Endpoint gia hạn trả sai envelope lỗi

`POST /package-subscriptions/{id}/extend` khi lỗi trả RFC9110 ProblemDetails
(`{"type","title","status","detail","traceId"}`) thay vì envelope chuẩn dự án
`{"error":{"code","message","details"}}` — FE không đọc được `error.code` để i18n.
(So sánh: `/stock-transfers/*` và `/billings/*/qr-dynamic` đều trả đúng envelope chuẩn.)

### 🟡 LOW-02 — Message lỗi thiếu dấu tiếng Việt

`PaymentHandlers.cs`: `"Khong tim thay hoa don"`, `"Hoa don khong con so tien phai thu"`, `"Hoa don da huy"`;
`ServicePriceOverrideHandlers.cs`: `"Ban khong co quyen thao tac gia override dich vu"`, `"PRICE_OVERLAP"` message.
CLAUDE.md mục 6 yêu cầu message trong response JSON là **tiếng Việt có dấu**.

### 🟡 LOW-03 — Vài vấn đề dữ liệu / hiển thị nhỏ

- Setting `package_expiry_extension_days` có **2 dòng** trong `diab_his_sys_settings` (giá trị `0` và `30`) —
  chạy được vì `SettingsProvider` ưu tiên dòng theo tenant, nhưng nên dọn để tránh nhầm khi vận hành.
- Màn chi tiết bệnh nhân hiển thị `BNT01000002 • **undefined** • 36 tuổi` — trường giới tính null render ra
  chuỗi `"undefined"` (`ITE-H14_step2_nut-gia-han.png`).
- `GET /swagger/v1/swagger.json` trả **HTTP 500** → không xem được API docs (chưa truy nguyên nhân, ngoài phạm vi).
- Dữ liệu seed có tên bệnh nhân bị mojibake (`Nguy?n V?n Ki?m Th?`) — do insert qua MySQL CLI thiếu
  `--default-character-set=utf8mb4`. Lỗi dữ liệu test, không phải lỗi sản phẩm.

---

## 6. Quyết định cổng chất lượng

**❌ CONDITIONAL PASS** — cho qua sau khi vá 2 mục:

1. **BUG-02 (Blocker)** — 1 dòng sửa ở `ClsHandlers.cs:474`; nên sửa luôn BUG-03 cùng lúc.
2. **BUG-01 (High)** — sửa điều kiện `isExactMatch`; sau khi vá phải bỏ `Skip` để test giữ vai trò chốt chống tái phát.

Cần bàn với PO: GAP-01 (điều chuyển kho chưa có UI) và GAP-02 (QR động chưa tới thu ngân) — hai tính năng
đã tính là "Done" nhưng người dùng cuối chưa chạm được.

**Phần đã vững, không cần lo:** state machine điều chuyển kho + ngưỡng duyệt (13/13 case PASS),
QR động ở tầng API (số tiền nhúng đúng trong payload EMVCo), gia hạn gói H-14 (đúng cả chính sách
`max(HSD cũ, hôm nay) + N ngày`), rewrite bảng CLS mục C (không còn tham chiếu bảng chết ở bất kỳ đâu,
in phiếu XN/CĐHA đều ra PDF thật), và 833 test cũ vẫn xanh — **không có hồi quy nào**.

---

## 7. Phụ lục — vị trí file

| Loại | Đường dẫn |
|---|---|
| Test code UTC/UTE | `backend/tests/ProDiabHis.UnitTests/Sprint20260830/` |
| Log chạy UTE | `docs/qc/evidence-utc-ute-20260830/ute-run-full.log`, `ute-run-sprint20260830.log` |
| Evidence ITE (ảnh khoanh vùng) | `docs/qc/evidence-itc-ite-20260830/*.png` |
| Script tái lập evidence | `docs/qc/evidence-itc-ite-20260830/capture_evidence.py` |
| Tài liệu này | `docs/qc/utc-ute-itc-ite-20260830.md` |
