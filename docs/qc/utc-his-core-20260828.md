# UTC — Unit Test Case
## Hệ thống: Pro-Diab HIS (Hospital Information System)
## Phạm vi: API Core — Auth / Bệnh nhân / Lượt khám / Đơn thuốc / Dược / Thanh toán / Tiếp đón / Dashboard
## Ngày lập: 2026-08-28
## Người lập: QC Agent (Chi)
## Phiên bản: v1.0

---

## 1. Môi trường kiểm thử

| Mục | Giá trị |
|-----|---------|
| Backend URL | http://localhost:5000 |
| Frontend URL | http://localhost:3000 |
| Database | MySQL 8.0, DB: prodiab_his |
| Tài khoản QC | qc.admin@prodiab.test / Admin@123 |
| Vai trò | Quản trị viên (admin) |
| Tenant ID | 1 |
| Ngày chạy | 2026-08-28 |

---

## 2. Danh sách Test Case

### Module AUTH — Xác thực

| ID | Tên test case | Mô tả | Điều kiện tiên quyết | Dữ liệu đầu vào | Kết quả mong đợi | Mức độ |
|----|---------------|-------|----------------------|-----------------|------------------|--------|
| AUTH-001 | Đăng nhập hợp lệ | Đăng nhập với email/mật khẩu đúng | API backend đang chạy | `POST /api/v1/auth/login` `{"email":"qc.admin@prodiab.test","password":"Admin@123"}` | HTTP 200, trả về `accessToken`, `refreshToken`, thông tin `user` | Cao |
| AUTH-002 | Đăng nhập sai mật khẩu | Đăng nhập với mật khẩu sai | API backend đang chạy | `POST /api/v1/auth/login` `{"email":"qc.admin@prodiab.test","password":"WrongPass"}` | HTTP 401, `error.code = AUTH_INVALID_CREDENTIALS`, message tiếng Việt | Cao |
| AUTH-003 | Lấy thông tin user hiện tại | Gọi `/me` với token hợp lệ | Đã có accessToken | `GET /api/v1/me` Headers: `Authorization: Bearer <token>` | HTTP 200, trả về thông tin user (email, fullName, roles) | Cao |
| AUTH-004 | Truy cập không có token | Gọi API bảo vệ không có Authorization header | Không cần token | `GET /api/v1/patients` (không có Authorization header) | HTTP 401 Unauthorized | Cao |
| AUTH-005 | Truy cập token sai/hết hạn | Gọi API với token giả | Không cần token | `GET /api/v1/patients` `Authorization: Bearer INVALID_TOKEN` | HTTP 401 Unauthorized | Cao |

---

### Module PATIENT — Bệnh nhân

| ID | Tên test case | Mô tả | Điều kiện tiên quyết | Dữ liệu đầu vào | Kết quả mong đợi | Mức độ |
|----|---------------|-------|----------------------|-----------------|------------------|--------|
| PAT-001 | Lấy danh sách bệnh nhân | Phân trang danh sách bệnh nhân | Đã đăng nhập | `GET /api/v1/patients?page=1&pageSize=10` | HTTP 200, `data.items` là mảng, `data.total >= 0` | Cao |
| PAT-002 | Tạo bệnh nhân mới (body đầy đủ) | Tạo hồ sơ bệnh nhân mới với đầy đủ trường | Đã đăng nhập | `POST /api/v1/patients` body dạng JSON snake_case với `full_name`, `date_of_birth`, `gender`, `phone_number`, `address` (object) | HTTP 201, trả về `data.id` của bệnh nhân vừa tạo | Cao |
| PAT-003 | Tạo bệnh nhân thiếu trường bắt buộc | Gửi body thiếu `full_name` | Đã đăng nhập | `POST /api/v1/patients` không có trường full_name | HTTP 400, `errors.full_name` có message validation | Trung bình |
| PAT-004 | Lấy bệnh nhân theo ID | Lấy chi tiết một bệnh nhân | Đã tạo bệnh nhân | `GET /api/v1/patients/{id}` | HTTP 200, trả về đúng bệnh nhân | Cao |
| PAT-005 | Cập nhật bệnh nhân | Sửa thông tin bệnh nhân | Đã tạo bệnh nhân | `PUT /api/v1/patients/{id}` với dữ liệu mới | HTTP 200, thông tin được cập nhật | Cao |
| PAT-006 | Lấy bệnh nhân ID không tồn tại | ID không có trong DB | Đã đăng nhập | `GET /api/v1/patients/99999999` | HTTP 404, error phù hợp | Trung bình |
| PAT-007 | Tìm kiếm bệnh nhân theo tên | Dùng query search | Đã đăng nhập, có dữ liệu | `GET /api/v1/patients?search=Nguyen` | HTTP 200, kết quả lọc đúng | Trung bình |

---

### Module ENCOUNTER — Lượt khám

| ID | Tên test case | Mô tả | Điều kiện tiên quyết | Dữ liệu đầu vào | Kết quả mong đợi | Mức độ |
|----|---------------|-------|----------------------|-----------------|------------------|--------|
| ENC-001 | Tạo lượt khám mới | Tạo encounter cho bệnh nhân | Đã có patientId | `POST /api/v1/encounters` `{"patientId":"<id>","chiefComplaint":"Ho sot 3 ngay","visitType":"outpatient"}` | HTTP 201, trả về `data.id` của lượt khám | Cao |
| ENC-002 | Lấy danh sách lượt khám | Phân trang danh sách | Đã đăng nhập | `GET /api/v1/encounters?page=1&pageSize=10` | HTTP 200, `data.items` là mảng | Cao |
| ENC-003 | Lấy lượt khám theo ID | Chi tiết một encounter | Đã có encounterId | `GET /api/v1/encounters/{id}` | HTTP 200, dữ liệu đầy đủ | Cao |

---

### Module PRESCRIPTION — Đơn thuốc

| ID | Tên test case | Mô tả | Điều kiện tiên quyết | Dữ liệu đầu vào | Kết quả mong đợi | Mức độ |
|----|---------------|-------|----------------------|-----------------|------------------|--------|
| PRX-001 | Tạo đơn thuốc mới | Kê đơn cho một lượt khám | Đã có encounterId | `POST /api/v1/prescriptions` với danh sách thuốc | HTTP 201, `data.id` của đơn thuốc | Cao |
| PRX-002 | Lấy danh sách đơn thuốc | Phân trang | Đã đăng nhập | `GET /api/v1/prescriptions?page=1&pageSize=10` | HTTP 200, `data.items` là mảng | Cao |

---

### Module PHARMACY — Dược / Kho thuốc

| ID | Tên test case | Mô tả | Điều kiện tiên quyết | Dữ liệu đầu vào | Kết quả mong đợi | Mức độ |
|----|---------------|-------|----------------------|-----------------|------------------|--------|
| PHA-001 | Kiểm tra tồn kho | Lấy danh sách tồn kho warehouse | Đã đăng nhập | `GET /api/v1/pharmacy/warehouse?page=1&pageSize=10` | HTTP 200, danh sách mặt hàng tồn kho | Cao |
| PHA-002 | Danh sách thuốc (catalog) | Lấy danh mục thuốc | Đã đăng nhập | `GET /api/v1/drugs?page=1&pageSize=10` | HTTP 200, `data.items` hoặc array thuốc | Cao |

---

### Module BILLING — Thanh toán / Hóa đơn

| ID | Tên test case | Mô tả | Điều kiện tiên quyết | Dữ liệu đầu vào | Kết quả mong đợi | Mức độ |
|----|---------------|-------|----------------------|-----------------|------------------|--------|
| BIL-001 | Lấy danh sách hóa đơn | Phân trang hóa đơn | Đã đăng nhập | `GET /api/v1/billings?page=1&pageSize=10` | HTTP 200, danh sách hóa đơn | Cao |

---

### Module RECEPTION — Tiếp đón

| ID | Tên test case | Mô tả | Điều kiện tiên quyết | Dữ liệu đầu vào | Kết quả mong đợi | Mức độ |
|----|---------------|-------|----------------------|-----------------|------------------|--------|
| REC-001 | Danh sách tiếp đón | Lấy hàng chờ tiếp đón | Đã đăng nhập | `GET /api/v1/reception?page=1&pageSize=10` | HTTP 200, danh sách bệnh nhân chờ | Cao |

---

### Module DASHBOARD

| ID | Tên test case | Mô tả | Điều kiện tiên quyết | Dữ liệu đầu vào | Kết quả mong đợi | Mức độ |
|----|---------------|-------|----------------------|-----------------|------------------|--------|
| DSH-001 | Dashboard tổng quan | Lấy dữ liệu dashboard | Đã đăng nhập | `GET /api/v1/dashboard` | HTTP 200, thông tin thống kê | Cao |

---

### Module HEALTH — Kiểm tra sức khỏe hệ thống

| ID | Tên test case | Mô tả | Điều kiện tiên quyết | Dữ liệu đầu vào | Kết quả mong đợi | Mức độ |
|----|---------------|-------|----------------------|-----------------|------------------|--------|
| HLT-001 | Health check endpoint | Kiểm tra API đang sống | API đang chạy | `GET /health` | HTTP 200, status "Healthy" | Cao |

---

## 3. Cấu trúc Backend Controllers tìm thấy

Tổng cộng **57 controller** được phát hiện:
- `AuthController` — đăng nhập, refresh token
- `PatientsController` — quản lý bệnh nhân
- `EncountersController` — lượt khám
- `PrescriptionsController` — đơn thuốc
- `PharmacyWarehouseController`, `PharmacyDispensingController` — kho dược, cấp phát
- `DrugsController` — danh mục thuốc
- `BillingsController`, `CashierController`, `PaymentsController` — thanh toán
- `ReceptionController` — tiếp đón
- `DashboardController` — dashboard
- `BhytExportController`, `BhytReconcileController` — BHYT
- `ReportsController` — báo cáo
- `UsersController`, `RolesController` — quản lý người dùng
- Và 40+ controller khác (CLS, Lab, Rad, FHIR, CDSS, v.v.)

---

## 4. Frontend Routes tìm thấy

Tổng cộng **~70+ route** trên Next.js App Router:
- `/login`, `/forgot-password`, `/reset-password` — auth
- `/dashboard` — tổng quan
- `/patients`, `/patients/[id]`, `/patients/new` — bệnh nhân
- `/encounters`, `/encounters/[id]`, `/encounters/new` — lượt khám
- `/prescriptions`, `/prescriptions/new`, `/prescriptions/[id]` — đơn thuốc
- `/pharmacy`, `/pharmacy/dispense`, `/pharmacy/grn/new` — dược
- `/billings`, `/cashier` — thanh toán
- `/reception` — tiếp đón
- `/bhyt` — BHYT
- `/reports` — báo cáo
- `/admin` — quản trị

---

*Tài liệu này là UTC (Unit Test Case Specification). Kết quả thực thi xem file `ute-his-core-20260828.md`.*
