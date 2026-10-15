# Bộ test hồi quy (regression) — Pro-Diab HIS

Mục đích: **đảm bảo các bug đã fix không mở lại (regression guard)**. Chạy lại sau mỗi lần deploy để phát hiện hồi quy sớm.

## Cách chạy

Cần Node (dùng Playwright/Chromium có sẵn trong `frontend/node_modules`). Chạy từ **thư mục gốc repo**:

```bash
BASE_URL=https://his.diab.vn node docs/qc/regression/regression.js
```

- Mặc định `BASE_URL = https://his.diab.vn`.
- Kết quả in ra console và ghi vào `docs/qc/regression/RESULT.txt`.
- Exit code: `0` nếu tất cả PASS, `1` nếu có FAIL (dùng được trong CI).

> Yêu cầu bản deploy phải bật panel đăng nhập nhanh (`NEXT_PUBLIC_TEST_LOGIN_PANEL=true`) — môi trường test VNG đã bật.

## Cách hoạt động

- Đăng nhập bằng panel "Đăng nhập nhanh theo vai trò" (không gọi API tắt).
- Phần lớn assertion gọi **API thật** bằng access token lấy từ `localStorage` → robust, ít tạo dữ liệu rác.
- Dữ liệu tạo ra (nếu có) dùng tiền tố `REGR-`; test BM-81 cố tình gửi request thiếu dữ liệu nên **bị chặn**, không tạo record.
- Đặt **ngoài `frontend/`** để không ảnh hưởng `tsc`/`eslint`/`next build` của frontend.

## Các ticket được phủ (12 check)

| Check | Ticket | Hành vi đã fix được xác nhận |
|---|---|---|
| BM-49 | timezone | API `/encounters` trả datetime có hậu tố `Z` (UTC) → FE đổi sang giờ VN |
| BM-37/63 | quyền hoá đơn | JWT Lễ tân KHÔNG có `billing.finalize` / `billing.void` (FE ẩn nút) |
| BM-61 | strip lâm sàng | Lễ tân `GET /encounters/{id}` → `diagnoses` rỗng |
| BM-04 | tìm bệnh nhân | `GET /patients?search=...` trả kết quả |
| BM-81 | ràng buộc ngày sinh | `POST /patients` thiếu ngày sinh → 4xx (bị chặn) |
| BM-86 | tự gia hạn phiên | `POST /auth/refresh {refresh_token}` → 200 (không đăng xuất) |
| BM-51 | báo cáo 403 | KTV `GET /reports/revenue-daily/data` → 403 (không trả số mẫu) |
| BM-56 | doanh thu Tổng quan | KTV charts `revenue-trend`/`top-doctors` → 403 |
| BM-71 | casing cột báo cáo | Kế toán report → 200, cột snake_case (không camelCase) |
| BM-70 | chặn giá trị vô lý | Template DIABETES có `min`/`max` cho HbA1c (FE chặn 999) |
| BM-64 | required ngầm | Template DIABETES chỉ `ly_do_kham` required, có ô không-required |
| BM-47/65 | lưu EMR | Bác sĩ `PUT /encounters/{id}/emr` → 200 (không 500/không báo thất bại giả) |

Các ticket UI thuần (BM-07, 33, 36, 40, 48, 50, 59, 60, 67, 74, 82) được kiểm thủ công/evidence riêng; bộ này tập trung assertion API/JWT (robust) cho các hành vi đã fix.
