# Sprint 9 - EPIC 7 BHYT Export (QĐ 4750/QĐ-BYT)

Architect: Lành | Stack: .NET 8 + MySQL 8 | Stories: US-BH01..BH06

## Files
- `bhyt-export.yaml` - Generate XML Bảng 1-5, validate XSD, sign, submit
- `bhyt-reconcile.yaml` - Đối soát kết quả giám định BHYT

## Endpoints (20)

### BHYT Export (`/api/v1/bhyt/exports`)
| Method | Path | Permission | Mô tả |
|---|---|---|---|
| POST | `/` | `bhyt.export` | Tạo kỳ export (period YYYY-MM) |
| GET | `/` | `bhyt.read` | List exports |
| GET | `/{id}` | `bhyt.read` | Chi tiết |
| DELETE | `/{id}` | `bhyt.export` | Xóa (chỉ DRAFT) |
| POST | `/{id}/generate` | `bhyt.generate` | Build XML Bảng 1-5 |
| POST | `/{id}/regenerate` | `bhyt.generate` | Gen lại |
| POST | `/{id}/validate` | `bhyt.validate` | Validate XSD QĐ 4750 |
| POST | `/{id}/sign` | `bhyt.sign` | Ký số XML |
| POST | `/{id}/submit` | `bhyt.submit` | Submit cổng BHYT |
| GET | `/{id}/xml/{tableNo}` | `bhyt.read` | Download XML 1 bảng |
| GET | `/{id}/xml/all` | `bhyt.read` | Download ZIP 5 bảng |
| GET | `/{id}/items/table/{n}` | `bhyt.read` | Preview rows trong bảng |
| GET | `/{id}/items/table/{n}/{rowId}` | `bhyt.read` | Chi tiết 1 row |

### BHYT Reconcile (`/api/v1/bhyt/reconcile`)
| Method | Path | Permission | Mô tả |
|---|---|---|---|
| POST | `/import` | `bhyt.reconcile` | Upload kết quả giám định |
| GET | `/{exportId}` | `bhyt.read` | List items đã giám định |
| POST | `/{itemId}/dispute` | `bhyt.reconcile` | Khiếu nại |
| POST | `/{itemId}/accept` | `bhyt.reconcile` | Chấp nhận |
| GET | `/{exportId}/summary` | `bhyt.read` | Tổng hợp duyệt/từ chối |

## Status Machine

```
DRAFT --generate--> GENERATED --validate--> VALIDATED --sign--> SIGNED --submit--> SUBMITTED
                                                                                       |
                                                            +--------------------------+
                                                            v
                                              APPROVED | PARTIALLY_REJECTED | REJECTED
```

Sau khi SUBMITTED -> `BHYT_PERIOD_LOCKED` (không cho xóa/regenerate).

## XML Bảng 1-5 (QĐ 4750/QĐ-BYT)

| Bảng | Tên | Nguồn dữ liệu |
|---|---|---|
| 1 | Tổng hợp đợt khám/điều trị | `diab_his_clinic_encounters` + `diab_his_patients` + BHYT card |
| 2 | Thuốc | `diab_his_pharma_prescription_items` (BHYT only) |
| 3 | CLS (XN + CĐHA + vật tư) | `diab_his_clinic_lab_orders` + `diab_his_clinic_rad_orders` |
| 4 | DVKT cao | `diab_his_clinic_services` (loại = DVKT cao) |
| 5 | Tổng hợp chi phí theo nhóm | Aggregation từ billing |

`ma_lien_ket` = key nối Bảng 1 với Bảng 2/3/4/5 (UUID encounter).

## Error Codes
- `BHYT_EXPORT_NOT_FOUND`, `BHYT_EXPORT_INVALID_PERIOD`, `BHYT_EXPORT_NO_ENCOUNTERS`
- `BHYT_XML_GENERATION_FAILED`, `BHYT_XSD_VALIDATION_FAILED`
- `BHYT_SIGN_FAILED`, `BHYT_SUBMIT_FAILED`
- `BHYT_RECONCILE_FILE_INVALID`, `BHYT_RECONCILE_ITEM_NOT_FOUND`
- `BHYT_PERIOD_LOCKED`

## Permissions seed
`bhyt.read`, `bhyt.export`, `bhyt.generate`, `bhyt.validate`, `bhyt.sign`, `bhyt.submit`, `bhyt.reconcile`

## Migrations cần (MySQL 8)
- `0045_bhyt_export_extensions.sql` - ALTER `diab_his_int_bhyt_exports` + `_items` (thêm encounter_count, totals, timestamps, response_message, xml_file_path, record_index, source refs, request/approved amount, rejection_code/reason)
- `0046_bhyt_reconcile.sql` - CREATE `diab_his_int_bhyt_reconcile_uploads` (id, tenant_id, export_id, file_path, uploaded_at, parsed_at, parse_status, parse_error, audit). FK export_id -> exports.
- `0047_seed_permissions_sprint9.sql` - INSERT 7 permissions vào `diab_his_iam_permissions`.

## Services backend
- `IBhytXmlGenerator` - build Bảng 1-5 từ encounters theo period_month + scope_filter
- `IBhytXsdValidator` - validate XML với XSD QĐ 4750 (tải XSD từ BYT, lưu `backend/Resources/Xsd/qd4750/`)
- `IBhytSubmissionClient` - mock dev (return success), prod gọi cổng giám định BHYT (gdbhyt.baohiemxahoi.gov.vn)
- `IBhytReconcileParser` - parse XML response giám định, map về reconcile items
- `IBhytSigner` - ký số XML bằng chứng thư số (USB token / HSM)

## Background jobs
- `BhytGenerateJob` - long-running (>1000 encounters), publish progress qua SignalR
- `BhytReconcileParseJob` - parse file giám định trong background

## Multi-tenant
Tất cả bảng có `tenant_id` + RLS. JWT scope `bhyt.*` mới gọi được.

## FHIR mapping
Bảng 1 row <-> `Claim` resource (FHIR R4). Bảng 2/3/4 items <-> `Claim.item[]`.
