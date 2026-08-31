# Evidence — Fix 4 Blocker go-live (2026-08-31)

**Môi trường:** Docker local đã **rebuild** `prodiab-dev-backend` (code mới), MySQL `prodiab_his` thật.
Tái lập đúng kịch bản QC đã phát hiện bug (xem `ute-full-flow-20260831.md`).

Tự động hoá: `dotnet build` sạch · `dotnet test` **987 PASS / 0 FAIL** (963 unit + 7 architecture + 17 integration; +9 test mới) · `npx tsc --noEmit` exit 0.

---

## BUG-01 — Phát thuốc lỗi KHÔNG được trừ kho

Đơn test `RX-BUG01-TEST` (SIGNED) gồm Metformin 500mg (còn tồn) + Gliclazide 80mg (chỉ có lô `LOT-NEAR-001` đã HẾT HẠN 2026-07-15 → 0 khả dụng).

**Tồn kho + chứng từ TRƯỚC khi phát:**
```
Metformin LOT-M001=486, LOT-M001=500, LOT-M002=800, LOT-M002=800 (LOT-EXP-001=45 đã hết hạn)
EXPORT movements = 3 | dispense_records = 5
```

**Gọi API phát thuốc (duocsi):**
```
POST /api/v1/pharmacy/dispense/RX-BUG01-TEST
-> HTTP 422 PHARMACY_STOCK_INSUFFICIENT
   "Không đủ tồn kho để phát \"Gliclazide 80mg\": Ton kho khong du (con thieu 30)"
```
(Trước fix: HTTP 500 "Lỗi hệ thống, vui lòng thử lại sau".)

**Tồn kho + chứng từ SAU khi phát (KHÔNG đổi — không thất thoát):**
```
Metformin LOT-M001=486, LOT-M001=500, LOT-M002=800, LOT-M002=800  ← GIỮ NGUYÊN
EXPORT movements = 3 (không phát sinh) | dispense_records = 5 (không tạo phiếu)
prescription status = SIGNED (không bị set DISPENSED)
```

**Happy-path (đối chứng transaction commit hoạt động):** phát đơn chỉ Metformin (đủ tồn) → 201, LOT-M001 486→476, EXPORT 3→4, records 5→6. (Đã khôi phục lại về baseline sau kiểm thử.)

---

## BUG-02 — Tiếp đón nhiều bệnh nhân/ngày (capacity=1)

Đã khôi phục `diab_his_sys_rooms.capacity = 1` (bỏ workaround QC nâng 60) để verify đúng điều kiện thật.

```
check-in BN A (thứ 1) [BNT01000039] -> HTTP 201 OK
check-in BN B (thứ 2, KHÁC người, cùng PK02, cùng ngày) -> HTTP 201 OK
```
(Trước fix: BN B → 409 `RECEPTION_ROOM_FULL`.)

Logic mới đếm sức chứa theo vé **đang ở trong phòng** (`CALLED`,`IN_PROGRESS`), không luỹ kế cả ngày.

---

## BUG-03 — Ô chọn thuốc hiện đúng tên

Migration `9191` đã apply: 30/30 thuốc `name_vi = name`; junk TH001/TH002 đã sạch.

```
GET /api/v1/drugs/search?q=Metformin (bacsi)
-> HTTP 200, 1 kết quả: name = "Metformin 500mg" | generic = "Metformin HCl"
GET /api/v1/drugs?page_size=50 -> tổng 30 thuốc, số thuốc tên RỖNG = 0
```
(Trước fix: hiện "Paracetamol 500mg (HIEN moi CN)" cho Metformin, 28/30 tên rỗng.)

---

## BUG-04 — Chặn thu tiền/override giá âm, 0, vượt

```
POST /payments amount=0         -> HTTP 400 VALIDATION_ERROR "Số tiền thanh toán phải lớn hơn 0"
POST /payments amount=-50000    -> HTTP 400 VALIDATION_ERROR "Số tiền thanh toán phải lớn hơn 0"
POST /payments amount=999999999 -> HTTP 400 "Số tiền thanh toán (999,999,999đ) vượt quá số còn phải thu"
POST /service-price-overrides price=-999999 -> HTTP 400 VALIDATION_ERROR "Gia phai lon hon 0"
POST /payments amount=100000 (hợp lệ) -> HTTP 201 (đường thu tiền bình thường vẫn chạy)
```
(Trước fix: cả 3 trường hợp âm/0/vượt đều 201; override -999.999đ đều 201.)

Root cause: validator khai báo cho `*Request` nhưng MediatR pipeline resolve `IValidator<*Command>` → không khớp → validator chết. Đã thêm 5 validator cấp Command + test kiến trúc `ValidatorWrappingArchitectureTests` (xác nhận đúng 5 chỗ, không còn chỗ nào khác).

---

## Ghi chú dọn dữ liệu dev
- Đã khôi phục billing `HD-202608-5AF7E` bị hỏng do chính BUG-04 (paid=999.949.999, balance âm) về `paid=0, balance=490.000` + xoá 5 payment rác test.
- Đã xoá đơn thuốc test + phiếu phát happy-path, khôi phục tồn kho về baseline.
- Giữ `capacity=1` (giá trị đúng theo ngữ nghĩa "đồng thời").
