# PRD — Mẫu bệnh án (EMR Template)

> Tác giả: Đăng (PO/BA) · Ngày: 2026-08-23 · Version: 1.0
> Liên quan: CLAUDE.md §4 Encounter, §3 Multi-tenant · QĐ 4069/2001/QĐ-BYT (mẫu hồ sơ bệnh án) · QĐ 4750/QĐ-BYT (dữ liệu giám định BHYT) · TT 46/2018/TT-BYT (bệnh án điện tử)
> Cross-link: `docs/test/utc-emr-templates.md` (UTC `EMRT-CRUD-001`) · code: `EmrHandlers.cs`, `EmrTemplateValidators.cs`, `EmrTemplatesPageClient.tsx`, `EmrTemplateForm.tsx`, `db/migrations/0026_create_emr_templates.sql`, `db/migrations/9063_seed_emr_template_permissions.sql`

---

## 1. Bối cảnh & Mục tiêu

### 1.1 Bối cảnh nghiệp vụ
Trong một lượt khám ngoại trú, bác sĩ phải ghi bệnh án với các mục cố định lặp lại: lý do khám, tiền sử, khám lâm sàng, cận lâm sàng, chẩn đoán, hướng xử trí. Nếu gõ lại từ đầu mỗi ca thì:
- Mất thời gian khám (phòng khám nhỏ 2-5 bác sĩ, 30-80 lượt/ngày).
- Thiếu mục bắt buộc → hồ sơ không đạt khi giám định BHYT hoặc thanh tra.
- Mỗi bác sĩ ghi một kiểu → dữ liệu không đồng nhất, không thống kê được.

**Mẫu bệnh án (EMR Template)** là khung nội dung soạn sẵn (Tiptap JSON) để bác sĩ chọn 1 phát ngay khi mở bệnh án, rồi điền vào chỗ trống.

### 1.2 Hai loại mẫu
| Loại | `is_system` | `tenant_id` | Ai sửa được | Nguồn thay đổi |
|---|---|---|---|---|
| **Mẫu hệ thống** | `1` | `NULL` | **Không ai** (kể cả Admin tenant) | Chỉ qua migration/seed do DevOps deploy |
| **Mẫu tùy chỉnh** | `0` | `INT` (id phòng khám) | Admin/BacSi của **chính** phòng khám đó | API runtime `/api/v1/emr-templates` |

Mẫu hệ thống là tài sản dùng chung cho mọi phòng khám (hiện có 2: *Mẫu bệnh án tổng quát*, *Mẫu bệnh án đái tháo đường*). Chúng là baseline chất lượng — một phòng khám sửa hỏng thì mọi phòng khám khác hỏng theo, nên tuyệt đối read-only ở tầng runtime.

### 1.3 Mục tiêu
1. Bác sĩ chọn mẫu và có ngay khung bệnh án < 2 giây, không gõ lại tiêu đề mục.
2. Mỗi phòng khám tự tạo được bộ mẫu riêng theo chuyên khoa của mình mà **không cần** đụng vào mẫu hệ thống.
3. Cách ly tuyệt đối giữa các phòng khám: không đọc, không sửa, không xóa được mẫu của phòng khám khác.
4. Mọi thao tác ghi và mọi lần bị từ chối đều để lại vết audit truy ngược được.
5. Bệnh án đã lập/đã ký số **không bao giờ** bị thay đổi ngược khi mẫu gốc bị sửa hoặc xóa.

### 1.4 Ngoài mục tiêu
Mẫu bệnh án **không** là nơi định nghĩa trường dữ liệu có cấu trúc (structured field) để xuất XML BHYT. Nó chỉ là khung soạn thảo văn bản. Dữ liệu đi vào XML 4750 lấy từ Encounter (ICD-10, dịch vụ, thuốc), không parse từ nội dung mẫu.

---

## 2. Personas & Ma trận quyền

### 2.1 Personas
| Persona | Nhu cầu chính |
|---|---|
| **Admin phòng khám** | Chuẩn hóa bộ mẫu cho cả phòng khám, quản lý vòng đời mẫu |
| **BacSi** | Chọn mẫu khi khám; tự tinh chỉnh mẫu theo cách hành nghề của mình |
| **LeTan / DuocSi / KeToan / KyThuatVien** | Không liên quan — không được thấy màn hình này |

### 2.2 Ma trận quyền (RBAC)

| Vai trò | `emr_template.read` (xem + dùng khi khám) | `emr_template.write` (tạo/sửa/xóa mẫu tenant) | Sửa/xóa mẫu hệ thống | Ghi chú |
|---|---|---|---|---|
| **Admin** | ✅ | ✅ | ❌ (422) | Toàn quyền trên mẫu của phòng khám mình |
| **BacSi** | ✅ | ✅ **mặc định BẬT** | ❌ (422) | Xem §2.3 |
| **LeTan** | ❌ | ❌ | ❌ | 403 `PERMISSION_DENIED`, menu ẩn |
| **DuocSi** | ❌ | ❌ | ❌ | 403 |
| **KeToan** | ❌ | ❌ | ❌ | 403 |
| **KyThuatVien** | ❌ | ❌ | ❌ | 403 |

### 2.3 Quyết định PO — vì sao BacSi được `write` mặc định
Phòng khám mục tiêu chỉ có 2-5 bác sĩ và thường **không có** admin chuyên trách ngồi thường trực. Nếu bắt bác sĩ phải nhờ admin mỗi lần muốn thêm một mục vào mẫu, mẫu sẽ chết yểu và bác sĩ quay lại gõ tay. Vì vậy:
- **Mặc định**: role `bac_si` được cấp cả `read` + `write` (đúng như seed `9063_seed_emr_template_permissions.sql` đang chạy).
- **Cấu hình được**: Admin có thể thu hồi `emr_template.write` khỏi role `bac_si` trong màn hình Phân quyền nếu phòng khám muốn siết. Đây là cấu hình RBAC sẵn có, **không cần code thêm**.
- Ràng buộc an toàn không đổi trong mọi cấu hình: `write` **chỉ** áp lên mẫu của chính tenant, **không bao giờ** áp lên mẫu hệ thống (AC-01).

---

## 3. Use cases

| ID | Use case | Actor | Entry point |
|---|---|---|---|
| UC-01 | Xem danh sách mẫu (hệ thống + tùy chỉnh) | Admin, BacSi | `/admin/emr-templates` |
| UC-02 | Chọn mẫu khi soạn bệnh án | BacSi | Dropdown "Mẫu bệnh án" trong màn hình khám |
| UC-03 | Tạo mẫu tùy chỉnh mới | Admin, BacSi | `/admin/emr-templates/new` |
| UC-04 | Sửa mẫu tùy chỉnh | Admin, BacSi | `/admin/emr-templates/{id}/edit` |
| UC-05 | Xóa mềm mẫu tùy chỉnh | Admin, BacSi | Nút thùng rác + ConfirmDialog |
| UC-06 | **Nhân bản mẫu hệ thống → mẫu tùy chỉnh** | Admin, BacSi | Nút "Nhân bản" trên card mẫu hệ thống — **CHƯA implement** |
| UC-07 | Cập nhật mẫu hệ thống | DevOps (offline) | Migration `db/migrations/NNNN_*.sql` |

---

## 4. User stories & Acceptance Criteria

### US-01 — Xem danh sách mẫu
> **As a** Bác sĩ/Admin phòng khám, **I want** xem toàn bộ mẫu bệnh án đang dùng được (mẫu hệ thống + mẫu của phòng khám mình), **so that** tôi biết có sẵn khung nào để chọn khi khám mà không phải hỏi ai.

| ID | Acceptance Criteria |
|---|---|
| US-01.AC-1 | **Given** tôi đăng nhập với vai trò có `emr_template.read`, **When** tôi mở `/admin/emr-templates`, **Then** màn hình hiển thị 2 nhóm tách biệt: "Mẫu hệ thống" và "Mẫu tùy chỉnh", mỗi mẫu có badge phân loại ("Hệ thống"/"Tùy chỉnh"), tên mẫu và chuyên khoa. |
| US-01.AC-2 | **Given** phòng khám của tôi chưa tạo mẫu nào, **When** tôi mở danh sách, **Then** nhóm "Mẫu hệ thống" vẫn hiển thị đủ mẫu dùng chung, nhóm "Mẫu tùy chỉnh" hiển thị empty state "Chưa có mẫu tùy chỉnh" (không phải màn hình trắng, không phải lỗi). |
| US-01.AC-3 | **Given** tôi là bác sĩ đang khám, **When** tôi bấm dropdown "Mẫu bệnh án" trong màn hình khám, **Then** danh sách hiện đúng 2 nhóm như trên; chọn 1 mẫu thì nội dung mẫu được nạp vào editor bệnh án. |
| US-01.AC-4 | **Given** tôi gọi `GET /api/v1/emr-templates?speciality=DIABETES`, **Then** API chỉ trả các mẫu có `speciality=DIABETES`; với `?speciality=<giá trị không tồn tại>` trả `{"data":[]}` + HTTP 200 (không phải lỗi). |
| US-01.AC-5 | **Given** danh sách có ≤ 100 mẫu, **When** gọi `GET /api/v1/emr-templates`, **Then** thời gian phản hồi p95 < 500 ms. |
| US-01.AC-6 | **Given** tôi là LeTan/DuocSi/KeToan (không có `emr_template.read`), **When** gọi `GET /api/v1/emr-templates`, **Then** trả HTTP 403 `PERMISSION_DENIED` và mục menu "Mẫu bệnh án" không hiển thị trên sidebar. |
| US-01.AC-7 | **Given** API đã hỗ trợ tham số `?speciality=`, **When** tôi ở màn hình danh sách, **Then** có control lọc theo chuyên khoa trên UI (hiện **chưa có** — Defect#9, ưu tiên Thấp). |

---

### US-02 — Admin phòng khám tạo mẫu riêng
> **As a** Admin phòng khám (hoặc BacSi có quyền ghi), **I want** tạo mẫu bệnh án riêng cho phòng khám mình, **so that** khung bệnh án khớp đúng chuyên khoa và thói quen ghi chép của chúng tôi.

| ID | Acceptance Criteria |
|---|---|
| US-02.AC-1 | **Given** tôi có `emr_template.write`, **When** tôi bấm "Tạo mẫu mới" ở `/admin/emr-templates`, **Then** hệ thống điều hướng sang `/admin/emr-templates/new` hiển thị form gồm: **Tên mẫu** (bắt buộc), **Chuyên khoa** (select), **Nội dung mẫu** (editor Tiptap có toolbar Đậm/Nghiêng/Tiêu đề 1-2/Danh sách/Danh sách số/Trích dẫn/Kẻ ngang/Bảng/Ảnh). |
| US-02.AC-2 | **Given** tôi bỏ trống Tên mẫu, **When** tôi bấm Lưu, **Then** FE chặn submit và hiển thị "Vui lòng nhập tên mẫu" ngay dưới ô nhập; không phát sinh request lên BE. |
| US-02.AC-3 | **Given** tôi điền hợp lệ (tên + chuyên khoa + nội dung), **When** tôi bấm Lưu, **Then** BE trả HTTP 201, bản ghi mới có `tenant_id` = tenant trong JWT, `is_system = 0`, `created_by` = tôi, `content_json` là JSON Tiptap hợp lệ giữ nguyên tiếng Việt có dấu. |
| US-02.AC-4 | **Given** tạo thành công, **When** tôi quay lại danh sách, **Then** mẫu mới nằm trong nhóm "Mẫu tùy chỉnh" và xuất hiện ngay trong dropdown chọn mẫu ở màn hình khám. |
| US-02.AC-5 | **Given** tôi chèn ảnh vào nội dung mẫu, **When** tôi nhập URL không phải `http://`/`https://` (ví dụ `javascript:...`), **Then** hệ thống từ chối và hiện toast "URL ảnh không hợp lệ. Chỉ chấp nhận đường dẫn bắt đầu bằng http:// hoặc https://". |
| US-02.AC-6 | **Given** tôi tạo mẫu trùng tên với mẫu đã có trong cùng phòng khám, **When** tôi bấm Lưu, **Then** BE trả HTTP 409 `TEMPLATE_DUPLICATE_NAME` "Tên mẫu bệnh án đã tồn tại trong phòng khám" (hiện **chưa** có ràng buộc này — Defect#5, cần thêm UNIQUE `(tenant_id, name)` khi `deleted_at IS NULL`). |
| US-02.AC-7 | **Given** thao tác tạo thành công, **Then** ghi audit log `CREATE / EMR_TEMPLATE / INFO` kèm `resource_id`, `details.name`, `details.tenantId`, `details.userId`. |

---

### US-03 — Admin phòng khám sửa/xóa mẫu của phòng khám mình
> **As a** Admin phòng khám (hoặc BacSi có quyền ghi), **I want** sửa lại hoặc bỏ đi mẫu do phòng khám mình tạo, **so that** bộ mẫu luôn phản ánh phác đồ hiện hành, không tồn đọng mẫu lỗi thời gây chọn nhầm.

| ID | Acceptance Criteria |
|---|---|
| US-03.AC-1 | **Given** một mẫu tùy chỉnh thuộc phòng khám tôi, **When** tôi bấm biểu tượng bút chì, **Then** mở `/admin/emr-templates/{id}/edit` với form đã nạp sẵn tên, chuyên khoa và nội dung hiện tại. |
| US-03.AC-2 | **Given** tôi đổi tên/chuyên khoa/nội dung rồi bấm Lưu, **When** BE xử lý, **Then** trả HTTP 200; DB cập nhật đúng cả 3 trường, set `updated_by` = tôi, `updated_at` = thời điểm sửa; ghi audit `UPDATE / EMR_TEMPLATE / INFO`. |
| US-03.AC-3 | **Given** tôi bấm biểu tượng thùng rác, **When** hộp thoại xác nhận hiện ra và tôi xác nhận, **Then** BE trả HTTP 204, bản ghi **soft delete** (`deleted_at`, `deleted_by` được set), row **không** bị xóa cứng khỏi DB, mẫu biến mất khỏi danh sách và khỏi dropdown chọn mẫu. |
| US-03.AC-4 | **Given** một mẫu đã bị xóa mềm, **When** tôi gọi `DELETE` lần nữa hoặc `DELETE` với id không tồn tại, **Then** trả HTTP 404 `TEMPLATE_NOT_FOUND` "Không tìm thấy mẫu bệnh án". |
| US-03.AC-5 | **Given** mẫu bị xóa mềm đã từng được dùng để soạn bệnh án, **Then** các bệnh án cũ (kể cả đã ký số) giữ nguyên nội dung, không báo lỗi khi mở/in lại (xem §9 Edge case E-01). |
| US-03.AC-6 | **Given** mẫu hiển thị trong danh sách là **mẫu hệ thống**, **Then** UI **không** render nút Sửa và nút Xóa cho mẫu đó (chặn ở tầng trình bày, song song với chặn ở BE theo AC-01). |
| US-03.AC-7 | **Given** tôi không có `emr_template.write`, **Then** nút "Tạo mẫu mới", Sửa, Xóa đều bị ẩn; nếu gọi thẳng API thì trả 403 `PERMISSION_DENIED`. |

---

### US-04 — Nhân bản mẫu hệ thống thành mẫu riêng ⭐ MỚI · ưu tiên **Cao**
> **As a** Admin phòng khám (hoặc BacSi có quyền ghi), **I want** nhân bản một mẫu hệ thống thành mẫu riêng của phòng khám mình rồi sửa bản sao đó, **so that** tôi tùy biến được nội dung theo phòng khám mà hệ thống vẫn không phải mở quyền sửa mẫu dùng chung.

**Lý do tồn tại:** đây là lời giải nghiệp vụ cho căng thẳng giữa AC-01 (cấm sửa mẫu hệ thống) và nhu cầu thực tế của phòng khám (muốn thêm/bớt mục trong mẫu chuẩn). Không có tính năng này, người dùng sẽ liên tục đòi mở quyền sửa mẫu hệ thống — điều tuyệt đối không được chấp nhận.

| ID | Acceptance Criteria |
|---|---|
| US-04.AC-1 | **Given** tôi có `emr_template.write` và đang xem một mẫu hệ thống, **When** tôi bấm "Nhân bản", **Then** hệ thống gọi `POST /api/v1/emr-templates/{id}/clone` và trả HTTP 201 kèm mẫu mới. |
| US-04.AC-2 | **Given** clone thành công, **Then** mẫu mới có: `tenant_id` = tenant trong JWT, `is_system = 0`, `content_json` **sao chép nguyên vẹn** từ mẫu nguồn, `speciality` = của mẫu nguồn, `name` mặc định = `"{tên mẫu nguồn} (bản sao)"`, `created_by` = tôi. |
| US-04.AC-3 | **Given** clone thành công, **When** tôi kiểm tra mẫu nguồn, **Then** mẫu hệ thống nguồn **không thay đổi** bất kỳ trường nào (`name`, `content_json`, `speciality`, `updated_at` giữ nguyên). |
| US-04.AC-4 | **Given** mẫu vừa clone, **When** tôi bấm Sửa hoặc Xóa, **Then** thao tác thành công như mọi mẫu tùy chỉnh khác (US-03). |
| US-04.AC-5 | **Given** tôi clone một mẫu **của phòng khám khác** (`tenant_id` khác), **Then** trả HTTP 404 `TEMPLATE_NOT_FOUND` — không được dùng clone làm đường vòng để đọc trộm nội dung mẫu của tenant khác. |
| US-04.AC-6 | **Given** tôi clone cùng một mẫu hệ thống 2 lần, **Then** cả 2 lần đều thành công và tạo ra 2 bản ghi độc lập; tên bản thứ 2 tự tăng hậu tố `"(bản sao 2)"` để không vi phạm ràng buộc UNIQUE ở US-02.AC-6. |
| US-04.AC-7 | **Given** clone thành công, **Then** ghi audit `CLONE / EMR_TEMPLATE / INFO` với `details.sourceTemplateId`, `details.newTemplateId`, `details.tenantId`, `details.userId`. |
| US-04.AC-8 | **Given** tôi không có `emr_template.write`, **When** gọi endpoint clone, **Then** trả 403 `PERMISSION_DENIED`. |

> ⚠️ **Tình trạng:** US-04 **CHƯA được implement**. Đây là backlog cho sprint sau — xem §12 Out of scope.

---

### US-05 — Mẫu hệ thống chỉ thay đổi qua migration/seed
> **As a** người vận hành hệ thống (DevOps/Architect), **I want** mẫu hệ thống chỉ được tạo/sửa qua migration có review, **so that** một phòng khám bất kỳ (hoặc một tài khoản bị chiếm) không thể làm hỏng dữ liệu dùng chung của toàn bộ khách hàng SaaS.

| ID | Acceptance Criteria |
|---|---|
| US-05.AC-1 | **Given** bất kỳ request runtime nào tới `POST /api/v1/emr-templates`, **When** BE tạo bản ghi, **Then** `is_system` luôn bị ép cứng = `false` ở tầng server, **bất kể** client gửi `is_system: true` trong body. |
| US-05.AC-2 | **Given** toàn bộ API surface hiện tại, **Then** **không tồn tại** endpoint nào cho phép đặt `is_system = true` hoặc `tenant_id = NULL` cho một mẫu. |
| US-05.AC-3 | **Given** cần thêm/sửa một mẫu hệ thống, **When** thực hiện, **Then** thay đổi phải nằm trong file `db/migrations/NNNN_description.sql` idempotent (`INSERT IGNORE` / `ON DUPLICATE KEY UPDATE`), được review qua PR và deploy bởi DevOps — không có đường tắt qua UI. |
| US-05.AC-4 | **Given** migration cập nhật mẫu hệ thống chạy lại lần 2, **Then** kết quả không đổi (idempotent), không tạo bản ghi trùng, không đụng vào bất kỳ mẫu tùy chỉnh nào của tenant. |
| US-05.AC-5 | **Given** mẫu hệ thống có `id` cố định (`aaaaaaaa-0001-…`, `aaaaaaaa-0002-…`), **Then** migration về sau phải tham chiếu đúng `id` này, không tạo mới với `id` ngẫu nhiên gây trùng nội dung. |

---

## 5. Acceptance Criteria xuyên suốt — Bảo mật & Toàn vẹn dữ liệu

Bộ AC này áp cho **mọi** User Story ở trên. QC/Tester dùng đúng bộ này làm gate bảo mật của module.

| ID | Acceptance Criteria |
|---|---|
| **AC-01** | **Chặn sửa/xóa mẫu hệ thống với mọi vai trò.** **Given** một mẫu có `is_system = 1`, **When** bất kỳ user nào (kể cả Admin, kể cả user có `emr_template.write`) gọi `PUT` hoặc `DELETE` lên mẫu đó, **Then** hệ thống trả HTTP **422** `TEMPLATE_SYSTEM` với message "Không thể sửa mẫu bệnh án hệ thống" / "Không thể xóa mẫu bệnh án hệ thống"; bản ghi **nguyên vẹn 100 %** (`name`, `speciality`, `content_json`, `updated_at` không đổi); ghi audit `UPDATE_DENIED`/`DELETE_DENIED` severity **WARN** kèm `details.reason = "IS_SYSTEM"`. |
| **AC-02** | **Cách ly tenant tuyệt đối.** **Given** một mẫu tùy chỉnh thuộc `tenant_id = X`, **When** user của `tenant_id = Y` (Y ≠ X) gọi `GET`/`PUT`/`DELETE`/`clone` lên mẫu đó, **Then** hệ thống trả HTTP **404** `TEMPLATE_NOT_FOUND` (không tiết lộ mẫu có tồn tại hay không), dữ liệu không đổi, **và** ghi audit `UPDATE_DENIED`/`DELETE_DENIED` với cờ `cross_tenant_attempt = 1`, `details.reason = "TENANT_MISMATCH"` (hiện **chưa** ghi audit cho trường hợp tenant có id cụ thể vì bị Global Query Filter loại trước — Defect#11, cần fix). |
| **AC-03** | **Mẫu "mồ côi" không được rò rỉ.** **Given** một bản ghi bất thường `tenant_id = NULL` **và** `is_system = 0` (sinh từ seed/SQL tay sai), **When** user của bất kỳ tenant nào gọi `GET /api/v1/emr-templates`, **Then** bản ghi đó **KHÔNG** xuất hiện trong danh sách — điều kiện đọc phải là `(tenant_id = @currentTenant) OR (tenant_id IS NULL AND is_system = 1)` (hiện **đang rò rỉ** — Defect#10, cần fix); **và When** gọi `PUT`/`DELETE` lên bản ghi đó, **Then** trả 404 `TEMPLATE_NOT_FOUND` + audit `cross_tenant_attempt = 1` (đã đạt). |
| **AC-04** | **`is_system` và `tenant_id` do server tự gán.** **Given** client gửi body chứa `{"is_system": true, "tenant_id": 999, ...}`, **When** BE tạo hoặc cập nhật mẫu, **Then** BE **bỏ qua hoàn toàn** 2 trường này trong payload; giá trị lưu xuống DB là `is_system = 0` và `tenant_id` lấy từ `ITenantProvider` (nguồn duy nhất = JWT). Không có đường nào để client tự chọn tenant. |
| **AC-05** | **Validate đầu vào ở tầng BE, trả 400 nghiệp vụ, không bao giờ 500.** **Given** request tạo/sửa mẫu, **When** dữ liệu vi phạm, **Then** BE trả HTTP **400** với envelope chuẩn `{"error":{"code":"...","message":"..."}}`, message tiếng Việt có dấu, theo bảng dưới: <br>• `name` rỗng → "Tên mẫu bệnh án không được để trống" <br>• `name` > 200 ký tự → "Tên mẫu bệnh án tối đa 200 ký tự" <br>• `content_json` null/thiếu → "Nội dung mẫu bệnh án không được để trống" <br>• `speciality` rỗng → "Chuyên khoa không được để trống" <br>• `speciality` > 50 ký tự → "Chuyên khoa tối đa 50 ký tự" <br>• `speciality` ngoài enum `GENERAL\|DIABETES\|CARDIOLOGY\|ENDOCRINOLOGY\|NEPHROLOGY\|OPHTHALMOLOGY\|OTHER` → "Chuyên khoa không hợp lệ, phải là một trong: …" <br>**Và** không trường hợp nào trả HTTP 500 `INTERNAL_ERROR` (lỗi truncate MySQL `STRICT_TRANS_TABLES` không được lọt ra client). |

---

## 6. Data model

### `diab_his_cli_emr_templates` (hiện có — migration `0026`)
| Cột | Kiểu | Ghi chú |
|---|---|---|
| `id` | CHAR(36) PK DEFAULT (UUID()) | |
| `tenant_id` | INT **NULL** | `NULL` = mẫu hệ thống dùng chung. Index `idx_emr_tpl_tenant` |
| `name` | VARCHAR(200) NOT NULL | Hiện **chưa** UNIQUE — xem đề xuất bên dưới |
| `content_json` | LONGTEXT NOT NULL | Tiptap JSON document |
| `speciality` | VARCHAR(50) NOT NULL DEFAULT 'GENERAL' | Enum bằng COMMENT, enforce ở validator BE + select FE. Index `idx_emr_tpl_spec` |
| `is_system` | TINYINT(1) NOT NULL DEFAULT 0 | Server tự gán (AC-04) |
| `created_at` / `created_by` | DATETIME / CHAR(36) | |
| `updated_at` / `updated_by` | DATETIME ON UPDATE / CHAR(36) | |
| `deleted_at` | DATETIME NULL | Soft delete |

### Thay đổi schema đề xuất (cho sprint tới)
| # | Thay đổi | Phục vụ AC |
|---|---|---|
| 1 | Thêm cột `deleted_by CHAR(36) NULL` nếu chưa có trong schema chuẩn hóa | US-03.AC-3 |
| 2 | Thêm UNIQUE trên `(tenant_id, name)` áp dụng khi `deleted_at IS NULL` (MySQL 8 không có partial index → dùng cột sinh `name_active` hoặc kiểm tra ở tầng service + index thường) | US-02.AC-6 |
| 3 | Thêm index `(tenant_id, is_system, speciality)` phục vụ query list có filter | US-01.AC-5 |
| 4 | (US-04) Thêm cột `cloned_from_id CHAR(36) NULL` để truy vết mẫu được nhân bản từ mẫu nào | US-04.AC-7 |

### Liên hệ với bệnh án
`diab_his_cli_emr_contents.template_id` (VARCHAR(36)) là **tham chiếu lỏng, KHÔNG có khóa ngoại** tới bảng mẫu. Nội dung bệnh án được **snapshot** vào `content_json` của chính bệnh án tại thời điểm bác sĩ chọn mẫu. Đây là thiết kế **cố ý** và là điều kiện cần để đạt Edge case E-01/E-02 (§9) — không được đổi thành FK CASCADE.

---

## 7. API contract

| Method | Path | Quyền | Kết quả |
|---|---|---|---|
| GET | `/api/v1/emr-templates?speciality=&is_system=` | `emr_template.read` | 200 `{data:[…]}`, sort `is_system DESC, name ASC` |
| POST | `/api/v1/emr-templates` | `emr_template.write` | 201 `{data:{…}}` · 400 validate · 403 |
| PUT | `/api/v1/emr-templates/{id}` | `emr_template.write` | 200 · 400 · **422 `TEMPLATE_SYSTEM`** · 404 `TEMPLATE_NOT_FOUND` |
| DELETE | `/api/v1/emr-templates/{id}` | `emr_template.write` | 204 · **422 `TEMPLATE_SYSTEM`** · 404 |
| POST | `/api/v1/emr-templates/{id}/clone` | `emr_template.write` | **CHƯA CÓ (US-04)** — 201 · 404 · 403 |

Alias route đang tồn tại: `/api/v1/emr/templates` (giữ tương thích, không dùng cho tính năng mới).

**Mã lỗi:** `TEMPLATE_NOT_FOUND`, `TEMPLATE_SYSTEM`, `TEMPLATE_DUPLICATE_NAME` (đề xuất), `PERMISSION_DENIED`, `VALIDATION_ERROR`.

> ⚠️ Lỗi model-binding hiện trả ProblemDetails RFC9110 (`type`/`title`/`errors`/`traceId`) thay vì envelope chuẩn của dự án → FE không map được `code` để i18n (Defect#8). AC-05 yêu cầu chuẩn hóa về `{"error":{"code","message","details"}}`.

---

## 8. UX wireframe

```
┌─ Mẫu bệnh án ─────────────────────────────── [+ Tạo mẫu mới] ─┐
│ Quản lý template bệnh án cho từng chuyên khoa                  │
│ [Chuyên khoa: Tất cả ▾]  ← Defect#9: control này CHƯA có       │
├────────────────────────────────────────────────────────────────┤
│ MẪU HỆ THỐNG                                                   │
│ 📄 Mẫu bệnh án tổng quát        GENERAL   [Hệ thống] [⧉ Nhân bản]│
│ 📄 Mẫu bệnh án đái tháo đường   DIABETES  [Hệ thống] [⧉ Nhân bản]│
│        ↑ KHÔNG có nút Sửa / Xóa (AC-01)   ↑ US-04, chưa có     │
├────────────────────────────────────────────────────────────────┤
│ MẪU TÙY CHỈNH                                                  │
│ 📄 Khám ĐTĐ định kỳ PK Minh Anh DIABETES [Tùy chỉnh] [✎] [🗑]  │
│                                                                 │
│ (rỗng) → "Chưa có mẫu tùy chỉnh"                               │
└────────────────────────────────────────────────────────────────┘

┌─ Tạo mẫu bệnh án ────────────────── [Hủy] [Lưu mẫu] ──────────┐
│ Tên mẫu *              │ Chuyên khoa                           │
│ [_________________]    │ [Đái tháo đường ▾]                    │
├────────────────────────────────────────────────────────────────┤
│ Nội dung mẫu                                                   │
│ [B][I] │ [H1][H2] │ [•][1.][“”][—] │ [▦ Bảng][🖼 Ảnh]          │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │ ## Lý do khám                                              │ │
│ │ ## Tiền sử đái tháo đường                                  │ │
│ │ ## Khám lâm sàng                                           │ │
│ │ ## Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)            │ │
│ │ ## Chẩn đoán & biến chứng                                  │ │
│ │ ## Mục tiêu điều trị & hướng xử trí                        │ │
│ └────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────┘
```

---

## 9. Edge cases

| ID | Tình huống | Xử lý mong muốn |
|---|---|---|
| **E-01** | **Bệnh án đã ký số tham chiếu mẫu vừa bị xóa mềm** | Bệnh án **giữ nguyên 100 %**: nội dung đã snapshot vào `content_json` của bệnh án, `template_id` chỉ là chuỗi tham chiếu không FK. Mở lại / in lại / xuất PDF phải chạy bình thường, chữ ký số vẫn hợp lệ (hash tính trên nội dung bệnh án, không phụ thuộc mẫu). Nếu UI có hiển thị nguồn mẫu thì ghi "Mẫu đã bị xóa". **Tuyệt đối không hard-delete** mẫu, không dùng FK CASCADE. |
| **E-02** | **Sửa nội dung mẫu sau khi đã có bệnh án dùng mẫu đó** | Không áp ngược (no back-propagation). Bệnh án cũ không đổi. Chỉ bệnh án tạo **sau** thời điểm sửa mới nhận nội dung mới. |
| **E-03** | **Hồ sơ BHYT** | Mẫu chỉ là khung soạn thảo, không sinh trường XML 4750. Tuy nhiên mẫu hệ thống phải bảo đảm đủ các mục tối thiểu của hồ sơ bệnh án ngoại trú (lý do khám, tiền sử, khám lâm sàng, cận lâm sàng, chẩn đoán, hướng xử trí) để bản in đạt yêu cầu khi cơ quan BHXH giám định. Khi phòng khám tự tạo mẫu (US-02) mà thiếu các mục này, hệ thống **không chặn** nhưng nên cảnh báo vàng "Mẫu thiếu mục bắt buộc theo hồ sơ bệnh án ngoại trú: …" (đề xuất, ưu tiên Thấp). |
| **E-04** | **Tenant mới tinh, chưa có mẫu tùy chỉnh** | Vẫn dùng được ngay 2 mẫu hệ thống; empty state rõ ràng ở nhóm tùy chỉnh; dropdown khi khám không được rỗng. |
| **E-05** | **Tái khám** | Bác sĩ chọn lại chính mẫu đã dùng lần trước — mẫu không lưu trạng thái theo bệnh nhân. (Chức năng "sao chép bệnh án lần khám trước" là tính năng khác, thuộc module Encounter, ngoài phạm vi PRD này.) |
| **E-06** | **Hai mẫu trùng tên trong cùng phòng khám** | Hiện tạo được cả hai → bác sĩ chọn nhầm. Cần chặn theo US-02.AC-6. |
| **E-07** | **Nội dung mẫu rỗng** (`{"type":"doc","content":[]}`) | Cho phép lưu (bác sĩ có thể muốn mẫu trắng), nhưng FE hiện cảnh báo mềm; `content_json` vẫn phải là JSON hợp lệ, không được `null` (AC-05). |
| **E-08** | **Nội dung quá lớn** (ảnh base64, bảng dài) | `content_json` là LONGTEXT nên DB chịu được, nhưng cần giới hạn payload ~1 MB ở tầng API để tránh nghẽn; vượt → 413 hoặc 400 với message tiếng Việt. |
| **E-09** | **Hai người sửa cùng một mẫu đồng thời** | Chấp nhận last-write-wins ở v1 (rủi ro thấp: phòng khám nhỏ). Audit ghi đủ 2 lần UPDATE để truy vết. Hiển thị `updated_at` + người sửa cuối trên danh sách để người dùng tự nhận biết. |
| **E-10** | **Chèn ảnh bằng URL không an toàn** | Chỉ chấp nhận scheme `http://` / `https://` (đã có `promptForSafeImageUrl`). Nội dung Tiptap khi render ra HTML phải được sanitize ở cả FE lẫn khâu xuất PDF. |
| **E-11** | **Người dùng đòi sửa mẫu hệ thống** | Trả lời nghiệp vụ: dùng US-04 "Nhân bản" rồi sửa bản sao. **Không** mở quyền sửa mẫu hệ thống trong bất kỳ trường hợp nào. |

---

## 10. Non-functional

- **Hiệu năng:** `GET /api/v1/emr-templates` < 500 ms p95 (đo thực tế trên staging 23/08/2026: 133-146 ms — đạt).
- **Audit (CLAUDE.md §3, §6):** ghi `CREATE` / `UPDATE` / `DELETE` (severity INFO) và `UPDATE_DENIED` / `DELETE_DENIED` (severity WARN, có cờ `cross_tenant_attempt`) vào `diab_his_sec_audit_logs`. US-04 bổ sung action `CLONE`.
- **Multi-tenant:** enforce ở application layer (EF Core Global Query Filter) **cộng** defense-in-depth trong từng command handler — không dựa vào một lớp duy nhất.
- **i18n:** toàn bộ nhãn/thông báo tiếng Việt có dấu; mã lỗi SCREAMING_SNAKE tiếng Anh để FE map.
- **A11y/UX:** nút toolbar có `aria-label`; hộp thoại xóa dùng `ConfirmDialog` variant destructive; touch target ≥ 44px cho tablet phòng khám.
- **FHIR:** mẫu bệnh án là artifact soạn thảo nội bộ, **không** map thành FHIR resource. Bệnh án hoàn chỉnh sinh ra từ mẫu mới map sang `Composition` / `DocumentReference` (thuộc module EMR).

---

## 11. Trạng thái hiện tại (tính đến 23/08/2026)

### 11.1 Đã có
| Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|
| FE: danh sách mẫu tách 2 nhóm + badge + empty state | ✅ | `EmrTemplatesPageClient.tsx` |
| **FE: form Tạo/Sửa mẫu (Tiptap + toolbar + select chuyên khoa)** | ✅ **ĐÃ CÓ** | `EmrTemplateForm.tsx`, route `new/page.tsx` + `[id]/edit/page.tsx`; đã xác nhận bằng browser thật trên staging |
| FE: dropdown chọn mẫu khi khám | ✅ | `EmrTemplateSelector.tsx` |
| BE: CRUD + soft delete + `is_system` ép false | ✅ | `EmrHandlers.cs` |
| BE: chặn sửa/xóa mẫu hệ thống (AC-01) | ✅ | 422 `TEMPLATE_SYSTEM`, verify 23/08/2026 |
| BE: defense-in-depth cross-tenant + audit DENIED | ✅ | `isCrossTenantAttempt` trong Update/Delete handler |
| BE: validator FluentValidation (name/content_json/speciality) | ✅ trong code | `EmrTemplateValidators.cs` — **cần re-test staging** để đóng Defect#2/#4/#7 |
| RBAC: `emr_template.read`/`write` cho `admin` + `bac_si` | ✅ | `9063_seed_emr_template_permissions.sql` |
| Bảo vệ URL ảnh (chỉ http/https) | ✅ | `lib/tiptap-image.ts` dùng trong cả `EmrTemplateForm` và `EmrEditor` |

### 11.2 Đính chính tài liệu test
> **Defect#1 trong `docs/test/utc-emr-templates.md` ("FE thiếu toàn bộ form Tạo/Sửa") là nhận định LỖI THỜI.** Phiên kiểm tra bằng browser thật ngày 23/08/2026 xác nhận form Tạo/Sửa đã tồn tại và hoạt động đầy đủ tại `/admin/emr-templates/new` và `/admin/emr-templates/{id}/edit`. Mọi ticket/estimate dựa trên nhận định cũ này phải được xem lại — **không** tạo lại form.

### 11.3 Còn mở (backlog)
| Defect | Mức | Nội dung | AC liên quan | Owner |
|---|---|---|---|---|
| #2/#4/#7 | Cao/TB | Validator đã viết nhưng chưa xác nhận trên staging (name rỗng/quá dài, speciality sai enum từng trả 201 hoặc 500) | AC-05 | backend + tester |
| #5 | TB | Thiếu ràng buộc UNIQUE `(tenant_id, name)` → mẫu trùng tên | US-02.AC-6 | backend + DB |
| #8 | Thấp | Lỗi model-binding trả ProblemDetails thay vì envelope chuẩn | AC-05 | backend |
| #9 | Thấp | FE thiếu control lọc theo `speciality` | US-01.AC-7 | frontend |
| #10 | TB | `GET` liệt kê cả row mồ côi `tenant_id NULL, is_system 0` cho mọi tenant | AC-03 | backend |
| #11 | Thấp | Không sinh audit khi truy cập mẫu của tenant có id cụ thể (bị query filter loại trước) | AC-02 | backend |

---

## 12. Out of scope (đợt này)

1. **US-04 "Nhân bản mẫu" — CHƯA IMPLEMENT.** Đây là **backlog cho sprint sau**, không phải công việc đã hoàn thành. Cần: endpoint `POST /api/v1/emr-templates/{id}/clone`, cột `cloned_from_id`, nút "Nhân bản" trên card mẫu hệ thống, audit action `CLONE`.
2. Phân quyền theo từng mẫu (mẫu riêng của một bác sĩ, bác sĩ khác không thấy) — v1 dùng chung trong phạm vi phòng khám.
3. Versioning mẫu (lịch sử phiên bản, rollback về bản cũ) — bệnh án đã có versioning riêng, mẫu chưa cần.
4. Chia sẻ mẫu giữa các phòng khám trong cùng chuỗi (marketplace mẫu).
5. Biến động (placeholder/merge field) kiểu `{{tên bệnh nhân}}`, `{{tuổi}}` tự điền từ hồ sơ bệnh nhân.
6. Import/export mẫu ra file `.docx` / `.json`.
7. Kiểm tra tự động mẫu có đủ mục bắt buộc theo hồ sơ BHYT (E-03) — chỉ dừng ở khuyến nghị.
8. Khôi phục mẫu đã xóa mềm (thùng rác) — hiện chỉ khôi phục được bằng SQL tay.

---

## 13. Dependencies

- **Module Encounter/EMR:** nơi tiêu thụ mẫu (`EmrTemplateSelector` → `EmrEditor`), cột `template_id` trên nội dung bệnh án.
- **Module Users & RBAC:** quyền `emr_template.read` / `emr_template.write`, seed `9063` (role code lowercase `admin`, `bac_si`).
- **Audit Log:** `IAuditService` + bảng `diab_his_sec_audit_logs`, hỗ trợ severity + cờ `cross_tenant_attempt`.
- **Tenant provider:** `ITenantProvider` cấp `tenant_id` từ JWT (AC-04).
- **Migration `0026`** phải chạy trước; migration `9063` phải chạy trước khi bác sĩ dùng được dropdown mẫu.

---

## 14. Risks

| ID | Rủi ro | Mức | Giảm thiểu |
|---|---|---|---|
| R-01 | Bác sĩ có quyền `write` tạo mẫu rác/trùng lặp làm loãng danh sách | TB | US-02.AC-6 chặn trùng tên; Admin thu hồi được `write` qua RBAC; bổ sung filter theo chuyên khoa (Defect#9) |
| R-02 | Rò rỉ đọc mẫu mồ côi giữa các tenant (Defect#10) | **Cao** | Sửa điều kiện query filter theo AC-03; thêm test hồi quy đa tenant |
| R-03 | Cross-tenant attempt tới tenant có id cụ thể không để lại log (Defect#11) | TB | Bổ sung nhánh audit trước khi query filter loại bản ghi (dùng `IgnoreQueryFilters` có kiểm soát) |
| R-04 | Migration cập nhật mẫu hệ thống viết sai → ghi đè hoặc nhân bản mẫu của tenant | **Cao** | Bắt buộc idempotent + tham chiếu `id` cố định (US-05.AC-4/AC-5); review PR bởi architect; rollback bằng migration nghịch |
| R-05 | Không có US-04 → người dùng gây áp lực mở quyền sửa mẫu hệ thống | TB | Ưu tiên US-04 ngay sprint kế; trong thời gian chờ, hướng dẫn tạo mẫu mới từ đầu (US-02) |
| R-06 | Lỗi validate trả ProblemDetails → FE hiển thị message tiếng Anh cho người dùng cuối (Defect#8) | Thấp | Chuẩn hóa `InvalidModelStateResponseFactory` về envelope dự án |
| R-07 | Nội dung mẫu chứa HTML/URL độc hại lọt vào bản in bệnh án | TB | Đã chặn scheme URL ảnh; cần sanitize khi render HTML và khi xuất PDF (QuestPDF) |
