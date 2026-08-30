# Evidence browser — màn admin Giá theo chi nhánh + autocomplete
Ngày: 2026-08-30. Stack local (docker compose, đã rebuild BE/FE + apply migration 9185).
Tài khoản: qc.admin@prodiab.test (role admin, có quyền service.price_override + drug.price_override).

## Màn admin /admin/branch-pricing (render thật trên trình duyệt)
- Tiêu đề: "Giá theo chi nhánh — Override giá và ẩn/hiện dịch vụ, thuốc theo chi nhánh hoặc nhóm chi nhánh".
- 2 tab: "Dịch vụ" và "Thuốc". Bảng đủ cột: Tên item, Phạm vi, Chi nhánh/Nhóm, Giá, Trạng thái hiển thị,
  Hiệu lực từ, Hiệu lực đến, Ghi chú, Thao tác (Sửa/Xoá). Nút "Thêm override".
- Tab Dịch vụ hiển thị override đã tạo: "Chup X-quang phoi (se AN o CN2)" | Chi nhánh | Chi nhánh Quận 7 (test UTE)
  | 120.000đ | badge "Ẩn" | 1/8/2026 | ghi chú "An dich vu o CN2".
- Tab Thuốc hiển thị: "Amoxicillin 500mg (se AN o CN2)" | Chi nhánh | Chi nhánh Quận 7 (test UTE)
  | 1.500đ | badge "Ẩn" | 1/8/2026 | ghi chú "An thuoc o CN2 - verify".
=> Màn admin dựng đúng, hiển thị đúng cờ ẩn/hiện (badge "Ẩn") cho cả 2 loại item. (Đã chụp screenshot 2 tab.)

## Autocomplete ẩn/hiện theo chi nhánh (AC-5) — verify qua chính endpoint mà UI gọi
DrugAutocomplete gọi GET /api/v1/drugs/search; chọn dịch vụ (CLS/hoá đơn) gọi GET /api/v1/services/search.
Cả 2 endpoint nhận branch context qua header X-Branch-Id (BranchScopeMiddleware) — đúng cơ chế BranchSwitcher FE gửi.
Kết quả (xem chi tiết api-verify.md):
- Thuốc TH002 bị ẩn ở CN2: search @branch1 -> có TH002; search @branch2 -> KHÔNG có TH002.
- Dịch vụ DV-AN bị ẩn ở CN2: search @branch1 -> có DV-AN; search @branch2 -> KHÔNG có DV-AN.
- Component chỉ render danh sách do endpoint trả về => hành vi ẩn trong autocomplete được đảm bảo.

## Ghi chú kỹ thuật khi verify
- Form login RHF khó tương tác qua automation (focus/keystroke), nên phiên đăng nhập được thiết lập bằng
  cách gọi thật POST /api/v1/auth/login + /session/set-cookie (đúng luồng use-auth.ts) rồi vào màn — token thật,
  quyền thật (227 permissions gồm drug.price_override). Đây là ràng buộc của test harness, không phải lỗi tính năng.
