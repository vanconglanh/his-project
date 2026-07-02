# Sprint 6-7 — EPIC 5: Prescription + ĐTQG + Pharmacy

> Author: Lành (Architect) — Wedge MVP. Database: **MySQL 8** (note: lệch khỏi default PostgreSQL của CLAUDE.md, đã align theo dump cũ `pha_*` và prefix `diab_his_*` Thảo đang dùng).

## 1. Files trong thư mục này

| File | Mô tả | Endpoint count |
|------|-------|----------------|
| `prescriptions.yaml` | Kê đơn (CRUD), items, ký số PKCS#11, DDI, QR, PDF, print history | 12 |
| `dtqg.yaml` | Tích hợp Đơn thuốc Quốc gia: submit/retry/status, credentials | 8 |
| `drugs.yaml` | Drug master (CRUD + import Excel + search + equivalents + DDI + categories + sync Cục QLD) | 12 |
| `pharmacy-warehouse.yaml` | Warehouses, PO/GRN, stocks, adjustments, movements, transfers, alerts, stocktake PDF | 15 |
| `pharmacy-dispensing.yaml` | Dispense queue, FEFO dispense, reject, return, history, receipt PDF | 8 |
| **Total** |  | **~55** |

## 2. Status state machines

### Prescription
```
DRAFT ──update/add items──> DRAFT
DRAFT ──sign──────────────> SIGNED
SIGNED ──submit DTQG──────> SUBMITTED_DTQG
SIGNED|SUBMITTED ──dispense──> DISPENSED | PARTIAL_DISPENSED
DRAFT|SIGNED ──cancel─────> CANCELLED   (không cho cancel sau khi DISPENSED)
```

### ĐTQG submission
```
NONE -> PENDING -> SUBMITTED -> ACCEPTED | REJECTED
                            \-> (retry) -> PENDING
```

### Purchase Order
```
DRAFT -> SENT -> PARTIAL -> RECEIVED
                       \-> CANCELLED
```

### Dispense Record
```
(new) -> DISPENSED | REJECTED
DISPENSED -> PARTIAL (via return) -> RETURNED (full)
```

## 3. Multi-tenant & Security

- Tất cả bảng có `tenant_id` (RLS / app-layer filter, vì MySQL 8 không có RLS native -> dùng middleware enforce + index composite).
- Cột nhạy cảm AES-256-GCM:
  - `pha_dtqg_credentials.token`
  - `pha_prescriptions.signature_data` (PKCS#7 blob, ký số USB token)
  - `pha_prescription_items.instructions` (nếu chứa thông tin bệnh án — optional)
- Audit log: INSERT/UPDATE/DELETE trên `pha_prescriptions`, `pha_prescription_items`, `pha_dispense_records`.

## 4. Migrations (cho Thảo - backend)

| File | Mô tả |
|------|-------|
| `0035_create_prescription_extensions.sql` | ALTER `pha_prescriptions`: status enum, dtqg_code CHAR(14), dtqg_status, signed_at, signed_by, signature_data LONGBLOB. CREATE `pha_prescription_items`. CREATE `diab_his_pha_ddi_rules`. |
| `0036_drug_master_extensions.sql` | ALTER `pha_drug_master` add: atc_code, form ENUM, requires_prescription, is_psychotropic, is_narcotic, dtqg_drug_code, price. Unique idx (tenant_id, code). |
| `0037_create_purchase_orders.sql` | `diab_his_pha_purchase_orders`, `diab_his_pha_purchase_order_items`, `diab_his_pha_suppliers`. |
| `0038_create_dispense_records.sql` | `diab_his_pha_dispense_records`, `diab_his_pha_dispense_items`. |
| `0039_seed_permissions_sprint6_7.sql` | ~30 permission mới + role mapping. |

## 5. Services (cho Thảo - backend DI)

| Service | Trách nhiệm | Notes |
|---------|-------------|-------|
| `IDtqgClient` | HTTP POST submit, GET status. Auth: x509 client cert hoặc API key. | Polly retry, circuit breaker. |
| `IDtqgQrGenerator` | Gen QR từ `ma_don_thuoc` + URL portal. | Lib: QRCoder. |
| `IDdiChecker` | Query `pha_ddi_rules`, trả về list warnings. | Block submit nếu severity=CONTRAINDICATED -> `PRESCRIPTION_DDI_BLOCKED`. |
| `IUsbTokenSigner` | Dev: mock PKCS#7 giả; Prod: client-side ký, BE nhận signature_data + verify cert chain. | Server không cầm private key. |
| `IFefoStrategy` | Pick batch theo `expiry_date ASC`, loại lô expired/reserved, accumulate qty. | Pharmacist được override. |
| `IDrugCucQldSync` | Background job sync drug_master với CSDL Dược QG. | Hangfire/Quartz, mode FULL/INCREMENTAL. |
| `ICucQldLienThong` | Báo nhập/xuất với Cục QLD (bắt buộc nhà thuốc GPP). | Hook vào GRN + dispense events. |

## 6. Permissions (~30 mới)

```
prescription.read | prescription.create | prescription.update | prescription.sign | prescription.cancel
ddi.check
dtqg.submit | dtqg.retry | dtqg.admin
drug.read | drug.write | drug.import | drug.sync
warehouse.read | warehouse.write
stock.read | stock.adjust
dispense.queue | dispense.perform | dispense.reject | dispense.return
```

Role mapping:
- **BACSI**: prescription.read/create/update/sign/cancel, ddi.check, dtqg.submit, drug.read
- **DUOCSI**: drug.*, warehouse.*, stock.*, dispense.*, prescription.read, ddi.check
- **ADMIN**: tất cả + dtqg.admin, drug.sync
- **LETAN**: prescription.read, drug.read
- **KETOAN**: prescription.read, stock.read

## 7. Error code (tiếng Việt message)

```
PRESCRIPTION_NOT_FOUND | PRESCRIPTION_ALREADY_SIGNED | PRESCRIPTION_INVALID_STATE
PRESCRIPTION_DDI_BLOCKED | PRESCRIPTION_INSUFFICIENT_STOCK
PRESCRIPTION_SIGNATURE_FAILED | PRESCRIPTION_USB_TOKEN_NOT_FOUND
DTQG_SUBMIT_FAILED | DTQG_TOKEN_EXPIRED | DTQG_INVALID_RESPONSE
DTQG_CSKCB_NOT_REGISTERED | DTQG_PRESCRIPTION_ALREADY_SUBMITTED | DTQG_RETRY_EXCEEDED
DRUG_NOT_FOUND | DRUG_CODE_EXISTS | DRUG_IMPORT_INVALID_FORMAT | DRUG_DTQG_SYNC_FAILED
PHARMACY_STOCK_INSUFFICIENT | PHARMACY_BATCH_EXPIRED | PHARMACY_BATCH_NOT_FOUND
PHARMACY_WAREHOUSE_NOT_FOUND | PHARMACY_INVALID_FEFO_PICK | PHARMACY_DISPENSE_DUPLICATE
```

## 8. FHIR R4 mapping

| Entity nội bộ | FHIR Resource |
|---------------|---------------|
| `pha_prescriptions` + items | `MedicationRequest` (1 prescription = 1 bundle, mỗi item = 1 MedicationRequest với groupIdentifier) |
| `pha_drug_master` | `Medication` |
| `pha_dispense_records` + items | `MedicationDispense` |
| `pha_ddi_rules` | `DetectedIssue` (severity map) |

## 9. Trade-offs / Decisions

- **MySQL 8** thay vì PostgreSQL: theo dump cũ Thảo đang dùng. RLS multi-tenant phải làm ở app-layer + composite index `(tenant_id, ...)`. Cần ADR riêng (`docs/adr/006-mysql-instead-of-postgres.md`).
- **Ký số PKCS#11**: server **không** giữ private key. Client (Windows app/WebView2/extension) ký xong gửi signature_data lên. BE chỉ verify + lưu.
- **DDI rule source**: Sprint 6 dùng seed manual top ~200 cặp phổ biến (paracetamol+warfarin, ACEI+K-sparing, vv). Sprint 8+ tích hợp Drugbank/Lexicomp.
- **FEFO override**: cho phép dược sĩ chọn lô khác (vì lô FEFO có thể thiếu), nhưng phải audit log lý do.

## 10. Sequence diagrams cần (TODO Lành Sprint 7)

- `docs/sequence/prescription-sign-and-submit-dtqg.md`
- `docs/sequence/pharmacy-dispense-fefo.md`
- `docs/sequence/grn-stock-update.md`
