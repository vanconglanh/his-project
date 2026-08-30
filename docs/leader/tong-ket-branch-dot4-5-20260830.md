# Tổng kết triển khai Đợt 4 + Đợt 5 — Đa chi nhánh (2026-08-30)

> Nguồn nghiệp vụ: `docs/prd/dinh-nghia-nghiep-vu-da-chi-nhanh-20260829.md`.
> Đợt 0–3 đã xong từ phiên trước. Phiên này làm Đợt 4 (dashboard chuỗi + công nợ nội bộ)
> và Đợt 5 (BHYT theo chi nhánh + vận hành chi nhánh mới). BỎ HĐĐT theo Facility (BR-80, Q3=Không).

## ✅ Đã xong

### Đợt 4 — Dashboard chuỗi + công nợ nội bộ
- **Dashboard chuỗi (BR-90/91/92/93, US-6.1)** — `GET /api/v1/dashboard/branch-ranking`,
  `GET /api/v1/dashboard/branch/{id}/detail`. Bảng xếp hạng doanh thu/lượt khám/BN mới/% thay đổi
  kỳ trước, drill-down bác sĩ, banner phạm vi dữ liệu "x/y chi nhánh", enforce scope S1/S2/S3.
  → FE `/reports/chain-dashboard`. Files: `backend/src/ProDiabHis.Application/Dashboard/`,
  `backend/src/ProDiabHis.Api/Controllers/DashboardController.cs`,
  `frontend/app/(dashboard)/reports/chain-dashboard/`.
- **Công nợ nội bộ (BR-84/85/86/87)** — bảng `diab_his_bil_inter_branch_debts` (mig `9174`) + entity.
  Sinh bút toán khi (a) trả nợ chéo chi nhánh và (b) điều chuyển kho RECEIVED. `GET /inter-branch-debts`
  + `POST /{id}/settle`. Report descriptor `inter-branch-debt` (tự hiện trong `/reports`).
  → FE `/cashier/inter-branch-debts`. Files:
  `backend/src/ProDiabHis.Application/Billing/InterBranchDebts/`, `.../Billing/PaymentHandlers.cs`,
  `.../Pharmacy/StockTransfers/StockTransferHandlers.cs`, `.../Infrastructure/Reports/ReportRegistry.cs`.
- Commit: `12ddee7` (BE), `764aaef` (FE).

### Đợt 5 — BHYT theo chi nhánh + vận hành chi nhánh mới
- **BHYT/CSKCB theo chi nhánh (BR-100..108, US-7.1)** — mig `9175` thêm field branch
  (hospital_rank, kcb_tuyen, bhyt_contract_code/valid_from/to, bhyt_enabled, dtqg_enabled) + wire cột
  `status` (BR-08). `GET /branches/bhyt-compliance` (BR-107). Guard BR-108 chặn khi thiếu cskcb tại
  export XML 4210 + submit ĐTQG. → FE tab "Tuân thủ theo chi nhánh" trong `/bhyt` + mở rộng `BranchForm`.
- **Clone + checklist go-live (BR-110/111/112, US-8.1)** — `POST /branches/{id}/clone` (copy cấu hình
  phòng/kho/bộ đếm/giá override, không copy dữ liệu vận hành), `GET /branches/{id}/readiness`
  (checklist 8 mục), `POST /branches/{id}/activate` (chặn nếu chưa đạt + audit). Chi nhánh mới = DRAFT.
  → FE badge trạng thái + clone dialog + checklist dialog trong `/admin/branches`.
- **Chuyển cơ sở nội bộ (BR-29)** — mig `9176` bảng `diab_his_clinic_internal_referrals` + entity,
  `POST /internal-referrals`, `GET /incoming`, `PATCH /{id}/status`. → FE `/encounters/referrals`.
- Files: `backend/src/ProDiabHis.Application/Branches/`, `.../Api/Controllers/BranchesController.cs`,
  `.../Api/Controllers/InternalReferralsController.cs`, `.../Domain/Entities/{Branch,InternalReferral}.cs`,
  `frontend/components/domain/{BranchForm,BranchCloneDialog,BranchReadinessDialog,InternalReferralCreateDialog}.tsx`,
  `frontend/app/(dashboard)/{admin/branches,bhyt,encounters/referrals}/`.
- Commit: `1057b39` (BE), `13a0605` (FE).

### Fix phát hiện qua verify (không có trong plan ban đầu — leader tự phát hiện & sửa)
- `193936c` — **BR-85 trả nợ chéo** trả `BILLING_NOT_FOUND` do global branch query filter trên Billing.
  Fix `IgnoreQueryFilters()` + giữ check tenant. Verify E2E sinh đúng bút toán.
- `57d1701` — **BR-111 clone** lỗi 500 do trùng unique `(tenant, code)` khi copy phòng/kho.
  Fix suffix code `-B{newId}`. Counters unique theo (tenant,branch,code) nên giữ nguyên.
- `4bf31af` — **Màn READ cross-branch** (dashboard chuỗi / quản lý CN / tuân thủ BHYT) chỉ thấy 1 CN
  khi user đã chọn 1 chi nhánh (X-Branch-Id tắt IgnoreBranchFilter). Fix scope theo quyền
  `branch.cross_view` (entitlement), không theo branch đang chọn.

## 🔍 Đã verify thế nào
- `dotnet build` → 0 error (sau mỗi nhóm việc + mỗi fix).
- `dotnet test` → **879 unit + 6 architecture + 5 integration** pass (baseline 858, +21 test mới).
- Migration `9174/9175/9176` áp vào MySQL thật (container prodiab-mysql), chạy 2 lần idempotent sạch.
- `npx tsc --noEmit` (frontend) → sạch.
- **API E2E LIVE** (backend rebuild + login thật admin): branch-ranking, drill-down, bhyt-compliance,
  readiness, activate (chặn đúng), clone, internal referral, và **BR-85 trả nợ chéo** (thu 10.000đ tại
  CN2 cho HD CN1 → sinh đúng bút toán debtor=2/creditor=1, doanh thu CN1 không đổi). Chi tiết:
  `docs/qc/evidence-branch-dot4-5-20260830/api-verify-backend.md`.
- **Browser thật** 4 màn FE (dashboard chuỗi + drill-down, tab tuân thủ BHYT, quản lý chi nhánh 3 CN
  + badge, chuyển cơ sở nội bộ). Chi tiết: `docs/qc/evidence-branch-dot4-5-20260830/browser-verify.md`.

## ⚠️ Giả định đã dùng (cần BO review nếu muốn khác)
- **7 câu hỏi BO còn treo** (Q1/Q2/Q5/Q6/Q7/Q9/Q10): triển khai theo đúng phương án mặc định mục 15 BRD.
  Đặc biệt Q6=Có (trả nợ chéo chi nhánh) đã hiện thực đầy đủ.
- **BR-87 điều chuyển kho sinh công nợ nội bộ**: vì Q3=Không (1 pháp nhân), điều chuyển kho KHÔNG xuất
  hoá đơn (BR-55). Bút toán công nợ nội bộ từ điều chuyển kho được hiểu là **đối soát nội bộ** (giá vốn),
  không phải giao dịch mua bán. Nếu BO muốn coi điều chuyển kho là thuần chuyển giá vốn (không phát sinh
  công nợ), bỏ hook trong `StockTransferReceiveLogic`.
- **Guard BR-108 tại luồng tiếp nhận BHYT (reception)**: `CreateEncounterCommand` hiện KHÔNG có field
  payer/BHYT để bám → mới guard tại export XML + submit ĐTQG (2 điểm rủi ro cao nhất R2). Nếu BO cần chặn
  ngay tại tiếp nhận, phải bổ sung field "hình thức thanh toán = BHYT" vào luồng check-in (thay đổi
  schema/DTO) — xem mục "Chưa làm / còn tồn".

## ❌ Chưa làm / còn tồn (ghi rõ, không silent drop)
- **BR-108 guard tại reception intake**: chưa chèn (thiếu field payer/BHYT trên check-in). Cần bổ sung
  field hình thức thanh toán khi tiếp nhận rồi mới guard. Ưu tiên P2.
- **Clone — lịch trực cụ thể** (`sch_doctor_schedules`): KHÔNG copy (gắn bác sĩ cụ thể, nhân sự không
  copy theo AC-8.1.1). Chi nhánh mới tự tạo lịch. Nếu cần "mẫu lịch trực" cần bảng template riêng.
- **Chuyển trạng thái SUSPENDED/CLOSED trên UI**: backend chỉ có bật/tắt is_active + activate; chưa có
  endpoint set-status full enum. FE mới hiển thị badge 6 trạng thái, chưa có nút chuyển SUSPENDED/CLOSED.
  Kèm bug tồn tại từ trước: `frontend/lib/api/branches.ts#setBranchStatus` gọi `POST /branches/{id}/status`
  không khớp route controller `PATCH /{id}/status` (không thuộc phạm vi đợt này, không sửa để tránh side-effect).
- **Internal referral**: field `encounter_id`/`referring_doctor_id` chưa có UI chọn (trang độc lập, chưa
  có ngữ cảnh encounter). Nút tạo referral đặt ở trang riêng, chưa nhúng vào chi tiết BN/encounter.
- **Công nợ nội bộ — cột chi nhánh trong danh sách công nợ bệnh nhân (DebtsTab)**: FE chưa chắc có branch
  info trong list công nợ hiện tại (nếu backend list chưa trả branch → cần bổ sung sau).
- **BR-114** (cảnh báo chi nhánh ≤5 lượt sau 30 ngày go-live) và **BR-113** (PDF báo cáo sẵn sàng go-live):
  chưa làm — mở rộng, ưu tiên thấp.

## 👉 Cần user/BO quyết
- Không có mục nào chặn. Tất cả giả định đã ghi ở trên, triển khai theo default BRD, BO chỉ cần review
  lại nếu muốn khác (đặc biệt: cơ chế công nợ nội bộ từ điều chuyển kho, và có cần guard BR-108 ngay tại
  tiếp nhận không).
