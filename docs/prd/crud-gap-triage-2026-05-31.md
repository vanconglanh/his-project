# Triage 26 Action SKIP — CRUD Evidence 2026-05-31

> **PO/BA:** Đăng · **Ngày:** 2026-05-31 · **Nguồn:** [docs/test/crud-evidence.md](../test/crud-evidence.md)
> **Mục đích:** Phân loại 26 action SKIP do UI gap thành P0/P1/P2 làm input cho FE sprint planning.

---

## 1. Tổng quan

| Priority | Số action | % | Ý nghĩa |
|---|---|---|---|
| **P0 — MVP critical** | **11** | 42% | Phòng khám không vận hành đủ luồng nếu thiếu. Bắt buộc Sprint hiện tại. |
| **P1 — Sprint sau** | **10** | 38% | Quan trọng nhưng có workaround (DB admin, in PDF tay, refresh). |
| **P2 — Won't fix MVP** | **5** | 19% | Tính năng nâng cao, defer sau go-live. |
| **Tổng** | **26** | 100% | |

Effort ước P0: **~14 ngày FE-dev** (S=1, M=2, L=4). Đủ cho 1 sprint 2 tuần với 1 FE full-time.

---

## 2. Bảng chi tiết 26 action

| # | Module | Action | Lý do SKIP | Priority | User Story | Acceptance Criteria | Effort |
|---|---|---|---|---|---|---|---|
| 1 | AdminRoles | CREATE | khong co nut create | **P1** | Là Admin tôi muốn tạo vai trò mới để gán quyền tuỳ chỉnh ngoài 6 role mặc định. | Given trang /admin/roles, When click "Tạo vai trò", Then mở form nhập tên + mô tả + permission list, Lưu thành công hiển thị trong list. | M |
| 2 | AdminRoles | EditPermissions | action not found | **P1** | Là Admin tôi muốn sửa danh sách permission của một role để điều chỉnh phân quyền không cần redeploy. | Given row role, When click "Sửa quyền", Then mở dialog checkbox permission theo module, Save -> reload role mới apply ngay session sau. | L |
| 3 | AdminTenants | SuspendActivate | action not found | **P1** | Là Super Admin tôi muốn tạm khoá/mở phòng khám để xử lý khi tenant nợ phí. | Given row tenant, When click "Tạm khoá" / "Kích hoạt", Then status đổi + user của tenant bị chặn login với mã TENANT_SUSPENDED. | S |
| 4 | AdminUsers | CREATE | khong co nut create | **P0** | Là Admin tôi muốn tạo tài khoản nhân viên mới để onboard bác sĩ/lễ tân vào phòng khám. | Given /admin/users, When click "Thêm người dùng", Then form (họ tên, email, role, mật khẩu tạm), Lưu thành công user login được. | M |
| 5 | AdminUsers | AssignRoles | row dropdown khong co action | **P0** | Là Admin tôi muốn gán/đổi vai trò cho user để phân quyền truy cập module. | Given row user, When chọn "Gán vai trò", Then multi-select role, Save -> menu user thay đổi theo role mới. | S |
| 6 | AdminUsers | LockUnlock | row dropdown khong co action | **P1** | Là Admin tôi muốn khoá tài khoản nhân viên nghỉ việc để chặn truy cập dữ liệu bệnh nhân. | Given row user, When click "Khoá", Then is_active=false, user bị logout + không login lại được (mã USER_LOCKED). | S |
| 7 | BHYT | ViewDetail | action not found | **P1** | Là Kế toán BHYT tôi muốn xem chi tiết file XML 4210 đã export để đối soát với cổng giám định. | Given row export, When click "Xem chi tiết", Then mở trang hiển thị metadata + danh sách hồ sơ + link download XML. | M |
| 8 | Billing | ViewBill | row dropdown khong co action | **P0** | Là Thu ngân tôi muốn xem chi tiết hoá đơn để xác nhận dịch vụ trước khi thu tiền. | Given row billing, When click "Xem", Then hiển thị danh sách item + tổng tiền + trạng thái + nút thanh toán. | S |
| 9 | Billing | ReceivePayment | row dropdown khong co action | **P0** | Là Thu ngân tôi muốn ghi nhận thanh toán trên hoá đơn để cập nhật công nợ. | Given hoá đơn unpaid, When click "Thu tiền" + nhập số tiền + phương thức (cash/chuyển khoản), Then status=paid + tạo receipt. | M |
| 10 | Billing | PrintInvoice | row dropdown khong co action | **P0** | Là Thu ngân tôi muốn in hoá đơn A5/A4 để giao cho bệnh nhân. | Given hoá đơn, When click "In", Then mở preview PDF chuẩn + nút in trực tiếp browser. | M |
| 11 | Cashier | OpenBill | row dropdown khong co action | **P0** | Là Thu ngân tôi muốn mở phiếu thu từ ca trực để xử lý thanh toán cho lượt khám. | Given row encounter chờ thu, When click "Mở phiếu", Then sinh billing draft từ encounter + chuyển sang màn thu tiền. | M |
| 12 | Cashier | ReceivePayment | row dropdown khong co action | **P0** | Là Thu ngân tôi muốn nhận tiền trực tiếp từ ca trực mà không qua màn Billing để rút ngắn thao tác. | Given row, When click "Thu tiền" -> dialog inline nhập số tiền + method, Lưu cập nhật ca trực + billing. | M |
| 13 | Cashier | PrintReceipt | row dropdown khong co action | **P0** | Là Thu ngân tôi muốn in biên lai khổ K80 ngay sau khi thu tiền. | Given giao dịch vừa thu, When click "In biên lai", Then sinh PDF/HTML K80 + auto open print dialog. | M |
| 14 | Drug | Search | action not found | **P2** | Là Dược sĩ tôi muốn search nhanh trong danh mục thuốc theo tên/hoạt chất. | Given /drugs, When gõ vào ô search, Then list filter realtime (debounce 300ms) theo name/active_ingredient. | S |
| 15 | Encounter | AddVital | action not found | **P0** | Là Điều dưỡng/Bác sĩ tôi muốn nhập sinh hiệu (mạch/HA/T°/SpO2) vào lượt khám. | Given encounter open, When click tab "Sinh hiệu" + nhập số liệu, Save -> hiển thị trong timeline encounter. | M |
| 16 | Encounter | AddDiagnosis | action not found | **P0** | Là Bác sĩ tôi muốn ghi chẩn đoán ICD-10 cho lượt khám để hoàn tất hồ sơ và export BHYT. | Given encounter, When chọn mã ICD-10 (autocomplete) + ghi chú, Save -> diagnosis hiển thị + bắt buộc trước khi Close. | M |
| 17 | Encounter | CloseEncounter | action not found | **P0** | Là Bác sĩ tôi muốn đóng lượt khám sau khi hoàn tất chẩn đoán + kê đơn để chuyển sang Thu ngân. | Given encounter có diagnosis, When click "Hoàn tất khám", Then status=finished + push sang queue Cashier + lock edit. | S |
| 18 | PharmacyDispense | TAB:Queue | tab not found | **P1** | Là Dược sĩ tôi muốn xem hàng đợi đơn thuốc chờ phát để xử lý theo thứ tự. | Given /pharmacy/dispense, When click tab "Hàng đợi", Then list đơn status=pending sắp xếp theo thời gian kê. | S |
| 19 | PharmacyDispense | TAB:History | tab not found | **P1** | Là Dược sĩ tôi muốn xem lịch sử phát thuốc đã hoàn thành để tra cứu/đối chiếu. | Given /pharmacy/dispense, When click tab "Lịch sử", Then list đơn dispensed có filter ngày + tên BN. | S |
| 20 | PharmacyStock | TAB:Adjustment | tab not found | **P1** | Là Dược sĩ tôi muốn xem lịch sử điều chỉnh tồn kho để truy vết kiểm kê. | Given /pharmacy, When click tab "Điều chỉnh", Then list adjustment có loại (+/-), lý do, người tạo. | S |
| 21 | PharmacyStock | CreateAdjustment | action not found | **P1** | Là Dược sĩ tôi muốn tạo phiếu điều chỉnh tồn kho khi kiểm kê phát hiện chênh lệch. | Given tab Adjustment, When click "Tạo điều chỉnh" + chọn thuốc/lot + số lượng ±/lý do, Save -> tồn kho cập nhật + log audit. | M |
| 22 | Prescription | AddDrugItem | action not found | **P0** | Là Bác sĩ tôi muốn thêm dòng thuốc vào đơn để hoàn tất kê đơn cho bệnh nhân. | Given form đơn, When click "Thêm thuốc" + chọn drug + liều + cách dùng + số lượng, Then dòng hiển thị trong bảng + tính tổng. | M |
| 23 | Prescription | Submit | action not found | **P0** | Là Bác sĩ tôi muốn submit đơn thuốc để đẩy lên Đơn thuốc Quốc gia và in QR. | Given đơn có ≥1 drug item + diagnosis, When click "Gửi đơn", Then call API ĐTQG -> nhận ma_don_thuoc + status=submitted + nút in QR. | L |
| 24 | Reception | PrintTicket | action not found | **P2** | Là Lễ tân tôi muốn in vé số thứ tự khổ K58 cho bệnh nhân chờ khám. | Given check-in thành công, When click "In số", Then sinh phiếu HTML K58 (STT, phòng, tên BN) + auto print. | M |
| 25 | ServiceCatalog | UpdatePrice | action not found | **P2** | Là Admin tôi muốn cập nhật giá dịch vụ khi có thay đổi. | Given row service, When click "Sửa giá" + nhập giá mới + ngày hiệu lực, Save -> giá mới apply cho encounter mới. | S |
| 26 | Supplier | Update | action not found | **P2** | Là Admin tôi muốn sửa thông tin nhà cung cấp (địa chỉ/MST/SĐT). | Given row supplier, When click "Sửa", Then form prefill + Save cập nhật. | S |

---

## 3. Phân bổ theo Priority

### P0 — MVP critical (11 action, ~14 ngày)

| Module | Actions | Effort |
|---|---|---|
| AdminUsers | CREATE (M), AssignRoles (S) | 3 |
| Billing | ViewBill (S), ReceivePayment (M), PrintInvoice (M) | 5 |
| Cashier | OpenBill (M), ReceivePayment (M), PrintReceipt (M) | 6 |
| Encounter | AddVital (M), AddDiagnosis (M), CloseEncounter (S) | 5 |
| Prescription | AddDrugItem (M), Submit (L) | 6 |

> Lý do: Đây là **xương sống critical path** Tiếp đón → Khám (vital/diagnosis/close) → Kê đơn (add drug/submit) → Thu ngân (open/receive/print). Thiếu bất kỳ action nào trong nhóm này, phòng khám **không hoàn thành được 1 lượt khám end-to-end** trong UI. Onboard nhân viên mới (AdminUsers CREATE/AssignRoles) cũng là pre-requisite ngày đầu go-live.

### P1 — Sprint sau (10 action, ~12 ngày)

AdminRoles CREATE/EditPermissions, AdminTenants SuspendActivate, AdminUsers LockUnlock, BHYT ViewDetail, PharmacyDispense TAB:Queue/History, PharmacyStock TAB:Adjustment/CreateAdjustment.

> Có workaround: role/permission có thể seed sẵn DB; lock user qua SQL update; BHYT detail có thể đọc file XML download; điều chỉnh tồn kho 1-2 tuần đầu chấp nhận chỉnh tay.

### P2 — Won't fix MVP (5 action)

Drug Search, Reception PrintTicket, ServiceCatalog UpdatePrice, Supplier Update, (đã chuyển 1 mục PrintTicket).

> Defer sau go-live: list thuốc <500 dòng filter manual OK; phòng khám nhỏ 2-5 BS không cần vé số (gọi tên trực tiếp); giá dịch vụ + supplier sửa qua admin DB chấp nhận được tháng đầu.

---

## 4. Sprint plan đề xuất

### Sprint hiện tại (2026-06-01 → 2026-06-14, 2 tuần)
- **Mục tiêu:** Đóng toàn bộ P0 (11 action / ~14 ngày FE).
- **Thứ tự ưu tiên trong sprint:**
  1. Tuần 1: Encounter (Vital/Diagnosis/Close) + Prescription (AddDrugItem/Submit) → đóng critical path lâm sàng.
  2. Tuần 2: Billing + Cashier (6 action) → đóng luồng tài chính.
  3. Cuối tuần 2: AdminUsers (CREATE/AssignRoles) — nhanh, có thể chen.
- **DoD sprint:** spec `crud-actions.spec.ts` PASS rate ≥ 75% (từ 54% lên 73-77%).

### Sprint kế (2026-06-15 → 2026-06-28)
- Đóng P1 (10 action / ~12 ngày) + buffer cho bug regression từ P0.
- Song song BE fix BUG-CRUD-01 (Cashier shift timeout).

### Sau go-live
- P2 (5 action) đưa vào backlog Q3.

---

## 5. Phụ thuộc & risk

- **Prescription Submit (L)** phụ thuộc tích hợp ĐTQG token cho tenant. Nếu chưa có sandbox token thật, mock response để FE không block.
- **PrintInvoice/PrintReceipt** cần BE expose endpoint render PDF (hoặc dùng `react-to-print` client-side). Cần Architect xác nhận hướng tiếp cận trong tuần đầu sprint.
- **Encounter CloseEncounter** ràng buộc business: phải có ≥1 diagnosis ICD-10 (BYT yêu cầu). FE phải block UI + BE validate.
- 100% action SKIP đều UI gap, **không có lỗi BE logic** → giảm rủi ro coordination cross-team.
