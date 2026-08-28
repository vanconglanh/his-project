# Bảng phân công fix bug — 2026-08-29

Branch: `develop` (commit 173b514, merge sys_phong_kham_noi)
Nguồn bug: `docs/qc/utc-his-core-20260828.md`, `docs/qc/ute-his-core-20260828.md`

## Tình trạng image Docker
Backend image `prodiab-dev-backend` build lúc 00:09:43, commit merge lúc 00:06:56
→ image ĐÃ khớp code mới nhất, **bỏ qua build vòng 1**, dùng luôn để lấy stack trace.

## Nguyên nhân gốc (điều tra từ log container thật)

| BUG | Severity | Root cause | Liên quan merge? |
|-----|----------|-----------|------------------|
| BUG-004 | Blocker | `InvalidCastException: Guid -> String` tại `EncounterHandlers.cs:587` (ListEncountersQueryHandler). Trộn kiểu Guid/string trong LINQ filter + BuildEncounterResponse | CÓ — lệch kiểu Id sau merge |
| BUG-005 | Blocker | `FormatException: Could not parse CHAR(36) as Guid: rx000001-...`. Seed `pha_prescriptions` có 10 dòng id tiền tố `rx` không phải hex | KHÔNG — lỗi seed data có sẵn |
| BUG-007 | Blocker | `RuntimeBinderException: Cannot convert Guid to string` trong luồng BillingsController.List (dùng `dynamic` từ Dapper) | CÓ khả năng |
| BUG-002 | High | POST /patients trả 201 nhưng response thiếu `data.id` | KHÔNG |
| BUG-003 | Med | Validation message FluentValidation ra tiếng Anh, vi phạm CLAUDE.md | KHÔNG |
| BUG-006 | Med | **KHÔNG PHẢI BUG** — test gọi sai URL. Route thật `/api/v1/drugs` → 200 | — |
| BUG-009 | Med | **KHÔNG PHẢI BUG** — test gọi sai URL. Route thật `/api/v1/dashboard/overview` → 200 | — |

## Phân công

| BUG | Role | File chính | Status |
|-----|------|-----------|--------|
| BUG-004 + BUG-007 | backend (Thảo) | `backend/src/ProDiabHis.Application/Encounters/EncounterHandlers.cs`, handler Billings | Đang fix |
| BUG-005 + BUG-002 | backend (Thảo) | `db/seeds/*`, `db/migrations/NNNN_fix_prescription_invalid_uuid.sql`, `Patients/PatientCommandHandler.cs` | Đang fix |
| BUG-003 | backend (Thảo) | validator FluentValidation + LanguageManager vi | Đang fix |
| BUG-006, BUG-009 | qc-agent | — | Trả về QC: sửa lại URL trong test case, không assign dev |

## Quy tắc chống conflict giữa 3 agent song song
- Agent A chỉ chạm `Encounters/`, handler Billings
- Agent B chỉ chạm `db/seeds`, `db/migrations`, `Patients/PatientCommandHandler.cs`
- Agent C chỉ chạm validator + middleware validation

## KẾT QUẢ CUỐI (đã verify trên container sau rebuild)

### Nguyên nhân gốc THẬT SỰ của 3 Blocker (khác giả thuyết ban đầu)
`MySqlConnector 2.4.0` mặc định `GuidFormat=Default (Char36)` → **tự suy diễn MỌI cột `CHAR(36)` thành `System.Guid`**
ở tầng ADO.NET, bất kể property C# khai `string`. EF gọi `reader.GetString()` → `InvalidCastException`;
Dapper `dynamic` cast `(string?)` → `RuntimeBinderException`. Đây là nguyên nhân chung của BUG-004 + BUG-007
(và tầng 2 của BUG-005), KHÔNG phải do merge conflict resolve sai.

**Fix gốc 1 chỗ:** `ProDiabHis.Infrastructure/DependencyInjection.cs` — `EnsureGuidFormatNone()` ép
`GuidFormat=None` vào connection string, áp dụng cho cả EF Core lẫn Dapper.

### Bảng kết quả verify

| BUG | Endpoint | Trước | Sau | Trạng thái |
|-----|----------|-------|-----|-----------|
| BUG-004 | GET /api/v1/encounters | 500 | **200** | ĐÓNG |
| BUG-005 | GET /api/v1/prescriptions | 500 | **200** | ĐÓNG |
| BUG-007 | GET /api/v1/billings | 500 | **200** | ĐÓNG |
| BUG-002 | POST /api/v1/patients | thiếu data.id | **có data.id** | ĐÓNG |
| BUG-003 | validation message | tiếng Anh | **tiếng Việt (cả 2 tầng)** | ĐÓNG |
| BUG-006 | GET /api/v1/drugs | 404 (sai URL test) | **200** | KHÔNG PHẢI BUG |
| BUG-009 | GET /api/v1/dashboard/overview | 404 (sai URL test) | **200** | KHÔNG PHẢI BUG |
| BUG-010 (mới) | GET /api/v1/billings/{id} | 500 `Unknown column 'dob'` | **200** | ĐÓNG |

### Bug phát sinh thêm được phát hiện & xử lý trong vòng này
- **BUG-005 tầng 2:** sau khi sửa seed UUID, lộ tiếp lỗi Dapper map `doctor_id` (Guid) → `string?`. Đã sửa `PrescriptionRow.DoctorId` → `object?`.
- **Seed sai UUID `lab_orders`:** 10 dòng id tiền tố `lo` (không hex) trong `9020_seed_rich_demo.sql` → migration `9131_fix_lab_order_invalid_uuid.sql`. Đã áp, verify = 0.
- **BUG-010:** `BillingHandlers.cs:152` query cột `dob` nhưng schema thật là `date_of_birth`. Đã sửa alias.
- **Dedup bệnh nhân theo Phone (nghi vấn từ merge):** XÁC NHẬN ĐÚNG là bug — so sánh trên cột AES-GCM non-deterministic. Đã chuyển sang blind-index `PhoneBidx` (HMAC). Verify: tạo trùng SĐT+họ tên+ngày sinh → trả `possible_duplicate: true` + `match_reason: SDT_HOTEN_NGAYSINH_TRUNG`.

### Ghi chú
- Tiếng Việt lưu DB đúng UTF-8 (`Lê Thị Hoa Đào`) — dấu `?` khi xem qua console chỉ là lỗi hiển thị cp932, không phải bug.
- Không push git theo yêu cầu.
