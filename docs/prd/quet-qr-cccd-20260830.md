═══════════════════════════════════════════════════
PRODUCT REQUIREMENTS DOCUMENT (PRD)
Dự án: Quét mã QR CCCD để tự điền form bệnh nhân + kiểm tra trùng
Phiên bản: 1.0
Ngày tạo: 2026-08-30
PO: ATDS-Lanh
Trạng thái: Draft
Module: Patient / Reception — Pro-Diab HIS
═══════════════════════════════════════════════════

---

## 1. TỔNG QUAN (Overview)

Tính năng cho phép lễ tân sử dụng máy quét mã vạch USB kiểu keyboard-wedge để quét
mã QR trên thẻ Căn cước công dân (CCCD) gắn chip. Hệ thống tự parse chuỗi QR, điền
sẵn thông tin vào form tạo/tra cứu bệnh nhân, đồng thời kiểm tra trùng lặp theo số
CCCD và hiển thị cảnh báo phù hợp. Đây là MVP ưu tiên, không yêu cầu camera hay OCR.

---

## 2. VẤN ĐỀ CẦN GIẢI QUYẾT (Problem Statement)

**Vấn đề hiện tại:**
Lễ tân phải nhập tay thông tin bệnh nhân (họ tên, ngày sinh, địa chỉ, số CCCD) khi
tiếp đón. Quá trình này chậm, dễ sai sót chính tả và gây nhầm lẫn bệnh nhân trùng
tên, dẫn đến hồ sơ trùng hoặc thông tin không khớp với giấy tờ thực tế.

**Người bị ảnh hưởng:**
- Lễ tân: mất 2-4 phút nhập liệu mỗi bệnh nhân mới, áp lực giờ cao điểm.
- Bác sĩ: dữ liệu bệnh nhân không chính xác ảnh hưởng chẩn đoán và kê đơn.
- Bệnh nhân: chờ đợi lâu hơn, thông tin cá nhân có thể bị ghi sai.

**Hệ quả nếu không giải quyết:**
- Dữ liệu bệnh nhân bị phân mảnh (nhiều hồ sơ cho 1 người).
- Sai thông tin BHYT/CCCD dẫn đến không quyết toán được với bảo hiểm.
- UX kém, giảm tính cạnh tranh của phần mềm so với các HIS khác.

---

## 3. MỤC TIÊU & TIÊU CHÍ THÀNH CÔNG (Goals & Success Metrics)

**Mục tiêu kinh doanh:**
- [ ] Rút ngắn thời gian tiếp đón bệnh nhân mới từ ~3 phút xuống dưới 45 giây.
- [ ] Giảm tỷ lệ hồ sơ trùng lặp do nhập tay sai số CCCD.
- [ ] Tăng độ chính xác dữ liệu nhân khẩu học so với giấy tờ gốc.

**KPI / Metrics:**
| Metric                          | Baseline   | Target         | Thời hạn     |
|---------------------------------|------------|----------------|--------------|
| Thời gian tạo hồ sơ BN mới     | ~3 phút    | < 45 giây      | Sau 1 tháng  |
| Tỷ lệ hồ sơ trùng CCCD         | Chưa đo    | Giảm ≥ 80%     | Sau 1 tháng  |
| Tỷ lệ parse QR thành công      | 0%         | ≥ 95%          | Ngay khi UAT |
| Lỗi nhập tay sai số CCCD        | Chưa đo    | Giảm ≥ 90%     | Sau 1 tháng  |

---

## 4. NGƯỜI DÙNG (User Personas)

### Persona 1: Lễ tân phòng khám (Actor chính)
- **Vai trò:** Nhận bệnh nhân, tạo hồ sơ, xếp lịch khám.
- **Mục tiêu:** Tiếp đón nhanh, không sai sót, không phải nhập tay nhiều.
- **Pain point:** Giờ cao điểm hàng chục bệnh nhân chờ, nhập tay chậm và hay sai tên/ngày sinh tiếng Việt có dấu.
- **Thiết bị:** PC để bàn, có gắn máy quét mã vạch USB keyboard-wedge.

### Persona 2: Quản trị viên phòng khám (Admin)
- **Vai trò:** Giám sát dữ liệu, xử lý hồ sơ trùng, cấu hình hệ thống.
- **Mục tiêu:** Dữ liệu bệnh nhân sạch, có audit trail đầy đủ.
- **Pain point:** Không biết ai đã cập nhật thông tin, không truy vết được thay đổi.

---

## 5. YÊU CẦU TÍNH NĂNG (Feature Requirements)

### 5.1 Quét và parse mã QR CCCD

**Mô tả:**
Khi lễ tân đặt con trỏ vào ô "Quét CCCD" và quét thẻ CCCD bằng máy quét USB
keyboard-wedge, máy quét tự "gõ" chuỗi ký tự vào ô đó rồi gửi phím Enter. Hệ thống
nhận chuỗi, parse theo định dạng chuẩn 7 field, và tự điền vào các ô tương ứng trên
form bệnh nhân.

**Định dạng chuỗi QR CCCD (chuẩn từ 2021):**
```
soCCCD|soCMNDCu|hoTen|ngaySinh(ddMMyyyy)|gioiTinh|diaChiThuongTru|ngayCap(ddMMyyyy)
```

Ví dụ thực tế:
```
001099012345|001234567890|Nguyễn Văn A|15031999|Nam|Số 1 Đường ABC, Phường X, Quận Y, TP Z|10052021
```

**Mapping field sang form:**
| Field QR        | Vị trí trong form       | Ghi chú                             |
|-----------------|-------------------------|-------------------------------------|
| soCCCD          | Số CCCD                 | Bắt buộc, dùng để check trùng       |
| soCMNDCu        | Số CMND cũ              | Lưu tham chiếu, không bắt buộc      |
| hoTen           | Họ và tên               | Giữ nguyên Unicode có dấu           |
| ngaySinh        | Ngày sinh               | Parse ddMMyyyy → dd/MM/yyyy         |
| gioiTinh        | Giới tính               | Map "Nam"/"Nữ" sang enum hệ thống   |
| diaChiThuongTru | Địa chỉ thường trú      | Điền vào ô địa chỉ                  |
| ngayCap         | Ngày cấp CCCD           | Lưu tham chiếu, không bắt buộc      |

**Business Rules — Parse QR:**

- **BR-QR-001:** Chuỗi QR phải có đúng 7 field phân cách bằng dấu `|`. Nếu số field
  khác 7, hệ thống hiển thị thông báo lỗi rõ ràng (xem BR-QR-004) và KHÔNG điền
  bất kỳ field nào vào form.

- **BR-QR-002:** Từng field được xử lý độc lập (graceful degradation). Nếu một field
  có giá trị rỗng hoặc không parse được (ví dụ ngày sai định dạng), field đó để
  trống trong form; các field hợp lệ còn lại vẫn được điền bình thường. Hệ thống
  KHÔNG throw exception vỡ luồng.

- **BR-QR-003:** Ngày sinh và ngày cấp phải theo định dạng `ddMMyyyy` (8 chữ số).
  Nếu không thỏa mãn (ví dụ: `32131999`, `abc`, chuỗi rỗng), field ngày tương ứng
  được để trống và ghi log cảnh báo.

- **BR-QR-004:** Log lỗi parse (server-side Serilog, mức WARN) phải ghi rõ:
  loại lỗi, vị trí field lỗi, và 20 ký tự đầu chuỗi QR (không log toàn bộ chuỗi
  tránh lộ thông tin cá nhân). Log KHÔNG dùng tiếng Việt có dấu để tránh lỗi
  encoding (theo quy ước CLAUDE.md).

- **BR-QR-005:** Trường `hoTen` và `diaChiThuongTru` có thể chứa ký tự Unicode
  tiếng Việt. Parser phải đọc chuỗi với encoding UTF-8. Nếu phát hiện ký tự thay
  thế (replacement character U+FFFD) do encoding cũ, trường đó vẫn được điền (không
  vỡ) nhưng UI hiển thị icon cảnh báo "Có thể có lỗi encoding — vui lòng kiểm tra
  lại họ tên / địa chỉ".

- **BR-QR-006:** Toàn bộ luồng parse xảy ra phía **client (browser)**, không gọi
  API để parse. Chỉ gọi API khi bước check trùng CCCD.

**Priority:** Must Have

**User Stories liên quan:** US-QR-001, US-QR-002

---

### 5.2 Kiểm tra trùng lặp theo số CCCD

**Mô tả:**
Sau khi parse xong, hệ thống gọi API check trùng dựa trên `soCCCD` (sử dụng
blind-index đã có sẵn). Có 3 case xử lý tùy kết quả trả về.

**Business Rules — Check trùng:**

- **BR-DUP-001:** Check trùng được thực hiện bằng cách tìm kiếm `soCCCD` qua
  API hiện có (blind-index), không so sánh plaintext CCCD trực tiếp.

- **BR-DUP-002 (Case 1 — Chưa tồn tại):** Nếu không tìm thấy bệnh nhân nào có
  cùng số CCCD, điền form bình thường và cho phép lưu tạo mới. Không hiển thị
  cảnh báo nào.

- **BR-DUP-003 (Case 2 — Tồn tại, khớp hoàn toàn):** Nếu tìm thấy bệnh nhân
  có cùng CCCD VÀ toàn bộ trường so sánh (họ tên, ngày sinh, giới tính, địa chỉ)
  khớp với dữ liệu quét, hiển thị thông báo "Bệnh nhân đã có hồ sơ trong hệ thống"
  kèm `patientId`, nút "Mở hồ sơ cũ" và nút "Thoát". Nút "Tạo mới" bị ẩn/disabled.
  Hành vi này nhất quán với UX cảnh báo trùng FR-101 đã có sẵn.

- **BR-DUP-004 (Case 3 — Tồn tại, có trường lệch):** Nếu tìm thấy bệnh nhân cùng
  CCCD NHƯNG có ít nhất một trường (họ tên / ngày sinh / giới tính / địa chỉ) khác
  với dữ liệu quét:
  - Hiển thị dialog so sánh dạng bảng 4 cột (xem wireframe mục 5.3).
  - Mặc định TẤT CẢ checkbox KHÔNG tích (giữ nguyên dữ liệu cũ). Đây là quyết
    định đã chốt, KHÔNG đổi default.
  - Lễ tân chủ động tích từng trường muốn cập nhật rồi bấm "Lưu thay đổi đã chọn".
  - Nếu không tích gì mà bấm "Lưu", không có gì thay đổi trong DB.
  - Nếu bấm "Thoát", đóng dialog, không thay đổi gì.
  - Hệ thống KHÔNG tự động cập nhật bất kỳ trường nào.

- **BR-DUP-005:** So sánh trường lệch (Case 3) dùng normalize tên trước khi so sánh:
  trim whitespace, lowercase để so sánh logic, nhưng hiển thị trong dialog vẫn giữ
  nguyên định dạng gốc (có dấu, có hoa thường).

**Priority:** Must Have

**User Stories liên quan:** US-QR-003, US-QR-004, US-QR-005

---

### 5.3 Wireframe ASCII — Luồng UI

#### A. Màn hình tạo bệnh nhân mới (/patients/new)

```
╔══════════════════════════════════════════════════════════════════╗
║  Tạo bệnh nhân mới                                              ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  ┌─────────────────────────────────────────────────────────┐    ║
║  │  [🔍 Quét CCCD]  ← Nút / vùng focus để quét            │    ║
║  │                                                         │    ║
║  │  Hướng dẫn: Đặt con trỏ vào ô bên dưới,                │    ║
║  │  sau đó quét mã QR trên thẻ CCCD bằng máy quét.        │    ║
║  │                                                         │    ║
║  │  Ô nhận chuỗi QR: [ _________________________ ] [Xóa]  │    ║
║  │   (readonly sau khi quét, chỉ nhận từ máy quét)        │    ║
║  └─────────────────────────────────────────────────────────┘    ║
║                                                                  ║
║  Họ và tên (*):  [ Nguyễn Văn A              ]                  ║
║  Ngày sinh (*):  [ 15/03/1999  ]  Giới tính: ( Nam ) ( Nữ )    ║
║  Số CCCD:        [ 001099012345              ]                   ║
║  Số CMND cũ:     [ 001234567890             ]                   ║
║  Địa chỉ:        [ Số 1 Đường ABC, Phường X, Quận Y, TP Z ]    ║
║  Ngày cấp CCCD:  [ 10/05/2021  ]                               ║
║  ...các trường khác...                                          ║
║                                                                  ║
║  [ Lưu bệnh nhân ]    [ Hủy ]                                   ║
╚══════════════════════════════════════════════════════════════════╝
```

#### B. Thông báo Case 2 — CCCD đã tồn tại, khớp hoàn toàn

```
╔═══════════════════════════════════════════════════╗
║  ⚠ Bệnh nhân đã có hồ sơ                         ║
╠═══════════════════════════════════════════════════╣
║                                                   ║
║  Số CCCD này đã được đăng ký trong hệ thống.     ║
║                                                   ║
║  Bệnh nhân: Nguyễn Văn A                         ║
║  Mã hồ sơ:  BN-000123                            ║
║  Ngày sinh: 15/03/1999                           ║
║                                                   ║
║  Vui lòng mở hồ sơ cũ thay vì tạo mới.          ║
║                                                   ║
║  [ Mở hồ sơ cũ ]           [ Thoát ]            ║
╚═══════════════════════════════════════════════════╝
```

#### C. Dialog Case 3 — CCCD tồn tại, có trường lệch

```
╔══════════════════════════════════════════════════════════════════════════╗
║  ⚠ Phát hiện thông tin có thể đã thay đổi                              ║
╠══════════════════════════════════════════════════════════════════════════╣
║                                                                          ║
║  Số CCCD: 001099012345 đã có hồ sơ trong hệ thống.                    ║
║  Một số thông tin quét từ CCCD khác với hồ sơ hiện có.                ║
║  Vui lòng kiểm tra và chọn trường muốn cập nhật.                      ║
║                                                                          ║
║  ┌──────────────┬──────────────────────┬──────────────────┬──────────┐  ║
║  │ Trường       │ Dữ liệu hồ sơ hiện có│ Dữ liệu từ CCCD │ Cập nhật │  ║
║  ├──────────────┼──────────────────────┼──────────────────┼──────────┤  ║
║  │ Họ và tên   │ Nguyen Van A         │ Nguyễn Văn A     │  [ ]     │  ║
║  │ Ngày sinh   │ 15/03/1999           │ 15/03/1999       │    —     │  ║
║  │ Giới tính   │ Nam                  │ Nam              │    —     │  ║
║  │ Địa chỉ     │ Số 1 Đường ABC, Q.Y  │ Số 1 Đường ABC,  │  [ ]     │  ║
║  │             │                      │ Phường X, Q.Y,   │          │  ║
║  │             │                      │ TP Z             │          │  ║
║  └──────────────┴──────────────────────┴──────────────────┴──────────┘  ║
║                                                                          ║
║  (Mặc định: tất cả checkbox KHÔNG tích — dữ liệu cũ được giữ nguyên)  ║
║  (Hàng khớp hoàn toàn hiển thị dấu "—", không có checkbox)            ║
║                                                                          ║
║  [ Lưu thay đổi đã chọn ]              [ Thoát — không thay đổi ]     ║
╚══════════════════════════════════════════════════════════════════════════╝
```

#### D. Cảnh báo lỗi parse QR

```
╔══════════════════════════════════════════════════╗
║  ✕ Không đọc được mã QR CCCD                    ║
╠══════════════════════════════════════════════════╣
║                                                  ║
║  Chuỗi quét không đúng định dạng CCCD.          ║
║  Vui lòng thử lại hoặc nhập thông tin thủ công. ║
║                                                  ║
║  Chi tiết: Số trường không hợp lệ (x/7 field)  ║
║                                                  ║
║               [ Đóng ]                          ║
╚══════════════════════════════════════════════════╝
```

**Priority:** Must Have

---

### 5.4 Audit Log

**Mô tả:**
Mọi thao tác cập nhật dữ liệu bệnh nhân phát sinh từ luồng quét CCCD phải được ghi
vào bảng audit log (`diab_his_sec_audit_logs`) với trường nguồn hành động là
`"CCCD_QR_SCAN"`.

**Business Rules — Audit:**

- **BR-AUDIT-001:** Ghi audit log cho TỪNG trường được cập nhật, bao gồm: tên trường,
  giá trị cũ, giá trị mới, `patientId`, `userId` (lễ tân thực hiện), `tenantId`,
  timestamp, và `source = "CCCD_QR_SCAN"`.

- **BR-AUDIT-002:** Việc tạo bệnh nhân mới từ luồng quét CCCD cũng ghi audit log
  với `action = "CREATE"` và `source = "CCCD_QR_SCAN"`.

- **BR-AUDIT-003:** Audit log KHÔNG được có ngoại lệ. Nếu ghi audit log thất bại,
  transaction cập nhật bệnh nhân phải rollback và trả lỗi cho client.

- **BR-AUDIT-004:** Không lưu toàn bộ chuỗi QR vào audit log (tránh lưu thông tin
  cá nhân dư thừa). Chỉ ghi giá trị từng trường riêng biệt.

**Priority:** Must Have

---

## 6. USER STORIES

### US-QR-001: Quét CCCD để tự điền form bệnh nhân mới

```
ID: US-QR-001
Tên: Quét mã QR CCCD để tự điền form tạo bệnh nhân
Epic: Patient Registration — QR CCCD Integration
Priority: High
Story Points: 5

As a lễ tân phòng khám,
I want to quét mã QR trên thẻ CCCD bằng máy quét USB keyboard-wedge,
So that thông tin bệnh nhân được điền tự động vào form, tiết kiệm thời gian
và giảm sai sót so với nhập tay.

Acceptance Criteria:

- Given lễ tân đang ở màn hình /patients/new và con trỏ đang ở ô nhận QR,
  When máy quét USB đọc thẻ CCCD hợp lệ (đúng 7 field, ngày hợp lệ),
  Then hệ thống parse chuỗi và tự điền: họ tên, ngày sinh, giới tính, số CCCD,
       số CMND cũ, địa chỉ thường trú, ngày cấp vào các ô tương ứng trong form.

- Given lễ tân đang ở màn hình ReceptionCheckInForm và con trỏ đang ở ô nhận QR,
  When máy quét USB đọc thẻ CCCD hợp lệ,
  Then các trường tương ứng cũng được điền tự động (nếu form có trường đó).

- Given chuỗi QR được điền thành công,
  When lễ tân kiểm tra lại form,
  Then ngày sinh và ngày cấp hiển thị đúng định dạng dd/MM/yyyy;
       họ tên giữ nguyên Unicode có dấu tiếng Việt.

Business Rules: BR-QR-001, BR-QR-002, BR-QR-003, BR-QR-005, BR-QR-006

Out of scope:
- Quét bằng camera/webcam, OCR ảnh CCCD.
- Lưu lịch sử chuỗi QR đã quét.
```

---

### US-QR-002: Xử lý lỗi parse chuỗi QR CCCD

```
ID: US-QR-002
Tên: Xử lý graceful khi chuỗi QR không hợp lệ
Epic: Patient Registration — QR CCCD Integration
Priority: High
Story Points: 2

As a lễ tân phòng khám,
I want to nhận thông báo lỗi rõ ràng khi chuỗi QR không đọc được,
So that tôi biết ngay vấn đề và có thể nhập thông tin thủ công mà không bị vỡ form.

Acceptance Criteria:

- Given lễ tân quét một thẻ CCCD cũ hoặc QR bị lỗi dẫn đến chuỗi có số field != 7,
  When hệ thống nhận chuỗi,
  Then hiển thị thông báo lỗi "Không đọc được mã QR CCCD — số trường không hợp lệ";
       KHÔNG điền bất kỳ trường nào vào form; lễ tân có thể đóng thông báo và nhập tay.

- Given chuỗi có đúng 7 field nhưng trường ngày sinh chứa giá trị không hợp lệ,
  When hệ thống parse,
  Then điền các trường hợp lệ còn lại bình thường;
       trường ngày sinh để trống;
       server ghi log WARN nêu rõ field lỗi và 20 ký tự đầu chuỗi QR.

- Given chuỗi có trường hoTen hoặc diaChiThuongTru chứa ký tự replacement (encoding cũ),
  When hệ thống parse,
  Then điền vào form dữ liệu đã có (kể cả ký tự lỗi, không vỡ);
       hiển thị icon cảnh báo kèm text "Có thể có lỗi encoding — vui lòng kiểm tra lại".

Business Rules: BR-QR-001, BR-QR-002, BR-QR-003, BR-QR-004, BR-QR-005
```

---

### US-QR-003: Thông báo khi CCCD chưa tồn tại (Case 1)

```
ID: US-QR-003
Tên: Tiếp tục tạo mới khi CCCD chưa có trong hệ thống
Epic: Patient Registration — QR CCCD Integration
Priority: High
Story Points: 1

As a lễ tân phòng khám,
I want to được tạo hồ sơ bệnh nhân mới bình thường khi CCCD chưa tồn tại,
So that luồng tiếp đón không bị gián đoạn không cần thiết.

Acceptance Criteria:

- Given lễ tân quét CCCD thành công và parse ra soCCCD = "001099012345",
  When hệ thống gọi API check trùng và kết quả trả về: không tìm thấy bệnh nhân nào,
  Then form hiển thị đầy đủ thông tin đã điền; không có bất kỳ cảnh báo trùng nào;
       nút "Lưu bệnh nhân" ở trạng thái enabled; lễ tân có thể lưu tạo mới bình thường.

Business Rules: BR-DUP-001, BR-DUP-002, BR-AUDIT-002
```

---

### US-QR-004: Cảnh báo khi CCCD đã tồn tại và khớp hoàn toàn (Case 2)

```
ID: US-QR-004
Tên: Ngăn tạo trùng khi CCCD đã có hồ sơ khớp hoàn toàn
Epic: Patient Registration — QR CCCD Integration
Priority: High
Story Points: 3

As a lễ tân phòng khám,
I want to được thông báo và gợi ý mở hồ sơ cũ khi CCCD đã tồn tại với dữ liệu khớp,
So that tôi không vô tình tạo hồ sơ trùng cho bệnh nhân đã có.

Acceptance Criteria:

- Given lễ tân quét CCCD và parse thành công,
  When API check trùng trả về: tìm thấy bệnh nhân có cùng soCCCD VÀ toàn bộ
       trường so sánh (họ tên, ngày sinh, giới tính, địa chỉ) khớp hoàn toàn,
  Then hiển thị dialog cảnh báo "Bệnh nhân đã có hồ sơ trong hệ thống" kèm
       tên bệnh nhân và patientId (mã hồ sơ);
       nút "Tạo mới" bị disabled/ẩn;
       nút "Mở hồ sơ cũ" chuyển hướng đến trang hồ sơ bệnh nhân tương ứng;
       nút "Thoát" đóng dialog.

- Given dialog cảnh báo trùng đang hiển thị,
  When lễ tân bấm "Mở hồ sơ cũ",
  Then hệ thống điều hướng đến trang hồ sơ bệnh nhân theo patientId trả về từ API.

Business Rules: BR-DUP-001, BR-DUP-003
```

---

### US-QR-005: Cảnh báo và cho phép cập nhật chọn lọc khi có trường lệch (Case 3)

```
ID: US-QR-005
Tên: So sánh và cập nhật chọn lọc khi CCCD tồn tại nhưng có dữ liệu lệch
Epic: Patient Registration — QR CCCD Integration
Priority: High
Story Points: 5

As a lễ tân phòng khám,
I want to thấy bảng so sánh rõ ràng giữa hồ sơ cũ và dữ liệu quét mới,
  và tự chọn trường nào muốn cập nhật,
So that tôi kiểm soát được việc cập nhật thông tin, tránh ghi đè nhầm dữ liệu đúng.

Acceptance Criteria:

- Given lễ tân quét CCCD thành công,
  When API check trùng trả về: tìm thấy bệnh nhân cùng soCCCD NHƯNG ít nhất một
       trường (họ tên / ngày sinh / giới tính / địa chỉ) khác với dữ liệu quét,
  Then hiển thị dialog so sánh dạng bảng 4 cột:
       | Trường | Dữ liệu hồ sơ hiện có | Dữ liệu quét từ CCCD | Checkbox cập nhật |;
       chỉ các hàng có giá trị lệch mới có checkbox; hàng khớp hiển thị dấu "—";
       MẶC ĐỊNH tất cả checkbox KHÔNG tích.

- Given dialog so sánh đang mở,
  When lễ tân tích chọn một hoặc nhiều checkbox rồi bấm "Lưu thay đổi đã chọn",
  Then chỉ các trường đã được tích được cập nhật trong DB;
       audit log được ghi với source = "CCCD_QR_SCAN" cho từng trường được cập nhật;
       dialog đóng lại; form hiển thị thông báo "Đã cập nhật thông tin thành công".

- Given dialog so sánh đang mở và lễ tân không tích checkbox nào,
  When lễ tân bấm "Lưu thay đổi đã chọn",
  Then không có trường nào bị cập nhật trong DB;
       hệ thống không ghi audit log (không có gì thay đổi);
       dialog đóng lại; không hiển thị thông báo cập nhật.

- Given dialog so sánh đang mở,
  When lễ tân bấm "Thoát — không thay đổi",
  Then dialog đóng lại; không có trường nào bị cập nhật; không ghi audit log.

- Given lễ tân đã chọn cập nhật một hoặc nhiều trường,
  When bấm "Lưu thay đổi đã chọn",
  Then hệ thống KHÔNG tự động cập nhật bất kỳ trường nào ngoài những trường
       được tích checkbox (kể cả trường lệch nhưng không tích).

Business Rules: BR-DUP-001, BR-DUP-004, BR-DUP-005, BR-AUDIT-001, BR-AUDIT-003
```

---

## 7. YÊU CẦU PHI CHỨC NĂNG (Non-Functional Requirements)

| Loại             | Yêu cầu                                                        | Ghi chú                              |
|------------------|----------------------------------------------------------------|--------------------------------------|
| Performance      | Parse chuỗi QR < 100ms phía client                            | Không gọi API để parse               |
| Performance      | API check trùng CCCD phản hồi < 500ms (p95)                   | Blind-index sẵn có                   |
| Security         | Mã hóa số CCCD khi lưu DB (AES-256-GCM, cột nhạy cảm)        | Theo CLAUDE.md                       |
| Security         | Blind-index cho số CCCD để tìm kiếm không lộ plaintext        | Đã có sẵn trong hệ thống            |
| Security         | Không log toàn bộ chuỗi QR (chứa thông tin cá nhân)           | Chỉ log 20 ký tự đầu khi cần        |
| Security         | JWT `tenant_id` bắt buộc trong mọi lời gọi API check trùng   | Multi-tenant, theo CLAUDE.md         |
| Audit            | Mọi cập nhật từ luồng QR phải có audit trail đầy đủ           | Không ngoại lệ, BR-AUDIT-001 đến 004|
| Availability     | Tính năng parse QR hoạt động offline (client-side only)        | Không phụ thuộc kết nối để parse    |
| UX/Accessibility | Hướng dẫn quét hiển thị rõ ràng, font đủ lớn cho lễ tân       | Tablet-friendly, min touch target    |
| UX/Accessibility | Dialog cảnh báo và bảng so sánh đọc được trên màn hình nhỏ    | Responsive, scrollable trên tablet   |
| Compatibility    | Tương thích máy quét USB keyboard-wedge phổ biến (HID class)  | Không cần driver đặc biệt            |
| Encoding         | Xử lý đúng UTF-8 cho chuỗi QR có tiếng Việt                   | Không giả định ASCII                 |

---

## 8. PHẠM VI (Scope)

**In Scope:**
- Parse chuỗi QR CCCD từ máy quét USB keyboard-wedge.
- Tự điền form tạo bệnh nhân mới tại `/patients/new` (PatientEditorLayout).
- Tự điền form tiếp đón nhanh tại `ReceptionCheckInForm` (nếu có trường tương ứng).
- Check trùng CCCD (3 case) và hiển thị cảnh báo/dialog phù hợp.
- Cập nhật chọn lọc từng trường theo xác nhận của lễ tân (Case 3).
- Audit log đầy đủ cho mọi thao tác cập nhật từ luồng quét CCCD.
- Xử lý lỗi parse graceful (không vỡ form, không throw exception).

**Out of Scope:**
- Quét CCCD bằng camera hoặc webcam (camera scanning).
- OCR ảnh CCCD (nhận dạng văn bản từ ảnh chụp).
- Đọc chip NFC từ CCCD gắn chip (chip reading).
- Xác thực tính hợp lệ của CCCD với cơ sở dữ liệu dân cư quốc gia.
- Lưu trữ lịch sử chuỗi QR đã quét.
- Tự động cập nhật thông tin BHYT từ dữ liệu CCCD.
- Hỗ trợ định dạng QR CCCD trước 2021 (dưới 7 field).
- Mobile app (tính năng này chỉ áp dụng cho web client có máy quét USB).

---

## 9. PHỤ THUỘC (Dependencies)

| #  | Phụ thuộc                                        | Team / Hệ thống    | Trạng thái       |
|----|--------------------------------------------------|--------------------|------------------|
| 1  | API check trùng CCCD (blind-index)               | Backend            | Sẵn có           |
| 2  | Mã hóa cột CCCD (AES-256-GCM)                   | Backend / DB       | Sẵn có           |
| 3  | UX cảnh báo trùng FR-101                         | Frontend           | Sẵn có           |
| 4  | PatientEditorLayout component                    | Frontend           | Sẵn có           |
| 5  | ReceptionCheckInForm component                   | Frontend           | Cần xác nhận có không |
| 6  | Bảng audit log diab_his_sec_audit_logs           | Backend / DB       | Sẵn có           |
| 7  | ITenantProvider + TenantScopeMiddleware          | Backend            | Sẵn có           |
| 8  | Máy quét USB keyboard-wedge tại phòng khám       | Hạ tầng khách hàng | Không thuộc scope phần mềm |

---

## 10. RỦI RO & GIẢ ĐỊNH (Risks & Assumptions)

**Rủi ro:**

| Rủi ro                                                      | Xác suất | Ảnh hưởng | Biện pháp                                                         |
|-------------------------------------------------------------|----------|-----------|-------------------------------------------------------------------|
| Máy quét USB gửi chuỗi không kết thúc bằng Enter (thiếu trigger) | M    | H         | Thêm nút "Parse ngay" bên cạnh ô QR; cho phép trigger thủ công. |
| Chuỗi QR CCCD trước 2021 có định dạng khác (< 7 field)    | M        | M         | Hiển thị lỗi rõ ràng, cho phép nhập tay; ghi log để phân tích.  |
| Encoding lỗi làm vỡ ký tự tiếng Việt trong tên bệnh nhân  | L        | M         | BR-QR-005: hiển thị icon cảnh báo, không vỡ form.               |
| Lễ tân vô tình cập nhật sai trường trong dialog so sánh    | M        | H         | Default checkbox không tích; yêu cầu xác nhận trước khi lưu.    |
| API check trùng chậm làm UX gián đoạn                      | L        | M         | Hiển thị spinner; timeout 5 giây với thông báo thử lại.          |
| ReceptionCheckInForm không có đủ trường để điền            | M        | L         | Chỉ điền các trường tồn tại trong form; bỏ qua trường thiếu.    |

**Giả định:**

- **GA-001:** Máy quét USB keyboard-wedge hoạt động như bàn phím HID chuẩn, gửi
  chuỗi QR và kết thúc bằng phím Enter (CR/LF). Không cần driver hay SDK đặc biệt.
- **GA-002:** Chuỗi QR CCCD từ 2021 trở đi luôn có đúng 7 field phân cách bằng `|`.
- **GA-003:** API check trùng CCCD hiện tại chấp nhận tham số `soCCCD` dạng plaintext
  và tự xử lý blind-index ở server. Frontend không cần tự hash.
- **GA-004:** `ReceptionCheckInForm` có ít nhất các trường: họ tên, ngày sinh,
  giới tính, số CCCD. Nếu không, chỉ tính năng tại `/patients/new` được áp dụng.
- **GA-005:** Giá trị giới tính trong chuỗi QR chỉ là "Nam" hoặc "Nữ" (không có
  giá trị khác). Nếu khác, giới tính để trống.
- **GA-006:** Lễ tân có quyền `ROLE_LeTan` hoặc cao hơn mới thấy và sử dụng
  tính năng quét CCCD (RBAC theo JWT).

---

## 11. TIMELINE

| Milestone                        | Mô tả                                                   | Ngày dự kiến |
|----------------------------------|---------------------------------------------------------|--------------|
| M1 — Design & API contract       | Architect ra API contract check trùng (nếu cần mở rộng)| 2026-09-03   |
| M2 — Backend (nếu cần)          | Extend API check trùng trả về trường lệch chi tiết      | 2026-09-05   |
| M3 — Frontend parse + fill form  | Implement ô quét QR, parse client-side, tự điền form    | 2026-09-08   |
| M4 — Frontend check trùng + UI  | 3 case dialog, bảng so sánh 4 cột, audit log trigger    | 2026-09-12   |
| M5 — Audit log integration       | Verify audit log ghi đủ trường, đúng source             | 2026-09-13   |
| M6 — QC / UAT                    | Test 3 case trùng + case parse lỗi + audit trail        | 2026-09-15   |
| M7 — Go-live                     | Deploy lên staging → production                         | 2026-09-17   |

---

## 12. PHÊ DUYỆT (Approval)

| Vai trò     | Tên        | Ngày       | Chữ ký |
|-------------|------------|------------|--------|
| PO          | ATDS-Lanh  | 2026-08-30 |        |
| Tech Lead   |            |            |        |
| Stakeholder |            |            |        |

---

*Tài liệu này được tạo tự động bởi PO Agent — Pro-Diab HIS.*
*Mọi thay đổi sau phê duyệt phải tạo phiên bản mới và cập nhật lịch sử thay đổi.*
