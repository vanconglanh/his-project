# Audit: Hard-code trong code vs. Master data / Config table

- **Ngày:** 2026-09-01
- **Người thực hiện:** Lành (architect)
- **Nhánh:** `develop`
- **Bối cảnh:** BO yêu cầu hạn chế hard-code danh mục / quy tắc nghiệp vụ trong code, chuyển sang master data hoặc config table để admin tự chỉnh, **không cần deploy lại**. Chuẩn tham chiếu là RBAC (role/permission) — đã làm đúng data-driven từ đầu.

---

## 0. Tóm tắt điều hành

Phát hiện quan trọng nhất: **hạ tầng master data đã tồn tại nhưng gần như không được dùng.**

| Hạ tầng đã có | Trạng thái sử dụng |
|---|---|
| `diab_his_sec_roles` / `sec_permissions` / `sec_role_permissions` + `RequirePermissionAttribute` | ✅ Dùng đúng, đầy đủ — CHUẨN THAM CHIẾU |
| `diab_his_sys_settings` (key-value, có `tenant_id NULL = global`) — migration 9095 | ⚠️ Chỉ dùng cho **3 khoá**: `stock_transfer_approval_threshold`, `pkg.min_deposit_percent`, `package_expiry_extension_days` |
| `diab_his_sys_code_master` / `diab_his_sys_code_detail` — migration 9034/9035 + API `GET /api/v1/codes/{groupId}` + hook `useCodes()` | ❌ **Gần như chết**: chỉ 2 file FE dùng (`ServiceForm.tsx`, `DrugForm.tsx`). Phần còn lại vẫn đọc `frontend/lib/constants/code-labels.ts` hard-code |
| `diab_his_sys_feature_flags` — migration 0061 | ✅ Dùng đúng |
| EMR template `structured_json` (0026, 9182, 9190) | ✅ Dùng đúng — template khám hoàn toàn data-driven |
| Branch price / item visibility override (9152, 9185) | ✅ Dùng đúng — giá & hiển thị theo chi nhánh |
| Notification channels config (9160) | ✅ Dùng đúng — kênh + secret lưu DB |
| CDSS tương tác thuốc `diab_his_cdss_ddi_pairs` (9045) | ✅ Dùng đúng — cặp tương tác là data |

> **Kết luận nhanh:** Không cần phát minh cơ chế mới. Việc cần làm chủ yếu là **(a) bổ sung `tenant_id` cho `code_master/code_detail`** để mỗi phòng khám override được, **(b) migrate các danh mục đang hard-code vào 2 cơ chế sẵn có**, **(c) rà lại các ngưỡng nghiệp vụ đang là literal trong code sang `sys_settings`**.

**Khiếm khuyết chặn ngay:** `diab_his_sys_code_master`/`code_detail` (migration 9034) **KHÔNG có cột `tenant_id`** → hiện chỉ là danh mục toàn cục. Mà yêu cầu của BO chính là "mỗi phòng khám có thể cần khác nhau chút ít". Đây là việc **P0 số 1**.

---

## 0b. Trạng thái xử lý (cập nhật 2026-09-01)

Đã triển khai đợt P0 nền tảng + 3 bug xác nhận. `dotnet test` 2165 PASS/0 FAIL, `tsc` sạch, migration 9193/9194 verify idempotent trên DB thật. Evidence: `docs/qc/evidence-master-data-config-20260901/`.

| Hạng mục | Trạng thái | Ghi chú |
|---|---|---|
| P0-1 `tenant_id` cho code_detail/master + `ICodeResolver` + Admin CRUD API | ✅ Done | Migration `9193`. Resolve tenant>global, fallback mặc định, cache 5 phút. Màn `/admin/master-codes`. Đã fix thêm bug hide mã hệ thống phát hiện khi verify. |
| P0-2 Migrate FE (A2/A3/A4) sang `useCodes()` | ✅ Done | patient-schema.ts, PatientGeneralTab.tsx, code-labels.ts (giữ làm fallback). ENCOUNTER_TYPE vs VISIT_TYPE xác nhận là 2 nhóm khác nhau, seed đã khớp code — không seed lại. |
| P0-4 Public settings endpoint (B2) | ✅ Done | `GET /api/v1/settings/public` (whitelist qua `sys_setting_meta.is_public`). FE bỏ hằng số 5tr. |
| P0-5 Role list động report sharing (A5) | ✅ Done | FE `listRoles`; BE validate `ROLE_NOT_FOUND`. |
| Việc 4 — Màn quản lý cấu hình chung | ✅ Done | Migration `9194` bảng `sys_setting_meta` (nhãn tiếng Việt + is_public). Màn `/admin/settings`. |
| A11 — LabPlausibleRanges đơn vị mg/dL | ✅ Done | `Check(...)` thêm tham số `unit`, bảng ngưỡng riêng mg/dL. |
| P0-2 các nhóm PAYMENT_METHOD/SERVICE_CATEGORY/LEGACY_DOC_TYPE... (A6/A7/A8) | ⏳ Chưa | Hạ tầng đã sẵn (tenant override + admin UI). Migrate BE validator sang `ICodeResolver` + seed nhóm còn thiếu là đợt kế tiếp. |
| Nhóm P1 (A9-A16, B7, D1) | ⏳ Chưa | Gom đợt sau theo khuyến nghị mục 4 (dùng chung pattern resolver + tenant override). |

---

## 1. Bảng audit chi tiết

### Nhóm A — Enum / const danh mục nghiệp vụ trong C#

| # | Vị trí (file:dòng) | Đang hardcode gì | Rủi ro / bất tiện | Đề xuất chuyển thành master data | Ưu tiên |
|---|---|---|---|---|---|
| A1 | `backend/src/ProDiabHis.Domain/Entities/Encounter.cs:63-69` `EncounterTypes` | 4 loại hình khám: `FIRST_VISIT`, `FOLLOW_UP`, `EMERGENCY`, `CONSULTATION` | Phòng khám Nội tiết muốn thêm "Khám tầm soát ĐTĐ", "Tư vấn dinh dưỡng", "Khám sức khoẻ doanh nghiệp" → phải sửa code + deploy. Đồng thời FE có bản sao **khác nhau** (`patient-schema.ts` dùng `SPECIALIST` thay cho `CONSULTATION`!) → lệch dữ liệu | Nhóm mã `ENCOUNTER_TYPE` trong `code_master`; validate bằng lookup DB thay vì `const` | **P0** |
| A2 | `frontend/app/(dashboard)/patients/_components/patient-schema.ts:6-8` | `PATIENT_TYPES`, `MARITAL_STATUSES`, `VISIT_TYPES` (zod enum cứng) | Đối tượng bệnh nhân (`SERVICE/BHYT/FREE/CONTRACT`) khác nhau theo phòng khám (có nơi cần "Bảo hiểm tư nhân", "Người nhà nhân viên", "Gói doanh nghiệp"). zod enum cứng chặn cả dữ liệu hợp lệ từ BE | Nhóm mã `PATIENT_TYPE`, `MARITAL_STATUS`, `VISIT_TYPE`; zod chuyển sang `z.string()` + validate runtime theo danh sách từ `useCodes()` | **P0** |
| A3 | `frontend/lib/constants/code-labels.ts` (toàn file) | ~15 nhóm mã (`GENDER`, `BLOOD_TYPE`, `NATIONALITY`, `MARITAL_STATUS`, `PATIENT_TYPE`, `VISIT_TYPE`, `RELATIONSHIP`, `ENCOUNTER_TYPE`, `ENCOUNTER_STATUS`, `DIABETES_TYPE`, `MODALITY`…) | Chính comment trong file đã ghi *"PHA 2: khi có API /codes/{groupId}, thay dần các hằng số này"* — API **đã có từ lâu** nhưng chưa migrate. Bundle FE giữ danh mục cứng, admin không sửa được nhãn | Xoá dần file, thay bằng `useCodes(groupId)`; giữ 1 bản fallback tối thiểu cho offline/SSR | **P0** |
| A4 | `frontend/app/(dashboard)/patients/_components/PatientGeneralTab.tsx:55-69` | `VISIT_TYPE_LABELS` + `NATIONALITY_OPTIONS` (6 quốc gia) | Bản sao thứ 3 của cùng danh mục (đã có ở `code-labels.ts` và `patient-schema.ts`). Phòng khám có bệnh nhân Lào/Campuchia/Đài Loan → không chọn được | Dùng `useCodes("NATIONALITY")` / `useCodes("VISIT_TYPE")` | **P0** |
| A5 | `frontend/app/(dashboard)/reports/builder/_components/SaveReportDialog.tsx:13-20` + `frontend/lib/api/reports.ts:237` `ReportRoleCode` | Danh sách 6 role cứng (`bac_si`, `le_tan`, `duoc_si`, `ke_toan`, `ky_thuat_vien`, `admin`) làm union type TypeScript | **Mâu thuẫn trực tiếp với RBAC động.** Admin tạo role mới (vd "Điều dưỡng", "Quản lý chi nhánh") qua UI → không chia sẻ báo cáo cho role đó được, vì FE không biết role đó tồn tại | Gọi `GET /api/v1/roles` (đã có) để render checkbox; đổi `ReportRoleCode` thành `string` | **P0** |
| A6 | `backend/src/ProDiabHis.Application/Reports/PaymentBreakdownCalculator.cs:12-22` + `frontend/components/domain/PaymentDialog.tsx:26` + `frontend/app/(dashboard)/cashier/_components/PaymentHistoryTab.tsx:18` + `frontend/lib/api/payments.ts:7` + `messages/vi.json:881` | Danh sách phương thức thanh toán (`CASH`, `BANK_TRANSFER`, `VISA`, `MASTER`, `QR_VIETQR`, `QR_MOMO`, `QR_VNPAY`, `OTHER`) — lặp lại ở **5 nơi** | Phòng khám không dùng Momo nhưng vẫn hiện; muốn thêm "Ví ZaloPay", "Công nợ công ty", "Voucher" → sửa 5 file + deploy. Nguy cơ lệch giữa BE và FE | Nhóm mã `PAYMENT_METHOD` (có `tenant_id`, `is_active`, `sort_order`, `extra` chứa icon + phím tắt). BE lấy label từ lookup, FE render động | **P0** |
| A7 | `backend/src/ProDiabHis.Application/Billing/ServiceCatalogHandlers.cs:52-53` | `ValidCategories = [CONSULTATION, PROCEDURE, LAB, RAD, PHARMACY, OTHER]` và `ValidVatRates = [0,5,8,10]` | Nhóm dịch vụ khác nhau theo mô hình phòng khám (vd "Vật lý trị liệu", "Dinh dưỡng", "Tiêm chủng"). **Thuế suất VAT thay đổi theo chính sách Nhà nước** (8% từng là ưu đãi có thời hạn) → thay đổi luật = phải deploy | Nhóm mã `SERVICE_CATEGORY`; VAT → `sys_settings` khoá `billing.valid_vat_rates` (CSV) | **P0** |
| A8 | `backend/src/ProDiabHis.Application/LegacyImport/LegacyImportDtos.cs:54-65` `LegacyImportDocTypes` | 3 loại tài liệu: `HO_SO_CU_SCAN`, `DON_THUOC_NGOAI`, `GIAY_CHUYEN_VIEN` | **BO nêu đích danh làm ví dụ.** Nhu cầu thực tế còn: "Kết quả CLS ngoại viện", "Giấy ra viện", "Sổ tiêm chủng", "CMND/CCCD scan", "Giấy cam kết". Mỗi lần thêm = 1 lần deploy | Nhóm mã `LEGACY_DOC_TYPE` (có `tenant_id`); `Normalize()` fallback về mã có `extra.is_default = true` | **P0** |
| A9 | `backend/src/ProDiabHis.Application/InBody/IInBodyDataProvider.cs:31-52` `InBodyIndicatorTypes` | 9 chỉ số InBody + danh sách `IndicatorTableTypes` ghi vào bảng generic | Máy InBody đời khác trả thêm chỉ số (ECW/TBW ratio, Segmental lean, Bone mineral, Waist-Hip Ratio). Muốn hiển thị/lưu thêm = deploy | Bảng riêng `diab_his_cli_indicator_definition` (`tenant_id`, `code`, `label_vi`, `unit`, `is_stored_in_indicator_table`, `sort_order`, `min_plausible`, `max_plausible`) — gộp luôn A10 | **P1** |
| A10 | `backend/src/ProDiabHis.Application/InBody/InBodyPlausibleRanges.cs:13-24` | 9 khoảng vật lý khả dĩ (Weight 2-400kg, BMI 5-100, PBF 1-70%…) | Cảnh báo sai/thiếu → người nhập bị làm phiền hoặc bỏ lọt lỗi OCR. Phòng khám nhi hoặc chuyên béo phì cần ngưỡng khác | Gộp vào bảng ở A9 (`min_plausible`/`max_plausible` per tenant). Fallback về hằng số hiện tại khi không có dòng cấu hình | **P1** |
| A11 | `backend/src/ProDiabHis.Application/LabResults/Ocr/LabPlausibleRanges.cs:22-48` | 17 quy tắc keyword → khoảng vật lý (HBA1C 2-20, GLUCOSE 1-100…) | **Vấn đề đơn vị:** ngưỡng đang giả định glucose mmol/L. Phòng khám dùng mg/dL (70-180) sẽ bị cảnh báo sai **mọi kết quả**. Thêm XN mới (Insulin, C-peptide, Microalbumin) không có ngưỡng | Bảng `diab_his_cli_lab_plausible_range` (`tenant_id`, `keyword`, `unit`, `min_value`, `max_value`, `priority`, `note`). Ưu tiên gắn theo `lab_test_id` nếu có danh mục XN chuẩn, fallback keyword | **P1** |
| A12 | `backend/src/ProDiabHis.Application/Documents/DocumentClassifierService.cs:38-61` | `LabFormMarkers` (9 chuỗi) + `InBodyLabels` (8 chuỗi) + ngưỡng điểm 0.9/0.75/0.6/0.55/0.5 + `ConfidenceThreshold = 0.6` | Mỗi labo/phòng khám dùng mẫu phiếu chữ khác ("Chỉ số", "Kết quả XN", "Hoá sinh"). Không chỉnh được → phân loại sai, người dùng phải sửa tay liên tục. Ngưỡng tin cậy không tinh chỉnh được theo thực tế từng nơi | Bảng `diab_his_doc_classifier_marker` (`tenant_id`, `doc_type`, `marker_text`, `weight`, `is_active`) + `sys_settings` khoá `doc.classifier_confidence_threshold` | **P1** |
| A13 | `backend/src/ProDiabHis.Application/LabResults/Ocr/LabResultOcrParser.cs:32-66` | `CodeAliases` (24 mã XN × nhiều bí danh) + `KnownUnits` (~30 đơn vị) | Danh mục XN mỗi nơi đặt mã khác nhau. Không thêm alias được → OCR không map ra field, phải nhập tay | Thêm cột `ocr_aliases JSON` vào bảng danh mục XN hiện có (đỡ tạo bảng mới); `KnownUnits` → nhóm mã `LAB_UNIT` | **P1** |
| A14 | `backend/src/ProDiabHis.Application/PublicApi/PortalMedReminderHandlers.cs:57-63` | Giờ nhắc uống thuốc: SÁNG 07:00, TRƯA 11:30, CHIỀU 15:00, TỐI 19:00 | Giờ sinh hoạt/giờ làm việc mỗi phòng khám khác nhau; bệnh nhân bị nhắc sai giờ → giảm tuân thủ điều trị | `sys_settings` 4 khoá `portal.med_reminder_slot.{SANG,TRUA,CHIEU,TOI}` hoặc nhóm mã `MED_REMINDER_SLOT` với `extra.time` | **P1** |
| A15 | `backend/src/ProDiabHis.Application/Appointments/AppointmentDtos.cs:45,52` | Trạng thái lịch hẹn + nguồn đặt hẹn (`WALK_IN`, `PHONE`, `WEB`, `API`, `APP`) | **Nguồn đặt hẹn** rất dễ phát sinh (Zalo OA, Facebook, Docosan, đối tác BHYT) → cần thống kê theo nguồn. Trạng thái thì ổn định hơn | Nguồn → nhóm mã `APPOINTMENT_SOURCE` (**P1**). Trạng thái → **giữ trong code** (gắn với state machine, xem mục 3) | **P1** |
| A16 | `backend/src/ProDiabHis.Application/ChronicCare/RecallHandlers.cs:98` | `ValidStatuses = [PENDING, CONTACTED, SCHEDULED, DONE, DISMISSED]` | Quy trình recall bệnh mạn tính mỗi nơi khác (có nơi cần "Không liên lạc được", "Từ chối", "Chuyển tuyến") | Nhóm mã `RECALL_STATUS` + bảng chuyển trạng thái cho phép cấu hình (hoặc bỏ ràng buộc thứ tự, chỉ validate thuộc danh sách) | **P2** |
| A17 | `backend/src/ProDiabHis.Domain/Entities/EncounterAddendum.cs:26-39` `AddendumSection` | 6 section được phép bổ sung bệnh án | Gắn với cấu trúc bệnh án; ít thay đổi nhưng nếu EMR template thêm section mới (đã data-driven!) thì addendum không theo kịp → **lệch giữa 2 cơ chế** | Sinh danh sách section từ EMR template `structured_json` thay vì const | **P2** |
| A18 | `backend/src/ProDiabHis.Application/Packages/PackageHandlers.cs:47` | `ValidTypes = [VISIT, SERVICE, DRUG]` cho gói dịch vụ | Gói combo thực tế còn có "Vật tư", "Tư vấn từ xa" | Nhóm mã `PACKAGE_ITEM_TYPE` — nhưng lưu ý enum này gắn với logic trừ quyền lợi khác nhau theo type → cần code path tương ứng, không thuần data | **P2** |
| A19 | `backend/src/ProDiabHis.Infrastructure/Cdss/CdssEngineImpl.cs:24` | `SeverityOrder = [CONTRAINDICATED, MAJOR, MODERATE, MINOR]` | Ít thay đổi (chuẩn quốc tế). Nhưng phòng khám có thể muốn "chỉ chặn cứng mức CONTRAINDICATED, cảnh báo mềm MAJOR" | Giữ thứ tự trong code; thêm `sys_settings` khoá `cdss.blocking_severity_min` để chọn mức chặn | **P2** |
| A20 | `backend/src/ProDiabHis.Application/Diabetes/Cgm/CgmHandlers.cs:27` | `SupportedProviders = { "Dexcom" }` | Thêm nhà cung cấp CGM (Abbott Libre, Medtrum) cần code adapter thật → không phải thuần data | **Giữ hard-code**, đây là capability của code chứ không phải master data. Chỉ cần bật/tắt qua feature flag | — |

### Nhóm B — Ngưỡng / quy tắc nghiệp vụ

| # | Vị trí | Đang hardcode gì | Đánh giá | Đề xuất | Ưu tiên |
|---|---|---|---|---|---|
| B1 | `backend/.../StockTransfers/StockTransferHandlers.cs:254` | `GetDecimalAsync("stock_transfer_approval_threshold", 5_000_000m)` | ✅ **XÁC NHẬN LÀM ĐÚNG.** Đọc từ `sys_settings`, seed sẵn ở migration `9151_stock_transfers.sql:75`, literal chỉ là fallback | Không đổi ở BE. **Nhưng xem B2** | — |
| B2 | `frontend/lib/api/stock-transfers.ts:103` | `export const STOCK_TRANSFER_APPROVAL_THRESHOLD = 5_000_000;` | ❌ **Lỗi rò rỉ**: BE configurable nhưng FE hard-code lại → admin đổi ngưỡng ở BE thì UI vẫn cảnh báo theo 5tr cũ. Sai lệch nghiệp vụ, khó phát hiện | Bổ sung `GET /api/v1/settings/public` trả các khoá được whitelist cho FE; FE dùng `useSetting("stock_transfer_approval_threshold")` | **P0** |
| B3 | `backend/.../Packages/PackageSubscriptionHandlers.cs:143` | `pkg.min_deposit_percent` qua `sys_settings` | ✅ Làm đúng | — | — |
| B4 | `backend/.../Infrastructure/Jobs/PackageAlertJob.cs` + `package_expiry_extension_days` (9172) | Qua `sys_settings` | ✅ Làm đúng | — | — |
| B5 | Timeout tích hợp: `DtqgOptions.TimeoutSeconds=30`, `DocosanOptions=20`, `AzureOpenAiOptions=15`, `VnptSmartCa=30`, `LabPartnerHttpClient=5s/30s` | Literal C# + override qua `appsettings` | Hầu hết đã bind từ `configuration[...]` (`DependencyInjection.cs:228,330`) → đổi được bằng env var, không cần build lại. **Đây là config kỹ thuật, KHÔNG phải master data** | Giữ nguyên. Ngoại lệ: `LabPartnerHttpClient.cs:24,54` timeout 5s/30s là literal cứng, không bind config → nên đưa vào `appsettings` | **P2** |
| B6 | Rate limit 100 req/phút/user, 1000/tenant (theo `CLAUDE.md` §6) | Cần kiểm tra nơi cài đặt thực tế | Kỹ thuật thuần, gắn với hạ tầng Redis. Không nên đưa vào master data nghiệp vụ | Để ở `appsettings` / env; nếu cần phân biệt theo gói SaaS thì đưa vào `diab_his_sys_tenants` (cột `plan_*`) | **P2** |
| B7 | `DocumentClassifierService.cs:31` `ConfidenceThreshold = 0.6` + các mốc điểm 0.9/0.75/0.55/0.5 | Ngưỡng thuật toán ảnh hưởng trực tiếp trải nghiệm vận hành hằng ngày | Xem A12 — nên tách ngưỡng ra `sys_settings` | **P1** |
| B8 | `backend/.../Files/FileHandlers.cs:132`, `Documents/SmartUploadBatchCommandHandler.cs:23`, `InBody/InBodyHandlers.cs:60`, `LabOcrHandlers.cs:22`, `RadOcrHandlers.cs:21`, `Jobs/LegacyImportFileKind.cs:25,27` | Whitelist MIME / đuôi file | **Quyết định bảo mật** — cố tình hẹp. Cho admin sửa = mở lỗ hổng upload | **Giữ hard-code.** Nếu cần mở rộng thì làm qua release có review bảo mật | — |
| B9 | `backend/.../Reports/Engine/SafeQueryBuilder.cs:21` `Allowed`, `Infrastructure/Reports/ReportCacheImpl.cs:55` `AllowedTables` | Whitelist bảng/hàm cho report builder | **Quyết định bảo mật chống SQL injection** | **Giữ hard-code tuyệt đối.** Không bao giờ đưa vào DB config | — |

### Nhóm C — State machine (đánh giá riêng)

| # | Vị trí | Nội dung | Kết luận |
|---|---|---|---|
| C1 | `Domain/Entities/Encounter.cs:47`, `ClsOrder.cs:49,70`, `ClsOrderRound.cs:34,53`, `LabResult.cs:98` | `ValidTransitions` — bảng chuyển trạng thái | **KHÔNG chuyển thành master data.** Mỗi transition gắn với side-effect trong code (trừ kho, tạo phiếu thu, khoá bệnh án). Cho admin sửa transition = tạo lỗ hổng nghiệp vụ không kiểm soát được. Chỉ nên cấu hình phần **hiển thị** (nhãn, màu badge) qua `code_master`, giữ transition trong code |
| C2 | `Domain/Entities/Encounter.cs:55-57` `IsLockedStatus` | Trạng thái terminal khoá bệnh án | Giữ trong code (quy định pháp lý về bệnh án) |

### Nhóm D — Menu / Navigation

| # | Vị trí | Hiện trạng | Đánh giá | Đề xuất | Ưu tiên |
|---|---|---|---|---|---|
| D1 | `frontend/lib/config/nav-items.ts:49` `NAV_GROUPS` + `frontend/components/layout/AppSidebar.tsx:76-80` | Cấu trúc menu hard-code ở FE, **nhưng đã lọc theo permission** (`item.permissions` + `isItemVisible()`) | ⚠️ **Đã làm đúng một nửa.** Menu tự ẩn theo quyền → role mới không thấy menu không có quyền. Phần chưa động: (a) thứ tự & gom nhóm cố định, (b) `labelVi` của group hard-code tiếng Việt trong code (item thì dùng i18n), (c) không tắt được module theo tenant (vd phòng khám không có kho dược vẫn thấy nhóm "Dược") | Ngắn hạn: dùng `diab_his_sys_feature_flags` (đã có) để ẩn cả nhóm theo tenant + đưa `labelVi` vào i18n. Dài hạn (P2): bảng `diab_his_sys_menu_item` (`tenant_id`, `parent_id`, `href`, `label_key`, `icon_name`, `permission_code`, `sort_order`, `is_active`) — icon map theo tên string vì `LucideIcon` không serialize được | **P1** (feature flag) / **P2** (menu table) |

---

## 2. Thiết kế cho các hạng mục P0

### P0-1. Thêm `tenant_id` cho code master (nền tảng cho mọi P0 còn lại)

Đây là **điều kiện tiên quyết**. Không có nó thì "mỗi phòng khám khác nhau chút ít" không thực hiện được.

```
db/migrations/92xx_code_detail_tenant_override.sql
```

```sql
-- 1) Cho phép override theo tenant. NULL = mã chuẩn toàn hệ thống.
CALL add_col_if_missing('diab_his_sys_code_detail', 'tenant_id',
    "INT NULL COMMENT 'NULL = ma chuan he thong; co gia tri = ma rieng cua tenant'");

-- 2) Cột sinh để làm UNIQUE (cùng thủ thuật đã dùng ở 9095_create_sys_settings)
CALL add_col_if_missing('diab_his_sys_code_detail', 'tenant_scope',
    "INT AS (COALESCE(`tenant_id`, 0)) STORED");

-- 3) Cho tenant ẨN một mã chuẩn mà không xoá được dòng global
CALL add_col_if_missing('diab_his_sys_code_detail', 'is_hidden',
    "TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Tenant an ma chuan nay'");

CALL add_col_if_missing('diab_his_sys_code_detail', 'is_system',
    "TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 = ma he thong, khong cho xoa (chi an duoc)'");

-- 4) UNIQUE mới: (tenant_scope, code_master_id, code)
--    Đồng thời cho phép code_master cũng có bản ghi riêng theo tenant nếu cần nhóm mã tuỳ biến.
```

**Quy tắc resolve (đặt trong `CodeResolverService`, cache Redis TTL 5 phút, invalidate khi ghi):**

1. Lấy toàn bộ `code_detail` của nhóm có `tenant_id IS NULL` (mã chuẩn) `UNION` `tenant_id = @tenantId` (mã riêng + override).
2. Nếu cùng `code` xuất hiện ở cả 2 → **bản của tenant thắng** (cho phép đổi `name`, `sort_order`).
3. Loại bỏ dòng `is_hidden = 1` hoặc `is_active = 0`.
4. Sắp xếp `sort_order`, `code`.

**API bổ sung (`docs/api/codes.yaml`):**

| Method | Path | Permission | Mô tả |
|---|---|---|---|
| `GET` | `/api/v1/codes` | (đã đăng nhập) | Danh sách nhóm mã (đã có) |
| `GET` | `/api/v1/codes/{groupId}` | (đã đăng nhập) | Mã trong nhóm — **sửa: resolve theo tenant** |
| `GET` | `/api/v1/codes/batch?ids=A,B,C` | (đã đăng nhập) | Nạp nhiều nhóm (đã có) |
| `POST` | `/api/v1/admin/codes/{groupId}/details` | `code.manage` | Tenant tạo mã riêng |
| `PUT` | `/api/v1/admin/codes/{groupId}/details/{id}` | `code.manage` | Sửa nhãn / thứ tự / `extra` |
| `PATCH` | `/api/v1/admin/codes/{groupId}/details/{code}/visibility` | `code.manage` | Ẩn/hiện mã chuẩn (`is_hidden`) |
| `DELETE` | `/api/v1/admin/codes/{groupId}/details/{id}` | `code.manage` | Xoá mã riêng (chặn nếu `is_system = 1`) |

**Mã lỗi:** `CODE_GROUP_NOT_FOUND`, `CODE_DUPLICATED`, `CODE_IS_SYSTEM_READONLY`, `CODE_IN_USE_CANNOT_DELETE`.

**Ràng buộc quan trọng — chống hỏng dữ liệu lịch sử:** không cho xoá mã đang được tham chiếu. Trước khi `DELETE`, đếm bản ghi nghiệp vụ đang dùng mã đó; nếu > 0 → trả `CODE_IN_USE_CANNOT_DELETE`, gợi ý dùng "ẩn" thay vì xoá. Đây là lý do bắt buộc phải có `is_hidden`.

**Permission mới cần seed:** `code.read`, `code.manage` (migration seed vào `diab_his_sec_permissions` + gán cho role `admin`).

### P0-2. Seed & migrate các nhóm mã đang hard-code

Migration seed các nhóm sau vào `code_master`/`code_detail` với `tenant_id = NULL`, lấy nguồn từ `frontend/lib/constants/code-labels.ts`:

| `code_master.id` | Nguồn hiện tại | Ghi chú |
|---|---|---|
| `ENCOUNTER_TYPE` | `Encounter.cs:63` + `code-labels.ts:44` | **Hoà giải lệch `CONSULTATION` vs `SPECIALIST`** trước khi seed |
| `PATIENT_TYPE` | `patient-schema.ts:6` | |
| `MARITAL_STATUS` | `patient-schema.ts:7` | |
| `VISIT_TYPE` | `patient-schema.ts:8` | |
| `NATIONALITY` | `PatientGeneralTab.tsx:62` | Nên seed đủ danh sách ISO 3166 thay vì 6 nước |
| `GENDER`, `BLOOD_TYPE`, `RELATIONSHIP`, `DIABETES_TYPE`, `MODALITY` | `code-labels.ts` | |
| `PAYMENT_METHOD` | `PaymentBreakdownCalculator.cs:12` | `extra` = `{"icon":"💵","hotkey":"1","requires_ref":false}` |
| `SERVICE_CATEGORY` | `ServiceCatalogHandlers.cs:52` | |
| `LEGACY_DOC_TYPE` | `LegacyImportDtos.cs:54` | `extra.is_default = true` cho `HO_SO_CU_SCAN` |
| `APPOINTMENT_SOURCE` | `AppointmentDtos.cs:52` | |

Các nhóm gắn state machine (`ENCOUNTER_STATUS`, `APPOINTMENT_STATUS`, `LAB_RESULT_STATUS`) vẫn seed vào `code_master` **nhưng chỉ để lấy nhãn/màu hiển thị**, đánh dấu `is_system = 1` cho toàn bộ mã — không cho thêm/xoá.

### P0-3. Chuẩn hoá cách BE validate

Thay pattern hiện tại:

```csharp
// TRƯỚC
private static readonly string[] ValidCategories = ["CONSULTATION", ...];
RuleFor(x => x.Category).Must(c => ValidCategories.Contains(c))
```

bằng:

```csharp
// SAU — validator inject ICodeResolver
RuleFor(x => x.Category)
    .MustAsync(async (c, ct) => await _codes.IsValidAsync("SERVICE_CATEGORY", c, ct))
    .WithMessage("Nhóm dịch vụ không hợp lệ");
```

`ICodeResolver` (Application layer) với cache in-memory + Redis, method:
`Task<IReadOnlyList<CodeItem>> GetAsync(string groupId, CancellationToken ct)`,
`Task<bool> IsValidAsync(string groupId, string? code, CancellationToken ct)`,
`Task<string> LabelAsync(string groupId, string code, CancellationToken ct)`.

Giữ nguyên các `const string` (`EncounterTypes.FirstVisit`…) làm **hằng số tham chiếu trong code** cho các luồng nghiệp vụ cần biết mã cụ thể — chỉ bỏ phần *"danh sách đóng"* (`All`, `ValidXxx`). Đây chính là cách RBAC đang làm: code vẫn nhắc tên permission cụ thể (`"patient.read"`), nhưng **tập hợp** permission là data.

### P0-4. Public settings endpoint (fix B2)

```
GET /api/v1/settings/public
→ { "data": { "stock_transfer_approval_threshold": "5000000",
              "pkg.min_deposit_percent": "30", ... } }
```

Chỉ trả các khoá nằm trong **whitelist hard-code ở BE** (`PublicSettingKeys`) — cố ý hard-code vì đây là quyết định bảo mật (tránh lộ khoá cấu hình nhạy cảm như token tích hợp). FE cache qua TanStack Query `staleTime: 5 phút`.

Xoá `STOCK_TRANSFER_APPROVAL_THRESHOLD` khỏi `frontend/lib/api/stock-transfers.ts:103`.

### P0-5. Role list động cho report sharing (fix A5)

- `frontend/lib/api/reports.ts:237`: `export type ReportRoleCode = string;`
- `SaveReportDialog.tsx`: bỏ `ROLE_OPTIONS`, dùng `useQuery(['roles'], rolesApi.list)` — endpoint `GET /api/v1/roles` đã tồn tại trong module RBAC.
- BE khi lưu `shared_roles` phải validate role tồn tại trong `diab_his_sec_roles` **của tenant hiện tại** → mã lỗi `ROLE_NOT_FOUND`.

---

## 3. Nguyên tắc phân loại (để team áp dụng cho code mới)

Trước khi viết một `enum` / `static readonly string[]`, tự hỏi:

| Câu hỏi | Trả lời | Kết luận |
|---|---|---|
| Thêm/bớt một giá trị có cần viết thêm code xử lý không? | **Có** (vd `PackageItemType` — mỗi type trừ quyền lợi kiểu khác) | Giữ enum trong code |
| Giá trị này là quyết định bảo mật? (whitelist MIME, whitelist bảng SQL) | **Có** | Giữ hard-code, tuyệt đối không đưa vào DB |
| Giá trị gắn với side-effect / state machine? | **Có** | Giữ transition trong code, chỉ đưa **nhãn hiển thị** vào `code_master` |
| Chỉ là nhãn/danh mục để chọn và thống kê? | **Có** | → `code_master` / `code_detail` |
| Là một con số ngưỡng nghiệp vụ? | **Có** | → `sys_settings` |
| Là bật/tắt cả một module? | **Có** | → `sys_feature_flags` |

---

## 4. Kết luận — Top 5 việc nên làm trước

Xếp theo tỉ lệ *giá trị vận hành / công sức*:

1. **Thêm `tenant_id` + `is_hidden` + `is_system` cho `diab_his_sys_code_detail`** và viết `ICodeResolver` với cache.
   → Đây là **khoá mở** cho toàn bộ phần còn lại. Không có bước này thì mọi việc khác chỉ dừng ở "danh mục toàn cục", chưa đạt yêu cầu "mỗi phòng khám khác nhau chút ít" của BO. Công sức: 1 migration + 1 service + sửa 3 endpoint.

2. **Migrate `frontend/lib/constants/code-labels.ts` sang `useCodes()`** (~15 nhóm mã) và xoá 3 bản sao trùng lặp (`patient-schema.ts`, `PatientGeneralTab.tsx`, `code-labels.ts`).
   → Cùng một danh mục đang tồn tại 3 bản khác nhau trong FE, đã có sai lệch thực tế (`CONSULTATION` vs `SPECIALIST`). Đây là nợ kỹ thuật đang sinh bug âm thầm, và chính file đó đã ghi TODO từ trước.

3. **`PAYMENT_METHOD` + `SERVICE_CATEGORY` + `VAT rates` thành master data.**
   → Ảnh hưởng vận hành **hằng ngày** ở quầy thu ngân — nơi khác biệt rõ nhất giữa các phòng khám. Riêng VAT còn chịu tác động thay đổi chính sách Nhà nước, hiện đang phải deploy để đổi.

4. **Fix rò rỉ ngưỡng: `GET /api/v1/settings/public` + bỏ hằng số 5tr ở FE.**
   → Công sức rất nhỏ nhưng đang là **lỗi nghiệp vụ tiềm ẩn**: admin đổi ngưỡng duyệt điều chuyển ở BE mà UI vẫn cảnh báo theo giá trị cũ. Đồng thời tạo sẵn kênh để mọi ngưỡng tương lai đều dùng chung.

5. **Role list động ở report sharing + bật/tắt nhóm menu theo `feature_flags`.**
   → Hai chỗ đang **phá vỡ chính nguyên tắc RBAC mà dự án đã làm đúng**: tạo role mới qua UI nhưng không chia sẻ báo cáo cho role đó được; và phòng khám không có kho dược vẫn thấy toàn bộ nhóm menu "Dược". Cả hai đều tái dùng hạ tầng sẵn có, không cần bảng mới.

**Các hạng mục P1 (OCR plausible range, classifier marker, InBody indicator, med reminder slot, appointment source)** nên gom thành một đợt sau, vì cùng chia sẻ một pattern: *"bảng cấu hình ngưỡng/từ khoá theo tenant, fallback về hằng số hiện tại khi không có dòng cấu hình"*. Làm sau P0-1 sẽ nhanh hơn nhiều vì đã có sẵn `ICodeResolver` và mẫu resolve tenant-override.

---

## 5. Việc cần po-analyst làm rõ

1. **`ENCOUNTER_TYPE` đang lệch giữa BE và FE** (`CONSULTATION` vs `SPECIALIST`) — tập mã chuẩn cuối cùng là gì? Cần chốt trước khi seed, nếu không sẽ seed sai vĩnh viễn.
2. Danh mục nào là **bắt buộc theo quy định BYT/BHYT** (không được cho tenant sửa, phải đánh `is_system = 1`)? Ứng viên: `GENDER`, `MODALITY`, mã nhóm dịch vụ dùng cho XML 4210. Cần xác nhận để tránh tenant sửa gây sai hồ sơ giám định.
3. Phòng khám có được **tự tạo nhóm mã mới** (`code_master`) không, hay chỉ được thêm mã trong nhóm có sẵn? Ảnh hưởng phạm vi màn hình quản trị danh mục.
4. Đơn vị đo XN mặc định của hệ thống là **mmol/L hay mg/dL**, và có cần hỗ trợ cả hai cùng lúc không? Quyết định này ảnh hưởng thiết kế bảng plausible range (A11).
