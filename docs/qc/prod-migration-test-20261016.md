# Kiểm thử E2E hệ thống PRODUCTION với dữ liệu migrate — 2026-10-16

> Môi trường: prod stack `/srv/apps/pro/his` (VNG), nginx prod `127.0.0.1:19090`, test qua SSH tunnel.
> FE prod build API base `https://his.diab.com.vn` (domain CHƯA cutover) → Playwright `route.fetch` rewrite `his.diab.com.vn/*` → `localhost:19090/*` để tránh gọi nhầm staging.
> Tài khoản: 5 user non-admin (lễ tân/bác sĩ/dược sĩ/kế toán/điều dưỡng) từ `USER_CREDENTIALS.tsv` (admin bỏ qua vì bắt buộc 2FA; mật khẩu KHÔNG ghi ở đây).
> Dữ liệu: 303 BN legacy (`LEG-IMPORT-2026`), 649 lượt khám, 2.609 lab result, 147 đơn thuốc, danh mục SUNS (47 dịch vụ, 248 thuốc, 9 phòng, 9 phác đồ, 1 gói).
> Evidence (screenshot + JSON kết quả): `docs/qc/evidence-prod-migration-20261016/` (không commit ảnh).

## 1. Tổng kết

**KẾT LUẬN: Dữ liệu migrate hiển thị và vận hành ĐÚNG trên production. Không phát hiện lỗi dữ liệu mới.** 0 lỗi 5xx, 0 ngày `0001-01-01`, 0 mojibake, 0 FAIL trong 34 ca read + chi tiết + search + write flow. Các đợt sửa data trước (`prod-data-check-20261016.md`) đã có hiệu lực trên prod.

| Nhóm kiểm thử | Kết quả |
|---|---|
| Đăng nhập 5 vai (non-admin) | 5/5 PASS |
| Hiển thị danh sách (BN/lượt khám/đơn/thuốc/XN/hóa đơn) | PASS, 0 lỗi |
| Chi tiết lượt khám legacy (EMR/sinh hiệu/chẩn đoán/CLS/đơn/tái khám) | PASS, tiếng Việt đúng |
| Tìm kiếm BN (tên/mã/SĐT, SĐT dùng chung) | PASS |
| Tiếp đón → tạo lượt khám mới (BN test) | PASS |
| Dashboard/biểu đồ với dữ liệu lịch sử | PASS |
| Dọn dữ liệu test (soft-delete + backup) | PASS |

## 2. Phương pháp
- Playwright headless (Chromium), 1 context/vai, bật rewrite host để FE prod gọi đúng backend prod qua tunnel.
- Mỗi trang: quét `innerText` tìm `0001-01-01`, ký tự thay thế `�`, mojibake latin1, `undefined/NaN`; bắt mọi response `/api/*` ≥400 và console error; chụp screenshot.
- Đối chiếu API trực tiếp (curl) cho search, giá danh mục, chi tiết đơn thuốc.
- Soi `docker logs pro-his-backend` tìm exception.

## 3. Ca test chi tiết

### 3.1 Lễ tân (`lchoan`) — PASS
- Tìm BN: `""`→303, chuỗi vô nghĩa `ZZZQWX999`→0, mã `DIAB2610140386`→1, tên `HOÀNG`→21 (lọc đúng, không trả hết 303). **Xác nhận fix A1 (search có lọc) đã lên prod.**
- **SĐT dùng chung gia đình**: `0986787865` → đúng **3 người** (BÙI PHỤ HOÀNG NAM, VÕ THỊ TƯỜNG VI, BÙI MINH KHUÊ). Blind-index SĐT hoạt động đúng trên prod.
- Mở hồ sơ BN + reception render đủ (5 phòng khả dụng từ `/reception/rooms`, 9 phòng tổng từ `/rooms`).
- **Tiếp đón tạo lượt khám MỚI**: tạo BN test → `check-in` (phòng PK01, ưu tiên Thông thường, lý do khám) → `admit` → sinh `encounter_id` thành công. Luồng tiếp đón hoạt động với phòng migrate.

### 3.2 Bác sĩ (`bshuong`) — PASS
- Danh sách lượt khám: status đồng loạt `DONE`/"Hoàn thành" (xác nhận fix FIX-5 IN_PROGRESS→DONE đã lên prod).
- Chi tiết lượt khám legacy: tên BN + tuổi + mã + SĐT đúng; chẩn đoán ICD (vd `E66`); banner "Bệnh án đã khoá — chỉ xem"; các tab Bệnh án/Tiền sử/Chỉ định/Kết quả CLS/Chẩn đoán/Đơn thuốc/Tái khám đều mở, 0 lỗi, không `0001-01-01`.
- `doctor_id` NULL legacy → hiển thị "Chưa phân công" (xử lý null an toàn, không 500) — đúng quyết định C1.
- Danh mục thuốc/XN hiển thị đủ.

### 3.3 Dược sĩ (`qtthuong`) — PASS
- Danh mục thuốc: 248 thuốc, giá bán hiển thị (một số = 0 đúng như gap đã ghi nhận — 85 thuốc chờ nhập giá tay).
- Trang kho/cấp phát + danh sách đơn thuốc render, 0 lỗi 5xx.
- Chi tiết đơn thuốc: `full_name` Unicode đúng (vd "HOÀNG VĂN TIẾN" = codepoint chuẩn, KHÔNG mojibake), `total_amount` tính đúng, danh sách thuốc + liều dùng hiển thị.

### 3.4 Kế toán (`accounting`) — PASS
- Thu ngân/hóa đơn/công nợ render, 0 lỗi.
- **Dashboard**: biểu đồ "Lượt khám 30 ngày" và "Top 10 thuốc (doanh thu)" có dữ liệu lịch sử thật. "Doanh thu 30 ngày" và "Top 10 bác sĩ" trống ("Chưa có dữ liệu") — **đúng** do migration không mang revenue/billing và `doctor_id` NULL; UI xử lý trống mượt, không lỗi.

### 3.5 Điều dưỡng (`dhnvan`) — PASS (có cảnh báo RBAC)
- Trang điều dưỡng + lượt khám + BN render đủ dữ liệu.

## 4. Quan sát (KHÔNG phải lỗi data migrate)
1. **403 RBAC lành tính** (do FE gọi endpoint ngoài quyền của vai, không ảnh hưởng chức năng chính):
   - `dieu_duong`: `GET /branches`, `/reception/rooms`, `/reception/stats`.
   - `duoc_si`: `GET /reports/diabetes/cohort`.
   - `bac_si`: `GET /patients/{id}/package-summary`.
   - *Gợi ý (không bắt buộc)*: FE nên gate các call này theo permission để giảm 403 noise trên console.
2. **Chi tiết đơn thuốc (DTO) không expose giá/dòng** (`items[]` chỉ có field lâm sàng, không có `unit_price/line_total`); tổng vẫn đúng qua `total_amount`. Là thiết kế DTO, không phải lỗi dữ liệu.
3. **"Doanh thu" dashboard trống**: hệ quả migration không mang dữ liệu thanh toán — cần clinic nhập/vận hành thực tế để có revenue.

## 5. Dọn dữ liệu test
- Tạo 1 BN test `ZZ TEST QA 20261016` (code `BNT01000001`) + 1 vé tiếp đón + 1 lượt khám.
- **Backup rows trước khi xóa**: `/home/stackops/backups/pro-his/qa-testdata-20261016_224308.sql`.
- **Soft-delete** (set `deleted_at`) cả 3 bản ghi: patient, encounter (`c07f13f3...`), ticket (`a1d3a617...`). KHÔNG xóa cứng, KHÔNG đụng dữ liệu legacy.
- Verify sau dọn: tổng BN sống **303** (nguyên vẹn), search "ZZ TEST QA" → **0**. `docker logs pro-his-backend` 0 exception/500 trong phiên test.

## 6. Lỗi cần sửa (đợt READ)
Không có lỗi dữ liệu hay lỗi code mới phát sinh từ đợt test này. Các vấn đề đã biết (doctor_id NULL, một số thuốc giá 0, revenue chưa có) nằm trong mục "chờ clinic quyết định" của `prod-data-check-20261016.md`, không phải lỗi.

---

# PHẦN II — Kiểm thử luồng GHI lâm sàng end-to-end (2026-10-17)

> Mục tiêu: chạy trọn 1 ca khám MỚI trên production với danh mục migrate (SUNS) để xác nhận nghiệp vụ ghi + giá chảy đúng.
> Ưu tiên UI; UI wizard bị chặn bởi onboarding tour + loading state nên phần lớn thao tác GHI thực hiện qua **API thật của backend prod** (cùng handler UI gọi), có ghi rõ. 1 BN test `ZZ TEST QA 20261017`.

## 7. Kết quả luồng GHI

| # | Bước | Vai | Kết quả |
|---|---|---|---|
| 1 | Tạo BN test | lễ tân | PASS (code BNT01000003) |
| 2 | Check-in phòng PK01 → admit tạo lượt khám | lễ tân | PASS (encounter tạo) |
| 3 | Bắt đầu khám (`start`) | bác sĩ | PASS (200) |
| 4 | Nhập sinh hiệu (HA 130/85, SpO2 98, cân nặng, glucose…) | bác sĩ | PASS (201) |
| 5 | EMR (lý do khám, bệnh sử, khám) — bản nháp | bác sĩ | PASS (200) |
| 6 | Chẩn đoán ICD-10 `E66` (béo phì) PRIMARY | bác sĩ | PASS (201) |
| 7 | **Chỉ định XN bằng phác đồ "Giảm cân"** → 17 XN | bác sĩ | PASS (17 orders, tên XN đúng) |
| 8 | Nhập 17 kết quả XN | điều dưỡng | PASS (17/17); verify cần người khác (SELF_FORBIDDEN) → bác sĩ verify chéo PASS |
| 9 | **Kê đơn ADENURIC 80MG × 14 từ danh mục SUNS** | bác sĩ | PASS — `total_amount = 546.000 = 14 × 39.000` (**giá SUNS đúng**) |
| 10 | Thu ngân: tạo hoá đơn từ lượt khám | kế toán | PASS — 17 dòng XN **đúng giá SUNS** (HbA1c 250k, TSH 190k, Acid Uric 75k…), subtotal **1.932.000** |
| 11 | Finalize + thanh toán tiền mặt | kế toán | PASS — payment COMPLETED, bill **PAID**, balance 0 |
| 12 | Dashboard doanh thu cập nhật | kế toán | PASS — `today_revenue = 1.932.000` ngay sau thanh toán |

## 8. Bước bị CHẶN (không phải lỗi data migrate — là yêu cầu hạ tầng)

1. **Ký số EMR + ký đơn thuốc + hoàn tất (close) lượt khám**: cần **chữ ký số bằng chứng thư thật** (cert). Thử ký đơn → `409 PRESCRIPTION_SIGNATURE_FAILED`; close → `422 EMR_NOT_SIGNED`. Tài khoản test không có cert, admin bắt buộc 2FA → không ký được. Đây là tính năng bảo mật, **không tự bypass**.
2. **Dược sĩ cấp phát (dispense)**: hàng đợi cấp phát CHỈ nhận đơn **đã ký**; đơn test dừng ở trạng thái `DRAFT` nên không vào được hàng đợi → không test được cấp phát end-to-end. Ngoài ra tồn kho ADENURIC = 0 (**kho trống thật** — khớp ghi nhận migration chưa nạp tồn kho), nhưng luồng "báo thiếu tồn/nhập kho" không chạm tới được do chặn upstream ở bước ký. **Thuốc vì vậy chưa vào hoá đơn** (hoá đơn gồm khám-service chưa gắn + 17 XN; phần thuốc cần đơn đã cấp phát).

→ Giá trị migration cần kiểm (danh mục SUNS chảy đúng giá vào đơn + hoá đơn) **đã được xác nhận** qua bước 9–10. Hai điểm chặn trên là tiền đề vận hành (cert ký số, nạp tồn kho), đề xuất clinic/DevOps thiết lập để chạy trọn khám→cấp phát→thu thuốc.

## 9. Dọn dữ liệu test (production về sạch)
- Void payment + void billing (đảo doanh thu → dashboard `today_revenue` về 0).
- Backup rows trước khi xóa: `/home/stackops/backups/pro-his/qa-testdata2-rows-20261017_001555.sql` và `qa-testdata2b-rows-20261017_001707.sql`.
- **Soft-delete** (set `deleted_at`) toàn bộ: 2 BN test (1 chính + 1 orphan sinh ra do script chạy lại), lượt khám, sinh hiệu, chẩn đoán, EMR, lab order (17), lab result (17), đơn thuốc + dòng đơn, hoá đơn. Payment để trạng thái **VOID** (bảng payment không có cột `deleted_at`, KHÔNG hard-delete).
- Verify cuối: BN test còn sống = **0**, tổng BN sống = **303**, dashboard `today_revenue=0` `today_encounters=0`, `docker logs pro-his-backend` **0 exception/500** suốt phiên test.

## 10. Lỗi cần sửa (đợt GHI)
Không phát hiện lỗi dữ liệu hay lỗi code. Giá SUNS (dịch vụ/XN/thuốc) chảy đúng vào đơn + hoá đơn + thanh toán + dashboard. Hai điểm chặn (ký số cert, tồn kho trống) là cấu hình vận hành, không phải lỗi migrate.
