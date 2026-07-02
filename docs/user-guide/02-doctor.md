# 02 — Bác sĩ / Khám bệnh

Dành cho vai trò: **BacSi** (Bác sĩ)

---

## 1. Màn hình Khám bệnh

Vào **Khám bệnh** (`g e`). Danh sách lượt khám hôm nay của bác sĩ đang đăng nhập.

Cột lọc: Trạng thái (Chờ / Đang khám / Hoàn thành / Đã huỷ), ngày, bác sĩ.

---

## 2. Mở lượt khám

Click vào dòng lượt khám hoặc nút **Xem chi tiết**. Màn hình chi tiết gồm 3 cột:

| Cột trái | Cột giữa | Cột phải |
|---|---|---|
| Hồ sơ BN, BHYT, dị ứng | Bệnh án điện tử (EMR) | Kê đơn, CLS, thanh toán |

---

## 3. Nhập sinh hiệu (Vital Signs)

Điều dưỡng thường nhập trước. Bác sĩ có thể điều chỉnh trong 24 giờ:

- Nhiệt độ, Mạch, HA tâm thu/tâm trương, SpO2, Nhịp thở
- Cân nặng, Chiều cao → hệ thống tự tính BMI
- Đường huyết, Điểm đau (0-10)

---

## 4. Viết bệnh án điện tử (EMR)

1. Chọn **Mẫu bệnh án** (nếu có)
2. Nhập nội dung: Lý do khám, Bệnh sử, Khám thực thể, Chẩn đoán, Kế hoạch điều trị
3. **Lưu nháp** (tự động mỗi 30 giây hoặc nhấn `Ctrl+S`)
4. Khi hoàn tất → **Ký số** bệnh án

> Lưu ý: Sau khi ký số, bệnh án không thể chỉnh sửa nội dung. Chỉ có thể thêm ghi chú bổ sung.

---

## 5. Chẩn đoán ICD-10

Trong phần Chẩn đoán của bệnh án:

1. Click **Thêm chẩn đoán**
2. Gõ tên bệnh hoặc mã ICD-10 vào ô tìm kiếm
3. Chọn chẩn đoán từ danh sách
4. Đánh dấu **Chẩn đoán chính** (Primary)

Lưu ý: Phải có ít nhất 1 chẩn đoán chính mới có thể ký số bệnh án.

---

## 6. Chỉ định CLS (Xét nghiệm / CĐHA)

### Xét nghiệm
1. Trong tab **CLS** → **Chỉ định XN**
2. Chọn danh mục xét nghiệm (tìm kiếm theo tên/mã)
3. Chọn độ ưu tiên: Thường / Khẩn
4. Xác nhận → hệ thống gửi lệnh đến phòng lab

### Chẩn đoán hình ảnh (CĐHA)
1. Tab **CLS** → **Chỉ định CĐHA**
2. Chọn kỹ thuật (X-quang, Siêu âm, CT, MRI...)
3. Nhập vị trí/vùng cơ thể, có cản quang không
4. Xác nhận

---

## 7. Kê đơn thuốc

1. Tab **Kê đơn** → **Thêm thuốc**
2. Gõ tên thuốc/hoạt chất — hệ thống gợi ý từ danh mục BHYT và kho
3. Nhập liều dùng, cách dùng, số ngày
4. Hệ thống kiểm tra **tương tác thuốc (DDI)** — cảnh báo nếu có xung đột
5. **Ký số đơn thuốc** → hệ thống đẩy lên **Đơn thuốc Quốc gia (ĐTQG)**
6. In QR Code đơn thuốc cho bệnh nhân

---

## 8. Đánh giá tiểu đường

Dành cho bệnh nhân đái tháo đường, tab **Đánh giá ĐTĐ**:

- Nhập HbA1c, đường huyết đói, đường huyết sau ăn, eGFR
- Đánh dấu biến chứng (mắt, thận, tim mạch, thần kinh, bàn chân)
- Đặt mục tiêu điều trị
- Hệ thống vẽ biểu đồ xu hướng HbA1c theo thời gian

---

## 9. Hoàn thành lượt khám

1. Đảm bảo bệnh án đã ký số
2. Chẩn đoán chính đã có
3. Click **Hoàn thành** → trạng thái chuyển sang "Hoàn thành"
4. Bệnh nhân được chuyển sang Thu ngân
