# UTC — Quy ước chuẩn Nhật (単体テスト仕様書 共通規約)

> File chuẩn **dùng chung** cho mọi tài liệu UTC màn hình trong Pro-Diab HIS. Mỗi file UTC màn hình (`utc-<màn-hình>.md`) tham chiếu file này cho phần quy ước & catalog test viewpoint, chỉ tập trung vào **field matrix + test case + defect** riêng của màn hình đó.

## 1. Cột trong bảng test case
| Cột | Ý nghĩa |
|---|---|
| **No** | Số hiệu case duy nhất (`A01`, `C12`…) |
| **中項目 (Hạng mục)** | Nhóm chức năng đang test |
| **観点 (Viewpoint)** | Góc nhìn kiểm thử — *lý do* case tồn tại |
| **前提 (Tiền đề)** | Điều kiện trước khi thực hiện |
| **操作手順 (Thao tác)** | Các bước, đánh số |
| **テストデータ (Dữ liệu)** | Dữ liệu nhập cụ thể |
| **期待結果 (Kết quả mong đợi)** | Kết quả đúng theo spec |
| **判定** | `OK` / `NG` / `N/A` / `保留` — tester điền |
| **備考 (Ghi chú)** | Defect ID, số liệu thực tế |

**Ký hiệu 判定:** `OK`=đúng · `NG`=sai→defect · `N/A`=không áp dụng · `保留`=chưa test được.

## 2. Catalog test viewpoint chuẩn (観点カタログ) — 12 nhóm
Khi làm UTC cho **bất kỳ** màn hình nhập liệu, quét đủ 12 nhóm:

| # | 大項目 (Nhóm lớn) | Điểm cần phủ |
|---|---|---|
| 1 | **初期表示** (Load ban đầu) | Tiêu đề, control hiển thị đủ, default, focus, trạng thái nút, quyền |
| 2 | **項目表示・設定** (Load data vào field) | Mỗi field nhận & hiển thị đúng, dropdown load đủ option, format |
| 3 | **入力チェック** (Validate input) | Bắt buộc, độ dài max/min, kiểu (số/chuỗi/email/ngày), control type, ký tự đặc biệt |
| 4 | **境界値** (Giá trị biên) | max, max+1, min, min-1, rỗng, 0, 1 |
| 5 | **業務ルール** (Business rule) | Trùng khóa unique, quan hệ field, trạng thái |
| 6 | **DB登録** (Ghi DB) | INSERT đúng bảng/cột/kiểu, tenant_id, cột audit, mã hóa |
| 7 | **登録後再表示** (Load sau ghi) | List/detail phản ánh đúng data, không mất/đổi |
| 8 | **更新・削除** (Sửa/Xóa) | Update đúng, soft-delete set `deleted_at`, không xóa cứng |
| 9 | **権限・テナント** (Quyền & đa tenant) | Chặn khi thiếu quyền, cách ly tenant |
| 10 | **セキュリティ** (Bảo mật) | XSS, SQL injection, mã hóa cột nhạy cảm |
| 11 | **文字コード** (Encoding) | Tiếng Việt có dấu, emoji, khoảng trắng |
| 12 | **UI/UX・異常系** (Giao diện & luồng lỗi) | Cancel không lưu, nút disable khi submit, message lỗi, timeout |

## 3. Quy trình nhân bản UTC cho 1 màn hình
1. Đọc **3 tầng** để lập field matrix: form FE (`*Form.tsx`/`*Tab.tsx`) + DTO/validator BE (`*Handlers.cs`, `AbstractValidator`) + `CREATE TABLE` trong `db/migrations`.
2. Với mỗi field sinh case theo 12 nhóm観点 + bảng biên.
3. **Đối chiếu 3 tầng** để lộ defect thiết kế (chênh lệch FE/BE/DB) — giá trị lớn nhất của UTC.

## 4. Ràng buộc hệ thống chung (mọi màn hình master)
- **Multi-tenant:** `tenant_id` server tự gán từ JWT, KHÔNG nhận từ client; query luôn lọc theo tenant → case cách ly tenant là bắt buộc.
- **Soft delete:** mọi bảng nghiệp vụ có `deleted_at`; xóa = set `deleted_at`, list lọc `deleted_at IS NULL`.
- **Audit:** `created_at/by`, `updated_at/by` tự set.
- **Envelope API:** success `{ "data": ..., "meta": ... }`; lỗi `{ "error": { "code", "message", "details" } }`; `code` SCREAMING_SNAKE tiếng Anh, `message` tiếng Việt có dấu.
- **Encoding:** DB `utf8mb4`; mọi field text phải chịu được tiếng Việt có dấu.

## 5. Tiêu chí PASS (chung)
100% case mức Cao OK; ≥ 95% tổng case OK; mọi NG có defect ticket & retest.

---
## Danh sách UTC màn hình master

**Nhóm master có CRUD đầy đủ (form FE + API):**
- `utc-nha-cung-cap.md` — Nhà cung cấp
- `utc-benh-nhan.md` — Bệnh nhân
- `utc-danh-muc-thuoc.md` — Danh mục thuốc
- `utc-dich-vu.md` — Dịch vụ
- `utc-vai-tro.md` — Vai trò & Quyền
- `utc-api-partner.md` — API Partner
- `utc-phong-kham-tenant.md` — Phòng khám (Tenant)
- `utc-nguoi-dung.md` — Người dùng
- `utc-labrad-partners.md` — Đối tác CLS (Lab Partners)

**Nhóm master có API nhưng FE thiếu/hỏng (UTC test tầng API + ghi defect FE):**
- `utc-emr-templates.md` — Mẫu bệnh án (FE form chưa nối)
- `utc-kho-warehouse.md` — Kho thuốc (chỉ API, FE dropdown)
- `utc-phong-rooms.md` — Phòng khám/buồng (chỉ API)
- `utc-nhom-thuoc-categories.md` — Nhóm thuốc (chỉ List+Create)

**Nhóm tra cứu / read-only:**
- `utc-icd10.md` — ICD-10 (tra cứu, không CRUD)

**KHÔNG cần UTC master riêng** (enum cứng / config 1 bản ghi / mock): Nhóm dịch vụ (enum 6 giá trị), DTQG (config token), E-Invoice (mock FE), Notifications/VAPID (config). Có thể viết "UTC config" riêng cho DTQG & VAPID nếu cần.
