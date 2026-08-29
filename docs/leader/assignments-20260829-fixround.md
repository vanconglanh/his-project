# Fix vòng QC full-flow 2026-08-29 (GATE FAIL 2 PASS/6 FAIL)

Nguồn: `docs/qc/evidence-fullflow-20260829/README.md`. Branch develop.

## ƯU TIÊN 1 — RBAC (XONG, verified UI)
- Root cause: seed gán role mã LEGACY chết (patient.create, vitals.write, cls.result,
  dispense.create, pharmacy.*, reception.create...) không controller nào enforce; thiếu
  mã mới `[RequirePermission]` thật sự đòi.
- Fix: migration `db/migrations/9139_reconcile_role_permissions.sql` — REPLACE mapping 5
  role non-admin bằng bộ curated chỉ gồm mã enforced đúng nghiệp vụ. Admin giữ nguyên.
- Verify: `frontend/e2e/qc-roles.js` → 6/6 role 403=0 trên màn của mình (trước: le_tan 11,
  bac_si 11, ktv 5, duoc_si 8, ke_toan 10). Không over-grant mã admin. Login mọi role OK
  (JWT < 4KB, không tái phát login loop).
- Commit: "fix(rbac): dong bo role_permissions..."

## ƯU TIÊN 2 — 6 Blocker
| BUG | Trạng thái | Cách fix |
|-----|-----------|----------|
| BUG-02 CCCD rỗng 400 | XONG (code) | PatientEditorLayout.buildPayload: empty string optional -> undefined |
| BUG-03 ghi sinh hiệu | XONG (code) | EncounterDetailClient: Sheet VitalSignsForm + useCreateVitalSigns, tách onOpenVitalForm |
| BUG-05 deadlock CLS | XONG (code) | ClsRoundCard nút "Thu tiền"/"Miễn phí" cho round SUBMITTED+UNPAID; hook usePay/useWaiveClsRound; cấp cls_round.pay/waive cho bac_si |
| BUG-06 dropdown thuốc | frontend agent đang chạy | — |
| BUG-07 URL kho 404 | XONG (code) | lib/api/pharmacy-warehouse.ts: /warehouses -> /pharmacy/warehouses |
| BUG-08 hoá đơn undefined | XONG (code) | billings/[id]/page.tsx: async + await params (Next 16 Promise) |

- Frontend là baked image (KHÔNG bind-mount) => phải rebuild image trước khi verify UI.
- Commit: "fix(fe): BUG-08+07+02", "fix(cls+vitals): BUG-03+05".

## ƯU TIÊN 3 — schema debt (KHÔNG fix, đã document + chip)
- `diab_his_lab_results` 62 dòng: 60 trỏ legacy `diab_his_lab_orders`, 2 trỏ `cli_lab_orders`.
  Report/exporter join legacy đang khớp ĐA SỐ data cũ; swap mù mất 60/62 dòng.
- `diab_his_rad_results` FK -> legacy `diab_his_rad_orders`, nhưng rad orders tạo ở
  `cli_rad_orders` => latent FK bug (rad 0 data nên chưa lộ).
- => Là bài toán hợp nhất 2 họ bảng (cần migration + sửa join đồng bộ), rủi ro cao, KHÔNG
  fix ẩu trong vòng này. Đã tạo task chip theo dõi.

## Bước còn lại
1. Chờ agent BUG-06 xong.
2. Rebuild frontend image 1 lần (cả fix của leader + BUG-06).
3. Verify UI full-flow: BUG-02/03/05/06/07/08 + qc-roles.
