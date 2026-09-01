# Evidence — Master data / Config qua UI admin (Việc 1-4 + 3 bug)

- Ngày: 2026-09-01
- Nhánh: develop
- Nguồn yêu cầu: docs/prd/audit-hardcode-vs-master-data-20260901.md

## Tổng quan verify

| Hạng mục | Cách verify | Kết quả |
|---|---|---|
| Build backend | `dotnet build` toàn solution | 0 error |
| Test backend | `dotnet test` (Unit+Integration+Architecture) | 2165 pass / 0 fail (baseline giữ nguyên) |
| Type-check FE | `npx tsc --noEmit` | 0 error |
| Migration 9193/9194 | apply thật qua `docker exec prodiab-mysql`, chạy lại lần 2 | idempotent OK |
| Việc 1 — tenant override + hide | SQL mô phỏng đúng query resolver cho 2 tenant | isolation OK (xem tenant-isolation.md) |
| Việc 3.1 — ngưỡng kho | FE dùng `useSettingNumber`, BE `/settings/public` whitelist is_public | OK |
| Việc 3.2 — role động | BE `EnsureSharedRolesExistAsync` + FE `listRoles` | OK |
| Việc 3.3 — đơn vị mg/dL | LabPlausibleRanges.Check thêm tham số unit | OK |
| Contract BE↔FE | đối chiếu field-by-field, JSON policy SnakeCaseLower | khớp |

## BUG phát hiện & fix trong lúc verify

**Ẩn mã hệ thống theo tenant không hoạt động** (CodeResolver.GetAsync):
- Query cũ lọc `is_hidden = 0` ở SQL → row đánh dấu-ẩn của tenant (is_hidden=1) bị loại,
  còn row global (is_hidden=0) vẫn lọt → mã "đã ẩn" vẫn hiển thị.
- Fix: bỏ lọc is_hidden ở SQL, xử lý ở tầng app — thu tập `hiddenCodes` (row tenant is_hidden=1)
  rồi loại luôn cả bản global cùng code. File:
  backend/src/ProDiabHis.Infrastructure/Services/CodeResolver.cs
- Đã re-verify bằng SQL: tenant A ẩn EMERGENCY → không còn trong kết quả; tenant B vẫn thấy EMERGENCY.
</content>
</invoke>
