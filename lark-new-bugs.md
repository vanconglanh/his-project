# Lark board — rà soát bug Pro-Diab HIS

Board: DiaB His(Web) — https://sgkgryovqjc.sg.larksuite.com/base/FaAhbhLrJaCsPms7XnWlL9gngeh?table=tblaqjTDAwrsRcOB

---

## ĐỢT 6 — Rà soát toàn board (2026-10-22)

### Phương pháp
Quét toàn bộ grid "Task summary" (70 record active) + các cột Kanban. Lark API bị chặn (RolePermNotAllow / UAT hết hạn) nên rà bằng UI.

### Phân bố trạng thái (Progress)
- **In test**: ~65 ticket (BM-36 → BM-107, trừ 4 ticket To BA). Đây là các bug Đợt 1–5 đã fix + deploy staging, **đang chờ tester verify**.
- **To BA**: 4 ticket — BM-57, BM-69, BM-73, BM-97.
- **Not started / In dev / Return in dev**: **0 ticket**. Không có ticket nào tester trả về dev (không có cột Return in dev được populate), không có ticket dev đang dở.
- **Closed / Cancelled**: ngoài phạm vi grid active.

### Kết luận actionable
Hiện KHÔNG có ticket mới ở Not started/In dev/Return in dev. Không tìm thấy comment mới (ngày ~20–22/10) của tester "Dư Mỹ Vân" trên các ticket đã mở kiểm (comment gần nhất trên các ticket To BA là 10–11/10 từ Nguyễn Ngọc Minh Chi / ghi chú "Claude" test lại). Các ticket In test đang chờ tester.

### Chi tiết 4 ticket To BA
| Ticket | Issue | Bản chất | Hướng xử lý |
|---|---|---|---|
| **BM-57** | [HIS-12] Bác sĩ 2 sửa được bệnh án ca của bác sĩ khác (đang khám, chưa ký số), KHÔNG cảnh báo/chặn | **Bug thật** — tester re-test 11/10 "KHÔNG ĐẠT". Expectation: mở ca của bác sĩ khác → cảnh báo hoặc khoá editor (EmrTabPanel) | **DEV-ACTIONABLE** → fix (cảnh báo + read-only cho bác sĩ không phải chủ ca) |
| **BM-69** | [HIS-24] Khám bệnh — đường huyết dùng hai đơn vị, cùng chỉ số | Quyết định chuẩn đơn vị (mg/dL vs mmol/L) | Cần BA chốt chuẩn đơn vị |
| **BM-73** | [HIS-28] Danh mục — dữ liệu danh mục thuốc & dịch vụ | Nội dung dữ liệu danh mục | Cần BA/nghiệp vụ cung cấp data |
| **BM-97** | [HIS-51] Dược sĩ — chưa có chức năng bán thuốc/thiết bị y tế | **Feature mới** (bán lẻ thuốc/thiết bị) | Cần BA/product quyết định scope |

### Hành động Đợt 6 — KẾT QUẢ

| Ticket | Kết quả | Commit / Ghi chú |
|---|---|---|
| **BM-57** | ✅ Fix + deploy staging + test E2E (Bác sĩ 2 mở ca Bác sĩ 1 → banner cảnh báo + editor khoá) → **In test** | `7b2566f` (FE EncounterDetailClient canEdit theo chủ ca + banner; BE SaveEmrDraft chặn FORBIDDEN) |
| **BM-69** | ✅ Fix + deploy staging + test (5.6 mmol/L ↔ 100 mg/dL) → **In test** | `1e32bf2` (chuẩn hoá hiển thị mmol/L + toggle, hệ số 18.016; KHÔNG đổi schema/quy đổi dữ liệu cũ — glucose_mg_dl vẫn là nguồn sự thật) |
| **BM-73** | ✅ Nạp SUNS catalog vào staging + test UI (/drugs, /services có data + giá) → **In test** | Import `import_suns_catalog.py`; GOTCHA: clinic test là **tenant 2** (DIAB-TEST), lần đầu nạp nhầm tenant 1 → đã re-import tenant 2 (165 thuốc có giá, 49 dịch vụ, 159 XN, 13 CĐHA). Backup: `/home/stackops/backups/stg-his/prodiab_his_before_suns_20261022_190609.sql.gz` |
| **BM-97** | ⏸ Giữ **To BA** | Feature mới (bán thuốc/thiết bị y tế) — cần BA/product chốt scope |

Evidence: `docs/qc/evidence-lark-20261020/BM-57-*.jpg`, `BM-69-*.jpg`, `BM-73-*.jpg`.

### Quan sát phụ (ngoài scope Đợt 6, cần theo dõi)
- Màn nhập sinh hiệu ở tiếp đón (vai Lễ tân) báo "Lịch sử đo: Không tải được lịch sử sinh hiệu" — endpoint history có thể lỗi/thiếu quyền cho Lễ tân. Chưa xử lý, nên kiểm tra riêng.
- Tenant 1 (DiaBetis HCM) đã bị nạp data SUNS thừa từ lần import đầu (devops để nguyên, không xoá vì khó tách). Nếu tenant 1 là môi trường có ý nghĩa, cần dọn.

---

## ĐỢT 7 — Ticket mới ≥ BM-109 (2026-10-22)

Nguồn: grid "Task summary" board DiaB His(Web). 4 ticket mới, đều module **Kho dược** (Pharmacy inventory), Role Quản trị viên, Priority Low, Progress Not started. Người file: Nguyen Duyen.

| Ticket | Màn | Mô tả | Expectation | Hướng |
|---|---|---|---|---|
| **BM-109** | Nhập kho > Tạo đơn đặt hàng (PO) | Tạo đơn thất bại nhưng KHÔNG hiện lý do | Hiện rõ lý do vì sao tạo đơn thất bại (vd: chưa điền đủ ô nào) | FE: hiển thị lỗi validation/BE rõ ràng |
| **BM-110** | Nhập kho (GRN) | Lỗi logic: bắt chọn đơn đặt hàng (PO) mới tạo được phiếu nhập kho | Cho phép tạo phiếu nhập kho (GRN) KHÔNG cần chọn PO — PO là tuỳ chọn | FE (+BE nếu bắt buộc PO ở API) |
| **BM-111** | Điều chỉnh kho | Thiếu trường Tên thuốc, tồn cuối, số lượng sau tăng/giảm | Dropdown thuốc→số lô→auto tồn cuối; 2 cách nhập: Số lượng (+/-) hoặc Số lượng sau — ô còn lại tự tính | FE form enhancement |
| **BM-112** | Tồn kho | Thiếu mã thuốc | Thêm cột Mã thuốc trước cột Tên thuốc | FE: thêm cột bảng |

### Hành động: xử lý từng ticket (ưu tiên bản mặc định, chỉ To BA khi thật cần BA).
