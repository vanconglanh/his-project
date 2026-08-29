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

## KẾT QUẢ CUỐI — verified UI thật (rebuild frontend image + Playwright)
| Hạng mục | Verify | Kết quả |
|---|---|---|
| P1 RBAC | qc-roles.js | 6/6 role 403=0 |
| BUG-02 | POST /patients (bỏ CCCD) | 201 |
| BUG-03 | POST /vital-signs | 201, DB +1 dòng |
| BUG-05 | pay 200 + KTV POST /lab-results | 201, hết CLS_ORDER_UNPAID |
| BUG-06 | agent frontend | dropdown đủ + nút Lưu, API 200/201 |
| BUG-07 | GET /pharmacy/warehouses | 200, dropdown có 'Kho chính'/'Kho lẻ' |
| BUG-08 | /billings/{id} | load OK, hết 'Không tìm thấy' |

Bug phát sinh sửa thêm: VitalSignsForm chặn submit khi field số bỏ trống (NaH→NaN);
thiếu warehouse data (seed 9140); /users?role 403 (cấp user.read).

LƯU Ý HẠ TẦNG: frontend là baked image (docker-compose.local-app.yml, KHÔNG bind-mount)
=> mọi sửa .tsx phải `docker compose build frontend` + `up -d --force-recreate frontend`.
Cạm bẫy: `build ... | tail && echo DONE` che exit code — build fail vẫn in DONE. Phải
grep 'failed to solve' trong log.

Ưu tiên 3 (schema debt lab/rad orders): KHÔNG fix, đã tạo task chip riêng.
