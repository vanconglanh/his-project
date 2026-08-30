# Evidence — Master data thuốc + Tham chiếu diaB + EMR template hoá (2026-08-30)

Nguồn thiết kế: `docs/prd/kien-truc-master-data-package-emr-20260830.md` (v1.2).
Verify trên stack local thật đã rebuild code mới (backend :5000, frontend :3000, MySQL `prodiab_his`).

## Tổng hợp verify tự động (build/test)

| Hạng mục | Kết quả |
|---|---|
| `dotnet build` backend | 0 error, 0 warning |
| `dotnet test` | 884 unit + 6 arch + 5 integration = **895 pass, 0 fail** |
| `EmrSignFlowTests` (ký số) | 9/9 pass — gồm case `V2_TamperStructuredValues_AfterSign_VerifyFails`, `V2_TamperSchemaSnapshot_AfterSign_VerifyFails`, `V1_Record_NullColumns_UsesV1Payload_VerifyOk`, `V1_And_V2_Payloads_AreDistinct` |
| `npx tsc --noEmit` frontend | sạch (exit 0) |
| Migration 9180/9181/9182 | áp dụng + verify cột/bảng trên DB thật; PRE-CHECK 9180: 30 dòng thuốc, tất cả `*_need_sync=0` (không lệch 9005/9010); `diabetes_templates` 0 dòng (convert no-op) |

## Kịch bản SNAPSHOT (yêu cầu trọng tâm) — verify qua API thật, có evidence JSON

Script: chạy trên encounter `1c49b2c1-...`, template tạo mới có `structured_json` = **S1** (3 field: Lý do khám / Huyết áp tâm thu / Khám bàn chân ĐTĐ).

| File | Bước | Kết quả |
|---|---|---|
| `01-create-template-S1.json` | Admin tạo EmrTemplate có `structured_json` S1 | 201, trả đúng S1 |
| `02-save-draft.json` | Bác sĩ lưu bệnh án dùng template + nhập `structured_values` | 200 |
| `03-sign.json` | Ký số | 200, `signed_at` set |
| `04-get-after-sign-S1.json` | Mở lại sau ký | `structured_values` đúng, **`schema_snapshot == S1`** |
| `05-update-template-S2.json` | **Sửa template gốc** sang **S2** (đổi label, textarea, bỏ field, thêm field mới) | 200 |
| `06-get-after-template-edit.json` | Mở lại bệnh án cũ | **`schema_snapshot` VẪN == S1** (không rò S2), `structured_values` không đổi |
| `07-get-template-now-S2.json` | GET template hiện tại | `structured_json == S2` (xác nhận template thật sự đã đổi) |

**KẾT LUẬN: SNAPSHOT IMMUTABILITY PASS = True.** Bệnh án đã ký render theo bản chụp schema tại thời điểm ký (S1), KHÔNG bị ảnh hưởng khi template gốc đổi (S2). Đúng thiết kế §5.8.2 (QĐ5).

## Mục 4 — Tham chiếu lộ trình diaB (phần HIS)

| File | Kết quả |
|---|---|
| `08-external-pathway.json` | `GET /api/v1/patients/{id}/external-pathway` → **HTTP 200**, `status=NOT_CONFIGURED`, `milestones=[]`, `error_message=null` (NullExternalPathwayProvider — luôn 200, không chặn luồng khám). Phần gọi API diaB thật vẫn BỊ CHẶN vì diaB chưa có endpoint. |

## Browser evidence (bổ sung trực quan)

- `10-*`, `11-*` (nếu có): screenshot màn khám render form động theo snapshot S1 + màn admin nhập `structured_json`. Xem báo cáo qc-agent.
