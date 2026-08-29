# Luồng khám bệnh đầy đủ — Tiếp đón đến Tái khám

> Tài liệu này mô tả **toàn bộ hành trình của một ca khám bệnh**, đi qua nhiều vai trò (Lễ tân → Bác sĩ → Kỹ thuật viên/Điều dưỡng → Thu ngân → Dược sĩ), theo đúng trình tự thời gian thực tế diễn ra tại phòng khám. Nếu bạn cần hướng dẫn chi tiết thao tác của riêng một vai trò, xem thêm các tài liệu trong [README.md](README.md) — tài liệu này chỉ tập trung vào **cách các bước nối tiếp nhau**.

## 1. Tổng quan luồng

Một ca khám bệnh tại Pro-Diab HIS đi qua các bước sau:

1. **Tiếp đón** (Lễ tân) — tìm/tạo hồ sơ bệnh nhân, tạo lượt khám mới
2. **Khám bệnh** (Bác sĩ) — bắt đầu khám, ghi bệnh án, chẩn đoán ICD-10
3. **Chỉ định CLS** (Bác sĩ) — nếu cần xét nghiệm/chẩn đoán hình ảnh
4. **Thực hiện & trả kết quả CLS** (Kỹ thuật viên/Điều dưỡng)
5. **Kê đơn thuốc** (Bác sĩ) — kê thuốc, ký số, gửi Đơn thuốc Quốc gia
6. **Thu ngân** (Kế toán/Thu ngân) — thu tiền khám, CLS, thuốc (đủ hoặc một phần)
7. **Cấp phát thuốc** (Dược sĩ) — xuất thuốc từ kho, giao cho bệnh nhân
8. **Kết thúc khám / Xuất viện** (Bác sĩ) — chốt hồ sơ khám
9. **Đặt lịch tái khám** (Bác sĩ/Lễ tân) — nhắc bệnh nhân qua SMS/Zalo

Toàn bộ các bước 2 → 9 diễn ra trên **cùng một màn hình "Chi tiết lượt khám"**, chuyển đổi qua các tab: *Bệnh án – Tiến sử – Cận lâm sàng – Kết quả CLS – Chẩn đoán – Đơn thuốc – Tái khám*.

### Bảng vai trò tham gia

| Vai trò | Tham gia bước | Quyền cần có |
|---|---|---|
| Lễ tân (LeTan) | 1. Tiếp đón, tạo hồ sơ, tạo lượt khám | `patient.write`, `encounter.create`, `reception.checkin` |
| Bác sĩ (BacSi) | 2, 3, 5, 8, 9. Khám, chẩn đoán, chỉ định CLS, kê đơn, kết thúc khám, đặt tái khám | `encounter.*`, `prescription.write` |
| Kỹ thuật viên (KTV) / Điều dưỡng | 4. Nhập kết quả CLS | `cls.result.write` |
| Thu ngân/Kế toán (KeToan) | 6. Thu tiền, quản lý công nợ | `billing.*` |
| Dược sĩ (DuocSi) | 7. Cấp phát thuốc từ kho | `pharmacy.dispense` |

### Điều kiện tiên quyết

- Đã có tài khoản test theo từng vai trò (xem `docs/user-guide/00-getting-started.md`) hoặc dùng bảng "Đăng nhập nhanh theo vai trò" ở màn hình đăng nhập (chỉ có ở môi trường DEV).
- Phòng khám đã cấu hình sẵn danh mục dịch vụ CLS, danh mục thuốc, danh mục ICD-10.
- Nếu muốn thao tác với bệnh nhân có BHYT hoặc gói dịch vụ, cần dữ liệu mẫu tương ứng đã được tạo sẵn.

---

## 2. Chi tiết từng bước

### Bước 1 — Tiếp đón, tạo hồ sơ bệnh nhân (Lễ tân)

Lễ tân là người đầu tiên tiếp xúc bệnh nhân. Vào menu **Bệnh nhân**, hệ thống hiển thị danh sách hồ sơ đã có kèm ô tìm kiếm theo tên/SĐT/CMND/BHYT.

- **Bệnh nhân đã có hồ sơ**: gõ tên hoặc số điện thoại vào ô tìm kiếm để tra cứu, không tạo hồ sơ mới.
- **Bệnh nhân mới**: nhấn **"+ Tạo bệnh nhân mới"**, điền các trường Họ và tên (bắt buộc), Ngày sinh, Số điện thoại, CMND/CCCD, và mục **"Đối tượng"** — đây là nơi phân nhánh nghiệp vụ:

| Đối tượng | Ý nghĩa |
|---|---|
| Dịch vụ | Bệnh nhân tự trả toàn bộ chi phí |
| Bảo hiểm y tế | Áp dụng đồng chi trả BHYT (module BHYT đang được bật cho phòng khám này) |
| Miễn phí | Không thu phí |
| Hợp đồng | Thanh toán theo hợp đồng với đơn vị/công ty |

> ⚠️ **Chống trùng hồ sơ theo CCCD**: khi lưu bệnh nhân mới, hệ thống tự động so khớp CCCD (và tổ hợp SĐT + họ tên + ngày sinh) với các hồ sơ đã có. Nếu phát hiện trùng, hệ thống **tạm dừng và hiển thị danh sách hồ sơ nghi trùng** để lễ tân xác nhận — đây là cảnh báo mềm, lễ tân vẫn có thể chọn "vẫn tạo mới" nếu xác nhận là hai người khác nhau (ví dụ CMND cũ bị cấp trùng). Không có bước này bị bỏ qua âm thầm.

Sau khi lưu, hồ sơ được cấp mã bệnh nhân dạng `BNTxxxxxxx`. Số CMND/CCCD hiển thị ở dạng che một phần (ví dụ `07********34`) để bảo vệ dữ liệu nhạy cảm.

**Tạo lượt khám (tiếp đón vào phòng khám):** vào menu **Khám bệnh → "+ Tạo lượt khám"**, tìm bệnh nhân, chọn bác sĩ phụ trách (có thể để trống, phân sau), chọn **Loại khám** (Khám mới / Tái khám / Cấp cứu…) và nhập **Lý do khám**. Nhấn "Tạo lượt khám" để đưa bệnh nhân vào hàng chờ khám với trạng thái "Chờ khám".

> ⚠️ Ô tìm bệnh nhân trong màn "Tạo lượt khám mới" cần dữ liệu bệnh nhân đã được lập chỉ mục tìm kiếm — bệnh nhân **vừa tạo xong trong vài giây** có thể chưa xuất hiện ngay trong kết quả tìm kiếm (độ trễ lập chỉ mục). Nếu không thấy tên bệnh nhân vừa tạo, đợi vài chục giây rồi thử lại.

### Bước 2 — Bác sĩ bắt đầu khám, ghi bệnh án

Bác sĩ đăng nhập, vào **Khám bệnh**, chọn bệnh nhân đang "Chờ khám" từ danh sách, nhấn **"Bắt đầu khám"** ở góc trên bên phải. Trạng thái lượt khám chuyển sang "Đang khám".

Tab **Bệnh án** là trình soạn thảo văn bản đa dạng thức (in đậm/nghiêng, tiêu đề, danh sách, bảng) để bác sĩ ghi lại diễn biến khám: than phiền của bệnh nhân, khám thực thể, nhận định ban đầu. Nội dung có nút "Lưu nháp" riêng.

- **Khám mới vs Tái khám**: được xác định ngay từ lúc tạo lượt khám (mục "Loại khám" ở Bước 1). Nếu là tái khám, tab **Tiến sử** sẽ hiển thị lại các lần khám trước để bác sĩ tham khảo.

### Bước 3 — Chẩn đoán ICD-10

Chuyển sang tab **Chẩn đoán**. Gõ tên bệnh hoặc mã ICD-10 (ví dụ `E11` cho đái tháo đường típ 2) vào ô tìm kiếm, chọn kết quả phù hợp. Có thể gắn mỗi chẩn đoán là **Chính** hoặc **Phụ** bằng hai nút chuyển đổi phía trên ô tìm kiếm. Ngoài ra có bảng "Nhập nhanh nhiều chẩn đoán" cho phép gõ trực tiếp mã + tên bệnh khi cần thêm hàng loạt. Chẩn đoán đã thêm hiển thị trong "Danh sách chẩn đoán" bên dưới, có thể xoá từng dòng.

### Bước 4 — Chỉ định cận lâm sàng (CLS): xét nghiệm / chẩn đoán hình ảnh

Nếu cần cho bệnh nhân làm xét nghiệm hoặc chụp chiếu, chuyển sang tab **Cận lâm sàng**, nhấn **"Tạo đợt chỉ định mới"**. Trong hộp thoại, gõ tên dịch vụ (ví dụ "đường huyết") để tìm, nhấn dấu **+** ở từng dịch vụ để thêm vào cột "Dịch vụ đã chọn" bên phải — hệ thống tự cộng dồn **Tổng tiền**. Có thể ghi chú thêm cho đợt chỉ định rồi nhấn **"Lưu đợt chỉ định"**.

Sau khi lưu, đợt chỉ định hiển thị dạng bảng với trạng thái từng dịch vụ (ví dụ "Chờ thực hiện") và trạng thái thanh toán của cả đợt ("Chưa thanh toán" / đã thu). Bác sĩ có thể **"Chốt đợt"** để khoá không cho sửa thêm, hoặc **"In phiếu"** để đưa bệnh nhân cầm sang phòng xét nghiệm.

> ⚠️ **Bệnh nhân có gói dịch vụ (package subscription)**: nếu bệnh nhân đang có gói dịch vụ còn hiệu lực bao gồm dịch vụ CLS được chỉ định, hệ thống **tự động trừ định mức ngay tại thời điểm tạo đợt chỉ định** (không phải đợi đến lúc thanh toán) — cơ chế khoá bản ghi định mức theo thứ tự hết hạn gần nhất trước (FIFO), ghi log sử dụng, và tự chuyển gói sang trạng thái "đã dùng hết" khi cạn định mức. Nếu huỷ chỉ định, định mức được hoàn lại — trừ trường hợp hoá đơn liên quan đã thanh toán hoặc thuốc đã cấp phát thì không hoàn được nữa.

### Bước 5 — Kỹ thuật viên/Điều dưỡng thực hiện & trả kết quả CLS

Kỹ thuật viên đăng nhập, vào **Khám bệnh**, mở đúng lượt khám, chuyển đến tab **Cận lâm sàng** để xem danh sách dịch vụ cần thực hiện, sau đó nhập kết quả. Kết quả đã trả về sẽ hiển thị lại ở tab **Kết quả CLS** cho bác sĩ tham khảo khi ra quyết định chẩn đoán/kê đơn.

> ⚠️ Trong quá trình xác minh, thao tác nhập kết quả trực tiếp trên dòng dịch vụ ở trạng thái **"Chưa thanh toán"** không mở được form nhập — gợi ý rằng quy trình đúng là **thu ngân thu tiền đợt chỉ định trước**, sau đó kỹ thuật viên mới nhập kết quả. Ghi nhận vào mục Ghi chú kỹ thuật bên dưới để dev xác nhận lại có đúng là chủ đích thiết kế hay là lỗi.

### Bước 6 — Kê đơn thuốc

Bác sĩ chuyển sang tab **Đơn thuốc**. Gõ tên thuốc vào ô tìm kiếm (ví dụ "Metformin"), chọn thuốc từ danh sách gợi ý — hệ thống tự tạo một "Đơn thuốc nháp". Điền **Liều dùng**, **Tần suất**, **Đường dùng**, **Số ngày** cho thuốc rồi nhấn **"Thêm vào đơn"**; lặp lại để thêm nhiều thuốc. Mỗi dòng thuốc trong đơn hiển thị đầy đủ liều – tần suất – đường dùng – số ngày – số lượng quy đổi.

Khi đơn có từ hai thuốc trở lên, hệ thống kiểm tra **tương tác thuốc (DDI — Drug-Drug Interaction)** theo danh mục đã khai báo; nếu cặp thuốc có tương tác, cảnh báo sẽ hiển thị ngay trong màn kê đơn để bác sĩ cân nhắc trước khi ký đơn.

> ⚠️ Trong lần kiểm chứng, cặp thuốc thử (Metformin + Insulin glargine) không có tương tác được khai báo nên không phát sinh cảnh báo — đây là hành vi đúng, không phải lỗi. Cảnh báo DDI chỉ xuất hiện với các cặp thuốc đã có trong danh mục tương tác của hệ thống.

Có hai nút hành động cho đơn thuốc:
- **"Lưu đơn"** — chỉ lưu nháp, đơn **chưa** được đẩy sang màn "Kê đơn" của dược sĩ.
- **"Ký số & gửi ĐTQG"** — bác sĩ ký số điện tử cho đơn thuốc, đồng thời đơn được đẩy lên hệ thống **Đơn thuốc Quốc gia** (theo TT 27/2021/TT-BYT) để lấy mã đơn thuốc và mã QR. Chỉ sau bước này đơn thuốc mới xuất hiện trong danh sách chờ cấp phát của Dược sĩ.

> ⚠️ **Bệnh nhân có gói dịch vụ**: tương tự CLS, nếu thuốc kê nằm trong gói dịch vụ bệnh nhân đang có, định mức bị trừ ngay khi thuốc được thêm/xác nhận vào đơn.

### Bước 7 — Thu ngân thu tiền (đủ hoặc một phần)

Kế toán/Thu ngân đăng nhập, vào **Hoá đơn**, tìm hoá đơn theo tên bệnh nhân hoặc số hoá đơn. Mỗi hoá đơn hiển thị **Phải trả / Đã thu / Còn lại** và trạng thái (Nháp, Đã xác nhận, Thanh toán một phần, Đã thanh toán…). Mở hoá đơn, nhấn **"Thu tiền"** để mở khay thanh toán bên phải:

1. Chọn phương thức: Tiền mặt / Chuyển khoản / Visa / Mastercard / VietQR / MoMo / VNPay (có phím tắt số 1–7 tương ứng).
2. Nhập **Số tiền** muốn thu — có thể **nhỏ hơn** số tiền còn phải trả để thu một phần.
3. Với Tiền mặt, có thể nhập thêm "Khách đưa" để hệ thống tự tính tiền thừa.
4. Nhấn **"Xác nhận thu tiền (F4)"**.

- **Thu đủ**: trạng thái hoá đơn chuyển "Đã thanh toán", Còn lại = 0đ.
- **Thu một phần**: trạng thái chuyển **"Thanh toán một phần"**, phần "Còn lại" vẫn hiển thị màu đỏ — đây chính là **công nợ** của bệnh nhân, có thể thu tiếp ở lần đến sau bằng cách mở lại đúng hoá đơn và lặp lại thao tác "Thu tiền".

Có thể **"Phát hành HĐĐT"** (hoá đơn điện tử) ngay tại màn chi tiết hoá đơn khi cần xuất hoá đơn cho bệnh nhân/doanh nghiệp.

> ⚠️ Phím số 1–7 trên bàn phím là **phím tắt chọn phương thức thanh toán** trong khay "Thu tiền" — nếu đang gõ số tiền mà vô tình để con trỏ rời khỏi ô nhập, các phím số sẽ đổi phương thức thanh toán thay vì nhập vào ô số tiền. Luôn bấm chuột vào đúng ô "Số tiền (VND)" trước khi gõ số.

### Bước 8 — Dược sĩ cấp phát thuốc

Dược sĩ đăng nhập, vào menu **Kê đơn**, danh sách hiển thị các đơn thuốc **đã được bác sĩ ký số** (đơn còn ở trạng thái nháp chưa ký sẽ không xuất hiện ở đây). Mở đúng đơn của bệnh nhân, đối chiếu thuốc — số lượng — lô/hạn sử dụng trong kho, thực hiện xuất kho và đánh dấu đã cấp phát cho bệnh nhân. Chi tiết thao tác kho (nhập/xuất/tồn/lô/HSD/kiểm kê) xem thêm [04-pharmacy.md](04-pharmacy.md).

> ⚠️ Sau khi thuốc đã được đánh dấu **"Đã cấp phát" (DISPENSED)**, định mức gói dịch vụ đã trừ cho thuốc đó **không thể hoàn lại** được nữa dù đơn có bị huỷ sau đó — cần cân nhắc kỹ trước khi xác nhận cấp phát.

### Bước 9 — Kết thúc khám / Xuất viện

Sau khi đã hoàn tất bệnh án, chẩn đoán, kê đơn, bác sĩ quay lại lượt khám, nhấn **"Kết thúc khám"** ở góc trên bên phải màn hình chi tiết lượt khám. Lượt khám chuyển sang trạng thái hoàn thành, không thể chỉnh sửa thêm nội dung khám (trừ khi có quyền mở lại hồ sơ). Đây được xem là bước "xuất viện" đối với khám ngoại trú thông thường tại phòng khám đa khoa quy mô nhỏ (không có nội trú).

Nút **"Ký số bệnh án"** cho phép bác sĩ ký số điện tử xác nhận nội dung bệnh án, đảm bảo tính pháp lý của hồ sơ.

### Bước 10 — Đặt lịch tái khám

Vẫn trong màn chi tiết lượt khám, chuyển sang tab **Tái khám**. Chọn **Thời gian tái khám** (ngày giờ cụ thể) và có thể nhập **Dặn dò bệnh nhân** (ví dụ: nhịn ăn trước khi xét nghiệm, mang theo sổ khám cũ). Nhấn **"Đặt lịch tái khám"**.

Lịch hẹn mới tạo hiển thị ngay trong khối "Lịch hẹn của bệnh nhân" bên dưới với trạng thái **"Chờ xác nhận"**, kèm dòng chú thích "Đặt lịch tái khám để nhắc bệnh nhân qua SMS/Zalo" — nghĩa là hệ thống sẽ tự gửi tin nhắc trước ngày hẹn. Lễ tân cũng có thể quản lý tập trung tất cả lịch tái khám sắp tới qua menu **"Nhắc tái khám"** (`/recall`) để chủ động gọi điện xác nhận với bệnh nhân trước ngày hẹn.

---

## 3. Câu hỏi thường gặp

**Hỏi:** Bệnh nhân có BHYT thì luồng khám có gì khác?
**Đáp:** Khi tạo hồ sơ, chọn Đối tượng = "Bảo hiểm y tế" và khai báo thông tin thẻ BHYT ở tab "Bảo hiểm y tế" trên hồ sơ bệnh nhân. Các dịch vụ/thuốc có đánh dấu BHYT sẽ được tính đồng chi trả theo tỷ lệ khi lên hoá đơn (xem thêm phần BHYT trong hoá đơn ở Bước 7 — mục hàng có gắn nhãn "BHYT"). Đối soát và xuất XML giám định BHYT thực hiện ở menu riêng **BHYT**, không thuộc phạm vi tài liệu này.

**Hỏi:** Nếu bệnh nhân không đủ tiền thanh toán ngay thì sao?
**Đáp:** Thu ngân thu một phần theo Bước 7, hoá đơn giữ trạng thái "Thanh toán một phần" và phần còn lại được xem là công nợ, có thể thu tiếp ở lần khám sau hoặc bất kỳ lúc nào mở lại hoá đơn đó.

**Hỏi:** Làm sao biết đơn thuốc có bị cảnh báo tương tác thuốc không?
**Đáp:** Cảnh báo hiển thị ngay trong màn kê đơn (tab Đơn thuốc) khi thêm thuốc thứ hai trở lên nếu cặp thuốc đó có trong danh mục tương tác đã khai báo trong hệ thống.

## Liên hệ hỗ trợ

Email: support@prodiab.vn
Hotline: 1800-xxx-xxx (giờ hành chính)

---

## Ghi chú kỹ thuật (dành cho đội phát triển)

Các điểm sau được ghi nhận trong lúc thao tác thật trên môi trường local để soạn tài liệu này (2026-08-29/30), cần đội dev xác nhận lại — **không tự sửa code**:

1. **Độ trễ lập chỉ mục tìm kiếm bệnh nhân**: API `GET /api/v1/patients/search` trả về rỗng trong vài chục giây đầu sau khi tạo bệnh nhân mới, dù `GET /api/v1/patients?q=...` (danh sách có filter) trả về đúng ngay lập tức. Ảnh hưởng: ô chọn bệnh nhân trong màn "Tạo lượt khám mới" và "Tạo lịch hẹn" không tìm thấy bệnh nhân vừa tạo nếu thao tác quá nhanh.
2. **Ô tìm kiếm bệnh nhân (combobox) trong "Tạo lượt khám mới" không phản hồi khi gõ nhanh nhiều ký tự cùng lúc** (paste-like input) — chỉ hoạt động khi gõ từng phím một có độ trễ. Cần kiểm tra lại debounce/onChange handler của component autocomplete này.
3. **Lỗi tạm thời khi tạo đợt chỉ định CLS lần đầu**: lần đầu tiên bấm "Lưu đợt chỉ định" gặp lỗi "Tạo đợt chỉ định thất bại" kèm console log `ERR_CONNECTION_RESET` / `ERR_EMPTY_RESPONSE` (nghi backend restart hoặc timeout tức thời); thử lại ngay sau đó thành công bình thường. Cần theo dõi log backend quanh mốc thời gian này để xác định nguyên nhân gốc (có thể do hot-reload container trong môi trường dev, không chắc có xảy ra ở production).
4. **KTV không mở được form nhập kết quả CLS khi đợt chỉ định đang ở trạng thái "Chưa thanh toán"** — click vào dòng dịch vụ không có phản ứng. Cần dev xác nhận đây là chủ đích nghiệp vụ (bắt buộc thu tiền CLS trước khi xét nghiệm) hay là lỗi UI thiếu xử lý sự kiện click.
5. **Phím số 1–7 trong khay "Thu tiền" là phím tắt toàn cục chọn phương thức thanh toán**, kể cả khi người dùng đang gõ trong ô "Số tiền"/"Khách đưa" nhưng ô đó chưa thực sự có focus — dễ khiến người dùng gõ nhầm số tiền thành đổi phương thức thanh toán. Đề xuất chỉ kích hoạt phím tắt khi không có input nào đang focus.
6. **Ô "Số tiền (VND)" trong khay thu tiền có bước nhảy (step) 1.000đ** — nhập số tiền lẻ hàng trăm (ví dụ 2.200đ) bị trình duyệt chặn với thông báo "Please enter a valid value" (thông báo tiếng Anh, chưa được Việt hoá). Cần xem lại thuộc tính `step` của input hoặc đổi sang input dạng text có định dạng số.
7. **In phiếu thu** (`/api/v1/cashier/receipts/{id}/print`) mở tab mới nhưng hiển thị trang trắng — chưa xác định do PDF chưa kịp render hay lỗi template in phiếu.
8. Đơn thuốc ở trạng thái **"Nháp"** (chưa được bác sĩ "Ký số & gửi ĐTQG") **không xuất hiện** trong danh sách "Kê đơn" của Dược sĩ — hành vi này hợp lý về nghiệp vụ (tránh cấp phát nhầm đơn chưa chốt) nhưng cần xác nhận đây là chủ đích thiết kế đã được PO chấp thuận.
