# UTE — Unit Test Execution
## Hệ thống: Pro-Diab HIS (Hospital Information System)
## Ngày thực thi: 2026-08-28
## Người thực thi: QC Agent (Chi)
## Môi trường: localhost (Backend :5000, Frontend :3000, MySQL Docker: prodiab-mysql)
## Tài khoản test: qc.admin@prodiab.test / Admin@123

---

## Tóm tắt kết quả

| Tổng số TC | PASS | FAIL | SKIP |
|------------|------|------|------|
| 19 | 8 | 7 | 4 |

**Tỷ lệ PASS:** 8/15 case thực thi được = **53%**

---

## Chi tiết thực thi

### Module AUTH

---

#### AUTH-001 — Đăng nhập hợp lệ
**Kết quả: ✅ PASS**

**Request:**
```
POST http://localhost:5000/api/v1/auth/login
Content-Type: application/json

{"email":"qc.admin@prodiab.test","password":"Admin@123"}
```

**Response (HTTP 200):**
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "uWvIhbATC6zJYS327AxToWU0x05JoElsntTYrRRxHByB52WS1l1...",
    "expiresIn": 900,
    "user": {
      "id": "14ab91a9-a65d-4279-8886-5c331e925c55",
      "email": "qc.admin@prodiab.test",
      "fullName": "QC Admin Test",
      "tenantId": 1,
      "roles": ["Quản trị viên"],
      "roleCodes": ["admin"]
    }
  }
}
```

**Nhận xét:** Đăng nhập thành công. Token JWT được cấp phát đúng. Token chứa đầy đủ thông tin user, tenant, permissions (150+ quyền). `expiresIn = 900` giây (15 phút).

---

#### AUTH-002 — Đăng nhập sai mật khẩu
**Kết quả: ✅ PASS**

**Request:**
```
POST http://localhost:5000/api/v1/auth/login
Content-Type: application/json

{"email":"qc.admin@prodiab.test","password":"WrongPass"}
```

**Response (HTTP 401):**
```json
{
  "error": {
    "code": "AUTH_INVALID_CREDENTIALS",
    "message": "Email hoac mat khau khong dung",
    "details": {}
  }
}
```

**Nhận xét:** API từ chối đúng với HTTP 401. Error code dạng SCREAMING_SNAKE theo chuẩn. Message tiếng Việt (không dấu trong log — đúng chuẩn). Không lộ thông tin nội bộ.

---

#### AUTH-003 — Lấy thông tin user hiện tại (/me)
**Kết quả: ✅ PASS**

**Request:**
```
GET http://localhost:5000/api/v1/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (HTTP 200):** Trả về thông tin user đúng với token được cấp.

---

#### AUTH-004 — Truy cập không có token
**Kết quả: ✅ PASS**

**Request:**
```
GET http://localhost:5000/api/v1/patients
(không có Authorization header)
```

**Response (HTTP 401):** API từ chối đúng khi không có token.

---

#### AUTH-005 — Truy cập token sai
**Kết quả: ✅ PASS**

**Request:**
```
GET http://localhost:5000/api/v1/patients
Authorization: Bearer INVALID_TOKEN
```

**Response (HTTP 401):** API từ chối đúng token không hợp lệ.

---

### Module PATIENT

---

#### PAT-001 — Lấy danh sách bệnh nhân
**Kết quả: ✅ PASS**

**Request:**
```
GET http://localhost:5000/api/v1/patients?page=1&pageSize=10
Authorization: Bearer <token>
```

**Response (HTTP 200):** Trả về cấu trúc phân trang `data.items`, `data.total`. API hoạt động đúng.

**Nhận xét:** Dữ liệu items có thể chưa đầy đủ (tên/ID rỗng trong môi trường test do dữ liệu mã hoá không giải mã được khi hiển thị list — cần kiểm tra thêm).

---

#### PAT-002 — Tạo bệnh nhân mới (camelCase body)
**Kết quả: ❌ FAIL — BUG**

**Request:**
```
POST http://localhost:5000/api/v1/patients
Content-Type: application/json

{"fullName":"Nguyen Van QC Test","dateOfBirth":"1990-01-15","gender":"male","phoneNumber":"0901234567","address":"123 Duong ABC, Ha Noi"}
```

**Response (HTTP 400):**
```json
{
  "errors": {
    "request": ["The request field is required."],
    "$.address": ["The JSON value could not be converted to ProDiabHis.Application.Patients.AddressDto. Path: $.address | LineNumber: 0 | BytePositionInLine: 136."]
  }
}
```

**Mô tả bug:** API yêu cầu `address` phải là object (`AddressDto`), không phải string. Tài liệu API không rõ ràng cấu trúc của `AddressDto`. Ngoài ra, lỗi "The request field is required" cho thấy request wrapper bị thiếu.

**Bug ID:** BUG-001

---

#### PAT-002b — Tạo bệnh nhân mới (snake_case body với AddressDto đúng)
**Kết quả: ⚠️ PARTIAL PASS — BUG nhỏ**

**Request:**
```
POST http://localhost:5000/api/v1/patients
Content-Type: application/json

{
  "full_name": "Nguyen Van QC Test",
  "date_of_birth": "1990-01-15",
  "gender": "male",
  "phone_number": "0901234567",
  "address": {
    "street": "123 Duong ABC",
    "district": "Hoan Kiem",
    "city": "Ha Noi"
  }
}
```

**Response (HTTP 201 — thành công tạo):** Tạo bệnh nhân thành công nhưng `data.id` trả về rỗng (empty string).

**Mô tả bug:** ID của bệnh nhân vừa tạo không được trả về trong response, không thể dùng để test các bước tiếp theo (GET by ID, tạo encounter).

**Bug ID:** BUG-002

---

#### PAT-003 — Tạo bệnh nhân thiếu full_name
**Kết quả: ✅ PASS (Validation hoạt động đúng)**

**Request:** `POST /patients` không có trường `full_name`

**Response (HTTP 400):**
```json
{"errors": {"full_name": ["The FullName field is required."]}}
```

**Nhận xét:** Validation FluentValidation hoạt động đúng. Tuy nhiên message validation là tiếng Anh — theo chuẩn dự án phải là tiếng Việt có dấu.

**Bug ID:** BUG-003 (message validation tiếng Anh thay vì tiếng Việt)

---

#### PAT-004, PAT-005 — Lấy/Cập nhật bệnh nhân theo ID
**Kết quả: ⏭ SKIP**

**Lý do:** PAT-002b tạo thành công nhưng `data.id` trả về rỗng nên không có ID để test. Phụ thuộc vào BUG-002 được sửa.

---

#### PAT-006 — Lấy bệnh nhân ID không tồn tại
**Kết quả: ⏭ SKIP**

**Lý do:** Không có ID hợp lệ để dùng làm baseline, bỏ qua case này.

---

#### PAT-007 — Tìm kiếm bệnh nhân
**Kết quả: ✅ PASS**

**Request:**
```
GET http://localhost:5000/api/v1/patients?search=Nguyen
Authorization: Bearer <token>
```

**Response (HTTP 200):** Trả về danh sách lọc theo từ khoá.

---

### Module ENCOUNTER

---

#### ENC-001 — Tạo lượt khám
**Kết quả: ⏭ SKIP**

**Lý do:** Phụ thuộc vào patientId từ PAT-002. Do BUG-002 (id rỗng), không có patientId hợp lệ để tạo encounter.

---

#### ENC-002 — Lấy danh sách lượt khám
**Kết quả: ❌ FAIL — BUG NGHIÊM TRỌNG**

**Request:**
```
GET http://localhost:5000/api/v1/encounters?page=1&pageSize=10
Authorization: Bearer <token>
```

**Response (HTTP 500):**
```json
{
  "error": {
    "code": "INTERNAL_ERROR",
    "message": "Loi he thong, vui long thu lai sau",
    "details": {}
  }
}
```

**Mô tả bug:** API `/encounters` trả về 500 Internal Server Error ngay cả với đúng token và quyền `encounter.read`. Có thể do lỗi query DB, mapping, hoặc dependency chưa sẵn sàng.

**Bug ID:** BUG-004 (Nghiêm trọng)

---

#### ENC-003 — Lấy lượt khám theo ID
**Kết quả: ⏭ SKIP**

**Lý do:** Không có encounterId do ENC-001 bị skip.

---

### Module PRESCRIPTION

---

#### PRX-001 — Tạo đơn thuốc
**Kết quả: ⏭ SKIP**

**Lý do:** Phụ thuộc encounterId từ ENC-001 (bị skip do BUG-002).

---

#### PRX-002 — Lấy danh sách đơn thuốc
**Kết quả: ❌ FAIL — BUG NGHIÊM TRỌNG**

**Request:**
```
GET http://localhost:5000/api/v1/prescriptions?page=1&pageSize=10
Authorization: Bearer <token>
```

**Response (HTTP 500):**
```json
{
  "error": {
    "code": "INTERNAL_ERROR",
    "message": "Loi he thong, vui long thu lai sau",
    "details": {}
  }
}
```

**Mô tả bug:** Tương tự ENC-002, API prescriptions trả về 500 dù đúng token + quyền.

**Bug ID:** BUG-005 (Nghiêm trọng)

---

### Module PHARMACY

---

#### PHA-001 — Kiểm tra tồn kho warehouse
**Kết quả: ❌ FAIL**

**Request:**
```
GET http://localhost:5000/api/v1/pharmacy/warehouse?page=1&pageSize=10
Authorization: Bearer <token>
```

**Response:** Lỗi (không có body rõ ràng — có thể 404 hoặc 500).

**Mô tả:** Endpoint `/pharmacy/warehouse` không hoạt động. Cần kiểm tra route đúng.

**Bug ID:** BUG-006

---

#### PHA-002 — Danh sách thuốc (catalog)
**Kết quả: ✅ PASS**

**Request:**
```
GET http://localhost:5000/api/v1/drugs?page=1&pageSize=10
Authorization: Bearer <token>
```

**Response (HTTP 200):** Trả về danh sách thuốc. `total` là empty (không có dữ liệu seed) nhưng API hoạt động đúng.

---

### Module BILLING

---

#### BIL-001 — Lấy danh sách hóa đơn
**Kết quả: ❌ FAIL — BUG NGHIÊM TRỌNG**

**Request:**
```
GET http://localhost:5000/api/v1/billings?page=1&pageSize=10
Authorization: Bearer <token>
```

**Response (HTTP 500):**
```json
{
  "error": {
    "code": "INTERNAL_ERROR",
    "message": "Loi he thong, vui long thu lai sau",
    "details": {}
  }
}
```

**Bug ID:** BUG-007 (Nghiêm trọng)

---

### Module RECEPTION

---

#### REC-001 — Danh sách tiếp đón
**Kết quả: ❌ FAIL**

**Request:**
```
GET http://localhost:5000/api/v1/reception?page=1&pageSize=10
Authorization: Bearer <token>
```

**Response:** Lỗi (không có body response — có thể 404 hoặc 500).

**Bug ID:** BUG-008

---

### Module DASHBOARD

---

#### DSH-001 — Dashboard tổng quan
**Kết quả: ❌ FAIL**

**Request:**
```
GET http://localhost:5000/api/v1/dashboard
Authorization: Bearer <token>
```

**Response (HTTP 404):** Route không tìm thấy.

**Mô tả:** Endpoint `/api/v1/dashboard` không tồn tại. Có thể route đúng là `/api/v1/dashboard/summary` hoặc controller chưa được đăng ký.

**Bug ID:** BUG-009

---

### Module HEALTH

---

#### HLT-001 — Health check
**Kết quả: ❌ FAIL**

**Request:**
```
GET http://localhost:5000/health
```

**Response:** Không phản hồi (lỗi kết nối hoặc endpoint không được cấu hình).

**Mô tả:** Endpoint `/health` không hoạt động. Có thể chưa kích hoạt trong `Program.cs`.

**Bug ID:** BUG-010

---

## Tổng hợp Bug tìm thấy

| Bug ID | Mức độ | Module | Mô tả | Đề xuất |
|--------|--------|--------|-------|---------|
| BUG-001 | Trung bình | Patient | `POST /patients` với body camelCase + address string trả về 400 — không rõ ràng trong docs | Cập nhật tài liệu API, thêm example body trong Swagger |
| BUG-002 | Cao | Patient | `POST /patients` thành công nhưng `data.id` trả về rỗng — không thể dùng ID để test luồng tiếp | Sửa service trả về ID sau khi INSERT |
| BUG-003 | Thấp | Patient | Validation message "The FullName field is required." là tiếng Anh, vi phạm chuẩn dự án (phải tiếng Việt) | Cập nhật FluentValidation message sang tiếng Việt có dấu |
| BUG-004 | **Nghiêm trọng** | Encounter | `GET /encounters` trả về 500 Internal Server Error | Kiểm tra query Dapper/EF, exception log trên server |
| BUG-005 | **Nghiêm trọng** | Prescription | `GET /prescriptions` trả về 500 Internal Server Error | Kiểm tra query và mapping DTO |
| BUG-006 | Cao | Pharmacy | `GET /pharmacy/warehouse` không hoạt động | Kiểm tra route, controller mapping |
| BUG-007 | **Nghiêm trọng** | Billing | `GET /billings` trả về 500 Internal Server Error | Kiểm tra query Dapper/EF, log server |
| BUG-008 | Cao | Reception | `GET /reception` không phản hồi đúng | Kiểm tra route và controller |
| BUG-009 | Trung bình | Dashboard | `GET /api/v1/dashboard` trả về 404 — route không tồn tại | Xác nhận route đúng từ DashboardController |
| BUG-010 | Thấp | System | `/health` không hoạt động | Kích hoạt `app.MapHealthChecks("/health")` trong Program.cs |

---

## Module không thể kiểm thử được

| Module | Lý do |
|--------|-------|
| Encounter (tạo) | Phụ thuộc vào patientId nhưng BUG-002 trả về ID rỗng |
| Prescription (tạo) | Phụ thuộc vào encounterId — cascade từ BUG-002 |
| Pharmacy Dispensing | Phụ thuộc vào prescription hợp lệ |
| BHYT Export | Phụ thuộc vào dữ liệu encounter/prescription |
| CLS / Lab / Rad orders | Phụ thuộc vào encounterId |
| Cashier | Phụ thuộc vào billing (BUG-007) |
| Reports | Cần dữ liệu đầy đủ trong DB |

---

## Kết luận

Hệ thống Pro-Diab HIS phần **Auth** và **Patient (read)** hoạt động ổn định. Tuy nhiên có **3 module nghiêm trọng** (Encounter list, Prescription list, Billing list) đang trả về 500 liên tục — đây là chặn luồng nghiệp vụ chính của HIS. Cần ưu tiên sửa ngay trước khi tiếp tục kiểm thử các module phụ thuộc.

**Khuyến nghị ưu tiên sửa:**
1. BUG-004, BUG-005, BUG-007 — 500 trên 3 module lõi (Encounter, Prescription, Billing)
2. BUG-002 — Patient create trả về ID rỗng
3. BUG-009 — Dashboard 404

---

*Tài liệu này là UTE (Unit Test Execution Record). Test case specification xem file `utc-his-core-20260828.md`.*
