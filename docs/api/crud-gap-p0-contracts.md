# Contract Spec — CRUD Gap P0 Endpoints

- **Ngày:** 2026-05-31 · **Tác giả:** Lành
- **Liên quan ADR:** [0001-pdf-rendering-strategy.md](../adr/0001-pdf-rendering-strategy.md), [0002-dtqg-sandbox-stub.md](../adr/0002-dtqg-sandbox-stub.md)
- **Phạm vi:** 3 endpoint mới cần thiết cho 3 action P0 (`Billing.PrintInvoice`, `Cashier.PrintReceipt`, `Prescription.Submit`).

> **Lưu ý kiến trúc:** Endpoint hoá đơn PDF (`GET /api/v1/billings/{id}/pdf`) **đã tồn tại** trong [BillingsController.cs](../../backend/src/ProDiabHis.Api/Controllers/BillingsController.cs) line 132-139. FE chỉ cần gọi. Doc này chuẩn hoá thêm `POST .../print` (alias trigger archive + audit) và 2 endpoint chưa có.

---

## 1. POST /api/v1/billings/{id}/print

**Mục đích:** Render hoá đơn A5 + archive bản gốc vào MinIO + ghi audit "đã in".

| Thuộc tính | Giá trị |
|---|---|
| Method | `POST` |
| Path | `/api/v1/billings/{id}/print` |
| Auth | `[Authorize]` + `[RequirePermission("billing.print")]` |
| Content-Type req | `application/json` (body optional) |
| Content-Type res | `application/pdf` |
| Idempotent | Không (mỗi lần in ghi audit log riêng) |

### Request
```json
{
  "copy_label": "BẢN KHÁCH HÀNG",  // optional, default "BẢN GỐC"
  "reprint": false                   // true = đánh dấu "REPRINT" trên góc PDF
}
```

### Response (200 OK)
- Header `Content-Disposition: inline; filename="invoice-{invoice_no}.pdf"`
- Header `X-Invoice-Archived-Url: minio://prodiab-invoices/{tenant}/{yyyy}/{mm}/{invoice_no}.pdf`
- Body: binary PDF stream (QuestPDF, A5, font Arial Unicode)

### Error codes
| HTTP | code | message |
|---|---|---|
| 404 | `BILLING_NOT_FOUND` | Không tìm thấy hoá đơn |
| 409 | `BILLING_NOT_FINALIZED` | Hoá đơn chưa chốt, không thể in bản chính thức |
| 403 | `FORBIDDEN_PERMISSION` | Không có quyền `billing.print` |

### Audit log
- Bảng `diab_his_sec_audit_logs`
- `action='billing.print'`, `entity_type='Billing'`, `entity_id={id}`, `metadata={"reprint":false,"copy_label":"..."}`

---

## 2. POST /api/v1/cashier/receipts/{paymentId}/print

**Mục đích:** In biên lai K80 (58mm × dynamic height) ngay sau giao dịch thu tiền.

| Thuộc tính | Giá trị |
|---|---|
| Method | `POST` |
| Path | `/api/v1/cashier/receipts/{paymentId}/print` |
| Auth | `[Authorize]` + `[RequirePermission("cashier.print_receipt")]` |
| Content-Type res | `application/pdf` |
| Idempotent | Không |

### Request
```json
{
  "reprint": false
}
```

### Response (200 OK)
- Header `Content-Disposition: inline; filename="receipt-{receipt_no}.pdf"`
- Body: PDF K80 layout — header tenant, mã BN, danh sách item, tổng tiền, phương thức (cash/transfer), QR mã giao dịch.

### Error codes
| HTTP | code | message |
|---|---|---|
| 404 | `PAYMENT_NOT_FOUND` | Không tìm thấy giao dịch |
| 409 | `PAYMENT_VOIDED` | Giao dịch đã huỷ, không thể in biên lai |
| 403 | `FORBIDDEN_PERMISSION` | Không có quyền in biên lai |

### Audit log
- `action='cashier.receipt.print'`, `entity_type='Payment'`, `entity_id={paymentId}`

---

## 3. POST /api/v1/prescriptions/{id}/submit-dtqg

**Mục đích:** Đẩy đơn thuốc lên Cổng ĐTQG, nhận `ma_don_thuoc`, sinh QR. DEV/STAGING dùng `MockDtqgClient` (xem ADR-0002).

| Thuộc tính | Giá trị |
|---|---|
| Method | `POST` |
| Path | `/api/v1/prescriptions/{id}/submit-dtqg` |
| Auth | `[Authorize]` + `[RequirePermission("dtqg.submit")]` |
| Content-Type req | `application/json` (body có thể rỗng `{}`) |
| Content-Type res | `application/json` |
| Idempotent | Có (lần 2 trả về cùng `ma_don_thuoc` nếu status=`submitted`) |

### Request
```json
{
  "force_resubmit": false  // optional, true = bỏ qua check idempotent, dành cho retry sau lỗi
}
```

### Response (200 OK)
```json
{
  "data": {
    "prescription_id": "9c1f...",
    "ma_don_thuoc": "VN2605310000042",
    "qr_url": "/api/v1/prescriptions/9c1f.../qr.png",
    "submitted_at": "2026-05-31T10:23:45Z",
    "status": "submitted",
    "portal_status": "ACCEPTED"
  },
  "meta": {
    "mode": "Mock"   // hoặc "Real"
  }
}
```
- Header `X-Dtqg-Mode: Mock` (DEV/STAGING) hoặc `Real` (Production)

### Error codes
| HTTP | code | message |
|---|---|---|
| 404 | `PRESCRIPTION_NOT_FOUND` | Không tìm thấy đơn thuốc |
| 422 | `PRESCRIPTION_NO_ITEMS` | Đơn thuốc chưa có dòng thuốc nào |
| 422 | `PRESCRIPTION_NO_DIAGNOSIS` | Đơn thuốc chưa gắn chẩn đoán ICD-10 |
| 409 | `PRESCRIPTION_ALREADY_SUBMITTED` | Đơn đã submit, dùng `force_resubmit=true` để gửi lại |
| 502 | `DTQG_PORTAL_UNAVAILABLE` | Cổng ĐTQG không phản hồi (đã enqueue retry job) |
| 401 | `DTQG_CREDENTIALS_INVALID` | Token ĐTQG không hợp lệ (chỉ xảy ra ở Real mode) |
| 403 | `FORBIDDEN_PERMISSION` | Không có quyền `dtqg.submit` |

### Side effects
- Cập nhật `pres_prescriptions`: `ma_don_thuoc`, `submitted_at`, `status='submitted'`, `dtqg_portal_status`
- Insert row vào `dtqg_submissions` (đã có entity) với `mode`, `payload`, `response`, `latency_ms`
- Audit: `action='prescription.submit_dtqg'`, `entity_type='Prescription'`, `metadata={"mode":"Mock","ma_don_thuoc":"..."}`
- Nếu portal trả lỗi tạm thời → enqueue `DtqgSubmitRetryJob` (đã có)

### FHIR mapping
- `MedicationRequest.identifier`: thêm system `urn:vn:moh:dtqg`, value = `ma_don_thuoc`
- `MedicationRequest.status` = `active` sau submit thành công

---

## 4. Tóm tắt cho FE (Nam)

| Action | Endpoint sẵn / mới | FE gọi như thế nào |
|---|---|---|
| `Billing.PrintInvoice` | `GET /billings/{id}/pdf` (sẵn) **hoặc** `POST /billings/{id}/print` (mới, khuyến nghị) | Mở blob trong `<iframe>` → `iframe.contentWindow.print()` |
| `Cashier.PrintReceipt` | `POST /cashier/receipts/{paymentId}/print` (mới) | Auto trigger sau khi `ReceivePayment` 201 |
| `Prescription.Submit` | `POST /prescriptions/{id}/submit-dtqg` (mới) | Disable nút submit khi đơn chưa đủ điều kiện (no items/no diagnosis). Hiển thị badge "Môi trường thử nghiệm" khi header `X-Dtqg-Mode=Mock`. |

## 5. Tóm tắt cho BE (Thảo)

| Việc | File | Effort |
|---|---|---|
| `InvoicePdfService` (QuestPDF A5) + `POST /billings/{id}/print` | `backend/src/ProDiabHis.Api/Services/InvoicePdfService.cs` + `BillingsController` | 1 ngày |
| `ReceiptPdfService` (QuestPDF K80) + `POST /cashier/receipts/{paymentId}/print` | `backend/src/ProDiabHis.Api/Services/ReceiptPdfService.cs` + `CashierController` | 1 ngày |
| `POST /prescriptions/{id}/submit-dtqg` handler dùng `IDtqgClient` | `backend/src/ProDiabHis.Application/Pharmacy/Dtqg/DtqgHandlers.cs` + `PrescriptionsController` | 0.5 ngày |
| Permission seed: `billing.print`, `cashier.print_receipt`, `dtqg.submit` (nếu chưa có) | `db/seeds/permissions.sql` | 0.5 ngày |
| Bucket MinIO `prodiab-invoices` + service archive | Infra | 0.5 ngày |

**Tổng BE:** ~3.5 ngày để unblock FE.
