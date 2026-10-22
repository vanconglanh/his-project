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

### Hành động Đợt 6
- BM-57: fix dev (xem phần triển khai bên dưới khi hoàn tất).
- BM-69, BM-73, BM-97: giữ To BA, chờ BA (ghi rõ lý do trong comment nếu cần).
