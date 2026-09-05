# Đi lại luồng nghiệp vụ qua API — his.diab.vn (2026-09-04)

Thực hiện bằng tài khoản test thật (`*.test@prodiab.test` / `Test@123`), gọi API production
staging theo đúng thứ tự nghiệp vụ, không insert thẳng DB.

## Kết quả từng bước

| # | Bước | Vai trò | HTTP | Kết quả |
|---|------|---------|------|---------|
| 1 | `POST /reception/check-in` | Lễ tân | 201 | PASS |
| 2 | `PUT /reception/queue/{id}/call` | Lễ tân | 200 | PASS |
| 3 | `POST /reception/queue/{id}/admit` | Lễ tân | 200 | PASS — sinh lượt khám |
| 4 | `POST /encounters` (tạo trực tiếp) | Lễ tân | 201 | PASS |
| 5 | `POST /encounters/{id}/start` | Bác sĩ | 200 | PASS |
| 6 | `POST /encounters/{id}/cls-rounds` | Bác sĩ | 201 | PASS (có lỗi dữ liệu, xem BUG-F02/F03) |
| 7 | `POST /cls-rounds/{id}/submit` | Bác sĩ | 200 | PASS |
| 8 | Thu ngân tìm đợt CLS chờ thu | Kế toán | — | **FAIL — xem BUG-F01** |

## BUG-F01 (Blocker) — Không màn hình nào lập được hóa đơn, thu ngân vĩnh viễn rỗng

### Bản chất vấn đề

Backend **không hỏng**. `POST /billings` với `include_dispensing = true` gom đúng và đủ:

```
LAB  | ACR      | Albumin/Creatinine Ratio |  65.000
LAB  | ALT      | ALT (SGPT)               |  30.000
RAD  | XRAY_CXR | X-quang ngực thẳng       |  80.000
DRUG |          | Amlodipine 5mg (60 viên) |  52.800
                                   subtotal = 227.800
```

Vấn đề là **không có gì kích hoạt việc lập hóa đơn**:

1. Chốt đợt CLS (`POST /cls-rounds/{id}/submit`) → không sinh billing, `billing_id` vẫn NULL.
2. Cấp phát thuốc (`POST /pharmacy/dispense/{id}`) → trừ tồn kho đúng nhưng không sinh billing.
3. Hook `useCreateBilling` đã viết trong `frontend/lib/hooks/use-billing.ts:49` nhưng
   **không màn hình nào import/sử dụng** (`grep -rn "useCreateBilling" app` → 0 kết quả).
4. Màn hình thu ngân chỉ liệt kê hóa đơn **đã tồn tại**. Không có màn hình nào liệt kê
   lượt khám đã dùng dịch vụ mà chưa lập hóa đơn.

→ Trên UI không ai bấm lập hóa đơn được, nên hóa đơn không bao giờ ra đời, nên thu ngân
không thấy gì — kể cả CLS lẫn thuốc. Toàn bộ doanh thu của lượt khám bị thất thoát.

Đây chính là triệu chứng tester báo: *"đổi qua làm BS cho CLS thì không thấy thông tin CLS
bên thu ngân"*. Không phải riêng CLS — thuốc cũng vậy.

### Chứng cứ trong DB

2 đợt CLS cũ ở trạng thái mồ côi: `payment_status = PAID` nhưng `billing_id = NULL`
— đã đánh dấu thu tiền mà không hóa đơn nào tồn tại.

### Đề xuất

- Gắn nút "Lập hóa đơn" vào màn hình lượt khám / thu ngân, dùng `useCreateBilling` đã có sẵn.
- Bổ sung endpoint + màn hình "hàng chờ thu ngân": liệt kê lượt khám có dịch vụ chưa thanh toán.
- Cân nhắc tự sinh/cập nhật billing khi chốt đợt CLS và khi cấp phát thuốc, gán ngược
  `cls_round.billing_id`.

## BUG-F02 (Medium) — Chấp nhận mã dịch vụ CĐHA không tồn tại, định giá 0đ

Gửi `procedure_code = "XQ-NGUC"` (mã không có trong `diab_his_dict_rad_procedures`) →
API trả **201 Created**, tạo chỉ định với `unit_price = 0.00` và không cộng vào `total_amount`.

Gửi đúng mã `XRAY_CXR` → `unit_price = 80.000`, `total_amount = 80.000` (đúng).

Vấn đề là API không validate mã dịch vụ: mã sai vẫn tạo được chỉ định và lặng lẽ tính 0đ,
thay vì trả lỗi. Bệnh nhân sẽ được chụp mà không bị tính tiền.

**Đề xuất:** validate `procedure_code`/`test_code` với danh mục, trả `VALIDATION_ERROR` nếu không khớp.

## ~~BUG-F03 — Mất dấu tiếng Việt~~ (ĐÍNH CHÍNH: không phải lỗi sản phẩm)

Lần chạy đầu thấy `"X-quang ng?c th?ng"`, `"N??c ti?u"` nên nghi hỏng encoding. **Sai.**
Nguyên nhân là terminal Windows (cp932/CP1252) của máy test làm hỏng chuỗi trong tham số
`curl -d` **trước khi** gửi đi.

Kiểm chứng: ghi payload ra file bằng UTF-8 rồi gửi `--data-binary` → chuỗi round-trip đúng
hoàn toàn. Đọc thẳng từ MySQL: `chief_complaint = "Mệt mỏi, khát nước nhiều về đêm"`, và
bản ghi có sẵn `"Creatinine huyết thanh" / "Máu tĩnh mạch"` vẫn nguyên dấu.

Connection string đã có `CharSet=utf8mb4`, server `character_set_server = utf8mb4`,
mọi cột `utf8mb4_0900_ai_ci`. **Tiếng Việt hoạt động đúng.**

> Bài học cho người test: trên Windows luôn gửi payload tiếng Việt qua file UTF-8 với
> `--data-binary @file.json`, không dùng `curl -d '...'` inline.

## BUG-F04 (Medium) — `max_per_day` của phòng khám không được enforce

Các phòng cấu hình `max_per_day = 1`. Check-in bệnh nhân thứ hai vào cùng phòng trong cùng ngày
vẫn trả 201, không trả `RECEPTION_ROOM_FULL` (409) như controller đã dự liệu.

## BUG-F05 (Medium) — `POST /encounters` không gán `branch_id`

Lượt khám tạo qua endpoint này trả `branch_id: null`. Trùng khớp với 12/15 lượt khám ngày 02/09
trong DB có `branch_id` NULL → không hiển thị trên các màn hình lọc theo chi nhánh.

## BUG-F06 (Medium) — Login trả token rỗng thay vì báo lỗi

`admin@prodiab.local` / `admin123` → HTTP 200 kèm `accessToken: ""`, `refreshToken: ""`,
`expiresIn: 0` nhưng vẫn có object `user`. Client sẽ tưởng đăng nhập thành công rồi hỏng ở
request kế tiếp. Nên trả lỗi tường minh.

## Ghi chú cho người test

API dùng **snake_case** cho JSON body. Gửi `patientId` sẽ nhận `VALIDATION_ERROR` với thông báo
`"PatientId là bắt buộc"` — thông báo nói tên field PascalCase trong khi API chỉ nhận snake_case,
dễ làm người test tưởng đã gửi đúng.

## Dữ liệu test đã tạo (đi đúng luồng, dùng để test tiếp)

- Lượt khám `8f6851aa-bdd3-4101-9342-a16cf46a7194` — BN00002 Nguyễn Thị Lan, đang IN_PROGRESS
- Đợt CLS `f1479f4f-2f16-4bbf-ae42-3f5fb48f0b4e` — 2 XN + 1 CĐHA, SUBMITTED/UNPAID
- Vé hàng đợi `91db3b42-...` — BN00003, còn WAITING để test tiếp đón

## BUG-F07 (Medium) — `POST /prescriptions` trả về `items: []` dù đã lưu

Tạo đơn với 1 thuốc → response 201 nhưng `items` rỗng. `GET /prescriptions/{id}` ngay sau đó
trả `items: 1`. Dữ liệu lưu đúng, chỉ response của lệnh tạo thiếu. FE điều hướng sang trang
chi tiết bằng dữ liệu trả về sẽ hiển thị đơn rỗng — trùng dạng bug đã fix ở commit 5ae6741.

## BUG-F08 (Medium) — Cấp phát thuốc không ghi người thực hiện

`POST /pharmacy/dispense/{id}` trả `dispensed_by: null`, `dispensed_by_name: null` dù gọi
bằng token dược sĩ hợp lệ. Không truy được ai đã phát thuốc — vi phạm yêu cầu audit log
đối với dữ liệu bệnh nhân.

## BUG-F09 (Medium) — `warehouse_id` bắt buộc nhưng bảng tồn kho không có khái niệm kho

`DispenseRequest.WarehouseId` là trường bắt buộc, và có bảng `pha_warehouses`
(Kho chính / Kho lẻ cấp phát). Nhưng bảng tồn kho `diab_his_pha_stock` **không có cột
`warehouse_id`**, chỉ có `branch_id`. Truyền `warehouse_id = "1"` vẫn trừ tồn kho theo
chi nhánh. Nghĩa là chọn kho nào cũng như nhau — mô hình kho khai báo ra nhưng không dùng.

## Các bước ĐÃ PASS ở nhánh kê đơn — dược

| Bước | Vai trò | HTTP | Kết quả |
|------|---------|------|---------|
| `GET /lab-results/pending-items` | KTV | 200 | PASS — thấy đúng 2 XN vừa chỉ định |
| `POST /prescriptions` | Bác sĩ | 201 | PASS (xem F07) |
| `POST /prescriptions/{id}/sign` | Bác sĩ | 200 | PASS |
| `GET /pharmacy/dispense/queue` | Dược sĩ | 200 | PASS — đơn đã ký vào hàng chờ |
| `POST /pharmacy/dispense/{id}` | Dược sĩ | 201 | PASS — tồn kho LOT-A002 300 → 240 |
| `POST /billings` | Kế toán | 201 | PASS — gom đủ 227.800đ |

Chữ ký số yêu cầu `signature_data` là **base64 hợp lệ**; gửi chuỗi thường trả
409 `PRESCRIPTION_SIGNATURE_FAILED`.

## BUG-F10 (Blocker) — Thu tiền không mở khóa được đợt CLS, KTV không nhập được kết quả

Chuỗi kiểm chứng đầy đủ:

1. KTV nhập kết quả XN → `400 CLS_ORDER_UNPAID` — *"Đợt chỉ định chưa thanh toán"*.
2. Kế toán lập hóa đơn 227.800đ (gồm cả đợt CLS) và thu đủ tiền:
   `POST /payments` → 201, `status = COMPLETED`, hóa đơn `paid_amount = 227.800`, `balance = 0`, `status = PAID`.
3. Kiểm tra lại đợt CLS: **`payment_status` vẫn `UNPAID`, `billing_id` vẫn NULL**.
4. KTV nhập kết quả lần nữa → vẫn `400 CLS_ORDER_UNPAID`.

Thanh toán hóa đơn **không cập nhật ngược** trạng thái đợt chỉ định. Phải gọi riêng
`POST /cls-rounds/{id}/pay` mới thông — và endpoint này bắt `amount` phải khớp **tổng tiền
đợt CLS** (95.000), không phải tổng hóa đơn:

```
POST /cls-rounds/{id}/pay  amount=175000 → 400 BILLING_AMOUNT_MISMATCH
                                            {"expected": 95000, "actual": 175000}
POST /cls-rounds/{id}/pay  amount=95000  → 200, payment_status = PAID
POST /lab-results                         → 201  (KTV nhập được ngay sau đó)
```

### Vì sao nghiêm trọng

Hóa đơn gộp (CLS + thuốc + dịch vụ) và thanh toán đợt CLS là **hai cơ chế song song không nói
chuyện với nhau**. Trên thực tế thu ngân thu một lần 227.800đ, nhưng hệ thống vẫn coi đợt CLS
chưa trả → phòng xét nghiệm từ chối làm. Muốn thông, thu ngân phải thao tác thêm một bước thu
95.000đ riêng cho đợt, trong khi tiền đó đã nằm trong 227.800đ vừa thu → rủi ro thu trùng và
sai sổ sách.

Cộng với BUG-F01 (không màn hình nào lập được hóa đơn), nhánh CLS **chết hoàn toàn trên UI**:
bác sĩ chỉ định được nhưng không ai thu được tiền, nên không kết quả xét nghiệm nào có thể
được nhập vào hệ thống.

### Đề xuất

Khi thanh toán hóa đơn, cập nhật ngược mọi `cls_round` thuộc lượt khám đó sang `PAID` và gán
`billing_id`. Bỏ ràng buộc `amount` phải khớp riêng tổng đợt, hoặc đối chiếu theo phần
dòng CLS trong hóa đơn thay vì tổng hóa đơn.

## Luồng đã chạy trọn vẹn (sau khi lách qua F01 + F10 bằng API)

| Bước | Vai trò | Kết quả |
|------|---------|---------|
| Tiếp đón → gọi số → chuyển vào khám | Lễ tân | PASS |
| Mở lượt khám | Bác sĩ | PASS |
| Chỉ định 2 XN + 1 CĐHA, chốt đợt | Bác sĩ | PASS |
| Kê đơn 1 thuốc, ký số | Bác sĩ | PASS |
| Cấp phát 60 viên, trừ tồn kho | Dược sĩ | PASS |
| Lập hóa đơn gộp 227.800đ | Kế toán | PASS (chỉ qua API) |
| Thu tiền mặt, hóa đơn về PAID | Kế toán | PASS |
| Đánh dấu đợt CLS đã thu | Kế toán | PASS (phải làm riêng) |
| Nhập kết quả xét nghiệm | KTV | PASS |

Nghiệp vụ backend chạy được từ đầu đến cuối. Hai điểm đứt đều nằm ở chỗ **thiếu liên kết
tự động** và **thiếu màn hình**, không phải sai logic tính toán.
