# Sprint 8 — EPIC 6 Cashier + Payment + Billing (MVP GO-LIVE)

Author: Lành (architect). Date: 2026-05-22. DB: **MySQL 8** (multi-tenant via `tenant_id`).

## Files

| File | Endpoints | Permission scope |
|---|---|---|
| `service-catalog.yaml` | 10 — services CRUD, packages, search, import Excel | `service.*`, `service_package.*` |
| `billing.yaml` | 12 — auto-bill từ encounter, finalize/void, apply BHYT, PDF | `billing.*` |
| `payments.yaml` | 10 — multi-method, QR generate/poll/webhook, card charge, refund/void | `payment.*`, `payment_qr.generate` |
| `einvoice.yaml` | 5 — issue/cancel/list/xml (MISA/VNPT/EFY adapter) | `einvoice.*` |
| `cashier-closing.yaml` | 7 — shift open/close, today report, history PDF, debt list | `cashier.*` |

Total ~44 endpoints.

## Stories cover
- US-C01..C10 — service catalog, billing autogen, multi-method payment, QR, eInvoice, cashier shift, debt
- US-SUNS-20 — BHYT co-pay scaffold (`/billings/{id}/apply-bhyt`, schema chuẩn bị EPIC 7)
- US-SUNS-21 — eInvoice provider abstraction (mock OK go-live)

## Key design decisions

1. **Billing items** — chọn bảng riêng `diab_his_bil_billing_items` thay vì `items_json` để query/báo cáo/BHYT XML 4210 dễ. ADR-008.
2. **Payment <-> Shift link** — mỗi `bil_payments` có `cashier_shift_id` (nullable lúc shift đóng auto-set khi insert). Cho phép tính shift summary chính xác.
3. **QR webhook idempotent** — khóa theo `provider + provider_txn_id` (unique). Webhook PUBLIC, verify HMAC ở header.
4. **eInvoice provider adapter** — `IEInvoiceProvider` với 3 implement: Misa/Vnpt/Efy + DevMock. Mock trả CQT code random 13 ký tự để demo go-live.
5. **Card charge** — chỉ nhận `card_token` (Stripe-like), KHÔNG nhận PAN. Out of PCI scope.
6. **BHYT co-pay** — schema sẵn (`bhyt_amount`, `patient_payable`, `bhyt_applicable` per item, `right_route` enum). Calculator chi tiết theo % nhóm BN làm Sprint 9.

## Sensitive fields (AES-256-GCM)

| Bảng | Cột |
|---|---|
| `diab_his_bil_billing` | `bhyt_card_no` (nếu lưu), `note` chứa thông tin BN |
| `diab_his_bil_payments` | `reference`, `provider_txn_id` (mask khi log) |
| `diab_his_bil_einvoices` | `cqt_code` (mask 4 ký tự cuối khi hiển thị list) |

## FHIR R4 mapping

| Entity | FHIR resource |
|---|---|
| `diab_his_bil_billing` | `Invoice` |
| `diab_his_bil_billing_items` | `Invoice.lineItem` + `ChargeItem` |
| `diab_his_bil_payments` | `PaymentNotice` + `PaymentReconciliation` |
| `diab_his_bil_services` | `ChargeItemDefinition` |

## Migrations (cho Thảo - MySQL 8)

- `0040_service_catalog.sql` — `diab_his_bil_services` + `diab_his_bil_service_packages` + `_items`; UNIQUE(tenant_id, code)
- `0041_billing_extensions.sql` — kiểm tra `bil_billing` cũ; ADD cols + tạo `diab_his_bil_billing_items`; bill_no UNIQUE per tenant
- `0042_payments_qr_einvoice.sql` — `diab_his_bil_payments` + extend `diab_his_bil_qr_codes` (0014) + `diab_his_bil_einvoices`
- `0043_cashier_shifts.sql` — `diab_his_bil_cashier_shifts` + ADD `cashier_shift_id` vào `diab_his_bil_payments`
- `0044_seed_permissions_sprint8.sql` — 22 permission codes + role mapping (KETOAN: billing+payment+cashier full; DUOCSI: read-only; ADMIN: einvoice)

## Error codes (consolidated)

```
SERVICE_NOT_FOUND, SERVICE_CODE_EXISTS
BILLING_NOT_FOUND, BILLING_ALREADY_FINALIZED, BILLING_VOID, BILLING_INVALID_BHYT
PAYMENT_AMOUNT_INVALID, PAYMENT_INSUFFICIENT, PAYMENT_QR_EXPIRED, PAYMENT_GATEWAY_ERROR
EINVOICE_PROVIDER_ERROR, EINVOICE_ALREADY_ISSUED, EINVOICE_TAX_CODE_MISSING
CASHIER_SHIFT_NOT_OPEN, CASHIER_SHIFT_ALREADY_CLOSED, CASHIER_CASH_DIFFERENCE
```

## Backend services required (cho Sơn)

- `IBillingCalculator` — gom từ encounter (services + cls_orders + prescriptions + dispensing)
- `IBhytCoPayCalculator` — co-pay theo card type + right_route (80/95/100%)
- `IPaymentGateway` (+ Cash/VietQr/Momo/Vnpay/VisaMaster adapter, dev mock)
- `IEInvoiceProvider` (+ Misa/Vnpt/Efy adapter, dev mock CQT)
- `ICashierShiftService` — open/close, snapshot summary, auto-attach payment vào shift đang mở

## Frontend screens (cho Linh)

1. Service catalog grid + Excel import dialog
2. Billing detail (header + items table + payment panel + finalize/void)
3. Payment dialog đa kênh (tab CASH/BANK/CARD/QR), QR modal poll 3s
4. eInvoice issue button + history table
5. Cashier dashboard: shift status bar, today summary cards (cash/card/transfer/qr), close-shift modal
6. Debt list page với filter older_than_days
