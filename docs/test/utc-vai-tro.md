# 単体テスト仕様書 (UTC) — Màn hình **Vai trò & Quyền hạn** (Role / RBAC)

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `roles/page.tsx` + `RoleForm.tsx` + `PermissionMatrix.tsx` · BE `CreateRoleCommand.cs`/`UpdateRoleCommand.cs`/`DeleteRoleCommand.cs` · DB `9001_create_sec_all.sql`.

| Mục | Nội dung |
|---|---|
| 機能ID | `ROLE-CRUD-001` |
| Màn hình | Quản trị → Vai trò & Quyền hạn |
| Route FE | `/admin/roles` |
| API base | `/api/v1/roles` (định danh theo **`{code}`**, không phải id) |
| Bảng DB | `diab_his_sec_roles` (PK `id` CHAR(36), UNIQUE `code`) + nối `diab_his_sec_role_permissions` (role_id, permission_id) |
| Permission (API) | Xem `role.read` · Ghi `role.write` |
| ⚠️ Permission (nút FE) | Nút "Tạo vai trò mới" gate `admin.role_manage` — **khác** permission API |
| Đặc thù | Role hệ thống (`role_type=SYSTEM`) **không sửa/xóa** (403 `ROLE_SYSTEM_PROTECTED`); role tạo mới luôn `CUSTOM` |

## 1. Field matrix (3 tầng)

| Field (FE id) | Nhãn | Control | FE rule | BE validator (Create) | DB | GAP |
|---|---|---|---|---|---|---|
| role-code | Mã vai trò | Input text (chỉ khi Tạo) | regex `^[A-Z][A-Z0-9_]{2,30}$` | `NotEmpty().Matches(^[A-Z][A-Z0-9_]{2,30}$)` | `code` VARCHAR(50) NOT NULL UNIQUE | text FE ghi "3-30" nhưng regex cho 3-31 |
| role-name | Tên vai trò | Input text | min2 | `NotEmpty()` (Update **ko** validator) | `name` VARCHAR(100) NOT NULL | Update chỉ FE chặn |
| role-desc | Mô tả | Input text | optional | — | `description` VARCHAR(500) NULL | |
| perm-{code} | Ma trận quyền | Checkbox nhóm theo resource | `array.min(1)` | `PermissionCodes NotEmpty()`; mỗi code phải tồn tại | bảng nối `role_permissions` | Không maxLength field nào |

**Bắt buộc (Create):** `role-code`, `role-name`, ≥1 permission. `role-desc` optional. **Update:** mọi field optional (chỉ set field gửi lên).

## 2. Test cases

> **Điều kiện thực thi:** staging thật `https://his.diab.vn` · ngày **23/08/2026** · build backend deploy `2026-08-23 05:41 UTC`, frontend `05:10 UTC` · tài khoản chính `admin@prodiab.local` (role SYSTEM `admin`, đủ `role.read`/`role.write`/`admin.role_manage`) · tài khoản phụ để test RBAC: `bacsi1@prodiab.local` (role `bac_si`, không có `role.*`) và 1 user tạm gắn role CUSTOM chỉ có `admin.role_manage` + `patient.read`. Toàn bộ dữ liệu test đã được xoá sạch sau khi chạy.
>
> **Quy ước chấm 判定 (phiên này):** `OK` = hành vi thực tế đúng spec nghiệp vụ mong muốn · `NG` = còn defect (kể cả defect đã được mô tả sẵn ở cột 期待結果 bằng ⚠️) · `保留` = case thao tác trên UI mà phiên test này không có browser thật; bằng chứng gián tiếp (API / code / build đã deploy) ghi ở cột 備考.

### A — Load ban đầu
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| A01 | List | Hiển thị | Mở `/admin/roles` | Bảng vai trò; cột Mã/Tên/Loại(SYSTEM/CUSTOM)/số quyền | 保留 | Cần browser thật. Tương đương API đã verify: `GET /api/v1/roles` → **200**, trả đủ `code`/`name`/`role_type`/`permission_codes`/`description` cho 6 role SYSTEM (admin, bac_si, duoc_si, ke_toan, ky_thuat_vien, le_tan). Thời gian 135–151 ms |
| A02 | Nút Tạo | Quyền | User có `admin.role_manage` | Nút "Tạo vai trò mới" hiển thị | 保留 | Cần browser thật. ⚠️ Đã chứng minh lệch quyền ở tầng API: user chỉ có `admin.role_manage` (không có `role.read`/`role.write`) gọi `GET/POST/PUT/DELETE /api/v1/roles` đều **403 PERMISSION_DENIED** → **Defect#1 VẪN CÒN NG**: màn hình hiện nút nhưng cả bảng danh sách lẫn submit đều lỗi 403 |
| A03 | Row SYSTEM | Bảo vệ | Xem 1 role SYSTEM | Chỉ nút "Sửa quyền" (xem ma trận); ẩn Sửa/Xóa | 保留 | Cần browser thật. Code gate `page.tsx:81` dùng `row.role_type === "CUSTOM"`; tầng API cũng chặn độc lập (D02/D03 = 403 `ROLE_SYSTEM_PROTECTED`) |
| A04 | Form Tạo | Control | Bấm Tạo | code/name/desc rỗng; ma trận quyền nhóm theo resource, tất cả bỏ chọn | 保留 | Cần browser thật. Route `/admin/roles/new` trên staging trả **HTTP 200** (78 KB); `GET /api/v1/permissions` → 200 (~170 ms) cấp đủ option cho ma trận |

### B — Nhập/hiển thị
| No | 中項目 | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| B01 | Tick quyền | State | chọn vài permission | State `permission_codes` cập nhật; nhóm theo resource | 保留 | Cần browser thật (state React). Kết quả cuối đã verify gián tiếp qua E01/E02: bộ quyền gửi lên được lưu và replace đúng |
| B02 | Sửa role CUSTOM | Load data | mở Sửa | name/desc/quyền đã lưu load đúng; **code ẩn** (ko sửa) | 保留 | Cần browser thật. `GET /api/v1/roles/{code}` trả đúng name/description/permission_codes (F02 = OK); `RoleForm.tsx:81` chỉ render field `role-code` khi `!isEdit` |

### C — Validate
| No | 中項目 | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| C01 | code bắt buộc | Required | rỗng | Chặn | OK | `POST /roles` → **400** `VALIDATION_ERROR`; `details.Code` chứa 2 message (NotEmpty + regex "Mã vai trò phải từ 3-31 ký tự…") |
| C02 | code sai regex | Format | `abc` (chữ thường) | Chặn (phải HOA đầu) | OK | **400** `VALIDATION_ERROR`. Regex BE `^[A-Z][A-Z0-9_]{2,30}$` (`CreateRoleCommand.cs:22`) **trùng khít** regex FE zod (`RoleForm.tsx:18`) → FE/BE đã nhất quán chữ HOA |
| C03 | code có ký tự lạ | Format | `AB-CD` | Chặn (chỉ A-Z0-9_) | OK | **400** `VALIDATION_ERROR` |
| C04 | code biên | 境界値 | `AB`(2) · `QA3`(3) · 31 ký tự · 32 ký tự | 2→NG, 3→OK, 31→OK, 32→NG | OK | Thực tế: 2 → **400** · 3 → **201** · 31 (`QATBCDEFGHIJKLMNOPQRSTUVWXYZ123`) → **201** · 32 → **400**. Biên khớp regex; message FE/BE đều ghi "3-31" (Defect#4 cũ đã sửa) |
| C05 | name min2 | Required / độ dài | `A` | Chặn | **NG** | FE zod `min(2)` có chặn (chưa click bằng browser) nhưng **BE Create chỉ `NotEmpty()`** → `POST` với `name` 1 ký tự trả **201**, DB lưu `name = A`. Mở rộng phạm vi **Defect#2** sang cả Create |
| C06 | không chọn quyền | Required | `permission_codes` rỗng | Chặn (min1) | OK | **400** `VALIDATION_ERROR`, message tiếng Việt "Phải chọn ít nhất một quyền" |
| C07 | **API Update bỏ FE** | Bypass FE | `PUT /roles/QA_E01` với `name` 1 ký tự | **Kỳ vọng chặn**; thực tế lưu (Update ko validator) | **NG** | ⚠️ **Defect#2 VẪN CÒN**. Thực tế **200 OK**; GET lại trả `name = A`; DB `diab_his_sec_roles.name = A`. `UpdateRoleCommand.cs` không có `AbstractValidator` |
| C08 | permission ko tồn tại | Ràng buộc FK | gửi kèm 1 code lạ `khong.ton.tai` | 4xx `PERMISSION_NOT_FOUND`, cả thao tác fail | OK | **422** `PERMISSION_NOT_FOUND` "Một hoặc nhiều quyền không tồn tại". Kiểm DB: **không** có row role tương ứng → rollback toàn bộ, đúng all-or-nothing |

### D/E — Business + DB
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| D01 | Mã trùng | Unique | Tạo lại role đã có code `QA_E01` | `ROLE_CODE_TAKEN` | OK | **422** `ROLE_CODE_TAKEN` "Mã vai trò đã tồn tại" |
| **D01b** | **Mã reserved SUPER_ADMIN** | **セキュリティ — leo thang đặc quyền** | `POST /roles` với `code = SUPER_ADMIN`, name hợp lệ, 1 permission | **422 `ROLE_CODE_RESERVED`** — không cho tenant tự tạo role CUSTOM giả mạo role hệ thống | OK | **Case MỚI (lỗ hổng bảo mật vừa fix).** Thực tế **422** `ROLE_CODE_RESERVED` "Mã vai trò này được dành riêng cho vai trò hệ thống, không thể sử dụng để tạo vai trò tùy chỉnh" (`CreateRoleCommand.cs:45` + `ReservedRoleCodes.cs`). Đã verify thêm **lớp 2 defense-in-depth**: hồi sinh tạm 1 role CUSTOM `code=SUPER_ADMIN` trong DB rồi gán cho user tạm → login lại, JWT **KHÔNG** có claim `is_super_admin`; gọi `/roles` và `/tenants` đều **403**. Đã revert DB về nguyên trạng |
| **D01c** | **Mã reserved ADMIN** | **セキュリティ — leo thang đặc quyền** | `POST /roles` với `code = ADMIN` | **422 `ROLE_CODE_RESERVED`** | OK | **Case MỚI.** Thực tế **422** `ROLE_CODE_RESERVED`. Biến thể `code = admin` (chữ thường) bị regex chặn trước → **400** `VALIDATION_ERROR`; `ReservedRoleCodes.IsReserved()` so sánh case-insensitive nên không có đường vòng. Near-miss `SUPER_ADMIN2` (không nằm trong danh sách reserved) → **201** đúng thiết kế, và JWT vẫn không cấp `is_super_admin` |
| D02 | Sửa role SYSTEM | Bảo vệ | `PUT /roles/bac_si` và `PUT /roles/admin` | **403** `ROLE_SYSTEM_PROTECTED` | OK | Cả 2 → **403** `ROLE_SYSTEM_PROTECTED`. ⚠️ Message trả về là "Không thể **xóa** vai trò hệ thống" dù thao tác là sửa → sai động từ (**Defect#5** mới, mức Thấp) |
| D03 | Xóa role SYSTEM | Bảo vệ | `DELETE /roles/bac_si` và `DELETE /roles/admin` | **403** | OK | Cả 2 → **403** `ROLE_SYSTEM_PROTECTED`; DB không thay đổi |
| D04 | Role mới = CUSTOM | Business | Tạo role | DB `role_type=CUSTOM`, `tenant_id`=JWT, `is_active=1` | OK | DB `diab_his_sec_roles`: `role_type=CUSTOM`, `tenant_id=1` (khớp JWT), `is_active=1`, `deleted_at=NULL`. Không có đường tạo role SYSTEM qua API |
| E01 | Insert role + quyền | Ghi DB | Tạo hợp lệ với 3 quyền | 1 row `sec_roles` + 3 row `role_permissions`; HTTP 201 | OK | **201**. DB: 1 row `diab_his_sec_roles` (`QA_E01`) + `n_perm = 3` trong `diab_his_sec_role_permissions`. Tiếng Việt có dấu và emoji lưu/đọc đúng (kiểm bằng `--default-character-set=utf8mb4`) |
| E02 | Update = replace quyền | Ghi DB | PUT đổi bộ quyền 3 → 2 | Xóa hết quyền cũ, thêm mới đúng | OK | PUT `billing.read` + `report.read` → DB còn **đúng 2** row; 3 quyền cũ (`patient.read`, `patient.create`, `encounter.read`) bị xoá sạch |

### F — Load sau insert
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| F01 | List | Re-display | reload `GET /roles` | Role mới xuất hiện, đúng số quyền | OK | **200**; role CUSTOM mới nằm trước nhóm SYSTEM, `permission_codes` đúng số lượng |
| F02 | Round-trip | Re-display | `GET /roles/{code}` | Bộ quyền tick đúng như đã lưu | OK | **200**; trả đúng name/description/permission_codes đã lưu, giữ nguyên dấu tiếng Việt và emoji |

### G/H/I — Xóa · Quyền · Tenant
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| H01 | Xóa mềm CUSTOM | Soft delete | `DELETE /roles/QA3` | `deleted_at` + `deleted_by` set, `is_active=0`; 204 | OK | **204**. DB: `deleted_at = 2026-08-23 06:00:36`, `deleted_by = a0000000-…-0001`, `is_active = 0`. Không xoá cứng |
| H02 | Ẩn sau xóa | Re-display | reload list và GET chi tiết | Ko hiển thị | OK | `GET /roles` không còn `QA3`; `GET /roles/QA3` → **404** `ROLE_NOT_FOUND` |
| I01 | Thiếu quyền API | Authz | GET/POST/PUT/DELETE khi thiếu `role.read`/`role.write` | 403 | OK | Cả 2 tài khoản (role `bac_si`; user chỉ có `admin.role_manage`) → **403** `PERMISSION_DENIED` "Bạn không có quyền thực hiện thao tác này" trên cả 4 verb. ⚠ Xác nhận **Defect#1**: user có `admin.role_manage` (FE hiện nút) vẫn bị 403 ở API |
| I02 | Cách ly tenant | Multi-tenant | Chèn tạm role CUSTOM `QA_T999` `tenant_id=999` vào DB rồi truy cập bằng JWT tenant 1 | Ko thấy / ko sửa | OK | `GET /roles` **không** liệt kê; `GET`/`PUT`/`DELETE /roles/QA_T999` đều **404** `ROLE_NOT_FOUND`; DB row nguyên vẹn (không bị sửa, không bị soft-delete). Role SYSTEM `tenant_id=NULL` dùng chung như thiết kế. Ghi chú: `POST` trùng code `QA_T999` trả `ROLE_CODE_TAKEN` do check unique dùng `IgnoreQueryFilters()` — đúng vì `code` UNIQUE toàn cục nhưng rò rỉ suy đoán mã vai trò tenant khác (**Defect#7**, Thấp). Đã xoá row tạm |

## 3. Defect candidates
> Cột **Trạng thái** cập nhật sau đợt re-test 23/08/2026 trên staging `https://his.diab.vn`. Giữ nguyên lịch sử defect cũ để truy vết.

| ID | Mức | Mô tả | Vị trí | Trạng thái (verify 23/08/2026) |
|---|---|---|---|---|
| #1 | **Cao** | Lệch permission FE↔API: nút gate `admin.role_manage` nhưng API cần `role.write`/`role.read`. User thấy nút vẫn bị 403; hoặc ngược lại. Các nút Sửa/Xóa/ma trận **không** gate FE | `page.tsx:118` vs `RolesController.cs` | ❌ **CÒN NG** — reproduce được: user chỉ có `admin.role_manage` → `GET/POST/PUT/DELETE /api/v1/roles` đều 403 (case A02, I01) |
| #2 | TB | Update không có validator BE → name 1 ký tự/rỗng qua API được (chỉ FE zod chặn) | `UpdateRoleCommand.cs` | ❌ **CÒN NG** — `PUT` với `name` 1 ký tự → 200, DB lưu nguyên (case C07). **Mở rộng:** Create cũng chỉ `NotEmpty()` nên POST name 1 ký tự → 201 (case C05) |
| #3 | TB | Không giới hạn maxLength FE/BE cho name(100)/description(500) → vượt chỉ lỗi ở DB | — | ⚠️ **CÒN** — phiên này không test trực tiếp; đối chiếu code vẫn không có `MaximumLength()` ở validator và không có `maxLength` ở `RoleForm.tsx`. Tham chiếu hành vi tương đương đo được ở `utc-emr-templates.md` C02/C05: vượt maxLen trả **HTTP 500** chứ không phải 400 |
| #4 | Thấp | Text lỗi FE ghi "3-30 ký tự" nhưng regex thực cho 3-31 (lệch 1) | `RoleForm.tsx:16` | ✅ **ĐÃ FIX — verify 23/08/2026**: message FE (`RoleForm.tsx:18`) và BE (`CreateRoleCommand.cs:23`) đều ghi "phải từ 3-31 ký tự", khớp regex `^[A-Z][A-Z0-9_]{2,30}$`; biên 2/3/31/32 đã test đúng (C04) |
| **#5** | Thấp | **(MỚI 23/08/2026)** `PUT /roles/{code}` lên role SYSTEM trả message sai động từ: "Không thể **xóa** vai trò hệ thống" trong khi thao tác là sửa | `UpdateRoleCommand.cs` nhánh `ROLE_SYSTEM_PROTECTED` | ❌ **NG mới** — case D02. Owner đề xuất: backend |
| **#6** | **Cao** | **(MỚI 23/08/2026)** **Không ghi audit log cho bất kỳ thao tác Create/Update/Delete vai trò nào.** Kiểm `diab_his_sec_audit_logs` trong cửa sổ 05:54–06:01 (đã tạo 6 role, sửa 3 lần, xoá 6 role) → **0 row** `resource_type='ROLE'`. Vi phạm CLAUDE.md mục "Audit log mọi thao tác"; RBAC là dữ liệu bảo mật trọng yếu, mất dấu vết ai cấp quyền cho ai | `CreateRoleCommand.cs` / `UpdateRoleCommand.cs` / `DeleteRoleCommand.cs` — không inject `IAuditService` | ❌ **NG mới**. Đối chiếu: EMR Template đã ghi audit đầy đủ (CREATE/UPDATE/DELETE + UPDATE_DENIED/DELETE_DENIED). Owner đề xuất: backend |
| **#7** | Thấp | **(MỚI 23/08/2026)** `POST /roles` với `code` đang thuộc tenant khác trả `ROLE_CODE_TAKEN` (check unique dùng `IgnoreQueryFilters()`) → tenant A suy đoán được mã vai trò của tenant B | `CreateRoleCommand.cs:50` | ❌ **NG mới** — chấp nhận được về mặt kỹ thuật vì `code` là UNIQUE toàn cục; cần quyết định thiết kế: đổi UNIQUE thành `(tenant_id, code)` hoặc chấp nhận rủi ro |
| **#8** | Thấp | **(MỚI 23/08/2026)** `UpdateRoleCommandHandler` không set `updated_by` → DB `diab_his_sec_roles.updated_by = NULL` sau khi PUT thành công (trong khi Delete có set `deleted_by`) | `UpdateRoleCommand.cs` | ❌ **NG mới** — verify trực tiếp DB. Owner đề xuất: backend |

**Đã xác nhận ĐÓNG về mặt bảo mật (không còn defect):** chặn tạo role CUSTOM trùng mã reserved `ADMIN`/`SUPER_ADMIN` → **422 `ROLE_CODE_RESERVED`**, **và** `JwtService` không cấp claim `is_super_admin` cho role CUSTOM dù trùng mã (defense-in-depth 2 lớp) — xem case D01b/D01c.

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/B | 6 | 0 | 0 | 6 |
| C Validate | 8 | 6 | 2 | 0 |
| D/E (gồm 2 case mới D01b, D01c) | 8 | 8 | 0 | 0 |
| F | 2 | 2 | 0 | 0 |
| H/I | 4 | 4 | 0 | 0 |
| **TỔNG** | **28** | **20** | **2** | **6** |

- **Tỉ lệ OK trên số case thực thi được (22 case):** 20/22 = **90,9 %** — chưa đạt ngưỡng ≥ 95 % của `utc-00-quy-uoc-chuan-nhat.md`.
- **NG:** C05, C07 — cùng gốc **Defect#2** (thiếu validator BE cho `name` ở cả Create và Update).
- **保留:** A01–A04, B01, B02 — toàn bộ là case thao tác UI; phiên test này chỉ chạy được tầng API (không có browser thật). Đã ghi bằng chứng gián tiếp ở cột 備考.
- **Hiệu năng:** `GET /roles` 135–151 ms, `GET /permissions` 157–205 ms (đo 5 lần, đã gồm ~45 ms TLS/kết nối từ máy test) → đạt yêu cầu < 500 ms cho query list.
- **Dọn dẹp:** 6 role CUSTOM, 1 user tạm và 1 row cross-tenant tạo trong phiên test đã bị xoá; staging trở lại đúng trạng thái trước khi test (chỉ còn 6 role SYSTEM active).
