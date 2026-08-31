# ITC — INTEGRATION TEST CASE PHỦ TOÀN BỘ CHỨC NĂNG
**Hệ thống:** Pro-Diab HIS · **Ngày:** 2026-08-31 · **Nhánh:** develop
**Đối chiếu danh mục:** `docs/qc/danh-muc-full-function-20260831.md` (579 endpoint)

---

## 1. Môi trường thực thi ITC (env parity)

| Hạng mục | Cấu hình khi chạy ITC | Ghi chú parity với prod |
|---|---|---|
| DB | **MySQL 8.0.36 thật** trong Docker (Testcontainers) | ✅ Đúng engine + version như prod |
| Host API | **API thật** boot bằng `WebApplicationFactory<Program>` | ✅ Đi qua đúng pipeline production |
| Pipeline | JwtBearer → TenantScope → BranchScope → Authorization → Controller → MediatR → DB | ✅ Không bỏ qua middleware nào |
| Auth | JWT **ký thật**, claim bám đúng `JwtService.GenerateAccessToken` | ✅ |
| Redis | Không có → `IRateLimiter` fallback in-memory | ⚠️ Prod dùng Redis |
| Rate limit | Thay bằng bản always-allow | ⚠️ **Chỉ là hạ tầng test** — tránh 429 giả khi chạy >100 request/phút. Không che giấu logic nghiệp vụ nào. |
| Schema | EF `EnsureCreated()` + `TestSchemaSupplement` (DDL trích nguyên văn từ `db/migrations`) | ⚠️ Xem mục 5 — hạn chế đã biết |

> **Vì sao không dựng schema bằng `db/migrations/*.sql`:** chính tài liệu
> `db/migrations/APPLY_ORDER.md` của dự án ghi nhận (kiểm chứng 2026-08-20) rằng
> **chuỗi migration hiện CHƯA dựng được DB sạch từ số 0 — 30/150 file lỗi SQL thật**.
> Đây là nợ kỹ thuật có sẵn, không phải do QC gây ra, nhưng nó **chặn** hướng dựng
> schema test bằng migration. Xem mục 5 để biết ảnh hưởng.

---

## 2. Ba khuôn ITC chuẩn (archetype)

Mọi ITC trong tài liệu này thuộc 1 trong 3 khuôn dưới. Mỗi khuôn đã định nghĩa sẵn
*mục tiêu / tiền điều kiện / bước / kết quả mong đợi*, nên phần liệt kê ở mục 3 chỉ cần
nêu mã ITC + endpoint + khuôn áp dụng.

### Khuôn A — Chặn truy cập khi CHƯA ĐĂNG NHẬP (bảo mật)
| Mục | Nội dung |
|---|---|
| **Mục tiêu** | Chứng minh endpoint KHÔNG thể gọi được nếu không có JWT hợp lệ |
| **Tiền điều kiện** | Không cần data. Không gắn header `Authorization` |
| **Bước** | 1. Gọi `<METHOD> <route>` không kèm token<br>2. Đọc HTTP status |
| **Kết quả mong đợi** | `401 Unauthorized` |
| **Áp dụng** | Mọi endpoint có `[Authorize]`. **Không** áp dụng cho `[AllowAnonymous]`, `PortalBearer`, `ApiKey` |
| **Vì sao quan trọng** | Đây là hàng rào cuối cùng chống lộ dữ liệu bệnh nhân (PHI). Một endpoint quên `[Authorize]` = lộ toàn bộ hồ sơ y tế. |

### Khuôn B — Chặn khi ĐÃ ĐĂNG NHẬP nhưng THIẾU QUYỀN (phân quyền RBAC)
| Mục | Nội dung |
|---|---|
| **Mục tiêu** | Chứng minh `[RequirePermission]` thực sự được thực thi, không chỉ khai báo |
| **Tiền điều kiện** | JWT hợp lệ (ký đúng, còn hạn, có `tenant_id`) nhưng **không có claim `permissions` nào** |
| **Bước** | 1. Gọi `<METHOD> <route>` kèm token thiếu quyền<br>2. Đọc status + body |
| **Kết quả mong đợi** | `403 Forbidden` **và** body chứa `PERMISSION_DENIED` |
| **Áp dụng** | Endpoint có `[RequirePermission("...")]` |
| **Không áp dụng** | `RequireSuperAdmin` (mã lỗi khác), `[Authorize]` trơn, `ApiKey` |
| **Biến thể "sai quyền chéo"** | Cấp **đúng 1 quyền khác** rồi gọi → vẫn phải 403. Bắt lỗi copy-paste sai chuỗi permission giữa các action (lỗi rất hay gặp khi clone module). |

### Khuôn C — Tiếp cận được khi ĐÚNG QUYỀN (reachability / không sập)
| Mục | Nội dung |
|---|---|
| **Mục tiêu** | Với đúng quyền, request phải đi lọt qua toàn bộ pipeline và handler không ném exception |
| **Tiền điều kiện** | JWT có **đúng** chuỗi permission mà controller khai báo |
| **Bước** | 1. Gọi `GET <route danh sách>` kèm token đúng quyền<br>2. Đọc status |
| **Kết quả mong đợi** | Status `< 500`, **không** 401, **không** 403.<br>Với GET danh sách chắc chắn trả rỗng-vẫn-200 → assert thẳng `200 OK` |
| **Áp dụng** | Endpoint GET danh sách không phụ thuộc bản ghi cụ thể |
| **Không áp dụng** | POST/PUT/DELETE (tránh ghi dữ liệu rác), GET `/{id}` (phụ thuộc data seed) |
| **Giá trị** | Bắt được: sai chuỗi permission (ra 403), lỗi SQL/handler (ra 500), route không khớp (404) |

### Vì sao KHÔNG dùng khuôn "happy path ghi dữ liệu"
Ở vòng này ưu tiên **phủ rộng toàn hệ thống** (579 endpoint) thay vì đào sâu vài module.
Case tạo/sửa/xóa thật cần seed dữ liệu phụ thuộc lẫn nhau (tenant → branch → user → patient →
encounter → order → result → billing…) và cần schema đầy đủ — bị chặn bởi vấn đề migration ở mục 5.
Đây là **nợ đã ghi nhận**, xem mục 6 "Đợt sau".

---

## 3. Bảng ITC theo module

`#Case` = số test **thật, chạy được** trong file tương ứng.

### 3.1 Lâm sàng
| Mã ITC | Module | File test | #Case | Khuôn |
|---|---|---|---|---|
| ITC-PATIENT-xx | Hồ sơ bệnh nhân | `Clinical/PatientsApiIntegrationTests.cs` | 53 | A+B+C |
| ITC-ENCOUNTER-xx | Lượt khám | `Clinical/EncountersApiIntegrationTests.cs` | 32 | A+B+C |
| ITC-EMR-xx | Bệnh án điện tử + Mẫu EMR | `Clinical/EmrApiIntegrationTests.cs` | 28 | A+B+C |
| ITC-VITAL-xx | Sinh hiệu | `Clinical/VitalSignsApiIntegrationTests.cs` | 18 | A+B+C |
| ITC-DIABETES-xx | Đánh giá ĐTĐ + Mẫu | `Clinical/DiabetesApiIntegrationTests.cs` | 19 | A+B+C |
| ITC-DIABETES-xx | Theo dõi & nguy cơ ĐTĐ | `Clinical/DiabetesDashboardApiIntegrationTests.cs` | 10 | A+B+C |
| ITC-INBODY-xx | Báo cáo InBody | `Clinical/InBodyReportsApiIntegrationTests.cs` | 11 | A+B+C |
| (đợt trước) | Trùng CCCD khi quét QR | `Patients/CccdDuplicateIntegrationTests.cs` | 8 | nghiệp vụ |

### 3.2 Cận lâm sàng
| Mã ITC | Module | File test | #Case | Khuôn |
|---|---|---|---|---|
| ITC-CLSORDER-xx | Chỉ định XN + CĐHA | `Cls/ClsOrdersApiIntegrationTests.cs` | 27 | A+B+C |
| ITC-CLSROUND-xx | Đợt chỉ định CLS | `Cls/ClsRoundsApiIntegrationTests.cs` | 16 | A+B+C |
| ITC-CLSUPLOAD-xx | Tệp kết quả CLS | `Cls/ClsUploadsApiIntegrationTests.cs` | 13 | A+B+C |
| ITC-LABRESULT-xx | Kết quả xét nghiệm | `Cls/LabResultsApiIntegrationTests.cs` | 30 | A+B+C |
| ITC-LABPARTNER-xx | Đối tác xét nghiệm | `Cls/LabPartnersApiIntegrationTests.cs` | 32 | A+B+C |
| ITC-LABINT-xx | Tích hợp XN + Webhook | `Cls/LabIntegrationApiIntegrationTests.cs` | 19 | A+B+C |
| ITC-RADRESULT-xx | Chẩn đoán hình ảnh | `Cls/RadResultsApiIntegrationTests.cs` | 18 | A+B+C |
| (đợt trước) | Cờ bất thường KQ XN | `LabResults/LabResultFlagIntegrationTests.cs` | 6 | nghiệp vụ |

### 3.3 Dược & Kho
| Mã ITC | Module | File test | #Case | Khuôn |
|---|---|---|---|---|
| ITC-PRESC-xx | Đơn thuốc + liên thông ĐTQG | `Pharmacy/PrescriptionsApiIntegrationTests.cs` | 35 | A+B+C |
| ITC-DRUG-xx | Danh mục thuốc | `Pharmacy/DrugsApiIntegrationTests.cs` | 28 | A+B+C |
| ITC-WAREHOUSE-xx | Kho, tồn, nhập, cảnh báo | `Pharmacy/PharmacyWarehouseApiIntegrationTests.cs` | 47 | A+B+C |
| ITC-DISPENSE-xx | Cấp phát thuốc | `Pharmacy/PharmacyDispensingApiIntegrationTests.cs` | 15 | A+B+C |
| ITC-TRANSFER-xx | Điều chuyển kho liên CN | `Pharmacy/StockTransfersApiIntegrationTests.cs` | 24 | A+B+C |
| ITC-SUPPLIER-xx | Nhà cung cấp | `Pharmacy/SuppliersApiIntegrationTests.cs` | 12 | A+B+C |
| ITC-DRUGPRICE-xx | Giá thuốc theo chi nhánh | `Pharmacy/DrugPriceOverridesApiIntegrationTests.cs` | 12 | A+B+C |
| ITC-DTQG-xx | Cổng Đơn thuốc Quốc gia | `Pharmacy/DtqgApiIntegrationTests.cs` | 12 | A+B+C |
| (đợt trước) | Gộp dòng khi cấp phát | `Pharmacy/DispenseMergeIntegrationTests.cs` | 3 | nghiệp vụ |

### 3.4 Tài chính & Gói dịch vụ
| Mã ITC | Module | File test | #Case | Khuôn |
|---|---|---|---|---|
| ITC-BILLING-xx | Viện phí | `Finance/BillingsIntegrationTests.cs` | 27 | A+B+C |
| ITC-PAYMENT-xx | Thanh toán, QR, thẻ | `Finance/PaymentsIntegrationTests.cs` | 22 | A+B+C |
| ITC-CASHIER-xx | Thu ngân & ca trực | `Finance/CashierIntegrationTests.cs` | 19 | A+B+C |
| ITC-EINVOICE-xx | Hóa đơn điện tử | `Finance/EInvoicesIntegrationTests.cs` | 12 | A+B+C |
| ITC-SERVICE-xx | Danh mục DV + gói DV | `Finance/ServicesIntegrationTests.cs` | 29 | A+B+C |
| ITC-PKGSUB-xx | Thuê bao gói dịch vụ | `Finance/PackageSubscriptionsIntegrationTests.cs` | 16 | A+B+C |
| ITC-PACKAGE-xx | Gói dịch vụ | `Finance/PackagesIntegrationTests.cs` | 12 | A+B+C |
| ITC-SVCPRICE-xx | Giá dịch vụ theo CN | `Finance/ServicePriceOverridesIntegrationTests.cs` | 12 | A+B+C |
| ITC-IBDEBT-xx | Công nợ liên chi nhánh | `Finance/InterBranchDebtsIntegrationTests.cs` | 6 | A+B+C |

### 3.5 Quản trị & Bảo mật
| Mã ITC | Module | File test | #Case | Khuôn |
|---|---|---|---|---|
| ITC-USER-xx | Người dùng, mời, 2FA | `Admin/UsersApiIntegrationTests.cs` | 29 | A+B |
| ITC-BRANCH-xx | Chi nhánh | `Admin/BranchesApiIntegrationTests.cs` | 28 | A+B+C |
| ITC-NOTIFY-xx | Thông báo, web push | `Admin/NotificationsApiIntegrationTests.cs` | 26 | A+B |
| ITC-APIPARTNER-xx | Đối tác API | `Admin/ApiPartnersApiIntegrationTests.cs` | 19 | A+B+C |
| ITC-TENANT-xx | Tenant (super admin) | `Admin/TenantsApiIntegrationTests.cs` | 15 | A+B |
| ITC-FILE-xx | Tệp & chú thích | `Admin/FilesApiIntegrationTests.cs` | 13 | A+B |
| ITC-NOTIFYCH-xx | Kênh thông báo | `Admin/NotificationChannelsApiIntegrationTests.cs` | 13 | A+B+C |
| ITC-ROLE-xx | Vai trò | `Admin/RolesApiIntegrationTests.cs` | 11 | A+B+C |
| ITC-ENCADMIN-xx | Quản trị mã hóa, xoay khóa | `Admin/EncryptionAdminApiIntegrationTests.cs` | 7 | A+B+C |
| ITC-AUDIT-xx | Nhật ký kiểm toán | `Admin/AuditLogsApiIntegrationTests.cs` | 5 | A+B+C |
| ITC-PERM-xx | Danh sách quyền | `Admin/PermissionsApiIntegrationTests.cs` | 3 | A+B+C |
| ITC-ME-xx | Ngữ cảnh chi nhánh của tôi | `Admin/MeApiIntegrationTests.cs` | 3 | A |
| ITC-FFLAG-xx | Cờ tính năng | `Admin/FeatureFlagsApiIntegrationTests.cs` | 3 | A |
| (đợt trước) | Đăng nhập | `Auth/LoginIntegrationTests.cs` | 2 | nghiệp vụ |

### 3.6 Vận hành & Tích hợp
| Mã ITC | Module | File test | #Case | Khuôn |
|---|---|---|---|---|
| ITC-REPORT-xx | Báo cáo + Report Builder + Dashboard tùy biến | `Ops/ReportsOpsIntegrationTests.cs` | 80 | A+B+C |
| ITC-BHYTEXP/REC-xx | BHYT xuất hồ sơ + đối chiếu | `Ops/BhytOpsIntegrationTests.cs` | 35 | A+B+C |
| ITC-RECEPTION-xx | Lễ tân, hàng đợi, điều phối | `Ops/ReceptionOpsIntegrationTests.cs` | 23 | A+B+C |
| ITC-DASHBOARD-xx | Dashboard tổng quan | `Ops/DashboardOpsIntegrationTests.cs` | 20 | A+B+C |
| ITC-CDSS/AI/DOCUMENT-xx | CDSS, gợi ý AI, upload thông minh | `Ops/ClinicalSupportOpsIntegrationTests.cs` | 19 | A+B+C |
| ITC-APPT-xx | Đặt lịch hẹn | `Ops/AppointmentsOpsIntegrationTests.cs` | 16 | A+B+C |
| ITC-DOCSCHED-xx | Lịch làm việc bác sĩ | `Ops/DoctorSchedulesOpsIntegrationTests.cs` | 16 | A+B+C |
| ITC-TELEADMIN-xx | Telehealth quản trị | `Ops/TelehealthAdminOpsIntegrationTests.cs` | 15 | A+B+C |
| ITC-LEGACY-xx | Nhập liệu hồ sơ giấy cũ | `Ops/LegacyImportsOpsIntegrationTests.cs` | 15 | A+B+C |
| ITC-ICD10/CODE-xx | ICD-10 + danh mục mã | `Ops/Icd10CodesOpsIntegrationTests.cs` | 14 | A+B+C |
| ITC-DOCTOR/ROOM-xx | Danh bạ bác sĩ + phòng khám | `Ops/DoctorsRoomsOpsIntegrationTests.cs` | 14 | A+B+C |
| ITC-FHIR-xx | FHIR R4 | `Ops/FhirOpsIntegrationTests.cs` | 13 | A+B+C |
| ITC-RECALL-xx | Nhắc tái khám | `Ops/RecallOpsIntegrationTests.cs` | 8 | A+B+C |
| (đợt trước) | Sinh XML BHYT | `Bhyt/BhytXmlGeneratorIntegrationTests.cs` | 2 | nghiệp vụ |

### 3.7 Bộ khung test (tự kiểm chứng)
| Mã ITC | Mục tiêu | File | #Case |
|---|---|---|---|
| ITC-HARNESS-01..06 | Chứng minh khung test chạy thật: API boot trên MySQL container, DB OK, 401 khi thiếu token, 403 khi thiếu quyền, 401 khi token hết hạn, super admin bypass | `Infrastructure/HarnessSmokeTests.cs` | 6 |

> Không có ITC-HARNESS thì mọi ITC khác **không đáng tin** — nếu khung sai, một suite
> "toàn PASS" có thể chỉ là không gọi tới đâu cả.

---

## 4. Ma trận phủ theo cơ chế phân quyền

| Cơ chế | Số endpoint | Khuôn A (401) | Khuôn B (403) | Ghi chú |
|---|---|---|---|---|
| `[RequirePermission]` | ~512 | ✅ phủ | ✅ phủ phần lớn | Trọng tâm của đợt này |
| `[Authorize]` trơn | 14 | ✅ phủ | ➖ không áp dụng | Không có permission để thiếu |
| `RequireSuperAdmin` | 10 | ✅ phủ | ➖ mã lỗi khác | Cần case riêng ở đợt sau |
| `AllowAnonymous` | 17 | ➖ không áp dụng | ➖ | 1 bug phát hiện: xem BUG-001 |
| `PortalBearer` | 29 | ⚠️ **CHƯA phủ** | ⚠️ **CHƯA phủ** | Cần token aud=`patient-portal` — đợt sau |
| `ApiKey` (B2B/webhook) | 10 | ⚠️ phủ 1 phần | ⚠️ | Cần seed API key thật — đợt sau |

---

## 5. Hạn chế đã biết của môi trường ITC (KHÔNG che giấu)

`EnsureCreated()` chỉ tạo bảng **có entity EF**. Nhiều bảng có thật trong hệ thống lại chỉ được
tạo bởi `db/migrations/*.sql`, và read-side dùng Dapper raw SQL đọc thẳng vào đó.
`TestSchemaSupplement.cs` đã bù đắp bằng DDL **trích nguyên văn từ migrations**, nhưng chưa phủ hết.

**Hệ quả:** một số ITC khuôn C trả `500 Table doesn't exist`. Đây là **thiếu schema môi trường test**,
KHÔNG phải bug sản phẩm — đã xác minh bằng cách đọc log exception thật (`MySqlException: Table
'prodiab_his_test.xxx' doesn't exist`), không suy đoán.

**Không case nào bị bỏ qua âm thầm.** Case chưa chạy được đều để nguyên trạng thái đỏ/SKIP
kèm lý do, và được thống kê ở báo cáo cuối.

### Phát hiện phụ về schema drift (cần dev xác nhận — chưa kết luận là bug)
| # | Hiện tượng | Bằng chứng | Rủi ro |
|---|---|---|---|
| SD-1 | EF map `scopes_json`, migration `9014` tạo cột `scopes` | `ApiPartnerConfiguration.cs:19` vs `9014_fix_dtqg_apipartners_schema.sql:47` | EF ghi 1 cột, SQL thô đọc cột khác → có thể mất dữ liệu scope |
| SD-2 | Raw SQL đọc `d.name_vi`, `d.name_en` nhưng entity EF chỉ có `name` | `DrugHandlers.cs:144,178`; `PharmacyConfiguration.cs:16` | Model EF lệch schema thật; code còn ghi trùng `name`+`name_vi` (comment BUG-03 trong chính source) |
| SD-3 | Raw SQL tham chiếu bảng tên legacy không prefix (`pat_patients`, `sec_users`, `pha_warehouses`, `cli_lab_inbound/outbound`) | Log ITC + `9009_create_legacy_views.sql` | Phụ thuộc VIEW tương thích ngược; nếu view bị drop, read-side sập |

---

## 6. Đợt sau — phần CHƯA làm (ghi rõ để PO quyết)

| Ưu tiên | Hạng mục | Vì sao chưa làm |
|---|---|---|
| 1 | **Case nghiệp vụ ghi dữ liệu** (tạo BN → khám → chỉ định → KQ → kê đơn → thu tiền) | Cần seed phụ thuộc + schema đầy đủ; bị chặn bởi vấn đề migration |
| 2 | **Sửa chuỗi migration dựng được DB sạch từ 0** | Nợ kỹ thuật có sẵn (30/150 file lỗi). Sửa xong thì mục 5 biến mất và ITC khuôn C phủ thật 100% |
| 3 | **Cổng bệnh nhân (29 endpoint PortalBearer)** | Cần token `aud=patient-portal` + kích hoạt tài khoản BN |
| 4 | **API công khai B2B (10 endpoint ApiKey)** | Cần seed đối tác + API key hợp lệ, test scope |
| 5 | **Validation biên (BVA)** cho từng field | Khối lượng rất lớn; nên làm ở tầng Unit test validator |
| 6 | **Đa tenant (cross-tenant isolation)** | Case quan trọng về bảo mật: tenant A không được đọc dữ liệu tenant B |
| 7 | **RequireSuperAdmin — case 403 cho user thường** | Mã lỗi khác `PERMISSION_DENIED`, cần đọc lại `RequireSuperAdmin` |

> Mục 6.6 (cross-tenant) và 6.2 (migration) là 2 hạng mục **rủi ro cao nhất** còn bỏ ngỏ.
