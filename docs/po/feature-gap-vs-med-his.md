# Phân tích GAP nghiệp vụ: Pro-Diab HIS vs. MedArmor HIS (tham chiếu)

- Người thực hiện: Đăng (PO/BA)
- Ngày: 18/08/2026
- Nguồn dữ liệu: 21 ảnh chụp màn hình hệ thống `his.uat.medarmor.vn` tại `D:\_Project\diaB\his\med\`
- Đối chiếu: `CLAUDE.md`, `docs/prd/`, `backend/src/ProDiabHis.*`, `frontend/app/(dashboard)/`

> Lưu ý phương pháp: mọi đề xuất trong tài liệu này đều gắn **lý do nghiệp vụ hoặc tuân thủ pháp lý**, KHÔNG lấy lý do "vì hệ thống tham chiếu có". Hạng mục nào không có lý do nghiệp vụ đủ mạnh thì xếp P2 hoặc đưa vào "Không đề xuất".

---

## 1. Inventory chức năng phát hiện từ ảnh

### 1.1 Cấu trúc menu quan sát được

| Nhóm menu | Mục con |
|---|---|
| TIẾP NHẬN | Camera AI, Phòng khám, Tiếp nhận, Điều phối khám, Đính kèm tài liệu, Lịch nhắc hẹn |
| ĐĂNG KÝ KHÁM | Đăng ký khám, Đặt lịch online |
| KHÁM BỆNH | Bệnh nhân, Đo sinh hiệu, Khám bệnh, Danh sách bệnh án, Báo cáo sức khoẻ |
| CẬN LÂM SÀNG | Màn hình xét nghiệm, Phiếu xét nghiệm, Lệnh CĐHA, Lệnh nội soi, Nhập kết quả CLS |
| VIỆN PHÍ & QUẦY THUỐC | Viện phí, Quầy thuốc |
| DANH MỤC | Thư viện mẫu, Danh mục vật tư |

### 1.2 Bảng inventory theo màn hình

| # | Ảnh | Màn hình / URL | Chức năng nghiệp vụ | Trường dữ liệu / trạng thái lộ ra |
|---|---|---|---|---|
| 1 | 13-33-37 | Camera AI `/camera` | Nhận diện khuôn mặt bệnh nhân tại quầy lễ tân qua camera LIVE; có "Lịch sử nhận diện (phiên hiện tại)" | Trạng thái LIVE, nút Dừng / Làm mới, panel "Đang chờ nhận diện" |
| 2 | 13-34-24 | Tiếp nhận `/booking/reception` | Danh sách lịch hẹn chờ khám theo ngày; 2 chế độ xem Danh sách / Lịch | Ngày, STT, Giờ hẹn, Bệnh nhân, Trạng thái; counter "0 Chờ khám"; empty state |
| 3 | 13-46-01 | Khám bệnh — tab Cận lâm sàng `/opd/consult/{id}` | Chỉ định CLS theo **đợt chỉ định**; picker dịch vụ có filter Tất cả / Xét nghiệm / CĐHA / Thủ thuật / Nội soi / **Chọn gói khám** | Đợt chỉ định #1, badge "Chưa thanh toán", Tên dịch vụ, Loại (Gói khám / Xét nghiệm), Đơn giá, Mã dịch vụ (XN0119...), "Lệnh đã tạo (1)" |
| 4 | 13-46-21 | Khám bệnh — CLS (đợt #2) | Cho phép **nhiều đợt chỉ định** trong 1 lượt khám, mỗi đợt có trạng thái thanh toán riêng | Đợt #1, Đợt #2, badge "Chưa thanh toán" từng đợt, "Lệnh đã tạo (2)" |
| 5 | 13-47-10 | Nhập kết quả CLS `/cls-results` | Worklist phiếu CLS theo loại (Xét nghiệm / Chẩn đoán hình ảnh / Nội soi), lọc khoảng ngày, tiến độ hoàn thành từng phiếu | Mã phiếu XN2026..., số dịch vụ, trạng thái "0/8 hoàn thành", "13/20", "8/8"; dropdown trạng thái từng chỉ số (Chưa hoàn thành / Hoàn thành); nút Nhập kết quả |
| 6 | 13-47-41 | Viện phí `/billing` | Danh sách hoá đơn theo ngày, tab lọc Tất cả / Chưa thu / Đã thu / Đã hoàn | Số HĐ `INV-2026-0007`, `PH-2026-0005` (2 tiền tố: viện phí vs. quầy thuốc), tên BN, mã BN, số tiền, trạng thái Chưa thu / Đã thu / **Đã huỷ** |
| 7 | 13-50-31 | Xem PDF kết quả XN đính kèm trong bệnh án | Viewer PDF nhúng, tải xuống; phiếu kết quả xét nghiệm có chữ ký Trưởng phòng Xét nghiệm | Họ tên, Năm sinh, Giới tính, Mã BN, Số ĐT, Địa chỉ, Chẩn đoán, BS chỉ định, Đơn vị chỉ định; bảng Tên XN / Kết quả / **Giá trị tham khảo** / Đơn vị |
| 8 | 13-52-32 | In "ĐƠN THUỐC DỊCH VỤ" (PDF) | Mẫu in đơn thuốc song ngữ Việt–Anh, có **barcode mã bệnh nhân** | Header cơ sở (địa chỉ, ĐT, email, website); BN: Họ tên, Ngày sinh, Điện thoại, Giới tính, Địa chỉ; Sinh hiệu in trên đơn (HA, Mạch, Nhiệt độ, Cân nặng); mỗi thuốc: tên + hàm lượng, Sáng/Trưa/Chiều/Tối, số lượng, số ngày, Cách dùng |
| 9 | 13-56-12 | Lịch nhắc hẹn `/opd/appointments/reminders` | Lịch tháng nhắc hẹn, phân loại theo màu: **Tái khám**, **Ngày lấy mẫu tại nhà**, **Ngày khám (lấy mẫu tại nhà)**; nút Thêm lịch nhắc | Bộ lọc Tất cả / Tái khám / Lấy mẫu tại nhà; panel chi tiết theo ngày |
| 10 | 13-57-04 | Khám bệnh — tab Chẩn đoán | Chẩn đoán ICD-10 nhiều mã, đánh dấu **chẩn đoán chính**; Hướng điều trị dạng rich-text | Tìm mã ICD-10, chip `Z00.0 Khám sức khoẻ tổng quát [Chẩn đoán chính]`, editor B/I/S/List cho "Phác đồ điều trị, chỉ định, lời dặn" |
| 11 | 13-59-21 | Chi tiết phiếu XN `/lab/orders/{id}` | Nhập kết quả từng chỉ số; **đính kèm file kết quả ngoài hệ thống**; **Scan phiếu XN**, **Import CSV**, Thêm chỉ số thủ công | Mốc thời gian: Chỉ định / Lấy mẫu / Có kết quả; cột Xét nghiệm (+mã LOINC-like), Kết quả, Phiếu(đơn vị), **Khoảng tham chiếu**, **Flag (BT)**, Ngày tạo; nút Lưu kết quả |
| 12 | 14-00-22 | Nhập kết quả CLS (đã hoàn thành) | Gắn kết quả dạng **link ngoài** (vd URL) hoặc **file PDF** cho từng chỉ số; nguồn kết quả ghi rõ đối tác (Medlink, Medlab) | Trạng thái 8/8 hoàn thành, badge "Đã xác nhận", hành động Mở / Xem / Tải xuống / Xoá |
| 13 | 14-00-33 | Hàng đợi khám `/opd/queue` | Worklist bác sĩ, KPI 3 ô: Chờ khám / Đang khám / Hoàn thành; **nhóm theo phòng khám**; auto-refresh 30 giây | Tab lọc Tất cả / Chờ khám / Đang khám / **Chờ kết quả CLS** / Hoàn thành; cột STT, Bệnh nhân(+gói), Bác sĩ, Phòng khám, Giờ hẹn, Trạng thái; nút Walk-in / Đặt lịch |
| 14 | 14-01-22 | Khám bệnh — tab **Ghi âm** | Ghi âm buổi khám, nhiều đoạn, player nghe lại + tải về; có bản ghi lỗi | Trạng thái "Hoàn tất" / "Lỗi", độ dài (1:44, 3:06, 3:34), timestamp, thông báo "Không có dữ liệu ghi âm (dừng quá sớm)"; toggle "Không ghi âm (tự động theo cấu hình)" |
| 15 | 14-10-10 | Điều phối khám `/opd/reassign` | Lễ tân đổi **bác sĩ / phòng khám** hoặc **huỷ lịch hẹn** trong ngày; **In mã vạch** | Quy tắc rõ: "Ca đã qua bước đang khám (hoặc đã huỷ) chỉ xem, không sửa được"; trạng thái Hoàn thành / Chờ khám |
| 16 | 14-10-20 | Hàng đợi khám (sau điều phối) | Xác nhận thay đổi phòng phản ánh ngay trên queue | Nút Khám (mở ca), badge Chờ khám |
| 17 | 14-10-29 | Khám bệnh — **Chuyển phòng** + tab Tiền sử | Dropdown chuyển phòng ngay trong ca khám (Phòng khám 1/2/3–Nội/Tim mạch); tab Tiền sử: Triệu chứng lâm sàng + Tiền sử bệnh (rich-text) | Placeholder "Tiền sử bệnh, thuốc đang dùng, dị ứng, gia đình, xã hội..." |
| 18 | 14-10-59 | Hàng đợi khám (bác sĩ khác) | Ca đã chuyển sang BS Hàn Tiểu Sáo / Phòng khám 2 | — |
| 19 | 14-16-47 | Khám bệnh — tab Đơn thuốc | Kê đơn nhiều **toa** trong 1 ca (Toa #1, "Kê thêm toa mới"); phân biệt **Thuốc có trong kho** vs **Thuốc ngoài (không bán, tham chiếu)** | Trạng thái toa "Đang soạn"; cột Tên thuốc, Sáng, Trưa, Chiều, Tối, **Thời gian(số ngày)**, SL, ĐV; ô "Cách dùng (VD: uống sau ăn, không dùng chung với...)"; nút In đơn / Lưu đơn thuốc |
| 20 | 14-18-31 | Cảnh báo chưa lưu | Guard rời trang khi bệnh án có thay đổi chưa lưu | 3 lựa chọn: Lưu & tiếp tục / Bỏ qua & rời trang / Ở lại trang này |
| 21 | 14-30-45 | Khám bệnh — CLS ca **Tái khám** | Dịch vụ thuộc gói được đánh dấu "trong gói" giá 0đ; có cột **In** và **Ẩn giá** cho từng dòng chỉ định | Lý do khám "Tái khám", chip chẩn đoán `A49.9 Nhiễm khuẩn, không đặc hiệu`; cột Mã, Loại (Bộ XN / Thủ thuật), Đơn giá "trong gói", checkbox In / Ẩn giá |

### 1.3 Quy tắc nghiệp vụ suy ra được

1. **Đợt chỉ định (order round)**: 1 lượt khám có nhiều đợt chỉ định CLS, mỗi đợt là một đơn vị thanh toán độc lập (`Chưa thanh toán`).
2. **Gói khám (service package)** ràng buộc giá: dịch vụ nằm trong gói hiển thị "trong gói", đơn giá 0đ, không tính thêm.
3. **Trạng thái ca khám** mở rộng hơn state machine 3 pha: có thêm **Chờ kết quả CLS** giữa "Đang khám" và "Hoàn thành".
4. **Khoá sửa sau khi kết thúc khám**: "Ca khám đã hoàn thành — chỉ xem, không thể chỉnh sửa".
5. **Khoá điều phối theo tiến độ**: chỉ đổi BS/phòng/huỷ khi ca chưa vào "đang khám".
6. **Sinh hiệu nhiều lần**: "Lần 1", có chip đo lại; sinh hiệu được in lên đơn thuốc.
7. **Kết quả CLS 3 nguồn**: nhập tay từng chỉ số, import CSV / scan phiếu, và đính kèm file/link từ đối tác ngoài; kết quả có **flag bất thường** so với khoảng tham chiếu.
8. **Đơn thuốc nhiều toa**, phân biệt thuốc trong kho (trừ tồn, bán) và thuốc ngoài (chỉ tham chiếu, không trừ kho, không tính tiền).
9. **Hoá đơn 2 nguồn**: `INV-` (viện phí/dịch vụ) và `PH-` (quầy thuốc), có trạng thái huỷ/hoàn.
10. **Nhắc hẹn đa loại**: tái khám, lấy mẫu tại nhà, ngày khám của dịch vụ lấy mẫu tại nhà.

---

## 2. Đối chiếu hiện trạng Pro-Diab HIS

Pro-Diab HIS đã có (từ backend controllers + frontend routes):

- Reception / queue, Appointments, DoctorSchedules, Rooms
- Patients, Encounters, VitalSigns, EMR templates, ICD-10, CDSS, AI suggestion
- ClsOrders, LabResults (đã có `ReferenceRangeLow/High`, `Flag`, `Status=PRELIMINARY`, `VerifiedAt/By`, `Source`), RadResults, LabPartners, LabIntegration, ClsUploads
- Prescriptions, DTQG, Pharmacy (kho + dispensing), Drugs, Suppliers
- Billings, Payments, Cashier (+ debts, closing), EInvoices
- BHYT export + reconcile, FHIR, Reports/BI + Report Builder, Recall, Portal bệnh nhân, Audit, RBAC
- ServicePackage (CRUD gói dịch vụ đã có trong `ServiceCatalogHandlers`)

Đây là phạm vi **rộng hơn** hệ tham chiếu ở nhiều mảng (BHYT, ĐTQG, kho dược, BI, portal). GAP nằm chủ yếu ở **chiều sâu luồng khám và CLS**.

---

## 3. Bảng GAP nghiệp vụ

Ký hiệu: **T** = thiếu hoàn toàn · **TP** = có nhưng thiếu trường/luồng · **OK** = đã đủ

| # | Hạng mục | Trạng thái | Mô tả GAP | Ưu tiên | Lý do nghiệp vụ |
|---|---|---|---|---|---|
| G01 | **Đợt chỉ định CLS + gate thanh toán** | T | `ClsOrders` chưa có khái niệm "đợt" (round/batch) và chưa có trạng thái thanh toán ở cấp đợt để chặn thực hiện CLS khi chưa thu tiền | **P0** | Phòng khám tư thu tiền trước khi làm CLS. Không có gate → thất thu, kỹ thuật viên không biết ca nào đã thu. Đây là rủi ro tài chính trực tiếp. |
| G02 | **Trạng thái "Chờ kết quả CLS"** | TP | `TicketStatus` chỉ có WAITING/CALLED/IN_PROGRESS/DONE/SKIPPED/CANCELLED. Không có trạng thái chờ CLS | **P0** | BN đi làm XN mất 30–60 phút; nếu vẫn để IN_PROGRESS thì bác sĩ bị "khoá" phòng, không gọi được BN kế tiếp → nghẽn hàng đợi, giảm công suất khám. |
| G03 | **Khoá chỉnh sửa bệnh án sau khi kết thúc khám** | TP (cần xác minh) | Chưa thấy rule khoá hồ sơ read-only sau `Kết thúc khám`, và cơ chế amendment có kiểm soát | **P0** | Yêu cầu pháp lý về tính toàn vẹn hồ sơ bệnh án (Luật KCB 2023, TT 32/2023). Bệnh án đã hoàn tất mà sửa tự do là vi phạm; nếu cần sửa phải là "bản đính chính" có log. |
| G04 | **Gói khám ràng buộc giá khi chỉ định** | TP | Đã có CRUD `ServicePackage`, nhưng chưa thấy luồng: BN mua gói → khi chỉ định dịch vụ thuộc gói thì đơn giá = 0 ("trong gói") và không lập hoá đơn lần 2 | **P0** | Bán gói khám sức khoẻ là nguồn doanh thu chính của phòng khám đa khoa. Thiếu ràng buộc → thu trùng tiền của BN (khiếu nại) hoặc miễn nhầm (thất thu). |
| G05 | **Điều phối khám (đổi BS/phòng, chuyển phòng giữa ca)** | T | Không có endpoint reassign encounter/ticket sang bác sĩ hoặc phòng khác | **P0** | Bác sĩ bận đột xuất, BN đông dồn 1 phòng — nghiệp vụ hằng ngày của lễ tân. Không có → phải huỷ và tạo lại lượt khám, mất mã lượt khám, sai dữ liệu thống kê và sai công BS. |
| G06 | **Chẩn đoán chính vs chẩn đoán kèm theo** | TP (cần xác minh) | Cần đảm bảo encounter lưu nhiều mã ICD-10 và đánh dấu **1 mã chính** | **P0** | XML 4210 (QĐ 4750) bắt buộc trường `MA_BENH` (chính) tách khỏi `MA_BENH_KHAC`. Thiếu → hồ sơ BHYT bị từ chối giám định. |
| G07 | **Nhiều toa trong 1 lượt khám + thuốc ngoài đơn** | TP | Cần xác nhận model prescription hỗ trợ N toa/encounter và cờ "thuốc ngoài (không trừ kho, không tính tiền)" | **P1** | Thực tế BS kê 1 toa BHYT + 1 toa dịch vụ, hoặc ghi thuốc BN đang dùng ở nơi khác để kiểm tra tương tác. Gộp chung → sai trừ kho và sai số tiền. |
| G08 | **Liều theo cữ Sáng/Trưa/Chiều/Tối + số ngày dùng** | TP | Cần đảm bảo prescription item có 4 cữ tách biệt, số ngày, và **tự tính số lượng** | **P1** | TT 27/2021 yêu cầu ghi rõ liều dùng, số lần/ngày, đường dùng. Nhập tự do dạng text không đẩy chuẩn được lên ĐTQG và dễ sai số lượng cấp phát. |
| G09 | **Import kết quả CLS hàng loạt (CSV) + scan phiếu** | T | Chỉ có nhập tay / tích hợp partner | **P1** | Máy XN của phòng khám nhỏ thường xuất CSV, chưa có LIS interface. Không có import → KTV gõ tay 20 chỉ số/phiếu, sai số cao trên dữ liệu lâm sàng. |
| G10 | **Tiến độ hoàn thành phiếu CLS (x/y) + trạng thái từng chỉ số** | TP | Có `Status` cấp `LabResult` nhưng chưa thấy tiến độ cấp phiếu để làm worklist | **P1** | Điều dưỡng/KTV cần biết phiếu nào còn thiếu chỉ số để trả kết quả đúng hẹn; BS cần biết chờ tới bao giờ. |
| G11 | **Mốc thời gian CLS: Chỉ định → Lấy mẫu → Có kết quả** | TP | `LabResult` có `PerformedAt`, thiếu `OrderedAt`/`CollectedAt` ở cấp order item | **P1** | Đo TAT (turnaround time) — KPI chất lượng bắt buộc để truy vết khi BN khiếu nại trả kết quả chậm. |
| G12 | **Nhắc hẹn đa loại (tái khám / lấy mẫu tại nhà) dạng lịch tháng** | TP | Đã có `Recall` + `Appointments`, thiếu phân loại nhiều nhóm nhắc và view lịch tháng | **P1** | Bệnh mạn tính (đái tháo đường — trọng tâm sản phẩm) sống nhờ tuân thủ tái khám. Phân loại sai → gọi nhắc nhầm nhóm. |
| G13 | **In mã vạch/QR định danh bệnh nhân & phiếu** | T | Chưa thấy chức năng in barcode BN | **P1** | Dán barcode lên ống mẫu XN là biện pháp an toàn người bệnh chống nhầm mẫu — sự cố nghiêm trọng nhất của khối CLS. |
| G14 | **Đơn thuốc in kèm sinh hiệu + barcode + song ngữ** | TP | Đã có print đơn thuốc; thiếu sinh hiệu/barcode trên mẫu in | **P2** | Tăng độ tin cậy khi đối chiếu tại quầy thuốc; song ngữ phục vụ BN nước ngoài (tuỳ phân khúc). |
| G15 | **Cảnh báo rời trang khi chưa lưu bệnh án** | T | FE chưa có guard unsaved changes | **P1** | Mất dữ liệu bệnh án đang nhập là mất thời gian BS và có thể mất thông tin lâm sàng không tái tạo được. |
| G16 | **Hoá đơn: huỷ / hoàn tiền có lý do** | TP | Có Billings/Payments; cần xác minh có trạng thái ĐÃ HUỶ / ĐÃ HOÀN kèm lý do và bút toán đối ứng | **P1** | Kế toán bắt buộc phải giải trình được mọi hoá đơn huỷ/hoàn; xoá cứng là vi phạm nguyên tắc kế toán và làm sai báo cáo doanh thu. |
| G17 | **Tách nguồn hoá đơn: viện phí (INV) vs quầy thuốc (PH)** | TP | Cần xác minh đánh số theo nguồn | **P2** | Giúp đối soát doanh thu theo quầy và kiểm kê chéo với xuất kho dược. |
| G18 | **Ghi âm buổi khám** | T | Không có | **P2** | Có giá trị làm bằng chứng tư vấn và tự động hoá ghi chép, nhưng phát sinh nghĩa vụ **đồng ý của người bệnh** và lưu trữ dữ liệu nhạy cảm. Chỉ làm sau khi có chính sách quyền riêng tư rõ ràng. |
| G19 | **Camera AI nhận diện bệnh nhân tại quầy** | T | Không có | **P2** | Dữ liệu sinh trắc học là dữ liệu cá nhân nhạy cảm theo NĐ 13/2023 — cần cơ sở pháp lý, đồng ý riêng, DPIA. Lợi ích (rút ngắn check-in) chưa tương xứng rủi ro ở quy mô 2–5 BS. **Đề xuất chưa làm.** |
| G20 | **Đặt lịch online cho bệnh nhân** | TP | Đã có Patient Portal; cần xác minh có luồng đặt lịch tự phục vụ | **P1** | Giảm tải điện thoại lễ tân, tăng lấp đầy khung giờ trống. |
| G21 | **Đính kèm tài liệu ở cấp tiếp đón/bệnh nhân** | TP | Có `ClsUploads`/`Files` gắn CLS; thiếu kho tài liệu cấp hồ sơ BN (giấy chuyển tuyến, thẻ BHYT, CCCD) | **P1** | Giám định BHYT yêu cầu lưu bản chụp thẻ/giấy chuyển tuyến; thiếu → không giải trình được khi bị xuất toán. |
| G22 | **Ẩn giá / chọn in từng dòng chỉ định** | T | Không có | **P2** | Dùng khi in phiếu cho BN thuộc gói hoặc BN doanh nghiệp không được xem đơn giá. Nhu cầu ngách. |
| G23 | **Thư viện mẫu (template) & Danh mục vật tư** | TP | Có `EMR templates`; chưa có **danh mục vật tư tiêu hao** tách khỏi danh mục thuốc | **P1** | Vật tư (kim, bơm tiêm, test nhanh) có quy tắc quản lý và thanh toán BHYT khác thuốc; gộp vào bảng thuốc gây sai báo cáo XML 4210 phần vật tư. |
| G24 | **Báo cáo sức khoẻ tổng hợp (health report) cho BN gói khám** | T | Chưa thấy | **P1** | Sản phẩm đầu ra bán được của gói khám sức khoẻ; không có thì gói khám mất giá trị cảm nhận. |
| G25 | **Nhóm hàng đợi theo phòng + auto-refresh** | TP | Có queue; cần group-by phòng và auto refresh | **P2** | Trải nghiệm vận hành, không phải rủi ro nghiệp vụ. |

---

## 4. EPIC + User Story rút gọn cho hạng mục P0

### EPIC-1: Đợt chỉ định CLS và kiểm soát thanh toán trước thực hiện (G01, G04)

- **US-01**: Là **bác sĩ**, tôi muốn tạo nhiều đợt chỉ định CLS trong một lượt khám, để chỉ định bổ sung sau khi có kết quả đợt đầu mà không lẫn với đợt trước.
  - **AC-01**: Given ca khám đang mở và đã có đợt chỉ định #1, When bác sĩ chọn thêm dịch vụ và bấm "Tạo lệnh", Then hệ thống tạo đợt #2 riêng biệt, mỗi đợt có mã đợt, thời điểm tạo, người tạo và tổng tiền riêng.
  - **AC-02**: Given đợt #1 đã thanh toán và đợt #2 chưa, When xem tab Cận lâm sàng, Then mỗi đợt hiển thị nhãn trạng thái thanh toán độc lập.
- **US-02**: Là **kỹ thuật viên CLS**, tôi muốn hệ thống chặn thực hiện dịch vụ của đợt chưa thanh toán, để không thất thu.
  - **AC-03**: Given đợt chỉ định có trạng thái `CHUA_THANH_TOAN`, When KTV bấm "Nhập kết quả" cho dịch vụ thuộc đợt đó, Then hệ thống từ chối với mã lỗi `CLS_ORDER_UNPAID` và thông báo "Đợt chỉ định chưa thanh toán".
  - **AC-04**: Given tenant bật cấu hình `cho_phep_no_vien_phi = true`, When KTV thực hiện thao tác trên, Then hệ thống cho phép nhưng ghi audit log kèm user và lý do.
- **US-03**: Là **kế toán**, tôi muốn dịch vụ thuộc gói khám BN đã mua được tính đơn giá 0đ, để không thu trùng tiền.
  - **AC-05**: Given BN đã thanh toán gói khám G chứa dịch vụ X, When bác sĩ chỉ định X, Then dòng chỉ định hiển thị "trong gói", đơn giá 0đ và không phát sinh dòng hoá đơn mới.
  - **AC-06**: Given dịch vụ X đã dùng hết số lượt trong gói, When chỉ định lần tiếp theo, Then tính theo bảng giá dịch vụ lẻ và cảnh báo "Đã dùng hết lượt trong gói".
  - **AC-07**: Given gói khám đã hết hạn sử dụng, When chỉ định dịch vụ thuộc gói, Then hệ thống tính giá lẻ và hiển thị lý do "Gói đã hết hạn ngày dd/MM/yyyy".
- **Role truy cập**: BacSi (tạo đợt), LeTan/KeToan (thu tiền đợt), KyThuatVien (thực hiện), Admin (cấu hình cho nợ).
- **Edge case**: BN BHYT (đợt chỉ định phần BHYT chi trả không cần thu trước); đợt rỗng 0 dịch vụ; huỷ đợt đã thanh toán → phải sinh phiếu hoàn.

### EPIC-2: Trạng thái luồng khám đầy đủ (G02)

- **US-04**: Là **bác sĩ**, tôi muốn chuyển ca sang trạng thái "Chờ kết quả CLS" khi BN đi làm xét nghiệm, để gọi bệnh nhân kế tiếp mà không mất ca đang khám.
  - **AC-08**: Given ca ở trạng thái `IN_PROGRESS` và có ít nhất 1 đợt chỉ định CLS chưa có kết quả, When bác sĩ bấm "Chờ kết quả CLS", Then trạng thái chuyển `WAITING_CLS`, phòng khám được giải phóng, ca vẫn nằm trong tab "Chờ kết quả CLS".
  - **AC-09**: Given ca ở `WAITING_CLS`, When toàn bộ chỉ số của các phiếu CLS đạt trạng thái Hoàn thành, Then hệ thống gửi thông báo cho bác sĩ phụ trách và cho phép chuyển lại `IN_PROGRESS`.
  - **AC-10**: Given ca ở `WAITING_CLS`, When bác sĩ bấm "Kết thúc khám" mà còn phiếu CLS chưa hoàn thành, Then hệ thống hỏi xác nhận và ghi lý do kết thúc sớm vào bệnh án.
- **Role**: BacSi, KyThuatVien (cập nhật kết quả), LeTan (xem).
- **Edge case**: BN bỏ về giữa chừng (`SKIPPED` từ `WAITING_CLS`); kết quả trả sau khi ca đã `DONE` → gắn vào lượt khám cũ, không mở lại ca.

### EPIC-3: Toàn vẹn hồ sơ bệnh án (G03)

- **US-05**: Là **quản lý phòng khám**, tôi muốn bệnh án đã kết thúc khám không sửa được tự do, để đảm bảo tính pháp lý của hồ sơ.
  - **AC-11**: Given ca ở trạng thái `DONE`, When bất kỳ user nào mở bệnh án, Then toàn bộ trường lâm sàng ở chế độ chỉ đọc và hiển thị banner "Ca khám đã hoàn thành — chỉ xem".
  - **AC-12**: Given user có quyền `ENCOUNTER_AMEND`, When user tạo bản đính chính, Then hệ thống lưu bản ghi mới dạng addendum (không ghi đè bản gốc), bắt buộc nhập lý do, và ghi audit log gồm user, thời điểm, nội dung trước/sau.
  - **AC-13**: Given bệnh án đã đưa vào hồ sơ BHYT đã gửi giám định, When user tạo đính chính, Then hệ thống cảnh báo "Hồ sơ đã gửi giám định — đính chính cần gửi lại XML".
- **Role**: chỉ Admin + BacSi chủ ca có `ENCOUNTER_AMEND`.

### EPIC-4: Điều phối khám (G05)

- **US-06**: Là **lễ tân**, tôi muốn đổi bác sĩ hoặc phòng khám của một lượt khám chưa bắt đầu, để cân bằng tải giữa các phòng.
  - **AC-14**: Given lượt khám ở trạng thái `WAITING` hoặc `CALLED`, When lễ tân chọn bác sĩ/phòng mới và lưu, Then lượt khám giữ nguyên mã lượt khám, cập nhật BS/phòng, và ghi log chuyển kèm người thực hiện + thời điểm.
  - **AC-15**: Given lượt khám ở `IN_PROGRESS`, `DONE` hoặc `CANCELLED`, When lễ tân mở màn hình điều phối, Then các trường bác sĩ/phòng ở chế độ chỉ đọc và không có nút Huỷ.
  - **AC-16**: Given bác sĩ đích không có lịch làm việc trong khung giờ đó, When lưu điều phối, Then hệ thống cảnh báo "Bác sĩ không có lịch trực khung giờ này" và yêu cầu xác nhận.
- **US-07**: Là **bác sĩ**, tôi muốn chuyển phòng ngay trong ca khám, để tiếp tục khám ở phòng chuyên khoa phù hợp.
  - **AC-17**: Given ca đang `IN_PROGRESS`, When bác sĩ chọn "Chuyển phòng", Then ca gắn phòng mới, dữ liệu đã nhập được giữ nguyên, và lịch sử chuyển phòng được lưu để tính công theo phòng.
- **Role**: LeTan, Admin (điều phối); BacSi (chuyển phòng trong ca).
- **Edge case**: dữ liệu rỗng (không có phòng nào active); BN đã thanh toán tiền công khám của BS cũ → cần quy tắc phân bổ doanh thu.

### EPIC-5: Chẩn đoán chuẩn BHYT (G06)

- **US-08**: Là **bác sĩ**, tôi muốn ghi nhiều mã ICD-10 và chỉ định một mã là chẩn đoán chính, để hồ sơ BHYT hợp lệ.
  - **AC-18**: Given bác sĩ đã thêm ≥2 mã ICD-10, When lưu chẩn đoán mà chưa đánh dấu mã chính, Then hệ thống chặn lưu với thông báo "Phải chọn 1 chẩn đoán chính".
  - **AC-19**: Given ca khám đã có chẩn đoán chính, When export XML 4210, Then `MA_BENH` = mã chính và `MA_BENH_KHAC` = danh sách mã còn lại phân tách bởi dấu `;`.
  - **AC-20**: Given ca khám chưa có chẩn đoán nào, When bác sĩ bấm "Kết thúc khám", Then hệ thống chặn với thông báo "Chưa có chẩn đoán".
- **Role**: BacSi (nhập), KeToan/Admin (export BHYT).

---

## 5. Rủi ro tuân thủ

| Rủi ro | Điều khoản liên quan | GAP gây ra | Hệ quả |
|---|---|---|---|
| Chẩn đoán không tách chính/kèm theo | QĐ 4750/QĐ-BYT — Bảng 1 XML1, trường `MA_BENH`, `MA_BENH_KHAC` | G06 | Hồ sơ bị từ chối giám định, **xuất toán** chi phí đã chi trả |
| Không lưu bản chụp thẻ BHYT / giấy chuyển tuyến | QĐ 4750 + quy định hồ sơ thanh toán BHYT | G21 | Không giải trình được khi giám định hậu kiểm → thu hồi tiền |
| Vật tư y tế gộp chung danh mục thuốc | QĐ 4750 — XML2 (thuốc) tách khỏi XML3 (dịch vụ kỹ thuật/VTYT) | G23 | Sai cấu trúc file XML, lỗi định dạng khi nộp cổng giám định |
| Liều dùng không có cấu trúc (số lần/ngày, số ngày, đường dùng) | TT 27/2021/TT-BYT — nội dung bắt buộc của đơn thuốc điện tử | G08 | Đẩy ĐTQG bị từ chối; đơn thuốc không hợp lệ về hình thức |
| Sửa bệnh án đã hoàn tất không có vết | Luật KCB 2023 (hồ sơ bệnh án), TT 32/2023 về bệnh án điện tử | G03 | Mất giá trị pháp lý của bệnh án điện tử khi có tranh chấp/thanh tra |
| Không truy vết được thời điểm lấy mẫu / trả kết quả | Yêu cầu chất lượng phòng XN, tranh chấp với BN | G11 | Không có bằng chứng khi BN khiếu nại |
| Ghi âm buổi khám không có đồng ý của BN | NĐ 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân | G18 | Xử phạt hành chính, mất uy tín |
| Nhận diện khuôn mặt (dữ liệu sinh trắc học) | NĐ 13/2023 — dữ liệu cá nhân **nhạy cảm**, cần đồng ý riêng + DPIA | G19 | Rủi ro pháp lý cao — **khuyến nghị không triển khai giai đoạn này** |

---

## 6. Roadmap đề xuất 3 đợt

### Đợt 1 — "Đóng luồng khám & tiền" (P0, ~4–6 tuần)
| Hạng mục | GAP |
|---|---|
| Đợt chỉ định CLS + gate thanh toán | G01 |
| Ràng buộc giá gói khám khi chỉ định | G04 |
| Trạng thái `WAITING_CLS` + tab hàng đợi | G02 |
| Khoá bệnh án sau kết thúc khám + addendum có audit | G03 |
| Điều phối khám (đổi BS/phòng, huỷ) + chuyển phòng trong ca | G05 |
| Chẩn đoán chính / kèm theo + validate trước khi kết thúc khám | G06 |

**Tiêu chí nghiệm thu đợt 1**: chạy được kịch bản end-to-end "BN mua gói khám → chỉ định 2 đợt CLS → thu tiền đợt 2 → chờ kết quả → kết thúc khám → bệnh án khoá" mà không thao tác thủ công ngoài hệ thống.

### Đợt 2 — "Chất lượng dữ liệu lâm sàng & tuân thủ" (P1, ~5–7 tuần)
G07 (nhiều toa + thuốc ngoài), G08 (liều theo cữ + số ngày), G09 (import CSV kết quả), G10 (tiến độ phiếu), G11 (mốc thời gian CLS), G13 (barcode BN/mẫu), G15 (guard chưa lưu), G16 (huỷ/hoàn hoá đơn có lý do), G21 (đính kèm hồ sơ BN), G23 (danh mục vật tư tách riêng).

**Tiêu chí nghiệm thu đợt 2**: xuất được bộ XML 4210 đầy đủ XML1/2/3 pass validate của cổng giám định trên dữ liệu mẫu 20 hồ sơ; đẩy ĐTQG thành công với đơn có đủ liều/số ngày/đường dùng.

### Đợt 3 — "Trải nghiệm & mở rộng" (P1–P2, ~4 tuần)
G12 (nhắc hẹn đa loại dạng lịch), G20 (đặt lịch online), G24 (báo cáo sức khoẻ gói khám), G14 (mẫu in đơn nâng cao), G17 (tách số hoá đơn theo nguồn), G22 (ẩn giá/chọn in), G25 (nhóm queue theo phòng + auto-refresh).

**Chưa đưa vào roadmap**: G18 (ghi âm) — chỉ mở khi có quy trình lấy đồng ý + chính sách lưu trữ; G19 (Camera AI nhận diện) — **không đề xuất**, rủi ro dữ liệu sinh trắc học vượt lợi ích ở quy mô 2–5 bác sĩ.

---

## 7. Câu hỏi cần làm rõ với stakeholder

1. Chính sách thu tiền: phòng khám có cho phép làm CLS trước, thu tiền sau không? Nếu có thì role nào được duyệt cho nợ?
2. Gói khám: có giới hạn số lượt/hạn sử dụng cho từng dịch vụ trong gói không? Gói có được chuyển nhượng/hoàn tiền phần chưa dùng?
3. Bệnh án sau khi kết thúc: cho phép đính chính trong bao lâu (24h / 72h / không giới hạn)? Ai duyệt?
4. Điều phối khám: khi đổi bác sĩ sau khi đã thu tiền công khám, doanh thu/công ghi cho bác sĩ nào?
5. Danh mục vật tư: phòng khám có thanh toán VTYT với BHYT không, hay chỉ dùng nội bộ?
6. Ghi âm buổi khám: có nhu cầu thực tế không, và đã có mẫu văn bản đồng ý của người bệnh chưa?
