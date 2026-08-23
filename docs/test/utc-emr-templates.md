# 単体テスト仕様書 (UTC) — Màn hình **Mẫu bệnh án / EMR Templates**

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `EmrTemplatesPageClient.tsx` · BE `EmrHandlers.cs`/`EmrCommands.cs` · DB `0026_create_emr_templates.sql`.

| Mục | Nội dung |
|---|---|
| 機能ID | `EMRT-CRUD-001` |
| Màn hình | Quản trị → Mẫu bệnh án |
| Route FE | `/admin/emr-templates` |
| API base | `/api/v1/emr-templates` |
| Bảng DB | `diab_his_cli_emr_templates` (PK `id` CHAR(36)) · **KHÔNG có UNIQUE** (name trùng thoải mái) |
| Permission | Xem `emr_template.read` · Ghi `emr_template.write` |
| ⚠️ Tình trạng | ~~**FE chưa có form Tạo/Sửa** (nút "Tạo mẫu mới" không có onClick; không có editor); **BE có CRUD** nhưng **không validator**.~~ **Cập nhật 23/08/2026:** FE **ĐÃ CÓ** form Tạo/Sửa đầy đủ (Tiptap editor, xác nhận bằng browser thật — xem Defect#1); BE **ĐÃ CÓ** validator `EmrTemplateValidators.cs` trong code (chưa re-test trên staging). Phần lớn case của phiên test trước vẫn thực hiện ở **tầng API**. |
| 📄 PRD | `docs/prd/emr-template-prd.md` — nguồn chuẩn về nghiệp vụ, AC-01…AC-05 và ma trận quyền |

## 1. Field matrix (3 tầng)

| Field | Nhãn | Control FE | FE rule | BE validator | DB: type/null/default | GAP |
|---|---|---|---|---|---|---|
| name | Tên mẫu | ❌ **không có control** | ❌ | ❌ ghi thẳng | VARCHAR(200) NOT NULL, **ko unique** | FE thiếu form; BE ko chặn rỗng/maxLen |
| content_json | Nội dung (Tiptap JSON) | ❌ không có editor | ❌ | ❌ `Serialize(object)` nhận cả null | LONGTEXT NOT NULL | `null` → lưu chuỗi `"null"` |
| speciality | Khoa/loại áp dụng | ❌ không có select | ❌ | ❌ ko enum-check | VARCHAR(50) NOT NULL DEF 'GENERAL' | enum chỉ trong COMMENT |
| is_system | Hệ thống/tùy chỉnh | Badge read-only + filter | ko sửa | Create ép false | TINYINT(1) NOT NULL DEF 0 | |
| tenant_id | — | — | — | gán JWT (null=system) | INT NULL | |

**Bắt buộc (shape DTO):** `name`, `content_json`, `speciality` — nhưng **không tầng nào** enforce rỗng/định dạng.

## 2. Test cases

> **Điều kiện thực thi:** staging thật `https://his.diab.vn` · ngày **23/08/2026** · build backend deploy `2026-08-23 05:41 UTC`, frontend `05:10 UTC` · tài khoản chính `admin@prodiab.local` (có `emr_template.read`/`emr_template.write`) · tài khoản phụ tạm không có `emr_template.*` để test RBAC. Toàn bộ dữ liệu test đã được xoá sạch sau khi chạy.
>
> **Quy ước chấm 判定 (phiên này):** `OK` = hành vi thực tế đúng spec nghiệp vụ mong muốn · `NG` = còn defect (kể cả defect đã được mô tả sẵn ở cột 期待結果 bằng ⚠️) · `保留` = case thao tác trên UI mà phiên test này không có browser thật; bằng chứng gián tiếp (API / code / build đã deploy) ghi ở cột 備考.

### A — Load ban đầu
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| A01 | List | Hiển thị | Mở `/admin/emr-templates` | Chia "Mẫu hệ thống" / "Mẫu tùy chỉnh"; badge is_system | 保留 | Cần browser thật. Route trả **HTTP 200**. API `GET /api/v1/emr-templates` → **200**, trả `is_system` cho từng mẫu, sort `IsSystem DESC, Name ASC` đúng thứ tự để FE tách 2 mục (`EmrTemplatesPageClient.tsx:20-21`). Thời gian 133–146 ms |
| A02 | Nút Tạo | UI gap | Bấm "Tạo mẫu mới" | ⚠️ **Kỳ vọng:** mở form. Trước đây: **không có gì xảy ra** (nút chết) | 保留 | Cần browser thật để click. **Bằng chứng Defect#1 nhiều khả năng ĐÃ FIX:** (1) `EmrTemplatesPageClient.tsx:23-25` nay có `onClick={openCreate}` → `router.push("/admin/emr-templates/new")`; (2) đã có `EmrTemplateForm.tsx` + route `new/page.tsx` + `[id]/edit/page.tsx`; (3) staging: `GET /admin/emr-templates/new` → **HTTP 200** (78 KB); (4) build FE **đang chạy trong container** có chứa chuỗi `emr-templates/new` và nhãn form ("Tên mẫu", "Chuyên khoa áp dụng") trong `/app/.next/static/chunks` |
| A03 | Lọc | Filter | Lọc theo `speciality` / `is_system` | Danh sách lọc đúng | OK | Tầng API đúng 100 %: `?speciality=DIABETES` → chỉ 3 mẫu DIABETES · `?is_system=true` → 2 (đều `true`) · `?is_system=false` → 4 (đều `false`) · `?speciality=KHONGCO` → `{"data":[]}` 200. ⚠ Ghi chú FE: màn hình **chưa có control lọc theo speciality**, chỉ tách 2 mục theo `is_system` (**Defect#9** mới, mức Thấp) |

### C — Validate (chủ yếu tầng API, do FE thiếu form)
| No | 中項目 | 観点 | データ (API) | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| C01 | name rỗng | Required | POST `name` rỗng, `content_json` hợp lệ, `speciality=GENERAL` | **Kỳ vọng 400**; trước đây **201** (ko validator) | **NG** | ⚠️ **Defect#2 VẪN CÒN**. Thực tế **201 Created**; DB `diab_his_cli_emr_templates.name` lưu chuỗi rỗng; list trả về mẫu có `"name":""`. Không có `AbstractValidator` cho `EmrTemplateRequest` |
| C02 | name > 200 | 境界値 / maxLen | 201 ký tự | Kỳ vọng chặn; trước đây lỗi/truncate DB | **NG** | ⚠️ **Defect#7 VẪN CÒN và nặng hơn dự đoán**: trả **HTTP 500** `INTERNAL_ERROR` "Loi he thong, vui long thu lai sau" (MySQL `STRICT_TRANS_TABLES` ném lỗi truncate, không bị bắt) — lộ lỗi hệ thống thay vì 400 nghiệp vụ |
| C03 | content_json null | Required | `content_json: null` | **Kỳ vọng 400**; trước đây lưu chuỗi `"null"` | OK | ✅ **Defect#3 ĐÃ ĐƯỢC CHẶN — verify 23/08/2026**: trả **400** `The ContentJson field is required.` (cả khi gửi `null` lẫn khi bỏ hẳn field). Cơ chế chặn là **model binding NRT** của ASP.NET (`object ContentJson` non-nullable), không phải validator ứng dụng. ⚠ Response dùng ProblemDetails RFC9110 chứ không theo envelope chuẩn `{error:{code,message}}` (**Defect#8** mới, mức Thấp) |
| C04 | speciality lạ | Enum | `speciality = XYZ123` | Kỳ vọng chặn enum; trước đây lưu nguyên | **NG** | ⚠️ **Defect#4 VẪN CÒN**. Thực tế **201**; DB `speciality = XYZ123`. Enum vẫn chỉ nằm trong COMMENT của cột, không tầng nào check |
| C05 | speciality > 50 | 境界値 / maxLen | 51 ký tự | Kỳ vọng chặn; lỗi DB | **NG** | Thực tế **HTTP 500** `INTERNAL_ERROR` (giống C02). Không có `MaximumLength(50)` ở BE, không có enum select ở FE cho phép nhập tự do qua API |

### D/E — Business + DB
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| D01 | Tên trùng | Unique | Tạo 2 mẫu cùng `name` | ⚠️ **Cả 2 thành công** (ko unique) | **NG** | ⚠️ **Defect#5 VẪN CÒN**. Thực tế đúng như mô tả: cả 2 POST đều **201**, DB có 2 row cùng `name` khác `id`. Bảng `0026_create_emr_templates.sql` không có UNIQUE trên `(tenant_id, name)` |
| E01 | Insert | Ghi DB | POST hợp lệ | 1 row `diab_his_cli_emr_templates`; HTTP 201 | OK | **201**. DB đúng 1 row mới; `created_by = a0000000-…-0001`, `created_at` tự set. Audit log ghi `CREATE / EMR_TEMPLATE / INFO` kèm `details.name`, `details.tenantId` |
| E02 | is_system ép false | Business | POST | DB `is_system=0`; `tenant_id`=JWT | OK | DB `is_system = 0`, `tenant_id = 1` (khớp JWT), không nhận `tenant_id`/`is_system` từ client (`CreateEmrTemplateCommandHandler` ép cứng `IsSystem = false`) |
| E03 | content_json lưu đúng | Ghi DB | POST JSON Tiptap | DB `content_json` = JSON serialize đúng | OK | DB lưu JSON hợp lệ, round-trip qua `GET` trả lại đúng tiếng Việt có dấu ("Tiếng Việt có dấu — đái tháo đường"). Ghi chú: `System.Text.Json` escape non-ASCII thành `\uXXXX` khi lưu (đúng chuẩn JSON, chỉ tốn thêm dung lượng — không phải defect) |

### F/G/H/I — Load lại · Sửa/Xóa · Quyền · Tenant
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| F01 | List sau tạo | Re-display | reload `GET /emr-templates` | Mẫu mới ở "Mẫu tùy chỉnh" | OK | **200**; mẫu mới trả `is_system:false` và đứng sau nhóm system theo `IsSystem DESC, Name` → FE xếp vào mục "Mẫu tùy chỉnh". 133–146 ms |
| G01 | Update | Ghi DB | `PUT /emr-templates/{id}` mẫu tùy chỉnh | Cập nhật name/content/speciality | OK | **200**. DB: `name`, `speciality` (GENERAL→CARDIOLOGY) và `content_json` đều đổi đúng; `updated_by` được set; audit log ghi `UPDATE / EMR_TEMPLATE / INFO` |
| G02 | Sửa mẫu hệ thống | Bảo vệ | `PUT` mẫu `is_system=1` | ⚠️ **Kỳ vọng 422/403**; trước đây Update **KHÔNG chặn** (chỉ Delete chặn) | OK | ✅ **Defect#6 ĐÃ FIX — verify 23/08/2026 bằng request thật.** `PUT` lên cả 2 mẫu hệ thống (`aaaaaaaa-0001-…`, `aaaaaaaa-0002-…`) đều trả **422** `TEMPLATE_SYSTEM` "Không thể sửa mẫu bệnh án hệ thống". Kiểm DB: 2 mẫu hệ thống **nguyên vẹn** (`name`, `speciality`, `updated_at = 2026-08-23 04:05:11` không đổi). Audit log ghi **`UPDATE_DENIED` / severity `WARN`** / `details.reason = IS_SYSTEM` cho cả 2 lần |
| H01 | Xóa mẫu tùy chỉnh | Soft delete | `DELETE` | `deleted_at` set; 204 | OK | **204**. DB `deleted_at = 2026-08-23 06:04:00`, `deleted_by = a0000000-…-0001`, row **không** bị xoá cứng; list không còn hiển thị. DELETE lần 2 và DELETE id không tồn tại đều **404** `TEMPLATE_NOT_FOUND` |
| H02 | Xóa mẫu hệ thống | Bảo vệ | `DELETE` mẫu `is_system` | 422 `TEMPLATE_SYSTEM` | OK | **422** `TEMPLATE_SYSTEM` "Không thể xóa mẫu bệnh án hệ thống"; row nguyên vẹn. **Audit log đã ghi nhận** (kiểm bổ sung ngoài bảng gốc): `DELETE_DENIED` / `WARN` / `details.reason = IS_SYSTEM` / `resource_id = aaaaaaaa-0001-…` |
| I01 | Thiếu quyền | Authz | POST/GET khi không có `emr_template.write`/`read` | 403 | OK | User tạm (chỉ `admin.role_manage` + `patient.read`): `POST /emr-templates` → **403** `PERMISSION_DENIED`; `GET /emr-templates` → **403** `PERMISSION_DENIED` |
| I02 | Cách ly tenant | Multi-tenant | Chèn tạm 2 row vào DB: (a) `tenant_id=999, is_system=0` và (b) `tenant_id=NULL, is_system=0` (hàng mồ côi) rồi thao tác bằng JWT tenant 1 | Ko sửa/xóa (mẫu system `tenant_id` null dùng chung) | OK | **Ghi đã cách ly hoàn toàn:** `PUT`/`DELETE` lên (a) → **404** `TEMPLATE_NOT_FOUND` (Global Query Filter loại từ đầu); lên (b) → **404** `TEMPLATE_NOT_FOUND` nhờ nhánh defense-in-depth `isCrossTenantAttempt`, đồng thời **ghi audit `UPDATE_DENIED`/`DELETE_DENIED` với `cross_tenant_attempt = 1`, `details.reason = TENANT_MISMATCH`** — đúng yêu cầu CLAUDE.md. ⚠ Phát hiện thêm 2 điểm: (1) `GET /emr-templates` **có liệt kê** row mồ côi (b) cho tenant 1 → rò rỉ phía đọc (**Defect#10** mới); (2) thao tác lên row (a) tenant 999 **không** sinh audit log nào (bị filter loại trước khi tới nhánh audit) → không truy vết được (**Defect#11** mới). Đã xoá 2 row tạm |

## 3. Defect candidates
> Cột **Trạng thái** cập nhật sau đợt re-test 23/08/2026 trên staging `https://his.diab.vn`. Giữ nguyên lịch sử defect cũ để truy vết.

| ID | Mức | Mô tả | Vị trí | Trạng thái (verify 23/08/2026) |
|---|---|---|---|---|
| #1 | ~~**Cao**~~ **ĐÓNG** | ~~FE thiếu toàn bộ form Tạo/Sửa: nút "Tạo mẫu mới" không có `onClick`; không editor content; không select speciality → **không tạo/sửa được qua UI**~~ | `EmrTemplatesPageClient.tsx:27` | ✅ **ĐÃ CÓ FORM — nhận định cũ SAI/LỖI THỜI, cập nhật 23/08/2026.** Đã xác nhận **bằng browser thật**: form Tạo/Sửa đầy đủ tại `/admin/emr-templates/new` và `/admin/emr-templates/{id}/edit` (Tiptap editor + toolbar Đậm/Nghiêng/Tiêu đề/Danh sách/Bảng/Ảnh, ô "Tên mẫu", select "Chuyên khoa áp dụng"). `EmrTemplateForm.tsx` + `new/page.tsx` + `[id]/edit/page.tsx` đều tồn tại; `onClick={openCreate}` → `router.push("/admin/emr-templates/new")`. **Không tạo ticket dựng lại form.** Xem `docs/prd/emr-template-prd.md` §11.2 |
| #2 | **Cao** | BE không validator → name rỗng/quá dài, speciality rác đều lưu được | `EmrHandlers.cs:447+` | ❌ **CÒN NG** — `name` rỗng → 201 (C01); `speciality = XYZ123` → 201 (C04); `name` 201 ký tự và `speciality` 51 ký tự → **HTTP 500** (C02/C05). Vẫn chưa có `AbstractValidator<EmrTemplateRequest>` |
| #3 | TB | `content_json` kiểu `object` ko kiểm null → `Serialize(null)` lưu `"null"` vào cột NOT NULL | `EmrHandlers.cs:463` | ✅ **ĐÃ CHẶN — verify 23/08/2026**: `content_json: null` và bỏ hẳn field đều trả **400** "The ContentJson field is required." Lưu ý: chặn nhờ model binding NRT của ASP.NET, **không** phải validator ứng dụng → nếu sau này đổi DTO sang `object?` thì lỗ hổng quay lại. Khuyến nghị bổ sung validator tường minh |
| #4 | TB | speciality không enum-check ở cả 3 tầng (chỉ COMMENT) | — | ❌ **CÒN NG** — `speciality = XYZ123` lưu thành công (C04) |
| #5 | TB | Không UNIQUE trên `name` → mẫu trùng tên không bị chặn | `0026_create_emr_templates.sql` | ❌ **CÒN NG** — 2 mẫu cùng tên đều 201 (D01) |
| #6 | TB | Update không chặn sửa mẫu hệ thống (chỉ Delete chặn) → PUT có thể sửa mẫu system | `EmrHandlers.cs:486` | ✅ **ĐÃ FIX — verify 23/08/2026 bằng request thật**: `PUT` mẫu `is_system=1` → **422 `TEMPLATE_SYSTEM`**, dữ liệu mẫu hệ thống nguyên vẹn, có audit `UPDATE_DENIED`/`WARN`. Đã bổ sung thêm lớp defense-in-depth chặn `tenant_id` NULL/khác tenant (case I02) |
| #7 | Thấp | maxLen name(200)/speciality(50) không áp ở FE/BE | — | ❌ **CÒN NG và nâng mức lên TB** — vượt maxLen trả **HTTP 500** `INTERNAL_ERROR` chứ không phải 400 nghiệp vụ (C02/C05). Lộ lỗi hệ thống ra client |
| **#8** | Thấp | **(MỚI 23/08/2026)** Lỗi model binding (thiếu `content_json`) trả ProblemDetails RFC9110 (`type`/`title`/`errors`/`traceId`) thay vì envelope chuẩn `{"error":{"code","message","details"}}` → FE không map được `code` để i18n | Pipeline `ApiBehaviorOptions` / `InvalidModelStateResponseFactory` | ❌ **NG mới** — case C03. Owner đề xuất: backend |
| **#9** | Thấp | **(MỚI 23/08/2026)** FE `/admin/emr-templates` **không có control lọc theo `speciality`** (chỉ tách 2 mục theo `is_system`) dù API đã hỗ trợ `?speciality=` | `EmrTemplatesPageClient.tsx` | ❌ **NG mới** — case A03. Owner đề xuất: frontend |
| **#10** | TB | **(MỚI 23/08/2026)** `GET /emr-templates` **liệt kê cả row mồ côi** `tenant_id = NULL, is_system = 0` cho mọi tenant (Global Query Filter chấp nhận `TenantId == null` để dùng chung mẫu hệ thống). Ghi đã có defense-in-depth nhưng **đọc thì chưa** | `ListEmrTemplatesQueryHandler` / query filter `EmrTemplate` | ❌ **NG mới** — case I02. Đề xuất: điều kiện đọc đổi thành `(TenantId == current) OR (TenantId == null AND IsSystem)`. Owner: backend |
| **#11** | Thấp | **(MỚI 23/08/2026)** Truy cập tới mẫu của **tenant khác có `tenant_id` cụ thể** (vd 999) bị Global Query Filter loại **trước** khi tới nhánh audit → trả 404 im lặng, **không sinh log** `cross_tenant_attempt`. Chỉ hàng mồ côi `tenant_id NULL` mới được ghi log | `UpdateEmrTemplateCommandHandler` / `DeleteEmrTemplateCommandHandler` | ❌ **NG mới** — case I02. CLAUDE.md yêu cầu audit "mọi truy cập cross-tenant attempt". Owner: backend |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A Load | 3 | 1 | 0 | 2 |
| C Validate (API) | 5 | 1 | 4 | 0 |
| D/E | 4 | 3 | 1 | 0 |
| F/G/H/I | 7 | 7 | 0 | 0 |
| **TỔNG** | **19** | **12** | **5** | **2** |

- **Tỉ lệ OK trên số case thực thi được (17 case):** 12/17 = **70,6 %** — **không đạt** ngưỡng ≥ 95 % của `utc-00-quy-uoc-chuan-nhat.md`.
- **NG:** C01, C02, C04, C05 (đều gốc **Defect#2/#7** — BE hoàn toàn không có validator cho `EmrTemplateRequest`) và D01 (**Defect#5** — thiếu UNIQUE trên `name`).
- **保留:** A01, A02 — case thao tác UI, phiên test này không có browser thật; đã ghi bằng chứng gián tiếp ở cột 備考.
- **Đã đóng trong đợt này:** Defect#6 (Update mẫu hệ thống) ✅, Defect#3 (content_json null) ✅, Defect#1 (FE thiếu form) ✅ — **đã xác nhận bằng browser thật ngày 23/08/2026, nhận định "FE chưa có form" là lỗi thời**.
- **Audit log:** đầy đủ cho CREATE / UPDATE / DELETE và cả 2 nhánh từ chối `UPDATE_DENIED` / `DELETE_DENIED` (severity WARN, có cờ `cross_tenant_attempt`) — trừ trường hợp nêu ở Defect#11.
- **Hiệu năng:** `GET /emr-templates` 133–146 ms (đo 5 lần, gồm ~45 ms TLS/kết nối) → đạt yêu cầu < 500 ms.
- **Dọn dẹp:** 4 mẫu bệnh án và 2 row cross-tenant tạo trong phiên test đã bị xoá; staging chỉ còn 2 mẫu hệ thống gốc như trước khi test.
