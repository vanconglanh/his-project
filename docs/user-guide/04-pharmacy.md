# 04 — Dược sĩ / Kho dược

Dành cho vai trò: **DuocSi**

---

## 1. Tổng quan Kho dược

Vào **Kho dược** trên sidebar. Các tab chức năng:

| Tab | Chức năng |
|---|---|
| Tồn kho | Xem số lượng tồn, lô, HSD |
| Nhập kho | Tạo phiếu nhập kho từ nhà cung cấp |
| Phát thuốc | Hàng đợi đơn thuốc cần phát |
| Kiểm kê | Điều chỉnh tồn kho |
| Cảnh báo | Thuốc sắp hết, sắp hết hạn |

---

## 2. Nhập kho (GRN — Goods Receipt Note)

1. Tab **Nhập kho** → **Tạo phiếu nhập**
2. Chọn **Nhà cung cấp**
3. Thêm từng thuốc:
   - Tìm thuốc theo tên/mã
   - Nhập số lô, ngày sản xuất, ngày hết hạn (HSD)
   - Nhập số lượng, đơn vị tính, giá nhập
4. Ghi số hoá đơn/chứng từ
5. Nhấn **Xác nhận nhập kho** → tồn kho cập nhật ngay

> Hệ thống áp dụng nguyên tắc **FEFO** (First Expired First Out) — thuốc hết hạn sớm nhất được xuất trước.

---

## 3. Phát thuốc theo đơn

### Hàng đợi phát thuốc
Tab **Phát thuốc** hiển thị danh sách đơn thuốc đã ký chờ phát.

### Xử lý từng đơn
1. Click vào đơn thuốc → xem danh sách thuốc cần phát
2. Hệ thống gợi ý lô thuốc theo FEFO
3. Kiểm tra số lượng tồn, xác nhận lô thuốc
4. Nhấn **Xác nhận phát** → ghi nhận xuất kho
5. In nhãn thuốc (tùy chọn)

### Phát một phần
Nếu thiếu thuốc, có thể phát một phần và ghi nhận phần còn lại là "Chờ bổ sung".

---

## 4. Kiểm kê kho

1. Tab **Kiểm kê** → **Tạo phiếu kiểm kê**
2. Chọn kho cần kiểm kê
3. Nhập số lượng thực tế đếm được cho từng thuốc/lô
4. Hệ thống tính toán chênh lệch với số liệu hệ thống
5. Nhập lý do chênh lệch (hao hụt, hỏng, nhầm...)
6. **Xác nhận điều chỉnh** → tồn kho cập nhật

---

## 5. Cảnh báo tồn kho

Tab **Cảnh báo** hiển thị:
- **Sắp hết hạn**: thuốc hết hạn trong 30/60/90 ngày tới
- **Tồn kho thấp**: dưới ngưỡng tối thiểu đã cài đặt
- **Thuốc hết**: tồn kho = 0

Hệ thống cũng gửi thông báo tự động khi có cảnh báo mới.

---

## 6. Danh mục thuốc

Vào **Danh mục thuốc**:

- Xem/tìm kiếm tất cả thuốc trong hệ thống
- Thêm thuốc mới (nhập tay hoặc import Excel)
- Đồng bộ từ **Cơ sở dữ liệu Dược Quốc gia**

---

## 7. Nhà cung cấp

Vào **Quản trị** → **Nhà cung cấp**:
- Thêm/sửa thông tin nhà cung cấp
- Xem lịch sử nhập kho theo NCC
