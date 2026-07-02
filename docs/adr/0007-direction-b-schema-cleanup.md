# ADR 0007 — Direction B: Schema Mismatch Cleanup toàn hệ thống

- **Trạng thái**: Đã chấp thuận
- **Ngày**: 2026-05-29
- **Người đề xuất**: Lành (architect)
- **Người duyệt**: Đăng (PO), Thảo (BE lead), Chương (DevOps)
- **Liên quan**: Plan `C:\Users\ADMIN\.claude\plans\hi-n-t-i-c-report-jaunty-cherny.md`

## 1. Bối cảnh

Sau khi feature *In báo cáo A4* go-live, team phát hiện 2 thế giới schema cùng tồn tại trong `prodiab_his`:

| Yếu tố | Thế giới A (dump production legacy) | Thế giới B (EF entity code agent) |
|---|---|---|
| Convention cột | UPPERCASE (`FIRST_NAME`, `PATIENT_ID`) | snake_case (`first_name`, `patient_id`) |
| PK | `INT AUTO_INCREMENT` | `Guid` / `CHAR(36)` |
| Audit columns | Không có hoặc rời rạc | 6 cột chuẩn (`created_at/by`, `updated_at/by`, `deleted_at/by`) |
| Prefix bảng | `pat_`, `cli_`, `pha_`, `bil_`, `sec_`, … | `diab_his_<group>_<entity>` |
| Generated cột | `FULL_NAME` STORED, … | Tính trong service layer |
| Multi-tenant | Không nhất quán | `tenant_id INT NOT NULL` + EF `HasQueryFilter` |

Hậu quả đã ghi nhận trong session vừa qua:
- 30/65 migration FAIL khi bootstrap (seed permissions, dict, sprint ext).
- Login crash `Unknown column 's.deleted_at'` (đã hotfix DROP 18 FK + recreate `sec_*` Guid).
- Reports query 3 loại đều giả định bảng chưa tồn tại (`diab_his_enc_encounters`, `diab_his_pha_stock`, `diab_his_sys_clinics`) — đã workaround bằng sample data + 9 dấu `TODO[dev]/TODO[schema]`.
- 21/28 module trong sidebar gọi Dapper raw SQL tới bảng legacy UPPERCASE → mở module sẽ crash 500.

## 2. Quyết định

Chọn **Direction B — Clean slate**: drop toàn bộ bảng legacy không có prefix `diab_his_`, recreate mới hoàn toàn theo EF convention (Guid PK + snake_case + audit 6 cột + multi-tenant).

### Naming convention chuẩn (ghi đè CLAUDE.md §3 mục "Quy ước")

1. **Tên bảng nghiệp vụ MỚI**: `diab_his_<group>_<entity>` (lowercase snake_case).
   - `group` 3-4 ký tự thuộc whitelist: `sec`, `sys`, `pat`, `enc`, `lab`, `rad`, `pha`, `bil`, `bhyt`, `nti`, `prl`, `dia`, `api`, `rep`.
   - VD: `diab_his_pat_patients`, `diab_his_enc_encounters`, `diab_his_pha_prescriptions`.
2. **Primary key**: `id CHAR(36) NOT NULL` (UUID v4 sinh từ EF / `UUID()` MySQL).
   - **Ghi đè quy ước cũ** "INT AUTO_INCREMENT kế thừa schema cũ" — quy ước cũ chỉ áp dụng cho bảng dump legacy (sẽ bị drop).
3. **Audit columns chuẩn 6 cột** (bắt buộc mọi bảng nghiệp vụ):
   ```
   created_at  DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
   created_by  CHAR(36)    NULL
   updated_at  DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)
   updated_by  CHAR(36)    NULL
   deleted_at  DATETIME(6) NULL
   deleted_by  CHAR(36)    NULL
   ```
4. **Multi-tenant**: `tenant_id INT NOT NULL` (giữ INT cho join với `diab_his_sys_tenants.id` AUTO_INCREMENT — tenant ít, hiệu năng index tốt hơn UUID).
   - Index bắt buộc: `idx_<table>_tenant` trên `(tenant_id)` hoặc composite `(tenant_id, <hot_col>)`.
5. **Soft-delete**: filter `deleted_at IS NULL` qua EF `HasQueryFilter`. Không hard delete.
6. **Foreign key**: tên cột `<entity>_id CHAR(36)` reference parent PK. FK constraint `fk_<child>_<parent>`.
7. **Index naming**:
   - Index thường: `idx_<table>_<col1>[_<col2>...]`
   - Unique: `uq_<table>_<col1>[_<col2>...]`
8. **Charset/Collation**: `utf8mb4 / utf8mb4_0900_ai_ci` (cột tên/địa chỉ tiếng Việt nếu cần sort thì set per-column `utf8mb4_vi_0900_ai_ci`).
9. **Mã hóa AES-256-GCM**: cột nhạy cảm (CMND, số BHYT, ghi chú bệnh án, token tích hợp DTQG/BHYT) — type `VARBINARY(512)`, suffix `_enc`.

## 3. Hệ quả

### Tích cực
- Schema duy nhất, không ambiguity → code consistent, dễ maintain, dễ on-board dev mới.
- Mọi bảng có audit + soft-delete + multi-tenant chuẩn → audit log, BI report, compliance HSBA dễ làm.
- EF Core hoạt động đúng convention, bỏ được hàng loạt `HasColumnName` workaround.
- FE RBAC + sidebar filter chạy được vì permission + role chuẩn hóa.

### Tiêu cực
- **Mất toàn bộ dump data** (~64 file `db/diab_his_*.sql`) — accept vì chỉ là sample demo, dữ liệu thật chưa go-prod.
- Phải viết lại 21 controller/handler đang dùng Dapper raw SQL UPPERCASE (Phase 3 plan, ~3 ngày BE).
- Hangfire schema phải re-init sau khi drop volume.
- Trong giai đoạn chuyển đổi (Phase 1–3), production deploy bị block ~1.5 tuần.

### Trung tính
- File dump `db/diab_his_*.sql` giữ trong repo dạng tham chiếu read-only, không apply nữa (đánh dấu `# LEGACY — DO NOT APPLY` ở header).
- WORKFLOW.md không đổi.

## 4. Phương án bị từ chối

### Alternative A — Rewrite EF entity về UPPERCASE + INT PK để khớp dump
- **Lý do bỏ**: 32 EF entity + 18 EF configuration + 5 handler đã viết theo Guid/snake_case. Quay đầu sẽ phá tất cả convention CLAUDE.md §6 ("tên cột snake_case tiếng Anh"). Audit columns + soft-delete + multi-tenant phải thêm thủ công vào 64 dump table → tốn hơn Direction B 2-3x. Trade-off mất nhiều hơn được.

### Alternative C — Hybrid: giữ cả 2 thế giới, viết adapter
- **Lý do bỏ**: phải maintain 2 set DTO, 2 set repository, 2 set EF config. Mỗi feature mới phải quyết định "viết theo thế giới nào" → tăng cognitive load. Audit/BI query phải UNION 2 schema. Multi-tenant filter dễ miss ở thế giới A. Đây chính xác là tình trạng hiện tại đã chứng minh không bền vững.

### Alternative D — ETL data dump sang schema mới rồi drop
- **Lý do bỏ**: data dump chỉ là sample demo, không có giá trị nghiệp vụ. Viết ETL tốn ~2 ngày BE cho zero return. Có thể bật lại sau nếu khách thật cần migrate (out of scope).

## 5. Tham chiếu

- Plan chi tiết: `C:\Users\ADMIN\.claude\plans\hi-n-t-i-c-report-jaunty-cherny.md`
- ERD v2: `docs/erd/full-schema-v2.md`
- Runbook migration: `docs/migration/clean-slate-runbook.md`
- CLAUDE.md §3 (Database), §6 (Quy ước code)
