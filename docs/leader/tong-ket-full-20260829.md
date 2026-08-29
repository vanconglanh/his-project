# Tổng kết phiên điều phối FULL — Pro-Diab HIS (2026-08-29)

> Leader điều phối 8 wave sub-agent xử lý phần còn lại của `docs/TASKLIST-20260829.md`:
> **H-2→H-15 (trừ H-14)**, **E/Đợt 2-3 handler**, **C bước 2**. Toàn bộ đã verify build + unit test.

## Cách chạy
- Chia thành 8 work-package theo module để tránh 2 agent ghi cùng file, chạy song song 2 wave.
- Mỗi agent tự build/tsc trước khi trả về; Leader verify lại **build tổng + 747 unit test + tsc** sau khi tích hợp, tự fix phần gãy do tích hợp (test constructor + NSubstitute + UserStatus).

## ✅ Đã xong (theo commit)

| Nhóm | Nội dung | Commit |
|---|---|---|
| WP-A | E/Đợt2 group scope + guard tìm kiếm BN (BR-25/33/24); E/Đợt3 state machine điều chuyển kho 8+2 trạng thái (BR-54..62); H-2 permission cross_branch | `90ca998` |
| WP-B | E/Đợt3 giá override 3 tầng + PRICE_OVERLAP + snapshot billing_items (BR-70..76); H-9 QR VietQR động | `02c2156` |
| WP-D | H-8 danh mục ICD-10 telehealth cấu hình; H-7 ràng đơn telehealth↔encounter; H-6 API đồng bộ CGM | `04a414e` |
| WP-E | H-10 bắt buộc 2FA theo role (soft-gate mfaSetupRequired) | `1472c52` |
| WP-G + H-15 | C bước 2 rewrite 30 tham chiếu bảng chết lab/rad→cli_; 3 report descriptor gói dịch vụ | `c09b5e4` |
| WP-F + C-FE | H-5 annotation ảnh lâm sàng; H-12 reception "Còn X/Y"; H-13 badge cảnh báo gói | `2e20464` |

**Phát hiện đã có sẵn (không cần code lại):**
- **H-4** (rủi ro pháp lý P0): `DeletePatientCommandHandler` là **soft-delete THẬT** (DeletedAt/DeletedBy + audit) → KHÔNG hard-delete, KHÔNG vi phạm. Verify tại `PatientCommandHandler.cs:262`.
- **H-11**: `AddSubscriptionPaymentHandler` + bảng `diab_his_pkg_payment_records` đã tồn tại → thu từng lần có bản ghi riêng, không xuất khống.
- **H-13 backend**: `PackageAlertJob` đã đủ 4 rule + đăng ký RecurringJob trong `Program.cs`.

## 🔍 Đã verify thế nào
- `dotnet build src/ProDiabHis.Api/ProDiabHis.Api.csproj` → **Build succeeded, 0 Error**.
- `dotnet test tests/ProDiabHis.UnitTests` → **Passed! Failed: 0, Passed: 747** (đã tự fix 3 lỗi tích hợp: constructor `SearchPatientsQueryHandler`, NSubstitute nested-Returns, `UserStatus.Active` trong 2 test 2FA mới).
- `npx tsc --noEmit` (frontend) → **exit 0, 0 error**.
- H-4: đọc trực tiếp handler xác nhận soft-delete.

## ⚠️ Giả định / quyết định cần BO review
1. **H-10 role bắt buộc 2FA**: DB hiện KHÔNG có role "Quản lý chi nhánh" riêng (chỉ 6 role SYSTEM), tạm map "Super Admin + QL chi nhánh" → role `admin` (cấu hình `Security:MandatoryMfaRoles`). Nếu BO xác nhận có role QL chi nhánh riêng thì thêm code vào config.
2. **H-10 gap có sẵn**: luồng login hiện CHƯA verify 2FA lúc đăng nhập kể cả user đã bật 2FA optional — đây là gap tồn tại từ trước, NGOÀI phạm vi task (task chỉ yêu cầu policy "bắt buộc theo role"). Nên lên lịch bổ sung verify TOTP ở login.
3. **H-9 cấu hình QR**: tài khoản nhận tiền đọc từ settings key `bil.qr_bank_bin/account_no/account_name` — chưa có UI cấu hình, tenant phải set qua API settings; thiếu → lỗi `BANK_ACCOUNT_NOT_CONFIGURED`. Cần bổ sung màn cấu hình.
4. **H-9 resolver giá**: chỉ áp cho dòng hoá đơn `Type=SERVICE` (BR-70..76 chỉ đề cập `bil_services`); dòng LAB/RAD/DRUG giữ nguyên logic giá cũ.
5. **Stock transfer**: quyền `stock_transfer.close/cancel` tái dùng `receive/create` vì BRD chỉ seed 5 quyền read/create/approve/ship/receive; role `quan_ly_vung` chưa tồn tại (migration cấp quyền bỏ qua an toàn nếu role chưa có).

## ❌ Chưa làm / còn tồn
- **H-14 (FR-1211)**: CỐ Ý BỎ QUA — quyết định kinh doanh (chính sách định mức chưa dùng khi gói hết hạn), cần hỏi BO trước khi thiết kế. Không tự giả định.
- **C bước 2 — DROP 2 bảng chết** `diab_his_lab_orders`/`diab_his_rad_orders`: **CHƯA DROP** (đúng ngoại lệ rủi ro mất dữ liệu). Đã rewrite xong 30 tham chiếu sang bảng cli_ (build+test xanh), NHƯNG WP-G flag nghi ngờ một số cột nhánh rad chưa chắc khớp 100% giữa bảng chết và bảng sống. Việc DROP không hồi phục → cần bước riêng: chạy query "0 dòng orphan" trên DB thật (docker MySQL local) + đối chiếu đủ cột rad, xác nhận an toàn rồi mới `DROP TABLE`. Để đợt sau.
- **Browser E2E evidence**: xem phần dưới.

## 📁 File quan trọng đã thay đổi
- Backend mới: `Application/Pharmacy/StockTransfers/*`, `Api/Controllers/StockTransfersController.cs`, `Application/Billing/{IServicePriceResolver,IVietQrBuilder,ServicePriceOverrideHandlers}.cs`, `Infrastructure/Billing/ServicePriceResolverImpl.cs`, `Domain/Entities/ServiceBranchPrice.cs`, `Api/Controllers/ServicePriceOverridesController.cs`, `Application/Telehealth/TelehealthIcd10AdminHandlers.cs`.
- Backend sửa: `Application/Patients/PatientQueryHandler.cs`, `Api/Middlewares/BranchScopeMiddleware.cs`, `Application/Auth/LoginCommandHandler.cs`, `Application/Billing/{BillingHandlers,PaymentHandlers}.cs`, 8 file schema-debt (lab/rad→cli_), `Infrastructure/Reports/ReportRegistry.cs` (H-15).
- Frontend mới: `components/domain/ImageAnnotationDialog.tsx`, `lib/api/packages.ts`, `lib/hooks/use-packages.ts`.
- Migration mới: `9161` (cross_branch permission), `9165` (service.price_override + billing branch), `9170` (telehealth_allowed_icd10) — đều idempotent theo convention.

## 👉 Cần user/BO quyết
- H-14: chính sách định mức gói hết hạn (cộng dồn/mất/gia hạn).
- 5 giả định ở mục ⚠️ ở trên (đặc biệt: role QL chi nhánh, verify 2FA lúc login, UI cấu hình QR).
- Xác nhận trên DB thật để DROP 2 bảng chết lab/rad_orders.
