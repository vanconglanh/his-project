# 単体テスト仕様書 (UTC) — Màn hình **Nhà cung cấp** (Supplier)

> Tài liệu kiểm thử đơn vị theo **chuẩn Nhật** (単体テスト仕様書). Vừa là **mẫu chuẩn tái sử dụng** cho mọi màn hình master CRUD, vừa là **bản áp dụng thật** cho màn hình Nhà cung cấp — mọi ràng buộc bám theo code thật (FE zod / BE DTO / DB schema), có trích dẫn `file:line`.

| Mục | Nội dung |
|---|---|
| 機能ID (Function ID) | `SUP-CRUD-001` |
| 画面名 (Màn hình) | Quản trị → Danh mục → **Nhà cung cấp** |
| URL (Route FE) | `/admin/suppliers` |
| API base | `/api/v1/suppliers` |
| Bảng DB | `diab_his_pha_suppliers` (MySQL 8, InnoDB, utf8mb4) — `db/migrations/9005_create_pharmacy.sql:185` |
| Quyền (Permission) | Xem: `supplier.read` · Ghi: `supplier.write` |
| Loại xóa | **Xóa mềm** (soft delete, `deleted_at IS NULL`) |
| 作成者 / 作成日 | QC (Tester) / 2026-07-06 |
| Môi trường test | `https://his.diab.com.vn` (deploy) hoặc `:3100` (local). DB: `prodiab_his`. Tài khoản: `admin@prodiab.local` / `admin123` |

---

## 0. Quy ước tài liệu (ドキュメント規約) — dùng lại cho mọi màn hình

### 0.1. Cột trong bảng test case
| Cột | Ý nghĩa |
|---|---|
| **No** | Số hiệu case, duy nhất trong tài liệu (`A01`, `C12`…) |
| **中項目 (Hạng mục)** | Nhóm chức năng đang test |
| **観点 (Test viewpoint)** | Góc nhìn kiểm thử — *lý do* case tồn tại |
| **前提 (Tiền đề)** | Điều kiện trước khi thực hiện |
| **操作手順 (Thao tác)** | Các bước thực hiện, đánh số |
| **テストデータ (Dữ liệu)** | Dữ liệu nhập cụ thể |
| **期待結果 (Kết quả mong đợi)** | Kết quả đúng theo spec |
| **判定** | `OK` / `NG` / `N/A` / `保留`(treo) — tester điền khi chạy |
| **備考 (Ghi chú)** | Defect ID, số liệu thực tế, lý do NG |

### 0.2. Ký hiệu 判定
`OK` = đúng mong đợi · `NG` = sai → tạo defect · `N/A` = không áp dụng · `保留` = chưa test được (chặn bởi case khác).

### 0.3. Catalog test viewpoint chuẩn (観点カタログ) — checklist tái sử dụng
Khi làm UTC cho **bất kỳ** màn hình nhập liệu, quét đủ 12 nhóm sau:

| # | 大項目 (Nhóm lớn) | Điểm cần phủ |
|---|---|---|
| 1 | **初期表示** (Load ban đầu) | Tiêu đề, control hiển thị đủ, giá trị mặc định, focus, trạng thái nút, quyền |
| 2 | **項目表示・設定** (Load data vào field) | Mỗi field nhận & hiển thị đúng data, dropdown load đủ option, format hiển thị |
| 3 | **入力チェック** (Validate input) | Bắt buộc, độ dài (max/min), kiểu (số/chuỗi/email/ngày), control type, ký tự đặc biệt |
| 4 | **境界値** (Giá trị biên) | max, max+1, min, min-1, rỗng, 0, 1 |
| 5 | **業務ルール** (Business rule) | Trùng khóa (unique), quan hệ field, trạng thái |
| 6 | **DB登録** (Ghi DB) | INSERT đúng bảng/cột/kiểu, tenant_id, cột audit, mã hóa |
| 7 | **登録後再表示** (Load sau ghi) | List/detail phản ánh đúng data vừa ghi, không mất/đổi data |
| 8 | **更新・削除** (Sửa/Xóa) | Update đúng, soft-delete set `deleted_at`, không xóa cứng |
| 9 | **権限・テナント** (Quyền & đa tenant) | Chặn khi thiếu quyền, cách ly tenant khác |
| 10 | **セキュリティ** (Bảo mật) | XSS, SQL injection, mã hóa cột nhạy cảm |
| 11 | **文字コード** (Encoding) | Tiếng Việt có dấu lưu & hiển thị đúng, emoji, khoảng trắng |
| 12 | **UI/UX・異常系** (Giao diện & luồng lỗi) | Cancel không lưu, nút disable khi submit, message lỗi, mất mạng/timeout |

---

## 1. Định nghĩa項目 (Field definition matrix) — bám code thật

> Nguồn: FE `SupplierForm.tsx`, BE `SupplierHandlers.cs` (DTO `SupplierRequest`), DB `9005_create_pharmacy.sql`.

| Field (FE id) | Nhãn VN | Control | Kiểu | Bắt buộc FE | Bắt buộc BE | Cột DB | Kiểu DB / Độ dài | NULL DB | Unique | Ghi chú |
|---|---|---|---|---|---|---|---|---|---|---|
| `code` | Mã NCC | Input text | chuỗi | ✅ `min(1)` | ❌ (chỉ non-null) | `code` | VARCHAR(**30**) | NOT NULL | ✅ `(tenant_id,code)` | Không maxLen FE/BE |
| `name` | Tên NCC | Input text | chuỗi | ✅ `min(1)` | ❌ | `name` | VARCHAR(**255**) | NOT NULL | — | Không maxLen FE/BE |
| `tax_code` | Mã số thuế | Input text | chuỗi | ❌ optional | ❌ | `tax_code` | VARCHAR(**20**) | NULL | — | Không regex |
| `phone` | Điện thoại | Input text | chuỗi | ❌ optional | ❌ | `phone` | VARCHAR(**30**) | NULL | — | **Không regex phone** |
| `email` | Email | Input `type=email` | email | ❌ optional | ❌ | `email` | VARCHAR(**100**) | NULL | — | FE validate format; BE không |
| `contact_person` | Người liên hệ | Input text | chuỗi | ❌ optional | ❌ | `contact_name` | VARCHAR(**100**) | NULL | — | **Tên field lệch** FE↔DB |
| `address` | Địa chỉ | Input text | chuỗi | ❌ optional | ❌ | `address` | TEXT | NULL | — | — |
| `status` | Trạng thái | *(không có control)* | enum | ❌ | mặc định `ACTIVE` | `is_active` | TINYINT(1) | NOT NULL, DEFAULT 1 | — | `INACTIVE`→0, else→1 |

**Cột hệ thống (không trên form):** `id` CHAR(36) PK · `tenant_id` INT NOT NULL (lấy từ JWT) · `created_at`/`updated_at` DATETIME · `created_by`/`updated_by`/`deleted_at`/`deleted_by`.

**Field bắt buộc thực tế:** `code`, `name` (FE chặn); `tenant_id` (server tự gán).

---

## 2. Chuẩn bị dữ liệu test (テストデータ準備)

| ID | Mục đích | Giá trị |
|---|---|---|
| DS-VALID | Bản ghi hợp lệ đầy đủ | code=`NCC{ts}`, name=`Công ty Dược ABC`, tax_code=`0301234567`, phone=`02838220000`, email=`ncc@test.vn`, contact=`Trần Văn Kho`, address=`12 Lê Lợi, Q1` |
| DS-MIN | Chỉ field bắt buộc | code=`NCCMIN{ts}`, name=`X` |
| DS-DUP | Trùng mã (đã tồn tại) | code = mã của DS-VALID đã tạo |
| `{ts}` | Hậu tố tránh trùng | 5 số cuối timestamp |

> Ràng buộc unique là `(tenant_id, code)` → mã có thể trùng giữa 2 tenant khác nhau nhưng **không** trùng trong cùng tenant.

---

## 3. Test cases

### Nhóm A — 初期表示 (Load màn hình ban đầu)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| A01 | Load list | Trang danh sách render | Đã đăng nhập, có `supplier.read` | 1. Mở `/admin/suppliers` | — | Bảng danh sách hiển thị; có nút "Tạo NCC"; ô tìm kiếm; cột: Mã, Tên, MST, ĐT, Email, Trạng thái, thao tác | | |
| A02 | Load list rỗng | Empty state | Tenant chưa có NCC | 1. Mở list | — | Hiển thị empty state tiếng Việt (không lỗi, không bảng trống vô nghĩa) | | |
| A03 | Mở form Tạo | Dialog khởi tạo | Ở list | 1. Bấm "Tạo NCC" | — | Dialog/form mở; **tất cả field rỗng**; `status` mặc định ACTIVE; focus vào field đầu (Mã NCC) | | |
| A04 | Control đầy đủ | Đủ 7 control | Form Tạo mở | 1. Quan sát | — | Hiển thị đúng 7 field: code, name, tax_code, phone, email, contact_person, address; **không** có input status | | Field status ẩn theo spec |
| A05 | Nút mặc định | Trạng thái nút | Form Tạo mở, chưa nhập | 1. Quan sát nút "Tạo nhà cung cấp" | — | Nút Submit hiển thị; nút Hủy/đóng hiển thị | | |
| A06 | Không quyền ghi | Ẩn nút tạo | User chỉ có `supplier.read` | 1. Mở list | — | Nút "Tạo NCC" **không hiển thị** (gated by `supplier.write`) | | Xem nhóm I |

### Nhóm B — 項目表示・設定 (Nhập & hiển thị từng field)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| B01 | code | Nhận & hiển thị | Form Tạo | 1. Nhập Mã NCC | `NCC{ts}` | Ký tự hiển thị đúng, con trỏ đúng | | |
| B02 | name | Nhận tiếng Việt có dấu | Form Tạo | 1. Nhập Tên | `Công ty Dược Hậu Giang` | Hiển thị đủ dấu, không mất ký tự | | 観点 encoding |
| B03 | tax_code | Nhập số | Form Tạo | 1. Nhập MST | `0301234567` | Hiển thị đúng | | |
| B04 | phone | Nhập số ĐT | Form Tạo | 1. Nhập ĐT | `02838220000` | Hiển thị đúng | | |
| B05 | email | Control type=email | Form Tạo | 1. Nhập Email | `ncc@test.vn` | Hiển thị đúng | | |
| B06 | contact_person | Người liên hệ | Form Tạo | 1. Nhập | `Trần Văn Kho` | Hiển thị đúng | | |
| B07 | address | Địa chỉ dài | Form Tạo | 1. Nhập | `12 Lê Lợi, P.Bến Nghé, Q1, TP.HCM` | Hiển thị đủ, field col-span-2 | | |
| B08 | Giữ data khi mở lại (Sửa) | Load data vào field | Đã có 1 NCC | 1. Bấm Sửa NCC đó | DS-VALID | **Mọi field** load đúng giá trị đã lưu; status đúng | | 観点 load đủ data |

### Nhóm C — 入力チェック (Validate từng field)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| C01 | code bắt buộc | Mandatory | Form Tạo | 1. Để trống Mã 2. Nhập Tên 3. Submit | code=`` | Chặn submit; message **"Bắt buộc"** dưới field Mã | | zod `min(1)` |
| C02 | name bắt buộc | Mandatory | Form Tạo | 1. Nhập Mã 2. Để trống Tên 3. Submit | name=`` | Chặn submit; message "Bắt buộc" dưới Tên | | |
| C03 | code chỉ khoảng trắng | Trim/whitespace | Form Tạo | 1. Mã = `"   "` 2. Submit | code=`"   "` | **Kỳ vọng:** coi như rỗng → báo bắt buộc | | ⚠️ zod `min(1)` KHÔNG trim → có thể lọt. Xem D-Bug#1 |
| C04 | email sai định dạng | Format | Form Tạo | 1. Nhập email thiếu `@` 2. Submit | `nctest.vn` | Chặn; message **"Email không hợp lệ"** | | zod `.email()` |
| C05 | email rỗng | Optional cho phép rỗng | Form Tạo | 1. Bỏ trống email 2. Submit hợp lệ | email=`` | Submit thành công (email optional) | | `.or(literal(""))` |
| C06 | code = 30 ký tự | Biên trên max (境界値) | Form Tạo | 1. Mã 30 ký tự 2. Submit | `A`×30 | Ghi thành công (đúng VARCHAR(30)) | | |
| C07 | code = 31 ký tự | Vượt max | Form Tạo | 1. Mã 31 ký tự 2. Submit | `A`×31 | **Kỳ vọng spec:** chặn + message "tối đa 30 ký tự" | | ⚠️ FE/BE KHÔNG chặn → xuống DB "Data too long" 500. **Defect#2** |
| C08 | name = 255 | Biên trên | Form Tạo | 1. Tên 255 ký tự 2. Submit | `X`×255 | Ghi thành công | | |
| C09 | name = 256 | Vượt max | Form Tạo | 1. Tên 256 ký tự 2. Submit | `X`×256 | **Kỳ vọng:** chặn có message | | ⚠️ Defect#2 (DB error) |
| C10 | tax_code = 21 | Vượt max(20) | Form Tạo | 1. MST 21 ký tự 2. Submit | `9`×21 | Kỳ vọng chặn | | ⚠️ Defect#2 |
| C11 | phone = 31 | Vượt max(30) | Form Tạo | 1. ĐT 31 ký tự 2. Submit | `0`×31 | Kỳ vọng chặn | | ⚠️ Defect#2 |
| C12 | email = 101 | Vượt max(100) | Form Tạo | 1. Email 101 ký tự hợp lệ 2. Submit | `a`×90+`@test.vn` | Kỳ vọng chặn | | ⚠️ Defect#2 |
| C13 | phone chứa chữ | Kiểu dữ liệu | Form Tạo | 1. ĐT = `abc-xyz` 2. Submit | `abcxyz` | **Kỳ vọng nghiệp vụ:** báo "ĐT không hợp lệ" | | ⚠️ Không có regex → hiện lọt. **Defect#3** |
| C14 | tax_code chứa chữ | Kiểu số | Form Tạo | 1. MST = `ABC` 2. Submit | `ABC` | Kỳ vọng báo lỗi định dạng MST | | ⚠️ Không validate → lọt |
| C15 | Ký tự đặc biệt name | Robustness | Form Tạo | 1. Tên = `A&B <C> "D" 'E'` 2. Submit | như bên | Lưu & hiển thị nguyên văn, không vỡ layout | | Liên quan XSS J01 |

### Nhóm D — 業務ルール (Business rule)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| D01 | Mã trùng trong tenant | Unique `(tenant_id,code)` | Đã tạo DS-VALID | 1. Tạo NCC mới cùng mã 2. Submit | DS-DUP | **Kỳ vọng spec:** báo "Mã NCC đã tồn tại" (tiếng Việt) | | ⚠️ Handler không precheck → DB duplicate key, message không thân thiện. **Defect#4** |
| D02 | Mã trùng khác tenant | Cách ly tenant | Mã tồn tại ở tenant B | 1. Đăng nhập tenant A 2. Tạo cùng mã | — | Ghi thành công (unique theo tenant) | | |
| D03 | status mặc định | Giá trị default | Tạo qua form (không set status) | 1. Tạo DS-MIN | — | DB `is_active=1` (ACTIVE) | | |

### Nhóm E — DB登録 (Insert & kiểm chứng DB)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| E01 | Insert đúng bảng | Ghi DB | Form hợp lệ | 1. Submit DS-VALID 2. Query DB | DS-VALID | 1 row mới trong `diab_his_pha_suppliers` | | |
| E02 | Map đúng cột | Ánh xạ field→cột | Sau E01 | 1. `SELECT * ... WHERE code=...` | — | `code,name,tax_code,phone,email,address` đúng; **`contact_name` = Người liên hệ** (không phải cột contact_person) | | 観点 field lệch tên |
| E03 | tenant_id | Gán từ JWT | Sau E01 | 1. Xem cột `tenant_id` | — | = tenant_id của user đăng nhập, **không** nhận từ client | | |
| E04 | Cột audit | created_* | Sau E01 | 1. Xem `created_at, created_by` | — | `created_at` = giờ tạo; `created_by` = user id | | |
| E05 | id định dạng | PK CHAR(36) | Sau E01 | 1. Xem `id` | — | Chuỗi GUID 36 ký tự | | |
| E06 | is_active | Trạng thái | Tạo DS-MIN | 1. Xem `is_active` | — | = 1 | | |
| E07 | HTTP response | API contract | Sau E01 | 1. Xem response POST | — | HTTP **201**, body `{ "data": { id, ... } }` | | |
| E08 | deleted_at khi tạo | Soft-delete init | Sau E01 | 1. Xem `deleted_at` | — | NULL | | |

### Nhóm F — 登録後再表示 (Load lại sau khi insert)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| F01 | Xuất hiện trong list | Reload list | Sau E01 | 1. Đóng dialog / reload list | — | NCC mới xuất hiện trên đầu/đúng vị trí sort; đủ cột đúng data | | |
| F02 | Mở lại detail/sửa | Round-trip data | Sau E01 | 1. Bấm Sửa NCC vừa tạo | — | Mọi field hiển thị **đúng như đã nhập** (đặc biệt tiếng Việt có dấu, contact_person) | | 観点 không mất data |
| F03 | Tìm kiếm | Search sau ghi | Sau E01 | 1. Gõ mã/tên vào ô tìm | code/name | NCC mới nằm trong kết quả | | |
| F04 | Không nhân đôi | Idempotent submit | Form vừa submit | 1. (Nếu double-click) | — | Chỉ 1 bản ghi (không tạo 2) | | Liên quan K03 |

### Nhóm G — 更新 (Sửa)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| G01 | Sửa tên | Update field | Có DS-VALID | 1. Sửa name 2. Submit (PUT) | name=`Tên mới` | HTTP 200; DB `name` cập nhật; `updated_at` đổi; `updated_by` set | | |
| G02 | Sửa không đổi mã trùng | Unique khi update | 2 NCC A,B | 1. Sửa B, đặt code = code của A | — | Kỳ vọng báo trùng | | ⚠️ Defect#4 |
| G03 | Bỏ trống field bắt buộc khi sửa | Mandatory update | Form Sửa | 1. Xóa Tên 2. Submit | name=`` | Chặn, message bắt buộc | | |
| G04 | Đổi trạng thái | is_active | NCC ACTIVE | 1. Chuyển INACTIVE (nếu UI hỗ trợ) | — | DB `is_active=0` | | status không có trên form → kiểm qua API |

### Nhóm H — 論理削除 (Xóa mềm)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| H01 | Xóa mềm | Soft delete | Có NCC | 1. Bấm Xóa 2. Xác nhận | — | HTTP 204; DB `deleted_at` set (KHÔNG xóa cứng); `deleted_by` set | | |
| H02 | Ẩn khỏi list sau xóa | Filter deleted | Sau H01 | 1. Reload list | — | NCC đã xóa **không** hiển thị (query lọc `deleted_at IS NULL`) | | |
| H03 | Tạo lại mã đã xóa | Unique vs soft-delete | Đã xóa mã X | 1. Tạo NCC mới mã X | — | **Kỳ vọng xác định:** hoặc cho tạo (nếu unique tính cả deleted thì lỗi) | | ⚠️ UNIQUE `(tenant_id,code)` không loại deleted → có thể chặn tạo lại. **Defect#5 cần xác nhận** |

### Nhóm I — 権限・テナント (Phân quyền & đa tenant)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| I01 | Thiếu quyền tạo | Authorization | User không có `supplier.write` | 1. POST `/api/v1/suppliers` | DS-VALID | HTTP 403 | | |
| I02 | Thiếu quyền xem | Authorization | Không có `supplier.read` | 1. GET list | — | HTTP 403 | | |
| I03 | Cách ly tenant (đọc) | Multi-tenant | NCC thuộc tenant B | 1. Login tenant A 2. GET `/suppliers/{id của B}` | — | HTTP 404 (không lộ data tenant khác) | | |
| I04 | Cách ly tenant (sửa) | Multi-tenant | Như I03 | 1. PUT NCC của B | — | 404/403, không cập nhật | | |

### Nhóm J — セキュリティ・文字コード (Bảo mật & encoding)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| J01 | XSS | Chống script | Form Tạo | 1. name = `<script>alert(1)</script>` 2. Submit 3. Xem list/detail | như bên | Hiển thị dưới dạng **text**, không thực thi script | | |
| J02 | SQL injection | Chống SQLi | Form Tạo | 1. code = `x'); DROP TABLE...--` 2. Submit | như bên | Lưu nguyên văn như chuỗi; **không** ảnh hưởng DB (Dapper param hóa) | | |
| J03 | Tiếng Việt có dấu | Encoding utf8mb4 | Form Tạo | 1. name = `Nhà thuốc Đặng Hữu Nghĩa` 2. Ghi 3. Load lại | như bên | Lưu & hiển thị **đủ dấu**, không thành `?` hay mojibake | | Cột collation utf8mb4 |
| J04 | Emoji / ký tự 4-byte | utf8mb4 | Form Tạo | 1. name chứa emoji 2. Ghi | `ABC 💊` | Lưu được (utf8mb4 4-byte) hoặc báo lỗi rõ ràng | | |

### Nhóm K — UI/UX・異常系 (Giao diện & luồng bất thường)

| No | 中項目 | 観点 | 前提 | 操作手順 | テストデータ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|---|---|
| K01 | Hủy không lưu | Cancel | Form đã nhập dở | 1. Nhập vài field 2. Bấm Hủy/đóng | — | Dialog đóng; **không** tạo bản ghi; list không đổi | | |
| K02 | Đóng rồi mở lại | Reset form | Sau K01 | 1. Mở lại form Tạo | — | Form **rỗng** (không giữ data lần trước) | | |
| K03 | Chống double submit | Nút disable | Form hợp lệ | 1. Bấm Submit liên tục 2 lần nhanh | DS-VALID | Nút disable khi đang gửi; chỉ 1 bản ghi | | Liên quan F04 |
| K04 | Message thành công | Feedback | Sau khi tạo | 1. Submit hợp lệ | — | Toast/thông báo **tiếng Việt** "Tạo thành công" | | |
| K05 | Lỗi server | Error handling | Giả lập 500 | 1. Submit khi BE lỗi | — | Hiển thị message lỗi thân thiện, không trắng trang, không mất data đã nhập | | |
| K06 | Mất mạng/timeout | Resilience | Ngắt mạng | 1. Submit | — | Báo lỗi kết nối; cho phép thử lại | | |

---

## 4. 境界値一覧 (Bảng giá trị biên tổng hợp)

| Field | min-1 | min | Bình thường | max | max+1 | Kỳ vọng tại max+1 |
|---|---|---|---|---|---|---|
| code | `""` (0) → NG bắt buộc | 1 ký tự | 5-10 | **30** → OK | 31 → cần chặn | C07 |
| name | `""` → NG bắt buộc | 1 | 20 | **255** → OK | 256 → chặn | C09 |
| tax_code | `""` → OK (optional) | 1 | 10 | **20** → OK | 21 → chặn | C10 |
| phone | `""` → OK | 1 | 11 | **30** → OK | 31 → chặn | C11 |
| email | `""` → OK | — | 15 | **100** → OK | 101 → chặn | C12 |
| contact_name | `""` → OK | 1 | 12 | **100** → OK | 101 → chặn | — |

---

## 5. 設計時点の指摘 (Defect candidates phát hiện khi thiết kế test)

> Các điểm dưới đây phát hiện **trước khi chạy** từ đọc code — cần dev xác nhận & vá.

| ID | Mức | Mô tả | Vị trí | Kỳ vọng đúng |
|---|---|---|---|---|
| **Defect#1** | Thấp | zod `min(1)` không `trim()` → mã/tên toàn khoảng trắng lọt qua validate | `SupplierForm.tsx:13,14` | `.trim().min(1)` hoặc trim trước submit |
| **Defect#2** | **Cao** | **Không** giới hạn maxLength ở FE và BE; nhập vượt độ dài cột → lỗi DB "Data too long" (thường 500), không có message thân thiện | FE `SupplierForm.tsx`, BE `SupplierHandlers.cs` (không có validator) | Thêm `maxLength`/zod `.max(n)` FE + FluentValidation `.MaximumLength(n)` BE khớp độ dài DB |
| **Defect#3** | TB | Không validate định dạng `phone`, `tax_code` (nhận cả chữ/ký tự lạ) | mọi tầng | Regex phone/MST theo chuẩn VN |
| **Defect#4** | **Cao** | `CreateSupplierHandler` không precheck mã trùng → dựa vào duplicate key DB, message không tiếng Việt | `SupplierHandlers.cs:143-171` | Precheck `(tenant_id,code)` → trả `SUPPLIER_CODE_DUPLICATE` + message VN |
| **Defect#5** | TB | UNIQUE `(tenant_id, code)` không tính `deleted_at` → không tạo lại được mã đã xóa mềm | `9005:206` | Unique lọc soft-delete, hoặc dùng partial/logic ở app |
| **Defect#6** | Thấp | Lệch tên `contact_person` (FE/DTO) ↔ `contact_name` (DB) dễ gây nhầm khi maintain | `SupplierHandlers.cs:162,190` | Thống nhất tên hoặc ghi chú rõ mapping |
| **Defect#7** | TB | Không có validator BE → mọi ràng buộc phụ thuộc FE; gọi API trực tiếp bỏ qua hết | BE | Thêm `SupplierRequestValidator : AbstractValidator<SupplierRequest>` |

---

## 6. 実施サマリ (Bảng tổng hợp thực thi)

| Nhóm | Tổng case | OK | NG | N/A | 保留 | Ghi chú |
|---|---|---|---|---|---|---|
| A — Load ban đầu | 6 | | | | | |
| B — Nhập/hiển thị field | 8 | | | | | |
| C — Validate input | 15 | | | | | |
| D — Business rule | 3 | | | | | |
| E — Insert DB | 8 | | | | | |
| F — Load sau insert | 4 | | | | | |
| G — Sửa | 4 | | | | | |
| H — Xóa mềm | 3 | | | | | |
| I — Quyền & tenant | 4 | | | | | |
| J — Bảo mật & encoding | 4 | | | | | |
| K — UI/UX & luồng lỗi | 6 | | | | | |
| **TỔNG** | **65** | | | | | |

**Tiêu chí PASS:** 100% case mức Cao OK; ≥ 95% tổng case OK; mọi NG có defect ticket & retest.

---

## Phụ lục — Cách nhân bản mẫu này cho màn hình khác

1. Copy file, đổi 機能ID + thông tin mục 1.
2. Chạy lại "field definition matrix" (mục 1) bằng cách đọc: form FE (`*Form.tsx`) + DTO/validator BE (`*Handlers.cs`) + `CREATE TABLE` trong `db/migrations`.
3. Với mỗi field, sinh case theo **catalog 12 nhóm観点** (mục 0.3) + bảng biên (mục 4).
4. Đối chiếu 3 tầng để lộ defect thiết kế (mục 5) — đây là giá trị lớn nhất của UTC.
