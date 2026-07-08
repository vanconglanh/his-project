# 09 — Tự tạo báo cáo (Trình tạo báo cáo / Self-service)

> **Đối tượng:** Power user có quyền `report.build` (mặc định: Quản trị, Kế toán, Bác sĩ, Dược sĩ).
> Cho phép bạn **tự thiết kế báo cáo mới** (bảng hoặc biểu đồ) từ dữ liệu phòng khám — kéo-thả, KHÔNG cần lập trình, KHÔNG cần gõ công thức SQL.

---

## 1. Khái niệm cần nắm (30 giây)

- **Nguồn dữ liệu (Dataset):** khung dữ liệu có sẵn và an toàn. Hiện có 6: **Thu ngân, Lượt khám, Kho dược, Đơn thuốc, Công nợ, Chỉ định CLS**. Bạn chỉ chọn trong đây (không đụng cơ sở dữ liệu thô).
- **Chiều (Dimension):** cột để **nhóm/xem theo** — ví dụ *Ngày, Bác sĩ, Nhóm thuốc, Trạng thái*.
- **Số đo (Measure):** cột **con số tính toán được** — ví dụ *Số lượng, Doanh thu, Số lượt* (cộng/đếm/trung bình...).

> Quy tắc vàng: **1 báo cáo = chọn vài Chiều để nhìn + vài Số đo để đo.**

---

## 2. Mở Trình tạo báo cáo

Menu **Trình tạo báo cáo** (biểu tượng ✨) → mở màn `/reports/builder`.
Nếu không thấy menu → bạn chưa có quyền `report.build`, liên hệ Quản trị.

---

## 3. Tạo báo cáo theo 6 bước (có ví dụ)

> **Ví dụ xuyên suốt:** *"Tồn kho theo thuốc"* — xem mỗi thuốc còn tồn bao nhiêu và giá trị bao nhiêu.

### Bước 1 — Chọn nguồn dữ liệu
Bấm thẻ **Kho dược**. Bên dưới hiện 2 khay: **Chiều** (Tên thuốc, Lô, HSD, Nhóm...) và **Số đo** (SL tồn, Giá trị tồn).

### Bước 2 — Chọn cột (kéo-thả)
Kéo (hoặc bấm) đưa vào vùng "Cột đã chọn":
1. **Tên thuốc** (Chiều)
2. **SL tồn** (Số đo) → chọn phép gộp **Tổng (SUM)** → tick **"dòng tổng"** nếu muốn có dòng TỔNG CỘNG.
3. **Giá trị tồn** (Số đo) → **Tổng (SUM)**.

### Bước 3 — (Tùy chọn) Cột tính toán
Muốn thêm cột *"Đơn giá trung bình"* = Giá trị tồn ÷ Số lượng:
- Mở khu **"Cột tính toán"** → nhập:
  - Tên: `Đơn giá TB`
  - Công thức: `stockValue / quantity`  *(chỉ dùng tên số đo + phép `+ − × ÷ ( )`)*
- Thêm cột này vào báo cáo như một số đo.

> Công thức chỉ chấp nhận **số đo của dataset + phép tính số học**. Gõ hàm lạ / ký tự lạ sẽ bị từ chối (an toàn).

### Bước 4 — Lọc / Nhóm / Sắp xếp
- **Bộ lọc:** ví dụ *Nhóm thuốc = Tim mạch* (chọn cột → toán tử → giá trị).
- **Sắp xếp:** *SL tồn* giảm dần.
- **Khoảng ngày** (bắt buộc, tối đa 366 ngày): mặc định tháng này — dùng để giới hạn dữ liệu.

### Bước 5 — Chọn hiển thị: Bảng hay Biểu đồ
- **Bảng:** giữ mặc định.
- **Biểu đồ:** chuyển tab **Biểu đồ** → chọn kiểu (Cột / Đường / Vùng / Tròn) → chọn **Chiều** (trục) + **Số đo** (giá trị). Ví dụ: cột, chiều = Tên thuốc, số đo = SL tồn.

### Bước 6 — Xem trước & Lưu
- Bấm **Xem trước** → kiểm số liệu (có KPI + bảng/biểu đồ).
- Bấm **Lưu** → đặt **Tên báo cáo** + chọn **Phạm vi** (xem §4) → Lưu.
- Báo cáo mới xuất hiện ngay trong menu **Báo cáo** → nhóm **"Báo cáo của phòng khám"**, chạy/lọc/**in PDF/xuất Excel** y hệt báo cáo hệ thống.

---

## 4. Phạm vi chia sẻ (ai được xem)

Khi lưu, chọn:
- **Riêng tôi** — chỉ mình bạn thấy.
- **Cả phòng khám** — mọi người có quyền xem báo cáo đều thấy.
- **Theo vai trò** — chỉ các vai trò được chọn (vd chỉ *Bác sĩ*, *Kế toán*).

> Người được chia sẻ cần **đăng nhập lại** nếu vừa được cấp vai trò mới.

---

## 5. Sửa / Xoá báo cáo đã tạo

Trong màn **Báo cáo**, mở báo cáo tự tạo → nút **Sửa** (mở lại builder) / **Xoá**. Chỉ **người tạo hoặc Quản trị** mới sửa/xoá được.

---

## 6. Ghép Bảng điều khiển (Dashboard)

Menu **Bảng điều khiển** → **Tạo**:
1. Đặt tên dashboard.
2. Thêm **widget**: chọn báo cáo đã lưu + kiểu hiển thị (Bảng / Biểu đồ / KPI) + kích thước ô.
3. Lưu → mở dashboard để xem tất cả widget cùng lúc, có **bộ lọc ngày chung**.

> Tối đa 12 widget / dashboard.

---

## 7. Đặt lịch gửi email tự động

Menu **Lịch báo cáo** → **Tạo**:
- Chọn **báo cáo**, **tần suất** (Hàng ngày / Tuần / Tháng) + giờ (+ thứ/ngày), **kỳ dữ liệu** (Hôm nay / Tuần này / Tháng này / Tháng trước), **định dạng** (PDF / Excel), **danh sách email nhận**, Bật.
- Hệ thống tự xuất báo cáo đúng lịch và gửi email đính kèm.

> Cần Quản trị/DevOps đã cấu hình máy chủ gửi mail (SMTP) — xem `docs/runbook/setup-report.md`.

---

## 8. Mẹo & giới hạn

- **Bắt đầu đơn giản:** 1–2 chiều + 1–2 số đo, xem trước, rồi thêm dần.
- **Số đo phải chọn phép gộp** (Tổng/Đếm/Trung bình...) — nếu không sẽ báo lỗi.
- **Dữ liệu rỗng** = trong kỳ chưa phát sinh nghiệp vụ (không phải lỗi) — thử mở rộng khoảng ngày.
- **Giới hạn an toàn:** ≤20 cột, ≤15 bộ lọc, ≤3 cấp nhóm, khoảng ngày ≤366 ngày.
- Muốn thêm **nguồn dữ liệu mới** (ngoài 6 cái) → đây là việc của đội kỹ thuật (thêm Dataset trong code), báo Quản trị.

---

## 9. Câu hỏi thường gặp

**Tôi không thấy menu Trình tạo báo cáo?** → Chưa có quyền `report.build` — báo Quản trị.

**Công thức cột tính toán báo lỗi đỏ?** → Chỉ dùng tên số đo + `+ − × ÷ ( )`, không dùng hàm/chữ khác.

**Lưu xong không thấy báo cáo?** → Kiểm ở menu Báo cáo, nhóm "Báo cáo của phòng khám"; nếu chọn phạm vi "Riêng tôi" thì chỉ bạn thấy.

**Biểu đồ trống?** → Đảm bảo đã chọn đúng Chiều (trục) + Số đo (giá trị) và khoảng ngày có dữ liệu.

**Sửa báo cáo người khác tạo?** → Chỉ người tạo hoặc Quản trị mới sửa được.
