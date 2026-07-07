# 08 — Báo cáo & Thống kê (BI)

> **Đối tượng:** Kế toán, Bác sĩ, Dược sĩ, Lễ tân, Quản trị (theo phân quyền).
> **Quyền cần có:** `report.read` (xem/tải), `report.export` (kết xuất file). Nếu không thấy menu **Báo cáo**, liên hệ Quản trị cấp quyền.

Module Báo cáo cung cấp **43 báo cáo** in/xuất trên một màn hình dùng chung: chọn báo cáo → chọn bộ lọc → **Lấy dữ liệu** → xem trên lưới → **In Phiếu (PDF)** hoặc **Xuất Excel**.

---

## 1. Mở màn hình Báo cáo

- Vào menu **Báo cáo** trên thanh điều hướng, hoặc mở trực tiếp đường dẫn `/reports`.
- Màn hình chia 2 phần:
  - **Bên trái — Danh mục báo cáo (sidebar):** danh sách báo cáo gom theo nhóm + ô tìm kiếm + mục Yêu thích/Gần dùng.
  - **Bên phải — Vùng chạy báo cáo:** thanh bộ lọc + thẻ tổng quan (KPI) + lưới kết quả + nút xuất.

---

## 2. Các bước sử dụng (quy trình chuẩn)

1. **Chọn báo cáo** ở sidebar bên trái (xem §3).
2. **Chọn khoảng thời gian**: dùng nút chọn nhanh (*Hôm nay / Tuần này / Tháng này / Tháng trước / Tùy chọn*) hoặc nhập trực tiếp **Từ ngày – Đến ngày**.
3. **Chọn bộ lọc riêng** của báo cáo nếu có (ví dụ *Người thu*, *Quầy thu*, *Bác sĩ*, *Trạng thái*…).
4. Bấm **Lấy dữ liệu**. Lưới sẽ hiển thị số liệu.
5. Xem kết quả trên lưới (có thể gồm dòng cộng theo nhóm và dòng **TỔNG CỘNG**).
6. Bấm **In Phiếu** để tải/PDF hoặc **Xuất Excel** để tải file `.xlsx`.

> 💡 Hai nút **In Phiếu** và **Xuất Excel** chỉ bật sau khi đã có dữ liệu trên lưới.

---

## 3. Danh mục báo cáo (sidebar bên trái)

- Báo cáo được **gom theo nhóm**: Tài chính · Khám bệnh/Sổ · Thống kê · Kho dược · BHYT.
- **Ô tìm kiếm** ở đầu sidebar: gõ tên báo cáo để lọc nhanh (không cần Enter).
- **⭐ Yêu thích / Gần dùng:** các báo cáo bạn hay dùng được ghim lên đầu (tối đa 5), lưu theo từng máy — giúp mở lại nhanh mỗi ngày.
- Chọn một báo cáo → vùng bên phải tự nạp bộ lọc tương ứng.

---

## 4. Thanh bộ lọc (bên phải)

| Thành phần | Ý nghĩa |
|---|---|
| Chọn nhanh ngày | Đặt sẵn khoảng ngày thông dụng; vẫn có thể chỉnh tay sau đó |
| Từ ngày – Đến ngày | Khoảng thời gian báo cáo. **Tối đa 366 ngày** một lần |
| Bộ lọc riêng | Tùy báo cáo: Người thu, Quầy thu, Bác sĩ, Trạng thái, Tiền chênh lệch… |
| **Lấy dữ liệu** | Truy vấn và hiển thị lên lưới |
| **In Phiếu** | Kết xuất **PDF A4** (có letterhead phòng khám) |
| **Xuất ra Excel** | Kết xuất **.xlsx** (số/ngày đúng định dạng để tính tiếp) |

**Ba trạng thái của lưới:**
- **Đang tải:** hiển thị khung xám (skeleton).
- **Không có dữ liệu:** *"Không có dữ liệu trong khoảng thời gian đã chọn"* — thử mở rộng khoảng ngày hoặc bỏ bớt bộ lọc.
- **Lỗi:** banner đỏ + nút **Thử lại**.

---

## 5. Đọc lưới kết quả

- **Thẻ tổng quan (KPI):** 2–3 ô ở đầu (ví dụ *Tổng thực thu*, *Số phiếu*, *Trung bình/phiếu*).
- **Nhóm (group):** một số báo cáo gom dòng theo nhân viên/nhóm, có **dòng cộng theo nhóm**.
- **Dòng TỔNG CỘNG:** cộng các cột số ở cuối bảng (nền đậm).
- Cột tiền/số **canh phải**; ngày dạng `dd/MM/yyyy`; ô trống hiển thị dấu `–`.

---

## 6. In & Xuất báo cáo

- **In Phiếu (PDF):** mở bản in A4 chuẩn diaB — đầu trang có logo, tên phòng khám, công ty, địa chỉ, liên hệ; có mã báo cáo (mã vạch), thẻ KPI, bảng số liệu và khối chữ ký cuối trang. Báo cáo nhiều cột tự chuyển **khổ ngang**.
- **Xuất Excel:** file `.xlsx` giữ đúng kiểu số và ngày (kế toán có thể SUM/lọc/ghép tiếp). Có dòng cộng nhóm/tổng cộng.
- **Đầu trang (letterhead)** lấy từ hồ sơ phòng khám (Quản trị cấu hình trong phần Tenant: tên, công ty, slogan, địa chỉ, điện thoại, website, email, logo).

---

## 7. Danh mục 43 báo cáo

### 💰 Tài chính — Thu ngân (9)
| Báo cáo | Mục đích | Thường dùng bởi |
|---|---|---|
| Doanh thu ngày | Thực thu theo ngày, gom theo người thu/quầy | Kế toán |
| Doanh thu theo tháng | Tổng hợp doanh thu theo tháng | Kế toán, Quản lý |
| Hoàn trả phiếu thu | Danh sách phiếu đã hoàn | Kế toán |
| Hủy phiếu thu | Danh sách phiếu đã hủy + lý do | Kế toán |
| Tạm ứng | Theo dõi khoản tạm ứng của bệnh nhân | Kế toán |
| Chi tiết viện phí | Bóc tách viện phí theo dịch vụ | Kế toán |
| Tổng hợp xét nghiệm | Doanh thu XN theo loại | Kế toán |
| Công nợ bệnh nhân | Phiếu còn nợ, số ngày quá hạn | Kế toán |
| Sổ quỹ tiền mặt | Thu – chi – tồn quỹ tiền mặt | Kế toán/Thủ quỹ |

### 🩺 Khám bệnh — Sổ (14)
| Báo cáo | Mục đích |
|---|---|
| CTDV BN: Khám bệnh / Siêu âm / X-Quang / Nội soi / Thủ thuật / Xét nghiệm | Chi tiết dịch vụ bệnh nhân theo từng loại |
| Sổ: Khám bệnh / Siêu âm / X-Quang / Nội soi / Thủ thuật / Xét nghiệm / Điện tim | Sổ liệt kê phục vụ lưu trữ/đối chiếu |
| Bệnh diễn tiến | Theo dõi diễn tiến bệnh mạn (HbA1c, Glucose, HA, BMI…) |

### 📊 Thống kê (11)
| Báo cáo | Mục đích |
|---|---|
| Lượt khám theo bác sĩ | Năng suất từng bác sĩ |
| Lượt khám theo phòng khám | Năng suất theo phòng |
| Lượt khám theo giờ | Khung giờ cao điểm (xếp nhân sự) |
| Thống kê ICD-10 | Mô hình bệnh tật (báo cáo y tế định kỳ) |
| Chỉ định CLS | Số lượt chỉ định XN/CĐHA |
| Top thuốc kê nhiều | Xếp hạng thuốc theo số lần kê |
| Top dịch vụ | Xếp hạng dịch vụ theo doanh thu |
| Tổng hợp nguồn khách | Phân bổ bệnh nhân theo nguồn |
| Tỷ lệ no-show lịch hẹn | Tỷ lệ bệnh nhân đặt nhưng không đến |
| Sử dụng kháng sinh | Thống kê kê đơn kháng sinh |
| Thời gian trả kết quả CLS (TAT) | Tốc độ trả kết quả xét nghiệm |

### 📦 Kho dược (8)
| Báo cáo | Mục đích |
|---|---|
| Tồn kho hiện tại | Số lượng & giá trị tồn theo thuốc |
| Thẻ kho theo lô | Tồn chi tiết theo lô + hạn dùng |
| Thuốc cận date / hết hạn | Cảnh báo HSD ≤ 90 ngày |
| Xuất – Nhập – Tồn | Biến động kho theo kỳ |
| Danh mục thuốc | Danh mục + hoạt chất, giá, phân loại |
| Thuốc kiểm soát đặc biệt | Gây nghiện/hướng thần (compliance) |
| Thuốc dưới định mức | Thuốc cần đặt thêm |
| Kiểm kê kho | Đối chiếu tồn hệ thống vs thực tế |

### 🏥 BHYT / BHXH (1)
| Báo cáo | Mục đích |
|---|---|
| Nghỉ hưởng BHXH | Danh sách giấy chứng nhận nghỉ việc hưởng BHXH |

---

## 8. Mẹo & lưu ý

- **Khoảng ngày tối đa 366 ngày**/lần truy vấn — kỳ dài hơn hãy tách nhiều lần.
- **Dữ liệu rỗng không phải lỗi:** nếu trong kỳ chưa phát sinh nghiệp vụ (ví dụ chưa có phiếu thu, chưa có biến động kho) báo cáo sẽ hiển thị 0/không có dòng.
- **Số liệu theo phòng khám của bạn:** hệ thống tự lọc theo phòng khám đang đăng nhập, không lẫn dữ liệu phòng khám khác.
- **Xuất Excel để phân tích tiếp:** file giữ đúng kiểu số/ngày nên có thể dùng hàm SUM, PivotTable trong Excel.
- **Menu báo cáo phụ thuộc quyền:** mỗi vai trò chỉ thấy/tải được báo cáo trong phạm vi quyền của mình.
- **Letterhead trên bản in** do Quản trị cấu hình (tên/công ty/slogan/website/logo). Nếu đầu trang thiếu thông tin, báo Quản trị cập nhật hồ sơ phòng khám.

---

## 9. Câu hỏi thường gặp

**Không thấy menu Báo cáo?** → Chưa được cấp quyền `report.read`, liên hệ Quản trị.

**Bấm Lấy dữ liệu nhưng trống?** → Kiểm tra khoảng ngày và bộ lọc; thử mở rộng khoảng thời gian.

**Nút In/Xuất mờ (không bấm được)?** → Cần Lấy dữ liệu ra lưới trước.

**Bản in thiếu tên/logo phòng khám?** → Quản trị cập nhật hồ sơ phòng khám (Tenant).

**Số tiền trên báo cáo lệch với thực tế?** → Kiểm tra đúng khoảng ngày, đúng bộ lọc (người thu/quầy thu), và các phiếu hoàn/hủy trong kỳ.
