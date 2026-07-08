# Kết quả THỰC THI UTC (UTE / 単体テスト実施結果) — API-assertive

> Chạy THẬT vào deploy `https://his.diab.com.vn` bằng curl, **assert HTTP + body thật**, đối chiếu DB/log backend. Ngày 2026-07-07. Tài khoản `admin@prodiab.local` (is_super_admin).
> Bổ trợ evidence UI: [ute-evidence.html](ute-evidence.html). Spec case: `utc-*.md`.
> **PHẦN A** = kết quả UTE lần đầu (phát hiện defect). **PHẦN B (mục 9)** = SAU KHI FIX + re-verify.

---
# 🟢 TÓM TẮT SAU KHI FIX (đọc trước) — xem chi tiết mục 9

| Đã fix (re-verify PASS) | Cách fix |
|---|---|
| **D1** ValidationBehavior (root-cause) | Thêm `ValidationBehavior<,>` vào MediatR pipeline → kích hoạt toàn bộ validator |
| **D3** LabPartner module 500 | Migration **9031** recreate bảng đúng schema → GET/POST 200/201 |
| **D4** maxLength → 500 + blood_type AB_POS → 500 | Validators MaximumLength → **400**; migration **9032** blood_type VARCHAR(10) → **201** |
| **D5** empty-string `""` lọt | Validator NotEmpty → **400** |
| **D6** User email sai → 500 | Validator EmailAddress chạy trước MimeKit → **400** |
| **D7** User role_codes `[]` → tạo user | Validator NotEmpty(min1) → **400** |
| **D8** Supplier duplicate → 500 | Precheck `(tenant,code)` → **409** `SUPPLIER_CODE_DUPLICATE` |
| **D9** Role regex/min-1-quyền | Validator kích hoạt → **400** |
| **D10** API Partner no-validator | Validator (rate/quota range, scope, email) → **400** |
| **D11** Service validator dead-code | Command-validator wiring (`SetValidator`) → vat/category/bhyt-âm **400** |
| **D13** Patient min-length/regex | Validator (min2, regex CMND/phone) → **400** |
| ⏸️ **D2** Tenant Guid↔INT | **HOÃN** (SuperAdmin-only; refactor entity PK int — làm riêng) |
| ⚪ D12(phần)/D14/D15 | Chưa xử lý (nhỏ): requires_prescription default, update-code im lặng, enable PENDING, text message |

Backend đã rebuild + redeploy healthy. Happy-path (valid create mọi master) vẫn **201** — không regression. **CMND vẫn mã hóa** (masked).

## 0. Lưu ý contract (quan trọng để đọc kết quả)
- **API dùng `snake_case`** (`JsonNamingPolicy.SnakeCaseLower`, `Program.cs:56`). Gửi camelCase → field bị bỏ qua **âm thầm** về default. (Field matrix trong UTC ghi id FE; payload API phải snake_case.)
- **Mandatory (null/thiếu field)** bị chặn **400** bởi ASP.NET NRT model-binding (`[ApiController]`) — KHÔNG phải FluentValidation. Nhưng **chuỗi rỗng `""` vẫn lọt**.

---

## 1. 🔴 ROOT CAUSE hệ thống (1 lỗi → hàng loạt defect)

**FluentValidation KHÔNG được nối vào MediatR pipeline → mọi validator là DEAD-CODE.**
- Bằng chứng code: chỉ có `AddValidatorsFromAssembly` (`DependencyInjection.cs:16`, `Program.cs:91`) đăng ký validator, **KHÔNG có `IPipelineBehavior`/`ValidationBehavior`** nào trong toàn `backend/src` (grep = 0).
- Bằng chứng runtime (Tenant): gửi 4 request field sai (`code="ab"`, `subdomain="PK"`, `email="abc@"`, `quota=1001`) kèm giá trị trùng → **tất cả vào tới handler** (trả 422 duplicate) thay vì 400. Nếu validator sống, phải 400 trước.
- **Hệ quả**: mọi rule `MaximumLength / Matches(regex) / InclusiveBetween / Must(enum) / min-items` ở BE **vô hiệu** — Service, Tenant, Role-create, API Partner… Chỉ FE (zod) chặn; gọi API thẳng bỏ hết.
- **Fix 1 điểm**: thêm `ValidationBehavior<TRequest,TResponse> : IPipelineBehavior` + `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))` → kích hoạt lại **toàn bộ** validator sẵn có.

---

## 2. 🔴 2 MODULE HỎNG TOÀN BỘ TRÊN PRODUCTION (chặn 100% CRUD)

| Module | Triệu chứng | Root cause (log backend) | Mức |
|---|---|---|---|
| **Tenant** (`/api/v1/tenants`) | GET list trả **mọi id = `00000000-...`**; POST/PUT/DELETE → **500** | `Unknown column 'status' in where clause` + `DbUpdateException`; PK `id INT AUTO_INCREMENT` vs entity gán `Guid.NewGuid()` (defect#1) | **Chặn** |
| **Lab Partners** (`/api/v1/lab-partners`) | **Mọi thao tác 500** kể cả GET list | `Unknown column 'api_key_encrypted'` / `'bhyt_token_encrypted'` — bảng deploy `diab_his_int_lab_partners` lệch schema EF (defect#5, sai migration apply) | **Chặn** |

> Cần migration vá schema deploy (giống đợt 9026-9030 trước) + sửa mapping PK Tenant. **Không tạo/sửa/xóa được tenant & đối tác CLS qua API cho tới khi vá.**

---

## 3. Defect XÁC NHẬN (có bằng chứng HTTP/log thật) — xếp theo mức

| # | Mức | Defect | Bằng chứng thật |
|---|---|---|---|
| D1 | **Cao** | ValidationBehavior thiếu → validator dead-code (mục 1) | Tenant isolation test + grep code |
| D2 | **Cao** | Tenant module hỏng (PK Guid/INT + cột `status`) | GET id=zero-guid; POST 500 |
| D3 | **Cao** | LabPartner module hỏng (cột `api_key_encrypted`) | Mọi op 500 + log |
| D4 | **Cao** | maxLength không chặn → **HTTP 500** "Data too long" | Supplier/Drug/Service code 60 ký tự → 500; Patient `blood_type=AB_POS` → 500 (`Data too long for 'blood_type'`) |
| D5 | **Cao** | Chuỗi rỗng `""` lọt qua "mandatory" | Drug POST `{code:"",name_vi:"",unit:"",form:""}` → **201** tạo record rỗng |
| D6 | **Cao** | User email sai → **500** (MimeKit) thay vì 400 | `email="abc@"` → `INTERNAL_ERROR` 500 + `MimeKit.ParseException` |
| D7 | **Cao** | User `role_codes:[]` → **201** tạo user không vai trò | body trả user_id |
| D8 | **Cao** | Supplier duplicate code → **500** (không precheck) | POST 2 lần → 500 (khác Drug/Service có 409) |
| D9 | TB | Role: regex code & min-1-quyền không enforce | `code="abc"`, `code="AB-CD"`, `permission_codes:[]` đều **201** |
| D10 | TB | API Partner: không validator (rate=0, quota=99tr, scope lạ, email sai, name="") | tất cả **201** |
| D11 | TB | Service: `bhyt_max_amount` âm, `category="XYZ"`, `vat_rate=7` đều lọt | **201** lưu giá trị lạ |
| D12 | TB | Drug: giá âm lọt; `requires_prescription` default lệch (API=false vs UI=true); Update **im lặng bỏ qua** đổi code | 201 price=-5; GET=false; PUT code không đổi |
| D13 | TB | Patient: min-length/regex CMND/phone/email không enforce | `full_name="A"`, `id_number="1234"` → 201 |
| D14 | TB | User: enable user PENDING → ACTIVE, bỏ qua đặt mật khẩu | POST /enable → status ACTIVE (PasswordHash rỗng) |
| D15 | Thấp | Role: message sửa role SYSTEM ghi nhầm "không thể **xóa**" (đúng ra "sửa") | body 403 |

---

## 4. ✅ Đã kiểm chứng ĐÚNG (PASS — chất lượng tốt)

| Hạng mục | Kết quả thật |
|---|---|
| **Mã hóa CMND/BHYT (AES)** | Patient `id_number=012345678901` → API trả `01********01` (masked) ở POST/GET/PUT; **không lộ plaintext**, không có cột `*_enc` trong JSON ✓ |
| **Soft-delete** | Mọi master: DELETE → 204, GET lại → 404 (`*_NOT_FOUND` message VN) ✓ |
| **Precheck trùng mã** | Drug `409 DRUG_CODE_EXISTS`, Service `409 SERVICE_CODE_EXISTS`, Role `422 ROLE_CODE_TAKEN`, Tenant `422 TENANT_CODE_TAKEN`/`SUBDOMAIN_TAKEN` ✓ (message VN) |
| **RBAC bảo vệ role hệ thống** | PUT/DELETE role SYSTEM → **403 ROLE_SYSTEM_PROTECTED** ✓ |
| **API key an toàn** | Trả `api_key_plain` 1 lần lúc tạo; GET chỉ `api_key_masked` (`pdh_live_****XXXX`); regenerate đổi key giữ scope ✓ |
| **status ép server-side** | API Partner ép `ACTIVE` (bỏ status client) ✓ |
| **Mandatory (null/thiếu)** | Chặn 400 "field is required" qua NRT binding ✓ (Supplier/Drug/Service/Patient) |
| **Code tự sinh** | Patient `BNT01000009` (BNT+tenant+seq) duy nhất, tăng dần ✓ |
| **Update + soft-delete round-trip** | PUT đổi field → 200 + GET xác nhận; updated_at đổi ✓ |

---

## 5. Bảng 判定 theo màn hình (tổng hợp)

| Màn hình | Run | OK | NG(defect) | 保留 | Ghi chú nổi bật |
|---|---|---|---|---|---|
| Nhà cung cấp | 10 | 7 | 2 | 1 | maxLen→500, dup→500; tax_code/contact_person **OK** (đã đính chính snake_case) |
| Danh mục thuốc | 14 | 8 | 5 | 1 | "" lọt, maxLen→500, giá âm, update code, default lệch |
| Dịch vụ | 14 | 8 | 5 | 1 | validator dead-code chứng minh (vat=7, category=XYZ, bhyt âm) |
| Vai trò | 11 | 6 | 4 | 1 | regex/min1 không enforce; SYSTEM protected OK |
| API Partner | 10 | 6 | 2 | 2 | no-validator; key masking OK |
| Người dùng | 11 | 6 | 3 | 2 | email→500, role rỗng→201, enable bỏ mật khẩu |
| Bệnh nhân | 12 | 8 | 3 | 1 | **CMND mã hóa OK**; blood_type→500; min/regex không enforce |
| Lab Partners | 9 | 1 | 6 | 2 | **module 500 toàn bộ** |
| Phòng khám (Tenant) | 8 | 3 | 4 | 1 | **module hỏng** (Guid/INT + cột status) |
| **TỔNG** | **99** | **53** | **34** | **12** | Pass ~54%; 34 defect thật; 12 保留 (thiếu token low-priv/tenant-2/invite) |

> Các case UI-level (load/form/validation FE) đã có evidence ảnh: [ute-evidence.html](ute-evidence.html) (49 ảnh, 8 màn × 6 step).

---

## 6. 保留 (chưa test được — cần chuẩn bị môi trường)
- **Permission-negative (403)**: cần tài khoản **thiếu quyền** (hiện chỉ có admin/super-admin). → tạo user LETAN + accept-invite + đăng nhập để có token low-priv.
- **Cách ly tenant (tenant isolation)**: cần **tenant thứ 2** + user của tenant đó.
- **Accept-invite + password policy (≥12 ký tự)**: cần token invite thật (API không lộ token, chỉ trả `invite_expires_at`).

---

## 7. Khuyến nghị fix (ưu tiên)
1. **[Chặn] Vá schema deploy** cho Tenant (`status`, PK) + LabPartner (`api_key_encrypted`, `bhyt_token_encrypted`) — migration mới; sửa mapping PK Tenant (INT vs Guid).
2. **[Cao] Thêm `ValidationBehavior` vào MediatR** → kích hoạt toàn bộ validator sẵn có (fix 1 điểm, sửa D9-D11 + phần lớn D4).
3. **[Cao] Thêm validator + maxLength** cho các DTO chưa có (Supplier/Drug/Patient/User/APIPartner): NotEmpty (chặn `""`), MaximumLength khớp DB, regex phone/CMND, range số ≥0, min-items role/scope.
4. **[Cao] Precheck trùng mã** cho Supplier (trả 409 thay vì 500); bọc lỗi email (MimeKit) → 400.
5. **[TB] blood_type**: nới cột DB VARCHAR(5)→(10) hoặc map enum ngắn; Drug update code nhất quán; User enable không bỏ qua đặt mật khẩu.

---

## 9. 🟢 PHẦN B — SAU KHI FIX + RE-VERIFY (2026-07-07)

Sau khi vá, chạy lại probe API-assertive THẬT trên deploy. Bảng before/after (HTTP thật):

| Case | Trước fix | Sau fix | Kỳ vọng | Kết quả |
|---|---|---|---|---|
| SUP maxLength code 60 | 500 | **400** | 400 | ✅ |
| SUP duplicate code | 500 | **409** `SUPPLIER_CODE_DUPLICATE` | 409 | ✅ |
| DRUG empty-string `""` | 201 | **400** | 400 | ✅ |
| DRUG maxLength code 60 | 500 | **400** | 400 | ✅ |
| DRUG price = -5 | 201 | **400** | 400 | ✅ |
| SVC vat_rate = 7 | 201 | **400** | 400 | ✅ |
| SVC category = "XYZ" | 201 | **400** | 400 | ✅ |
| SVC bhyt_max_amount < 0 | 201 | **400** | 400 | ✅ |
| APIP rate=0 / quota=99tr | 201 | **400** | 400 | ✅ |
| APIP name = "" | 201 | **400** | 400 | ✅ |
| PAT full_name = "A" | 201 | **400** | 400 | ✅ |
| PAT id_number = "1234" | 201 | **400** | 400 | ✅ |
| USER email = "abc@" | 500 | **400** | 400 | ✅ |
| USER role_codes = [] | 201 | **400** | 400 | ✅ |
| ROLE code = "abc" (regex) | 201 | **400** | 400 | ✅ |
| **LabPartner** GET list / create | 500 / 500 | **200 / 201** | 200/201 | ✅ |
| **Patient** blood_type AB_POS | 500 | **201** (lưu đúng) | 201 | ✅ |

**Happy-path (không regression):** valid create Supplier/Drug(+status)/Service(vat=8,bhyt=5000)/Patient(AB_POS+CMND)/API Partner/Role đều **201**; CMND vẫn trả masked `01********01`.

### Fix đã áp dụng
**Code (backend, rebuild+redeploy):**
- `ValidationBehavior<TRequest,TResponse>` (MediatR pipeline) + đăng ký DI → kích hoạt mọi FluentValidation.
- `ConflictException` + nhánh 409 trong `ErrorHandlingMiddleware`.
- Validators mới/hoàn thiện: Drug, API Partner, Patient (Create+Update, regex CMND/phone), Supplier (+precheck 409), Service (command-validator wiring + bhyt≥0), User invite (full_name min2).
- Program.cs: bỏ đăng ký validator trùng.

**DB (migration, idempotent):**
- `9031_fix_lab_partners_schema` — recreate `diab_his_int_lab_partners` đúng schema (id CHAR(36), api_key_encrypted…).
- `9032_pat_blood_type_widen` — `blood_type` VARCHAR(5)→(10).

### Còn lại (follow-up)
- **D2 Tenant** (SuperAdmin-only): entity kế thừa `BaseEntity` (Id Guid) nhưng DB `id INT` (khớp `tenant_id` INT toàn hệ thống) → cần map Tenant.Id = int + route `{id:int}` + bỏ `Guid.NewGuid()`. Refactor riêng.
- Nhỏ: Drug `requires_prescription` default lệch, Update code im lặng bỏ qua; User enable PENDING→ACTIVE bỏ đặt mật khẩu; text message D15.
- 保留 (12 case): permission-negative / tenant isolation / accept-invite — cần token low-priv + tenant-2 + token invite.

## 8. Ghi chú dữ liệu (trung thực)
- Toàn bộ record test tạo trong UTE **đã được soft-delete** (cleanup 204). Đã **rà DB xác nhận**: 4 bản ghi bệnh nhân bị xóa mềm đều là data test phiên này (`BNT01000009`–`000012`: "Nguyen Van Khoe", "Tran Thi Mai", "A", "Test C04") — **KHÔNG có bản ghi thật/cũ nào bị ảnh hưởng** (cảnh báo "xóa id cũ" của subagent là nhầm). Mọi soft-delete đều khôi phục được bằng cách bỏ `deleted_at`.
- Không tạo được tenant/lab-partner nào (mọi POST 500) → không phát sinh rác ở 2 module đó.
