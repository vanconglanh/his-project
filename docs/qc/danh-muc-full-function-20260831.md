# DANH MỤC ĐẦY ĐỦ CHỨC NĂNG HỆ THỐNG — Pro-Diab HIS
**Ngày lập:** 2026-08-31 · **Nhánh:** develop · **Người lập:** QC (UTC/UTE Agent)

## 1. Phạm vi & phương pháp

Danh mục này được lập bằng cách **quét toàn bộ 69 file** trong
`backend/src/ProDiabHis.Api/Controllers/*.cs`, đọc từng action method và trích xuất:
route tuyệt đối (route lớp + route action), HTTP verb, và chuỗi permission khai báo trong
`[RequirePermission("...")]`.

Đây là **nguồn sự thật theo code**, không phải theo tài liệu — nghĩa là danh mục phản ánh
đúng những gì API thực sự expose tại thời điểm quét.

### Lưu ý về số lượng controller
69 **file** controller nhưng thực tế có **74 class controller**, vì 5 file chứa 2 class:

| File | Chứa các class |
|---|---|
| `DiabetesController.cs` | `DiabetesController`, `DiabetesTemplatesController` |
| `EmrController.cs` | `EmrController`, `EmrTemplatesController` |
| `LabIntegrationController.cs` | `LabIntegrationController`, `LabWebhookController` |
| `ServicesController.cs` | `ServicesController`, `ServicePackagesController` |
| `HealthController.cs` | `HealthController` (+ endpoint `/` minimal API trong Program.cs) |

### Các cơ chế phân quyền tồn tại trong hệ thống
Không phải endpoint nào cũng dùng `[RequirePermission]`. Có **5 cơ chế** khác nhau — đây là
điểm quan trọng khi viết test phân quyền vì mỗi cơ chế trả về mã lỗi khác nhau:

| Cơ chế | Cách dùng | Lỗi trả về khi thiếu quyền |
|---|---|---|
| `[RequirePermission("x.y")]` | Đa số endpoint nghiệp vụ | `403` + `PERMISSION_DENIED` |
| `RequireSuperAdmin` | `TenantsController`, `FeatureFlagsController` | `403` (thông điệp khác) |
| `[Authorize]` trơn | `CodesController`, `MeController`, `users/me/*` | `401` nếu không token |
| `PortalBearer` scheme | `PatientPortalController`, `TelehealthPortalController`, `CgmPortalController` (aud=`patient-portal`) | `401` |
| `ApiKeyAuthFilter` (`X-Api-Key`) | `PublicApiController`, `LabWebhookController` | `401`/`403` theo scope |

**Bypass toàn cục:** claim `is_super_admin=true` bỏ qua **mọi** `RequirePermission`
(xem `RequirePermissionAttribute.OnAuthorization`).

---

## 2. Bảng danh mục chức năng

Ký hiệu cột **Permission**:
- `x.y` = chuỗi trong `[RequirePermission("x.y")]`
- `AllowAnonymous` = không cần đăng nhập
- `Authorize` = chỉ cần JWT hợp lệ, không kiểm permission cụ thể
- `RequireSuperAdmin` = chỉ super admin
- `ApiKey scope ...` = xác thực bằng API key của đối tác

### 2.1 Nhóm A — AI, Đối tác API, Đặt lịch, Kiểm toán, Xác thực, BHYT, Viện phí, Chi nhánh, Thu ngân, CDSS, CLS, Danh mục, Dashboard, ĐTĐ, Dược, ĐTQG

| # | Module | Function/Endpoint | HTTP Method + Route | Permission yêu cầu |
|---|---|---|---|---|
| 1 | Trợ lý AI điều trị | GenerateSuggestion | POST /api/v1/patients/{id}/ai/treatment-suggestion | ai.suggest |
| 2 | Trợ lý AI điều trị | UpdateStatus | PATCH /api/v1/ai/suggestions/{logId} | ai.suggest |
| 3 | Đối tác API | List | GET /api/v1/api-partners | api_partner.read |
| 4 | Đối tác API | Get | GET /api/v1/api-partners/{id} | api_partner.read |
| 5 | Đối tác API | Create | POST /api/v1/api-partners | api_partner.write |
| 6 | Đối tác API | Update | PUT /api/v1/api-partners/{id} | api_partner.write |
| 7 | Đối tác API | Delete | DELETE /api/v1/api-partners/{id} | api_partner.write |
| 8 | Đối tác API | RegenerateKey | POST /api/v1/api-partners/{id}/regenerate-key | api_partner.admin |
| 9 | Đối tác API | TestCall | POST /api/v1/api-partners/{id}/test-call | api_partner.admin |
| 10 | Đối tác API | UsageStats | GET /api/v1/api-partners/{id}/usage-stats | api_partner.read |
| 11 | Đối tác API | RequestLogs | GET /api/v1/api-partners/{id}/request-logs | api_partner.read |
| 12 | Đặt lịch | SlipPdf | GET /api/v1/appointments/{id}/slip-pdf | appointment.read |
| 13 | Đặt lịch | List | GET /api/v1/appointments | appointment.read |
| 14 | Đặt lịch | GetById | GET /api/v1/appointments/{id} | appointment.read |
| 15 | Đặt lịch | Create | POST /api/v1/appointments | appointment.write |
| 16 | Đặt lịch | Update | PUT /api/v1/appointments/{id} | appointment.write |
| 17 | Đặt lịch | UpdateStatus | PATCH /api/v1/appointments/{id}/status | appointment.write |
| 18 | Đặt lịch | OptionsDoctors | GET /api/v1/appointments/options/doctors | appointment.read |
| 19 | Đặt lịch | OptionsPatients | GET /api/v1/appointments/options/patients | appointment.read |
| 20 | Kiểm toán | ListAuditLogs | GET /api/v1/audit-logs | audit.review |
| 21 | Kiểm toán | ExportAuditLogs | GET /api/v1/audit-logs/export | audit.export |
| 22 | Xác thực | Login | POST /api/v1/auth/login | AllowAnonymous |
| 23 | Xác thực | Verify2fa | POST /api/v1/auth/2fa/verify | AllowAnonymous |
| 24 | Xác thực | Refresh | POST /api/v1/auth/refresh | AllowAnonymous |
| 25 | Xác thực | Logout | POST /api/v1/auth/logout | AllowAnonymous |
| 26 | Xác thực | ForgotPassword | POST /api/v1/auth/forgot-password | AllowAnonymous |
| 27 | Xác thực | ResetPassword | POST /api/v1/auth/reset-password | AllowAnonymous |
| 28 | BHYT Xuất hồ sơ | Create | POST /api/v1/bhyt/exports | bhyt.export |
| 29 | BHYT Xuất hồ sơ | List | GET /api/v1/bhyt/exports | bhyt.read |
| 30 | BHYT Xuất hồ sơ | GetDetail | GET /api/v1/bhyt/exports/{id} | bhyt.read |
| 31 | BHYT Xuất hồ sơ | Delete | DELETE /api/v1/bhyt/exports/{id} | bhyt.export |
| 32 | BHYT Xuất hồ sơ | Generate | POST /api/v1/bhyt/exports/{id}/generate | bhyt.generate |
| 33 | BHYT Xuất hồ sơ | Regenerate | POST /api/v1/bhyt/exports/{id}/regenerate | bhyt.generate |
| 34 | BHYT Xuất hồ sơ | Validate | POST /api/v1/bhyt/exports/{id}/validate | bhyt.validate |
| 35 | BHYT Xuất hồ sơ | Sign | POST /api/v1/bhyt/exports/{id}/sign | bhyt.sign |
| 36 | BHYT Xuất hồ sơ | Submit | POST /api/v1/bhyt/exports/{id}/submit | bhyt.submit |
| 37 | BHYT Xuất hồ sơ | DownloadTableXml | GET /api/v1/bhyt/exports/{id}/xml/{tableNo} | bhyt.read |
| 38 | BHYT Xuất hồ sơ | DownloadAllXml | GET /api/v1/bhyt/exports/{id}/xml/all | bhyt.read |
| 39 | BHYT Xuất hồ sơ | ListItems | GET /api/v1/bhyt/exports/{id}/items/table/{tableNo} | bhyt.read |
| 40 | BHYT Xuất hồ sơ | GetItem | GET /api/v1/bhyt/exports/{id}/items/table/{tableNo}/{rowId} | bhyt.read |
| 41 | BHYT Đối chiếu | Import | POST /api/v1/bhyt/reconcile/import | bhyt.reconcile |
| 42 | BHYT Đối chiếu | ListItems | GET /api/v1/bhyt/reconcile/{exportId} | bhyt.read |
| 43 | BHYT Đối chiếu | Dispute | POST /api/v1/bhyt/reconcile/{itemId}/dispute | bhyt.reconcile |
| 44 | BHYT Đối chiếu | Accept | POST /api/v1/bhyt/reconcile/{itemId}/accept | bhyt.reconcile |
| 45 | BHYT Đối chiếu | GetSummary | GET /api/v1/bhyt/reconcile/{exportId}/summary | bhyt.read |
| 46 | Viện phí | List | GET /api/v1/billings | billing.read |
| 47 | Viện phí | Create | POST /api/v1/billings | billing.create |
| 48 | Viện phí | GetById | GET /api/v1/billings/{id} | billing.read |
| 49 | Viện phí | Update | PUT /api/v1/billings/{id} | billing.update |
| 50 | Viện phí | AddItem | POST /api/v1/billings/{id}/items | billing.update |
| 51 | Viện phí | DeleteItem | DELETE /api/v1/billings/items/{itemId} | billing.update |
| 52 | Viện phí | Finalize | POST /api/v1/billings/{id}/finalize | billing.finalize |
| 53 | Viện phí | Void | POST /api/v1/billings/{id}/void | billing.void |
| 54 | Viện phí | Preview | GET /api/v1/billings/{id}/preview | billing.read |
| 55 | Viện phí | ExportPdf | GET /api/v1/billings/{id}/pdf | billing.read |
| 56 | Viện phí | ApplyBhyt | POST /api/v1/billings/{id}/apply-bhyt | billing.apply_bhyt |
| 57 | Viện phí | GetByEncounter | GET /api/v1/billings/encounter/{encounterId} | billing.read |
| 58 | Viện phí | GenerateDynamicQr | POST /api/v1/billings/{id}/qr-dynamic | billing.read |
| 59 | Viện phí | Print | POST /api/v1/billings/{id}/print | billing.print |
| 60 | Chi nhánh | ListBranches | GET /api/v1/branches | branch.read |
| 61 | Chi nhánh | GetBranch | GET /api/v1/branches/{id} | branch.read |
| 62 | Chi nhánh | CreateBranch | POST /api/v1/branches | branch.create |
| 63 | Chi nhánh | UpdateBranch | PUT /api/v1/branches/{id} | branch.update |
| 64 | Chi nhánh | SetStatus | PATCH /api/v1/branches/{id}/status | branch.update |
| 65 | Chi nhánh | SetDefault | POST /api/v1/branches/{id}/set-default | branch.update |
| 66 | Chi nhánh | DeleteBranch | DELETE /api/v1/branches/{id} | branch.delete |
| 67 | Chi nhánh | ListUsers | GET /api/v1/branches/{id}/users | branch.read |
| 68 | Chi nhánh | AssignUsers | POST /api/v1/branches/{id}/users | branch.assign_user |
| 69 | Chi nhánh | RemoveUser | DELETE /api/v1/branches/{id}/users/{userId} | branch.assign_user |
| 70 | Chi nhánh | GetBhytCompliance | GET /api/v1/branches/bhyt-compliance | branch.read |
| 71 | Chi nhánh | CloneBranch | POST /api/v1/branches/{id}/clone | branch.create |
| 72 | Chi nhánh | GetReadiness | GET /api/v1/branches/{id}/readiness | branch.read |
| 73 | Chi nhánh | ActivateBranch | POST /api/v1/branches/{id}/activate | branch.update |
| 74 | Thu ngân | Today | GET /api/v1/cashier/closing/today | cashier.report |
| 75 | Thu ngân | OpenShift | POST /api/v1/cashier/closing/open | cashier.shift_open |
| 76 | Thu ngân | CloseShift | POST /api/v1/cashier/closing/close | cashier.shift_close |
| 77 | Thu ngân | History | GET /api/v1/cashier/closing/history | cashier.report |
| 78 | Thu ngân | ExportPdf | GET /api/v1/cashier/closing/{id}/pdf | cashier.report |
| 79 | Thu ngân | GetCurrentShift | GET /api/v1/cashier/shift | cashier.report |
| 80 | Thu ngân | PrintReceipt | POST /api/v1/cashier/receipts/{paymentId}/print | cashier.print_receipt |
| 81 | Thu ngân | Debts | GET /api/v1/cashier/debts | cashier.debt_view |
| 82 | CDSS | Check | POST /api/v1/cdss/check | cdss.read |
| 83 | CDSS | Override | POST /api/v1/cdss/override | cdss.override |
| 84 | CDSS | ListRules | GET /api/v1/cdss/rules | cdss.admin |
| 85 | CDSS | UpsertRule | POST /api/v1/cdss/rules | cdss.admin |
| 86 | Cổng BN — CGM | Link | POST /api/v1/portal/cgm/link | Authorize (PortalBearer) |
| 87 | Cổng BN — CGM | Sync | POST /api/v1/portal/cgm/sync | Authorize (PortalBearer) |
| 88 | Chỉ định CLS | CreateLab | POST /api/v1/encounters/{encounterId}/lab-orders | lab_order.create |
| 89 | Chỉ định CLS | ListLab | GET /api/v1/encounters/{encounterId}/lab-orders | lab_order.read |
| 90 | Chỉ định CLS | UpdateLab | PUT /api/v1/lab-orders/{id} | lab_order.update |
| 91 | Chỉ định CLS | DeleteLab | DELETE /api/v1/lab-orders/{id} | lab_order.delete |
| 92 | Chỉ định CLS | CreateRad | POST /api/v1/encounters/{encounterId}/rad-orders | rad_order.create |
| 93 | Chỉ định CLS | ListRad | GET /api/v1/encounters/{encounterId}/rad-orders | rad_order.read |
| 94 | Chỉ định CLS | UpdateRad | PUT /api/v1/rad-orders/{id} | rad_order.update |
| 95 | Chỉ định CLS | DeleteRad | DELETE /api/v1/rad-orders/{id} | rad_order.delete |
| 96 | Chỉ định CLS | LabOrdersPdf | GET /api/v1/encounters/{encounterId}/lab-orders/pdf | lab_order.read |
| 97 | Chỉ định CLS | RadOrdersPdf | GET /api/v1/encounters/{encounterId}/rad-orders/pdf | rad_order.read |
| 98 | Chỉ định CLS | ListOverdue | GET /api/v1/lab-orders/overdue | lab_order.read |
| 99 | Chỉ định CLS | Catalog | GET /api/v1/cls-catalog/tests | lab_order.read |
| 100 | Đợt CLS | Create | POST /api/v1/encounters/{encounterId}/cls-rounds | cls_round.create |
| 101 | Đợt CLS | ListByEncounter | GET /api/v1/encounters/{encounterId}/cls-rounds | cls_round.read |
| 102 | Đợt CLS | GetById | GET /api/v1/cls-rounds/{id} | cls_round.read |
| 103 | Đợt CLS | Submit | POST /api/v1/cls-rounds/{id}/submit | cls_round.submit |
| 104 | Đợt CLS | Pay | POST /api/v1/cls-rounds/{id}/pay | cls_round.pay |
| 105 | Đợt CLS | Waive | POST /api/v1/cls-rounds/{id}/waive | cls_round.waive |
| 106 | Đợt CLS | Cancel | POST /api/v1/cls-rounds/{id}/cancel | cls_round.cancel |
| 107 | Tệp CLS | List | GET /api/v1/patients/{patientId}/cls-uploads | cls_upload.read |
| 108 | Tệp CLS | Upload | POST /api/v1/patients/{patientId}/cls-uploads | cls_upload.create |
| 109 | Tệp CLS | GetById | GET /api/v1/patients/{patientId}/cls-uploads/{id} | cls_upload.read |
| 110 | Tệp CLS | Delete | DELETE /api/v1/patients/{patientId}/cls-uploads/{id} | cls_upload.delete |
| 111 | Tệp CLS | ListByEncounter | GET /api/v1/encounters/{encounterId}/cls-uploads | cls_upload.read |
| 112 | Danh mục mã | Groups | GET /api/v1/codes | Authorize |
| 113 | Danh mục mã | Batch | GET /api/v1/codes/batch | Authorize |
| 114 | Danh mục mã | Items | GET /api/v1/codes/{groupId} | Authorize |
| 115 | Dashboard | GetOverview | GET /api/v1/dashboard/overview | dashboard.read |
| 116 | Dashboard | GetRevenueTrend | GET /api/v1/dashboard/charts/revenue-trend | dashboard.read |
| 117 | Dashboard | GetEncountersTrend | GET /api/v1/dashboard/charts/encounters-trend | dashboard.read |
| 118 | Dashboard | GetTopDoctors | GET /api/v1/dashboard/charts/top-doctors | dashboard.read |
| 119 | Dashboard | GetTopDrugs | GET /api/v1/dashboard/charts/top-drugs | dashboard.read |
| 120 | Dashboard | GetDiabetesHba1c | GET /api/v1/dashboard/charts/diabetes-hba1c | dashboard.read |
| 121 | Dashboard | GetAlerts | GET /api/v1/dashboard/alerts | dashboard.read |
| 122 | Dashboard | GetBranchRanking | GET /api/v1/dashboard/branch-ranking | dashboard.read |
| 123 | Dashboard | GetBranchDetail | GET /api/v1/dashboard/branch/{branchId}/detail | dashboard.read |
| 124 | ĐTĐ Đánh giá | Create | POST /api/v1/encounters/{encounterId}/diabetes-assessment | diabetes.assess |
| 125 | ĐTĐ Đánh giá | Get | GET /api/v1/encounters/{encounterId}/diabetes-assessment | diabetes.assess |
| 126 | ĐTĐ Đánh giá | Update | PUT /api/v1/encounters/{encounterId}/diabetes-assessment | diabetes.assess |
| 127 | ĐTĐ Đánh giá | History | GET /api/v1/patients/{patientId}/diabetes-assessments/history | diabetes.assess |
| 128 | ĐTĐ Đánh giá | History (legacy) | GET /api/v1/diabetes-assessments/patient/{patientId}/history | diabetes.assess |
| 129 | ĐTĐ Mẫu | List | GET /api/v1/diabetes-templates | diabetes.assess |
| 130 | ĐTĐ Mẫu | Create | POST /api/v1/diabetes-templates | diabetes.assess |
| 131 | ĐTĐ Mẫu | Update | PUT /api/v1/diabetes-templates/{id} | diabetes.assess |
| 132 | ĐTĐ Theo dõi | Trajectory | GET /api/v1/patients/{id}/diabetes/trajectory | diabetes.assess |
| 133 | ĐTĐ Theo dõi | DeteriorationFlags | GET /api/v1/patients/{id}/diabetes/deterioration-flags | diabetes.assess |
| 134 | ĐTĐ Theo dõi | RiskList | GET /api/v1/diabetes/risk-list | risk.read |
| 135 | Lịch bác sĩ | List | GET /api/v1/doctor-schedules | appointment.read |
| 136 | Lịch bác sĩ | Create | POST /api/v1/doctor-schedules | appointment.write |
| 137 | Lịch bác sĩ | Update | PUT /api/v1/doctor-schedules/{id} | appointment.write |
| 138 | Lịch bác sĩ | Delete | DELETE /api/v1/doctor-schedules/{id} | appointment.write |
| 139 | Lịch bác sĩ | ListBlocks | GET /api/v1/doctor-schedules/blocks | appointment.read |
| 140 | Lịch bác sĩ | CreateBlock | POST /api/v1/doctor-schedules/blocks | appointment.write |
| 141 | Lịch bác sĩ | DeleteBlock | DELETE /api/v1/doctor-schedules/blocks/{id} | appointment.write |
| 142 | Danh bạ bác sĩ | Lookup | GET /api/v1/doctors/lookup | appointment.read |
| 143 | Tài liệu | SmartUpload | POST /api/v1/documents/smart-upload | patient.clinical.write |
| 144 | Giá thuốc CN | List | GET /api/v1/drug-price-overrides | drug.price_override |
| 145 | Giá thuốc CN | GetById | GET /api/v1/drug-price-overrides/{id} | drug.price_override |
| 146 | Giá thuốc CN | Create | POST /api/v1/drug-price-overrides | drug.price_override |
| 147 | Giá thuốc CN | Update | PUT /api/v1/drug-price-overrides/{id} | drug.price_override |
| 148 | Giá thuốc CN | Delete | DELETE /api/v1/drug-price-overrides/{id} | drug.price_override |
| 149 | Danh mục thuốc | List | GET /api/v1/drugs | drug.read |
| 150 | Danh mục thuốc | Import | POST /api/v1/drugs/import | drug.import |
| 151 | Danh mục thuốc | Search | GET /api/v1/drugs/search | drug.read |
| 152 | Danh mục thuốc | ListCategories | GET /api/v1/drugs/categories | drug.read |
| 153 | Danh mục thuốc | CreateCategory | POST /api/v1/drugs/categories | drug.write |
| 154 | Danh mục thuốc | SyncCucQld | POST /api/v1/drugs/sync-cuc-qld | drug.sync |
| 155 | Danh mục thuốc | GetDetail | GET /api/v1/drugs/{id} | drug.read |
| 156 | Danh mục thuốc | Update | PUT /api/v1/drugs/{id} | drug.write |
| 157 | Danh mục thuốc | Delete | DELETE /api/v1/drugs/{id} | drug.write |
| 158 | Danh mục thuốc | Create | POST /api/v1/drugs | drug.write |
| 159 | Danh mục thuốc | GetEquivalents | GET /api/v1/drugs/{id}/equivalents | drug.read |
| 160 | Danh mục thuốc | GetInteractions | GET /api/v1/drugs/{id}/interactions | ddi.check |
| 161 | ĐTQG | ListSubmissions | GET /api/v1/dtqg/submissions | dtqg.submit |
| 162 | ĐTQG | CancelOnPortal | POST /api/v1/dtqg/submissions/{id}/cancel-on-portal | dtqg.admin |
| 163 | ĐTQG | GetCredentials | GET /api/v1/dtqg/credentials | dtqg.admin |
| 164 | ĐTQG | UpsertCredentials | PUT /api/v1/dtqg/credentials | dtqg.admin |
| 165 | ĐTQG | TestCredentials | POST /api/v1/dtqg/credentials/test | dtqg.admin |

### 2.2 Nhóm B — Hóa đơn ĐT, EMR, Lượt khám, Mã hóa, Feature flag, FHIR, Tệp, Health, ICD-10, InBody, Công nợ CN, Chuyển khám, Xét nghiệm, Nhập liệu cũ, Tài khoản, Thông báo, Gói dịch vụ, Cổng bệnh nhân

| # | Module | Function/Endpoint | HTTP Method + Route | Permission yêu cầu |
|---|---|---|---|---|
| 166 | Hóa đơn điện tử | List | GET /api/v1/einvoices | einvoice.read |
| 167 | Hóa đơn điện tử | Issue | POST /api/v1/einvoices/issue | einvoice.issue |
| 168 | Hóa đơn điện tử | GetById | GET /api/v1/einvoices/{id} | einvoice.read |
| 169 | Hóa đơn điện tử | Cancel | POST /api/v1/einvoices/{id}/cancel | einvoice.cancel |
| 170 | Hóa đơn điện tử | XmlDownload | GET /api/v1/einvoices/{id}/xml-download | einvoice.read |
| 171 | EMR | Get | GET /api/v1/encounters/{encounterId}/emr | emr.read |
| 172 | EMR | SaveDraft | PUT /api/v1/encounters/{encounterId}/emr | emr.write |
| 173 | EMR | Sign | POST /api/v1/encounters/{encounterId}/emr/sign | emr.sign |
| 174 | EMR | Unsign | POST /api/v1/encounters/{encounterId}/emr/unsign | emr.unsign |
| 175 | EMR | ExportPdf | GET /api/v1/encounters/{encounterId}/emr/pdf | emr.export |
| 176 | EMR | Versions | GET /api/v1/encounters/{encounterId}/emr/versions | emr.read |
| 177 | EMR | VersionDiff | GET /api/v1/encounters/{encounterId}/emr/versions/{versionId}/diff | emr.read |
| 178 | Mẫu EMR | List | GET /api/v1/emr-templates (alias /api/v1/emr/templates) | emr_template.read |
| 179 | Mẫu EMR | Get | GET /api/v1/emr-templates/{id} | emr_template.read |
| 180 | Mẫu EMR | Create | POST /api/v1/emr-templates | emr_template.write |
| 181 | Mẫu EMR | Update | PUT /api/v1/emr-templates/{id} | emr_template.write |
| 182 | Mẫu EMR | Delete | DELETE /api/v1/emr-templates/{id} | emr_template.write |
| 183 | Lượt khám | List | GET /api/v1/encounters | encounter.read |
| 184 | Lượt khám | Create | POST /api/v1/encounters | encounter.create |
| 185 | Lượt khám | Over12hAlerts | GET /api/v1/encounters/alerts/over-12h | encounter.read |
| 186 | Lượt khám | GetDetail | GET /api/v1/encounters/{id} | encounter.read |
| 187 | Lượt khám | Update | PUT /api/v1/encounters/{id} | encounter.update |
| 188 | Lượt khám | Start | POST /api/v1/encounters/{id}/start | encounter.start |
| 189 | Lượt khám | Close | POST /api/v1/encounters/{id}/close | encounter.close |
| 190 | Lượt khám | UpdateChiefComplaint | PUT /api/v1/encounters/{id}/chief-complaint | encounter.update |
| 191 | Lượt khám | AddDiagnosis | POST /api/v1/encounters/{id}/diagnoses | encounter.update |
| 192 | Lượt khám | DeleteDiagnosis | DELETE /api/v1/encounters/{id}/diagnoses/{diagnosisId} | encounter.update |
| 193 | Lượt khám | Timeline | GET /api/v1/encounters/{id}/timeline | encounter.read |
| 194 | Lượt khám | LockState | GET /api/v1/encounters/{id}/lock-state | encounter.read |
| 195 | Lượt khám | CreateAddendum | POST /api/v1/encounters/{id}/addenda | encounter.amend |
| 196 | Lượt khám | ListAddenda | GET /api/v1/encounters/{id}/addenda | encounter.amend.read |
| 197 | Quản trị mã hóa | PiiBackfill | POST /api/v1/admin/encryption/pii-backfill | encryption.rotate |
| 198 | Quản trị mã hóa | RotateKey | POST /api/v1/admin/encryption/rotate-key | encryption.rotate |
| 199 | Quản trị mã hóa | ListKeys | GET /api/v1/admin/encryption/keys | encryption.rotate |
| 200 | Feature flag | GetAll | GET /api/v1/admin/feature-flags | RequireSuperAdmin |
| 201 | Feature flag | Get | GET /api/v1/admin/feature-flags/{key} | RequireSuperAdmin |
| 202 | Feature flag | Set | PUT /api/v1/admin/feature-flags/{key} | RequireSuperAdmin |
| 203 | FHIR R4 | Metadata | GET /api/fhir/r4/metadata | AllowAnonymous |
| 204 | FHIR R4 | GetPatient | GET /api/fhir/r4/Patient/{id} | fhir.read |
| 205 | FHIR R4 | SearchPatient | GET /api/fhir/r4/Patient | fhir.read |
| 206 | FHIR R4 | GetEncounter | GET /api/fhir/r4/Encounter/{id} | fhir.read |
| 207 | FHIR R4 | SearchEncounter | GET /api/fhir/r4/Encounter | fhir.read |
| 208 | FHIR R4 | GetBundle | GET /api/fhir/r4/Bundle | fhir.read |
| 209 | Quản lý tệp | Upload | POST /api/v1/files/upload | file.upload |
| 210 | Quản lý tệp | GetSignedUrl | GET /api/v1/files/{id}/signed-url | Authorize |
| 211 | Quản lý tệp | Delete | DELETE /api/v1/files/{id} | file.delete |
| 212 | Quản lý tệp | ListAnnotations | GET /api/v1/files/{fileId}/annotations | file_annotation.read |
| 213 | Quản lý tệp | CreateAnnotation | POST /api/v1/files/{fileId}/annotations | file_annotation.write |
| 214 | Quản lý tệp | UpdateAnnotation | PUT /api/v1/files/{fileId}/annotations/{id} | file_annotation.write |
| 215 | Quản lý tệp | DeleteAnnotation | DELETE /api/v1/files/{fileId}/annotations/{id} | file_annotation.delete |
| 216 | Giám sát | Check | GET /api/v1/health | AllowAnonymous |
| 217 | Giám sát | Detailed | GET /api/v1/health/detailed | system.config |
| 218 | ICD-10 | List | GET /api/v1/icd10 | icd10.read |
| 219 | ICD-10 | Search | GET /api/v1/icd10/search | icd10.read |
| 220 | ICD-10 | Categories | GET /api/v1/icd10/categories | icd10.read |
| 221 | ICD-10 | GetByCode | GET /api/v1/icd10/{code} | icd10.read |
| 222 | InBody | Upload | POST /api/v1/patients/{patientId}/inbody-reports | patient.clinical.write |
| 223 | InBody | List | GET /api/v1/patients/{patientId}/inbody-reports | patient.read |
| 224 | InBody | Confirm | POST /api/v1/inbody-reports/{id}/confirm | patient.clinical.write |
| 225 | InBody | Delete | DELETE /api/v1/inbody-reports/{id} | patient.clinical.write |
| 226 | Công nợ liên CN | List | GET /api/v1/inter-branch-debts | inter_branch_debt.read |
| 227 | Công nợ liên CN | Settle | POST /api/v1/inter-branch-debts/{id}/settle | inter_branch_debt.settle |
| 228 | Chuyển khám nội bộ | Create | POST /api/v1/internal-referrals | internal_referral.write |
| 229 | Chuyển khám nội bộ | ListIncoming | GET /api/v1/internal-referrals/incoming | internal_referral.read |
| 230 | Chuyển khám nội bộ | UpdateStatus | PATCH /api/v1/internal-referrals/{id}/status | internal_referral.write |
| 231 | Tích hợp XN | Send | POST /api/v1/lab-integration/outbound/send/{labOrderId} | lab_integration.send |
| 232 | Tích hợp XN | ListOutbound | GET /api/v1/lab-integration/outbound | lab_integration.send |
| 233 | Tích hợp XN | RetryOutbound | POST /api/v1/lab-integration/outbound/{id}/retry | lab_integration.retry |
| 234 | Tích hợp XN | ListInbound | GET /api/v1/lab-integration/inbound | lab_integration.send |
| 235 | Tích hợp XN | ReprocessInbound | POST /api/v1/lab-integration/inbound/{id}/reprocess | lab_integration.retry |
| 236 | Tích hợp XN | GetRaw | GET /api/v1/lab-integration/inbound/{id}/raw | lab_integration.send |
| 237 | Tích hợp XN | Stats | GET /api/v1/lab-integration/stats | lab_integration.send |
| 238 | Webhook XN | Inbound | POST /api/public/v1/lab-results/webhook/{partnerCode} | AllowAnonymous (HMAC + X-Partner-Api-Key) |
| 239 | Đối tác XN | List | GET /api/v1/lab-partners | lab_partner.read |
| 240 | Đối tác XN | Create | POST /api/v1/lab-partners | lab_partner.write |
| 241 | Đối tác XN | Get | GET /api/v1/lab-partners/{id} | lab_partner.read |
| 242 | Đối tác XN | Update | PUT /api/v1/lab-partners/{id} | lab_partner.write |
| 243 | Đối tác XN | Delete | DELETE /api/v1/lab-partners/{id} | lab_partner.admin |
| 244 | Đối tác XN | TestConnection | POST /api/v1/lab-partners/{id}/test-connection | lab_partner.write |
| 245 | Đối tác XN | UpdateCredentials | PUT /api/v1/lab-partners/{id}/credentials | lab_partner.admin |
| 246 | Đối tác XN | RotateKey | POST /api/v1/lab-partners/{id}/credentials/rotate | lab_partner.admin |
| 247 | Đối tác XN | ListCosts | GET /api/v1/lab-partners/{id}/costs | lab_partner.finance_read |
| 248 | Đối tác XN | CreateCost | POST /api/v1/lab-partner-costs | lab_partner.finance_write |
| 249 | Đối tác XN | UpdateCost | PUT /api/v1/lab-partner-costs/{id} | lab_partner.finance_write |
| 250 | Đối tác XN | ListReconciliations | GET /api/v1/lab-partners/{id}/reconciliations | lab_partner.finance_read |
| 251 | Đối tác XN | CreateReconciliation | POST /api/v1/lab-partners/{id}/reconciliations | lab_partner.finance_write |
| 252 | Đối tác XN | UpdateReconciliationStatus | PUT /api/v1/lab-partner-reconciliations/{id}/status | lab_partner.finance_write |
| 253 | Kết quả XN | List | GET /api/v1/lab-results | lab_result.read |
| 254 | Kết quả XN | PendingItems | GET /api/v1/lab-results/pending-items | lab_result.write |
| 255 | Kết quả XN | Create | POST /api/v1/lab-results | lab_result.write |
| 256 | Kết quả XN | Update | PUT /api/v1/lab-results/{id} | lab_result.write |
| 257 | Kết quả XN | Verify | POST /api/v1/lab-results/{id}/verify | lab_result.verify |
| 258 | Kết quả XN | Unverify | POST /api/v1/lab-results/{id}/unverify | lab_result.verify |
| 259 | Kết quả XN | OcrExtract | POST /api/v1/lab-results/ocr-extract | lab_result.write |
| 260 | Kết quả XN | OcrConfirm | POST /api/v1/lab-results/ocr-confirm | lab_result.write |
| 261 | Kết quả XN | Import | POST /api/v1/lab-results/import | lab_result.import |
| 262 | Kết quả XN | Abnormal | GET /api/v1/lab-results/abnormal | lab_result.read |
| 263 | Kết quả XN | HistoryTrend | GET /api/v1/lab-results/history-trend | lab_result.read |
| 264 | Kết quả XN | ExportPdf | GET /api/v1/lab-results/{id}/pdf | lab_result.read |
| 265 | Kết quả XN | BatchVerify | POST /api/v1/lab-results/batch-verify | lab_result.verify |
| 266 | Nhập dữ liệu cũ | Create | POST /api/v1/legacy-imports | legacy_import.write |
| 267 | Nhập dữ liệu cũ | List | GET /api/v1/legacy-imports | legacy_import.write |
| 268 | Nhập dữ liệu cũ | GetById | GET /api/v1/legacy-imports/{id} | legacy_import.write |
| 269 | Nhập dữ liệu cũ | ListItems | GET /api/v1/legacy-imports/{id}/items | legacy_import.write |
| 270 | Nhập dữ liệu cũ | Match | PUT /api/v1/legacy-imports/items/{itemId}/match | legacy_import.write |
| 271 | Nhập dữ liệu cũ | Confirm | POST /api/v1/legacy-imports/items/{itemId}/confirm | legacy_import.write |
| 272 | Nhập dữ liệu cũ | Reject | POST /api/v1/legacy-imports/items/{itemId}/reject | legacy_import.write |
| 273 | Tài khoản của tôi | GetBranchContext | GET /api/v1/me/branch-context | Authorize |
| 274 | Tài khoản của tôi | SwitchBranch | POST /api/v1/me/switch-branch | Authorize |
| 275 | Kênh thông báo | List | GET /api/v1/notification-channels | notification_channel.read |
| 276 | Kênh thông báo | Get | GET /api/v1/notification-channels/{id} | notification_channel.read |
| 277 | Kênh thông báo | Create | POST /api/v1/notification-channels | notification_channel.write |
| 278 | Kênh thông báo | Update | PUT /api/v1/notification-channels/{id} | notification_channel.write |
| 279 | Kênh thông báo | Delete | DELETE /api/v1/notification-channels/{id} | notification_channel.write |
| 280 | Kênh thông báo | Test | POST /api/v1/notification-channels/{id}/test | notification_channel.write |
| 281 | Thông báo | ListInbox | GET /api/v1/notifications/inbox | notification.read |
| 282 | Thông báo | UnreadCount | GET /api/v1/notifications/unread-count | notification.read |
| 283 | Thông báo | MarkRead | POST /api/v1/notifications/{id}/mark-read | notification.read |
| 284 | Thông báo | MarkAllRead | POST /api/v1/notifications/mark-all-read | notification.read |
| 285 | Thông báo | Delete | DELETE /api/v1/notifications/{id} | notification.read |
| 286 | Thông báo | Subscribe | POST /api/v1/notifications/web-push/subscribe | notification.read |
| 287 | Thông báo | Unsubscribe | DELETE /api/v1/notifications/web-push/unsubscribe | notification.read |
| 288 | Thông báo | GetVapidStatus | GET /api/v1/notifications/vapid/status | notification.config |
| 289 | Thông báo | GenerateVapidKey | POST /api/v1/notifications/vapid/generate | notification.config |
| 290 | Thông báo | GetVapidPublicKey | GET /api/v1/notifications/web-push/vapid-public-key | AllowAnonymous |
| 291 | Thông báo | ListLogs | GET /api/v1/notifications/logs | notification.read |
| 292 | Thông báo | TestSend | POST /api/v1/notifications/test-send | notification.send |
| 293 | Thông báo | GetPreferences | GET /api/v1/notifications/preferences | notification.read |
| 294 | Thông báo | UpdatePreferences | PUT /api/v1/notifications/preferences | notification.read |
| 295 | Thuê bao gói DV | List | GET /api/v1/package-subscriptions | package_subscription.read |
| 296 | Thuê bao gói DV | Get | GET /api/v1/package-subscriptions/{id} | package_subscription.read |
| 297 | Thuê bao gói DV | Create | POST /api/v1/package-subscriptions | package_subscription.sell |
| 298 | Thuê bao gói DV | AddPayment | POST /api/v1/package-subscriptions/{id}/payments | package_subscription.collect |
| 299 | Thuê bao gói DV | Cancel | POST /api/v1/package-subscriptions/{id}/cancel | package_subscription.cancel |
| 300 | Thuê bao gói DV | Extend | POST /api/v1/package-subscriptions/{id}/extend | package_subscription.extend |
| 301 | Thuê bao gói DV | GetPatientSummary | GET /api/v1/patients/{patientId}/package-summary | package_subscription.read |
| 302 | Gói dịch vụ | List | GET /api/v1/packages | package.read |
| 303 | Gói dịch vụ | Get | GET /api/v1/packages/{id} | package.read |
| 304 | Gói dịch vụ | Create | POST /api/v1/packages | package.create |
| 305 | Gói dịch vụ | Update | PUT /api/v1/packages/{id} | package.update |
| 306 | Gói dịch vụ | Delete | DELETE /api/v1/packages/{id} | package.delete |
| 307 | Cổng bệnh nhân | TenantInfo | GET /api/portal/v1/tenant-info | AllowAnonymous |
| 308 | Cổng bệnh nhân | Activate | POST /api/portal/v1/auth/activate | AllowAnonymous |
| 309 | Cổng bệnh nhân | LoginPin | POST /api/portal/v1/auth/login-pin | AllowAnonymous |
| 310 | Cổng bệnh nhân | ForgotPin | POST /api/portal/v1/auth/forgot-pin | AllowAnonymous |
| 311 | Cổng bệnh nhân | ResetPin | POST /api/portal/v1/auth/reset-pin | AllowAnonymous |
| 312 | Cổng bệnh nhân | Logout | POST /api/portal/v1/auth/logout | PortalBearer |
| 313 | Cổng bệnh nhân | GetMe | GET /api/portal/v1/me | PortalBearer |
| 314 | Cổng bệnh nhân | GetEncounters | GET /api/portal/v1/me/encounters | PortalBearer |
| 315 | Cổng bệnh nhân | GetEncounterDetail | GET /api/portal/v1/me/encounters/{id} | PortalBearer |
| 316 | Cổng bệnh nhân | GetPrescriptions | GET /api/portal/v1/me/prescriptions | PortalBearer |
| 317 | Cổng bệnh nhân | GetPrescriptionPdf | GET /api/portal/v1/me/prescriptions/{id}/pdf | PortalBearer |
| 318 | Cổng bệnh nhân | GetLabResults | GET /api/portal/v1/me/lab-results | PortalBearer |
| 319 | Cổng bệnh nhân | GetHealthTrends | GET /api/portal/v1/me/health-trends | PortalBearer |
| 320 | Cổng bệnh nhân | GetLabResultPdf | GET /api/portal/v1/me/lab-results/{id}/pdf | PortalBearer |
| 321 | Cổng bệnh nhân | GetAppointments | GET /api/portal/v1/me/appointments | PortalBearer |
| 322 | Cổng bệnh nhân | CreateAppointment | POST /api/portal/v1/me/appointments | PortalBearer |
| 323 | Cổng bệnh nhân | CancelAppointment | DELETE /api/portal/v1/me/appointments/{id} | PortalBearer |
| 324 | Cổng bệnh nhân | GetQueueStatus | GET /api/portal/v1/me/queue | PortalBearer |
| 325 | Cổng bệnh nhân | GetBookingDoctors | GET /api/portal/v1/booking/doctors | PortalBearer |
| 326 | Cổng bệnh nhân | GetBookingSlots | GET /api/portal/v1/booking/slots | PortalBearer |
| 327 | Cổng bệnh nhân | GetMedReminders | GET /api/portal/v1/me/med-reminders | PortalBearer |
| 328 | Cổng bệnh nhân | CreateMedRemindersFromPrescription | POST /api/portal/v1/me/med-reminders/from-prescription/{prescriptionId} | PortalBearer |
| 329 | Cổng bệnh nhân | UpdateMedReminder | PUT /api/portal/v1/me/med-reminders/{id} | PortalBearer |
| 330 | Cổng bệnh nhân | GetNotificationPreferences | GET /api/portal/v1/me/notification-preferences | PortalBearer |
| 331 | Cổng bệnh nhân | UpdateNotificationPreferences | PUT /api/portal/v1/me/notification-preferences | PortalBearer |
| 332 | Cổng bệnh nhân | SubscribePush | POST /api/portal/v1/me/push-subscriptions | PortalBearer |
| 333 | Cổng bệnh nhân | UnsubscribePush | DELETE /api/portal/v1/me/push-subscriptions | PortalBearer |

### 2.3 Nhóm C — Bệnh nhân, Thanh toán, Phân quyền, Dược/Kho, Đơn thuốc, API công khai, CĐHA, Tái khám, Lễ tân, Báo cáo, Vai trò, Phòng, Giá DV, Dịch vụ, Điều chuyển kho, NCC, Telehealth, Tenant, Người dùng, Sinh hiệu

| # | Module | Function/Endpoint | HTTP Method + Route | Permission yêu cầu |
|---|---|---|---|---|
| 334 | Hồ sơ bệnh nhân | List | GET /api/v1/patients | patient.read |
| 335 | Hồ sơ bệnh nhân | Search | GET /api/v1/patients/search | patient.read |
| 336 | Hồ sơ bệnh nhân | ExternalPathway | GET /api/v1/patients/{id}/external-pathway | patient.read |
| 337 | Hồ sơ bệnh nhân | Create | POST /api/v1/patients | patient.write |
| 338 | Hồ sơ bệnh nhân | CheckCccdDuplicate | GET /api/v1/patients/check-cccd-duplicate | patient.read |
| 339 | Hồ sơ bệnh nhân | ApplyCccdFields | PUT /api/v1/patients/{id}/apply-cccd-fields | patient.write |
| 340 | Hồ sơ bệnh nhân | GetById | GET /api/v1/patients/{id} | patient.read |
| 341 | Hồ sơ bệnh nhân | Update | PUT /api/v1/patients/{id} | patient.write |
| 342 | Hồ sơ bệnh nhân | Delete | DELETE /api/v1/patients/{id} | patient.delete |
| 343 | Hồ sơ bệnh nhân | GetEncounters | GET /api/v1/patients/{id}/encounters | patient.read |
| 344 | Hồ sơ bệnh nhân | UploadAvatar | POST /api/v1/patients/{id}/avatar | patient.write |
| 345 | Hồ sơ bệnh nhân | GetAllergies | GET /api/v1/patients/{id}/allergies | patient.read |
| 346 | Hồ sơ bệnh nhân | AddAllergy | POST /api/v1/patients/{id}/allergies | patient.clinical.write |
| 347 | Hồ sơ bệnh nhân | DeleteAllergy | DELETE /api/v1/patients/{id}/allergies/{allergyId} | patient.clinical.write |
| 348 | Hồ sơ bệnh nhân | GetGuardians | GET /api/v1/patients/{id}/guardians | patient.read |
| 349 | Hồ sơ bệnh nhân | GetInsurance | GET /api/v1/patients/{id}/insurance | patient.read |
| 350 | Hồ sơ bệnh nhân | AddInsurance | POST /api/v1/patients/{id}/insurance | patient.write |
| 351 | Hồ sơ bệnh nhân | UpdateInsurance | PUT /api/v1/patients/{id}/insurance/{insuranceId} | patient.write |
| 352 | Hồ sơ bệnh nhân | DeleteInsurance | DELETE /api/v1/patients/{id}/insurance/{insuranceId} | patient.write |
| 353 | Hồ sơ bệnh nhân | GetEmergencyContacts | GET /api/v1/patients/{id}/emergency-contacts | patient.read |
| 354 | Hồ sơ bệnh nhân | AddEmergencyContact | POST /api/v1/patients/{id}/emergency-contacts | patient.write |
| 355 | Hồ sơ bệnh nhân | UpdateEmergencyContact | PUT /api/v1/patients/{id}/emergency-contacts/{contactId} | patient.write |
| 356 | Hồ sơ bệnh nhân | DeleteEmergencyContact | DELETE /api/v1/patients/{id}/emergency-contacts/{contactId} | patient.write |
| 357 | Hồ sơ bệnh nhân | GetConsents | GET /api/v1/patients/{id}/consents | patient.read |
| 358 | Hồ sơ bệnh nhân | AddConsent | POST /api/v1/patients/{id}/consents | patient.write |
| 359 | Hồ sơ bệnh nhân | UpdateReceptionNote | PUT /api/v1/patients/{id}/reception-note | patient.write |
| 360 | Hồ sơ bệnh nhân | GetCgmStatus | GET /api/v1/patients/{id}/cgm-status | patient.read |
| 361 | Thanh toán | List | GET /api/v1/payments | payment.read |
| 362 | Thanh toán | Create | POST /api/v1/payments | payment.collect |
| 363 | Thanh toán | GetById | GET /api/v1/payments/{id} | payment.read |
| 364 | Thanh toán | Refund | POST /api/v1/payments/{id}/refund | payment.refund |
| 365 | Thanh toán | Void | POST /api/v1/payments/{id}/void | payment.void |
| 366 | Thanh toán | ListMethods | GET /api/v1/payments/methods | payment.read |
| 367 | Thanh toán | GenerateQr | POST /api/v1/payments/qr/generate | payment_qr.generate |
| 368 | Thanh toán | GetQrStatus | GET /api/v1/payments/qr/{qrId}/status | payment.read |
| 369 | Thanh toán | QrWebhook | POST /api/v1/payments/qr/webhook/{provider} | AllowAnonymous (HMAC X-Signature) |
| 370 | Thanh toán | CardCharge | POST /api/v1/payments/card/charge | payment.collect |
| 371 | Phân quyền | ListPermissions | GET /api/v1/permissions | role.read |
| 372 | Dược Cấp phát | GetQueue | GET /api/v1/pharmacy/dispense/queue | dispense.queue |
| 373 | Dược Cấp phát | History | GET /api/v1/pharmacy/dispense/history | dispense.queue |
| 374 | Dược Cấp phát | Dispense | POST /api/v1/pharmacy/dispense/{prescriptionId} | dispense.perform |
| 375 | Dược Cấp phát | Reject | POST /api/v1/pharmacy/dispense/{id}/reject | dispense.reject |
| 376 | Dược Cấp phát | Return | POST /api/v1/pharmacy/dispense/{id}/return | dispense.return |
| 377 | Dược Cấp phát | ReceiptPdf | GET /api/v1/pharmacy/dispense/{id}/receipt-pdf | dispense.queue |
| 378 | Dược Kho | ListWarehouses | GET /api/v1/pharmacy/warehouses | warehouse.read |
| 379 | Dược Kho | CreateWarehouse | POST /api/v1/pharmacy/warehouses | warehouse.write |
| 380 | Dược Kho | GetWarehouse | GET /api/v1/pharmacy/warehouses/{id} | warehouse.read |
| 381 | Dược Kho | UpdateWarehouse | PUT /api/v1/pharmacy/warehouses/{id} | warehouse.write |
| 382 | Dược Kho | DeleteWarehouse | DELETE /api/v1/pharmacy/warehouses/{id} | warehouse.write |
| 383 | Dược Kho | ListPurchaseOrders | GET /api/v1/pharmacy/purchase-orders | warehouse.read |
| 384 | Dược Kho | CreatePurchaseOrder | POST /api/v1/pharmacy/purchase-orders | warehouse.write |
| 385 | Dược Kho | CreateGrn | POST /api/v1/pharmacy/purchase-orders/{id}/grn | warehouse.write |
| 386 | Dược Kho | ListStocks | GET /api/v1/pharmacy/stocks (alias /pharmacy/stock) | stock.read |
| 387 | Dược Kho | GetStockById | GET /api/v1/pharmacy/stock/{id} | stock.read |
| 388 | Dược Kho | LowStockList | GET /api/v1/pharmacy/stock/low | stock.read |
| 389 | Dược Kho | NearExpiryList | GET /api/v1/pharmacy/stock/near-expiry | stock.read |
| 390 | Dược Kho | CreateAdjustment | POST /api/v1/pharmacy/adjustments | stock.adjust |
| 391 | Dược Kho | ListMovements | GET /api/v1/pharmacy/movements | stock.read |
| 392 | Dược Kho | CreateTransfer | POST /api/v1/pharmacy/transfers | stock.adjust |
| 393 | Dược Kho | LowStockAlerts | GET /api/v1/pharmacy/alerts/low-stock | stock.read |
| 394 | Dược Kho | NearExpiryAlerts | GET /api/v1/pharmacy/alerts/near-expiry | stock.read |
| 395 | Dược Kho | ListLots | GET /api/v1/pharmacy/lots | stock.read |
| 396 | Dược Kho | StocktakePdf | GET /api/v1/pharmacy/stocktake | stock.read |
| 397 | Đơn thuốc | List | GET /api/v1/prescriptions | prescription.read |
| 398 | Đơn thuốc | Create | POST /api/v1/prescriptions | prescription.create |
| 399 | Đơn thuốc | GetDetail | GET /api/v1/prescriptions/{id} | prescription.read |
| 400 | Đơn thuốc | Update | PUT /api/v1/prescriptions/{id} | prescription.update |
| 401 | Đơn thuốc | Delete | DELETE /api/v1/prescriptions/{id} | prescription.update |
| 402 | Đơn thuốc | AddItems | POST /api/v1/prescriptions/{id}/items | prescription.update |
| 403 | Đơn thuốc | RemoveItem | DELETE /api/v1/prescriptions/{id}/items/{itemId} | prescription.update |
| 404 | Đơn thuốc | Sign | POST /api/v1/prescriptions/{id}/sign | prescription.sign |
| 405 | Đơn thuốc | Cancel | POST /api/v1/prescriptions/{id}/cancel | prescription.cancel |
| 406 | Đơn thuốc | DdiCheck | GET /api/v1/prescriptions/{id}/ddi-check | ddi.check |
| 407 | Đơn thuốc | GetQr | GET /api/v1/prescriptions/{id}/qr | prescription.read |
| 408 | Đơn thuốc | GetPdf | GET /api/v1/prescriptions/{id}/pdf | prescription.read |
| 409 | Đơn thuốc | GetPrintHistory | GET /api/v1/prescriptions/{id}/print-history | prescription.read |
| 410 | Đơn thuốc | SubmitDtqg | POST /api/v1/prescriptions/{id}/submit-dtqg | dtqg.submit |
| 411 | Đơn thuốc | DtqgSubmit | POST /api/v1/prescriptions/{id}/dtqg/submit | dtqg.submit |
| 412 | Đơn thuốc | DtqgStatus | GET /api/v1/prescriptions/{id}/dtqg/status | dtqg.submit |
| 413 | Đơn thuốc | DtqgRetry | POST /api/v1/prescriptions/{id}/dtqg/retry | dtqg.retry |
| 414 | API công khai B2B | RegisterPatient | POST /api/public/v1/patients/register | ApiKey scope public.patient.write |
| 415 | API công khai B2B | BookAppointment | POST /api/public/v1/appointments/book | ApiKey scope public.appointment.write |
| 416 | API công khai B2B | GetAppointment | GET /api/public/v1/appointments/{id} | ApiKey scope public.appointment.read |
| 417 | API công khai B2B | GetServicePackages | GET /api/public/v1/catalog/service-packages | ApiKey scope public.catalog.read |
| 418 | API công khai B2B | GetServices | GET /api/public/v1/catalog/services | ApiKey scope public.catalog.read |
| 419 | API công khai B2B | GetDoctors | GET /api/public/v1/catalog/doctors | ApiKey scope public.catalog.read |
| 420 | API công khai B2B | RequestVisitOtp | POST /api/public/v1/visits/{patientCode}/request-otp | ApiKey scope public.visit.lookup |
| 421 | API công khai B2B | VerifyVisitOtp | POST /api/public/v1/visits/{patientCode}/verify-otp | ApiKey scope public.visit.lookup |
| 422 | API công khai B2B | GetVisits | GET /api/public/v1/visits/{patientCode}/lookup | ApiKey scope public.visit.lookup |
| 423 | CĐHA | List | GET /api/v1/rad-results | rad_result.read |
| 424 | CĐHA | Create | POST /api/v1/rad-results | rad_result.write |
| 425 | CĐHA | Update | PUT /api/v1/rad-results/{id} | rad_result.write |
| 426 | CĐHA | Verify | POST /api/v1/rad-results/{id}/verify | rad_result.verify |
| 427 | CĐHA | DicomUpload | POST /api/v1/rad-results/{id}/dicom-upload | rad_result.write |
| 428 | CĐHA | ExportPdf | GET /api/v1/rad-results/{id}/pdf | rad_result.read |
| 429 | CĐHA | OcrExtract | POST /api/v1/rad-results/ocr-extract | rad_result.write |
| 430 | CĐHA | OcrConfirm | POST /api/v1/rad-results/ocr-confirm | rad_result.write |
| 431 | Tái khám | List | GET /api/v1/recall | recall.read |
| 432 | Tái khám | UpdateStatus | PATCH /api/v1/recall/{id} | recall.manage |
| 433 | Tái khám | Notify | POST /api/v1/recall/{id}/notify | recall.manage |
| 434 | Tái khám | CarePathwayTargets | GET /api/v1/care-pathway/targets | diabetes.assess |
| 435 | Lễ tân | CheckIn | POST /api/v1/reception/check-in | reception.checkin |
| 436 | Lễ tân | IssuePortalActivation | POST /api/v1/reception/patients/{id}/portal-activation | reception.checkin |
| 437 | Lễ tân | GetQueue | GET /api/v1/reception/queue | reception.queue.manage |
| 438 | Lễ tân | CallTicket | PUT /api/v1/reception/queue/{ticketId}/call | reception.queue.manage |
| 439 | Lễ tân | AdmitTicket | POST /api/v1/reception/queue/{ticketId}/admit | reception.queue.manage |
| 440 | Lễ tân | WaitCls | POST /api/v1/reception/tickets/{ticketId}/wait-cls | reception.queue.manage |
| 441 | Lễ tân | ResumeTicket | POST /api/v1/reception/tickets/{ticketId}/resume | reception.queue.manage |
| 442 | Lễ tân | SkipTicket | PUT /api/v1/reception/queue/{ticketId}/skip | reception.queue.manage |
| 443 | Lễ tân | CancelTicket | PUT /api/v1/reception/queue/{ticketId}/cancel | reception.queue.manage |
| 444 | Lễ tân | GetTicketPdf | GET /api/v1/reception/queue/{ticketId}/ticket-pdf | reception.queue.manage |
| 445 | Lễ tân | ReassignTicket | PUT /api/v1/reception/tickets/{ticketId}/reassign (alias /queue/{ticketId}/reassign) | reception.ticket.reassign |
| 446 | Lễ tân | GetTicketReassignments | GET /api/v1/reception/tickets/{ticketId}/reassignments | reception.queue.manage |
| 447 | Lễ tân | GetRooms | GET /api/v1/reception/rooms | reception.rooms.read |
| 448 | Lễ tân | GetStats | GET /api/v1/reception/stats | reception.stats.read |
| 449 | Báo cáo | GetRevenue | GET /api/v1/reports/revenue | report.read |
| 450 | Báo cáo | GetRevenueByDoctor | GET /api/v1/reports/revenue/by-doctor | report.read |
| 451 | Báo cáo | GetRevenueByService | GET /api/v1/reports/revenue/by-service | report.read |
| 452 | Báo cáo | GetRevenueByPaymentMethod | GET /api/v1/reports/revenue/by-payment-method | report.read |
| 453 | Báo cáo | GetCashierDailySummary | GET /api/v1/reports/cashier/daily-summary | report.read |
| 454 | Báo cáo | GetDebtsAging | GET /api/v1/reports/debts/aging | report.read |
| 455 | Báo cáo | GetBhytSummary | GET /api/v1/reports/bhyt/summary | report.read |
| 456 | Báo cáo | GetDiabetesCohort | GET /api/v1/reports/clinical/diabetes-cohort | report.read |
| 457 | Báo cáo | GetDiabetesCohortDetailed | GET /api/v1/reports/diabetes/cohort | report.read |
| 458 | Báo cáo | GetEncountersCount | GET /api/v1/reports/encounters/count | report.read |
| 459 | Báo cáo | GetTopDiagnoses | GET /api/v1/reports/diagnoses/top | report.read |
| 460 | Báo cáo | GetClinicalVisits | GET /api/v1/reports/clinical/visits | report.read |
| 461 | Báo cáo | GetClinicalIcd10 | GET /api/v1/reports/clinical/icd10 | report.read |
| 462 | Báo cáo | GetTopDrugs | GET /api/v1/reports/pharmacy/top-drugs | report.read |
| 463 | Báo cáo | GetInventoryValue | GET /api/v1/reports/pharmacy/inventory-value | report.read |
| 464 | Báo cáo | ReserveReportCode | POST /api/v1/reports/{type}/code | report.export |
| 465 | Báo cáo | GetPdf | GET /api/v1/reports/{type}/pdf | report.export |
| 466 | Báo cáo | ExportReport | POST /api/v1/reports/export | report.export |
| 467 | Báo cáo | GetCatalog | GET /api/v1/reports/catalog | report.read |
| 468 | Báo cáo | GetReportData | GET /api/v1/reports/{code}/data | report.read |
| 469 | Báo cáo | ExportGenericReport | GET /api/v1/reports/{code}/export | report.export |
| 470 | Báo cáo | GetReportOptions | GET /api/v1/reports/options/{source} | report.read |
| 471 | Báo cáo | GetDatasets | GET /api/v1/reports/datasets | report.build |
| 472 | Báo cáo | GetDefinitions | GET /api/v1/reports/definitions | report.build |
| 473 | Báo cáo | CreateDefinition | POST /api/v1/reports/definitions | report.build |
| 474 | Báo cáo | UpdateDefinition | PUT /api/v1/reports/definitions/{id} | report.build |
| 475 | Báo cáo | DeleteDefinition | DELETE /api/v1/reports/definitions/{id} | report.build |
| 476 | Báo cáo | PreviewDefinition | POST /api/v1/reports/preview | report.build |
| 477 | Báo cáo | GetSchedules | GET /api/v1/reports/schedules | report.build |
| 478 | Báo cáo | CreateSchedule | POST /api/v1/reports/schedules | report.build |
| 479 | Báo cáo | UpdateSchedule | PUT /api/v1/reports/schedules/{id} | report.build |
| 480 | Báo cáo | DeleteSchedule | DELETE /api/v1/reports/schedules/{id} | report.build |
| 481 | Báo cáo | GetDashboards | GET /api/v1/reports/dashboards | report.read |
| 482 | Báo cáo | GetDashboardById | GET /api/v1/reports/dashboards/{id} | report.read |
| 483 | Báo cáo | CreateDashboard | POST /api/v1/reports/dashboards | report.build |
| 484 | Báo cáo | UpdateDashboard | PUT /api/v1/reports/dashboards/{id} | report.build |
| 485 | Báo cáo | DeleteDashboard | DELETE /api/v1/reports/dashboards/{id} | report.build |
| 486 | Báo cáo | GetDashboardData | GET /api/v1/reports/dashboards/{id}/data | report.read |
| 487 | Vai trò | ListRoles | GET /api/v1/roles | role.read |
| 488 | Vai trò | CreateRole | POST /api/v1/roles | role.write |
| 489 | Vai trò | GetRole | GET /api/v1/roles/{code} | role.read |
| 490 | Vai trò | UpdateRole | PUT /api/v1/roles/{code} | role.write |
| 491 | Vai trò | DeleteRole | DELETE /api/v1/roles/{code} | role.write |
| 492 | Phòng khám | ListRooms | GET /api/v1/rooms | room.read |
| 493 | Phòng khám | GetRoom | GET /api/v1/rooms/{id} | room.read |
| 494 | Phòng khám | CreateRoom | POST /api/v1/rooms | room.write |
| 495 | Phòng khám | UpdateRoom | PUT /api/v1/rooms/{id} | room.write |
| 496 | Phòng khám | DeleteRoom | DELETE /api/v1/rooms/{id} | room.delete |
| 497 | Giá dịch vụ | List | GET /api/v1/service-price-overrides | service.price_override |
| 498 | Giá dịch vụ | GetById | GET /api/v1/service-price-overrides/{id} | service.price_override |
| 499 | Giá dịch vụ | Create | POST /api/v1/service-price-overrides | service.price_override |
| 500 | Giá dịch vụ | Update | PUT /api/v1/service-price-overrides/{id} | service.price_override |
| 501 | Giá dịch vụ | Delete | DELETE /api/v1/service-price-overrides/{id} | service.price_override |
| 502 | Danh mục dịch vụ | List | GET /api/v1/services | service.read |
| 503 | Danh mục dịch vụ | Create | POST /api/v1/services | service.write |
| 504 | Danh mục dịch vụ | Search | GET /api/v1/services/search | service.read |
| 505 | Danh mục dịch vụ | Categories | GET /api/v1/services/categories | Authorize |
| 506 | Danh mục dịch vụ | Import | POST /api/v1/services/import | service.write |
| 507 | Danh mục dịch vụ | GetById | GET /api/v1/services/{id} | service.read |
| 508 | Danh mục dịch vụ | Update | PUT /api/v1/services/{id} | service.write |
| 509 | Danh mục dịch vụ | Delete | DELETE /api/v1/services/{id} | service.write |
| 510 | Gói dịch vụ (catalog) | List | GET /api/v1/service-packages | service_package.read |
| 511 | Gói dịch vụ (catalog) | Create | POST /api/v1/service-packages | service_package.write |
| 512 | Gói dịch vụ (catalog) | GetById | GET /api/v1/service-packages/{id} | service_package.read |
| 513 | Gói dịch vụ (catalog) | Update | PUT /api/v1/service-packages/{id} | service_package.write |
| 514 | Gói dịch vụ (catalog) | Delete | DELETE /api/v1/service-packages/{id} | service_package.write |
| 515 | Điều chuyển kho | List | GET /api/v1/stock-transfers | stock_transfer.read |
| 516 | Điều chuyển kho | GetById | GET /api/v1/stock-transfers/{id} | stock_transfer.read |
| 517 | Điều chuyển kho | Create | POST /api/v1/stock-transfers | stock_transfer.create |
| 518 | Điều chuyển kho | Submit | POST /api/v1/stock-transfers/{id}/submit | stock_transfer.create |
| 519 | Điều chuyển kho | Approve | POST /api/v1/stock-transfers/{id}/approve | stock_transfer.approve |
| 520 | Điều chuyển kho | Reject | POST /api/v1/stock-transfers/{id}/reject | stock_transfer.approve |
| 521 | Điều chuyển kho | Ship | POST /api/v1/stock-transfers/{id}/ship | stock_transfer.ship |
| 522 | Điều chuyển kho | Receive | POST /api/v1/stock-transfers/{id}/receive | stock_transfer.receive |
| 523 | Điều chuyển kho | PartialReceive | POST /api/v1/stock-transfers/{id}/partial-receive | stock_transfer.receive |
| 524 | Điều chuyển kho | Close | POST /api/v1/stock-transfers/{id}/close | stock_transfer.receive |
| 525 | Điều chuyển kho | Cancel | POST /api/v1/stock-transfers/{id}/cancel | stock_transfer.create |
| 526 | Nhà cung cấp | List | GET /api/v1/suppliers | supplier.read |
| 527 | Nhà cung cấp | Get | GET /api/v1/suppliers/{id} | supplier.read |
| 528 | Nhà cung cấp | Create | POST /api/v1/suppliers | supplier.write |
| 529 | Nhà cung cấp | Update | PUT /api/v1/suppliers/{id} | supplier.write |
| 530 | Nhà cung cấp | Delete | DELETE /api/v1/suppliers/{id} | supplier.write |
| 531 | Telehealth QT | List | GET /api/v1/telehealth/service-mappings | telehealth.admin_mapping |
| 532 | Telehealth QT | Create | POST /api/v1/telehealth/service-mappings | telehealth.admin_mapping |
| 533 | Telehealth QT | Update | PUT /api/v1/telehealth/service-mappings/{id} | telehealth.admin_mapping |
| 534 | Telehealth QT | ListAllowedIcd10 | GET /api/v1/telehealth/allowed-icd10 | telehealth.icd10_read |
| 535 | Telehealth QT | CreateAllowedIcd10 | POST /api/v1/telehealth/allowed-icd10 | telehealth.icd10_manage |
| 536 | Telehealth QT | UpdateAllowedIcd10 | PUT /api/v1/telehealth/allowed-icd10/{id} | telehealth.icd10_manage |
| 537 | Telehealth QT | DeleteAllowedIcd10 | DELETE /api/v1/telehealth/allowed-icd10/{id} | telehealth.icd10_manage |
| 538 | Telehealth Cổng BN | CheckEligibility | GET /api/v1/portal/telehealth/eligibility | PortalBearer |
| 539 | Telehealth Cổng BN | LinkAccount | POST /api/v1/portal/telehealth/link-docosan-account | PortalBearer |
| 540 | Telehealth Cổng BN | CreateAppointment | POST /api/v1/portal/telehealth/appointments | PortalBearer |
| 541 | Telehealth Cổng BN | GetAppointment | GET /api/v1/portal/telehealth/appointments/{id} | PortalBearer |
| 542 | Telehealth Cổng BN | GetJoinLink | GET /api/v1/portal/telehealth/appointments/{id}/join-link | PortalBearer |
| 543 | Tenant | ListTenants | GET /api/v1/tenants | RequireSuperAdmin |
| 544 | Tenant | CreateTenant | POST /api/v1/tenants | RequireSuperAdmin |
| 545 | Tenant | GetTenant | GET /api/v1/tenants/{id} | RequireSuperAdmin |
| 546 | Tenant | UpdateTenant | PUT /api/v1/tenants/{id} | RequireSuperAdmin |
| 547 | Tenant | DeleteTenant | DELETE /api/v1/tenants/{id} | RequireSuperAdmin |
| 548 | Tenant | SuspendTenant | POST /api/v1/tenants/{id}/suspend | RequireSuperAdmin |
| 549 | Tenant | ActivateTenant | POST /api/v1/tenants/{id}/activate | RequireSuperAdmin |
| 550 | Tenant | GetCurrentTenant | GET /api/v1/tenants/current | tenant.read |
| 551 | Tenant | GetMyTenant | GET /api/v1/tenants/me | tenant.read |
| 552 | Tenant | GetMyLetterhead | GET /api/v1/tenants/me/letterhead | tenant.read |
| 553 | Tenant | UpdateMyTenant | PUT /api/v1/tenants/me | tenant.write |
| 554 | Người dùng | ListUsers | GET /api/v1/users | user.read |
| 555 | Người dùng | InviteUser | POST /api/v1/users/invite | user.invite |
| 556 | Người dùng | AcceptInvite | POST /api/v1/users/accept-invite | AllowAnonymous |
| 557 | Người dùng | GetUser | GET /api/v1/users/{id} | user.read |
| 558 | Người dùng | UpdateUser | PUT /api/v1/users/{id} | user.write |
| 559 | Người dùng | DeleteUser | DELETE /api/v1/users/{id} | user.delete |
| 560 | Người dùng | AssignRoles | POST /api/v1/users/{id}/roles | user.assign_role |
| 561 | Người dùng | RevokeRole | DELETE /api/v1/users/{id}/roles/{roleCode} | user.assign_role |
| 562 | Người dùng | GetUserBranches | GET /api/v1/users/{id}/branches | user.read |
| 563 | Người dùng | SetUserBranches | PUT /api/v1/users/{id}/branches | branch.assign_user |
| 564 | Người dùng | DisableUser | POST /api/v1/users/{id}/disable | user.write |
| 565 | Người dùng | EnableUser | POST /api/v1/users/{id}/enable | user.write |
| 566 | Người dùng | GetMe | GET /api/v1/users/me | Authorize |
| 567 | Người dùng | UpdateMe | PUT /api/v1/users/me | Authorize |
| 568 | Người dùng | ChangePassword | POST /api/v1/users/me/change-password | Authorize |
| 569 | Người dùng | Setup2FA | POST /api/v1/users/me/2fa/setup | Authorize (Bearer,MfaSetup) |
| 570 | Người dùng | Enable2FA | POST /api/v1/users/me/2fa/enable | Authorize (Bearer,MfaSetup) |
| 571 | Người dùng | Disable2FA | POST /api/v1/users/me/2fa/disable | Authorize |
| 572 | Sinh hiệu | Create | POST /api/v1/encounters/{encounterId}/vital-signs | vital_sign.write |
| 573 | Sinh hiệu | List | GET /api/v1/encounters/{encounterId}/vital-signs | vital_sign.read |
| 574 | Sinh hiệu | Latest | GET /api/v1/encounters/{encounterId}/vital-signs/latest | vital_sign.read |
| 575 | Sinh hiệu | Batch | POST /api/v1/encounters/{encounterId}/vital-signs/batch | vital_sign.write |
| 576 | Sinh hiệu | Update | PUT /api/v1/vital-signs/{id} | vital_sign.write |
| 577 | Sinh hiệu | Delete | DELETE /api/v1/vital-signs/{id} | vital_sign.delete |
| 578 | Sinh hiệu | History | GET /api/v1/patients/{patientId}/vital-signs/history | vital_sign.read |
| 579 | Sinh hiệu | Trend | GET /api/v1/patients/{patientId}/vital-signs/trend | vital_sign.read |

---

## 3. Tổng kết danh mục

| Chỉ số | Giá trị |
|---|---|
| File controller | 69 |
| Class controller | 74 |
| **Tổng số function/endpoint** | **579** |
| Endpoint dùng `[RequirePermission]` | ~512 |
| Endpoint `AllowAnonymous` | 17 |
| Endpoint `Authorize` trơn | 14 |
| Endpoint `RequireSuperAdmin` | 10 |
| Endpoint `PortalBearer` (cổng bệnh nhân) | 29 |
| Endpoint `ApiKey` (B2B / webhook) | 10 |
| Số chuỗi permission riêng biệt | ~120 |

### Ghi chú rủi ro phát hiện khi lập danh mục
1. **`service.price_override` dùng chung cho cả 5 thao tác CRUD** (đọc/tạo/sửa/xóa giá dịch vụ) —
   không tách read/write. Người chỉ cần xem giá buộc phải được cấp quyền sửa/xóa. Tương tự với
   `drug.price_override`. *Đây là rủi ro phân quyền, nên tách thành `.read` / `.write`.*
2. **`legacy_import.write` dùng cho cả các endpoint chỉ đọc** (`GET /legacy-imports`,
   `GET /legacy-imports/{id}/items`) — người xem tiến độ nhập liệu buộc phải có quyền ghi.
3. **`lab_result.write` gác endpoint chỉ đọc** `GET /lab-results/pending-items`.
4. **`dispense.queue` gác endpoint xuất PDF** `GET /pharmacy/dispense/{id}/receipt-pdf` — chấp nhận được
   nhưng không nhất quán với các module khác (thường dùng `.read`).
5. **`dtqg.submit` gác endpoint chỉ đọc** `GET /prescriptions/{id}/dtqg/status`.
