# Phân tích mở rộng Đa chi nhánh / Đa cơ sở — Rapid Assessment

- **Ngày**: 2026-08-29
- **Tác giả**: Đăng (PO/BA)
- **Branch khảo sát**: `develop` (đã merge `sys_phong_kham_noi`)
- **Loại tài liệu**: Đánh giá nhanh (rapid assessment) — KHÔNG phải PRD đầy đủ. Dùng để user quyết định hướng đi.
- **Câu hỏi gốc của user**: "phần bệnh viện, chi nhánh nhiều chỗ thì phải làm sao"

---

## 1. Tóm tắt điều hành (đọc 1 phút)

Mô hình hiện tại là **Tenant (1 tổ chức) → Branch (N chi nhánh, phẳng 1 cấp)**. Backend đã cài đặt tương đối chắc phần **nền tảng cách ly dữ liệu** (JWT claim, middleware, query filter, RBAC). Nhưng:

| Kết luận | Chi tiết |
|---|---|
| **Đủ dùng cho 2–5 chi nhánh / 1 phòng khám** | Đúng như phạm vi CLAUDE.md hiện tại. Nền tảng cách ly chạy được. |
| **KHÔNG đủ cho "nhiều bệnh viện, mỗi bệnh viện nhiều chi nhánh"** | Thiếu cấp trung gian. Hiện muốn làm chuỗi 2 bệnh viện thì phải chọn: (a) 2 tenant riêng → không tổng hợp báo cáo được, bệnh nhân không xuyên viện; hoặc (b) 1 tenant + branch phẳng → không phân quyền theo bệnh viện được. **Cả 2 đều sai nghiệp vụ.** |
| **KHÔNG đủ khi 1 tenant có hàng chục chi nhánh** | Thiếu: khái niệm khu vực/vùng, quản lý vùng, UX chọn chi nhánh có tìm kiếm, điều chuyển kho liên chi nhánh, báo cáo so sánh chi nhánh, giá theo cơ sở. |
| **Frontend = 0%** | Grep toàn bộ `frontend/` không tìm thấy bất kỳ tham chiếu `branch` / `X-Branch-Id` nào. Chưa có branch switcher, chưa có màn quản lý chi nhánh. Tính năng backend hiện **không dùng được qua UI**. |
| **Chất lượng hiện tại** | QC `api-sweep-20260829.md` đã bắt lỗi 500 ở `GET /api/v1/branches/{id}/users` (đã fix trong code hiện tại). Chưa có bộ test đa chi nhánh thật (nhiều branch, nhiều user cross-branch). |
| **Rủi ro kỹ thuật phát hiện thêm** | **Trùng số migration**: tồn tại song song `9080_helpers_branch.sql` / `9080_diagnosis_primary_g06.sql`, `9081_alter_sys_branches.sql` / `9081_create_cls_order_rounds.sql`, `9082_seed_default_branch.sql` / `9082_seed_cls_round_permissions.sql`. Thứ tự chạy phụ thuộc cách sort file → **không xác định**. Phải xử lý trước khi triển khai thêm bất cứ thứ gì. |

**Khuyến nghị hướng đi**: chốt trước với user 1 câu hỏi nghiệp vụ quyết định mọi thứ còn lại — *"Sản phẩm nhắm tới mô hình nào: (A) chuỗi phòng khám 1 pháp nhân nhiều cơ sở, hay (B) tập đoàn y tế nhiều bệnh viện, mỗi bệnh viện nhiều cơ sở?"*. Nếu (A) → chỉ cần các mục P0/P1 dưới. Nếu (B) → **bắt buộc** làm F-08 (thêm cấp trung gian) và nên làm SỚM, vì càng để lâu càng đắt.

---

## 2. Hiện trạng đã verify bằng code (không chỉ tin tài liệu)

### 2.1 Đã CÓ và chạy được
| Thành phần | File thực tế |
|---|---|
| Entity `Branch`, `UserBranch`, interface `IBranchScoped` | `backend/src/ProDiabHis.Domain/Entities/Branch.cs`, `UserBranch.cs`, `Common/IBranchScoped.cs` |
| CRUD chi nhánh + gán user + set default + invariant INV-1/2/3 | `backend/src/ProDiabHis.Application/Branches/BranchHandlers.cs` |
| Middleware branch context, header `X-Branch-Id`, hỗ trợ `X-Branch-Id: all`, chặn cross-tenant, fallback tra DB khi JWT cũ | `backend/src/ProDiabHis.Api/Middlewares/BranchScopeMiddleware.cs` |
| `IBranchProvider` + helper SQL cho Dapper | `Application/Common/IBranchProvider.cs`, `Common/BranchSql.cs`, `Infrastructure/Auth/BranchProvider.cs` |
| DB: 38 bảng vận hành có `branch_id`, bảng nối `sec_user_branches`, seed branch mặc định, backfill, seed 6 permission `branch.*` | `db/migrations/9080_helpers_branch.sql` → `9087_bil_counters_rep_cache_branch_unique.sql` |
| Bệnh nhân toàn cục theo tenant (cố ý KHÔNG có `branch_id`) | Thiết kế D4, đã tuân thủ |

### 2.2 Đã có nhưng CHƯA đủ / chưa hoàn tất
- ~10 Dapper handler (cấp phát thuốc, DTQG credentials/submissions, BHYT export) **chưa lọc theo branch** (nợ tự khai trong SESSION-SUMMARY, chưa thấy bằng chứng đã trả).
- Report engine: chỉ `GetReportOptionsHandler` và descriptor `bil_cash_out` có branch. Grep `Reports/` chỉ ra **14 lần xuất hiện "branch" trên toàn module** → phần lớn báo cáo vẫn mù chi nhánh.
- `ReportDescriptor` có `GroupByKey` nhưng chưa có dimension "branch" chuẩn hoá.

### 2.3 Hoàn toàn CHƯA CÓ
- Frontend (0 tham chiếu).
- Điều chuyển kho liên chi nhánh (grep `stock_transfer` → không có file nào ngoài chính tài liệu ERD).
- Giá dịch vụ theo chi nhánh (grep `branch_prices` / `service_branch` → không có).
- Khái niệm vùng/khu vực (grep `region` / `area_id` / `khu_vuc` → **0 kết quả toàn repo**).

---

## 3. Danh sách vấn đề & đề xuất

> Format: **Tên function/tính năng** — Hiện trạng — Vấn đề khi scale — Đề xuất (Ưu tiên / Độ phức tạp)

---

### F-01. Branch Switcher & UX chọn chi nhánh trên UI
**Hiện trạng**: Không tồn tại. Backend nhận `X-Branch-Id` nhưng frontend chưa gửi header này ở bất kỳ đâu → mọi request đang chạy bằng branch mặc định của user, không đổi được.

**Vấn đề khi scale**:
- Với 3 chi nhánh: dropdown đơn giản còn chấp nhận. Với 30–50 chi nhánh: dropdown phẳng không tìm kiếm là **không dùng nổi** — lễ tân/bác sĩ luân phiên phải cuộn tìm.
- Chưa có cơ chế "ghim chi nhánh gần đây", không nhóm theo tỉnh/khu vực.
- Chưa có chỉ báo trực quan "bạn đang ở chi nhánh nào" → **rủi ro an toàn người bệnh**: kê đơn/cấp phát nhầm cơ sở.

**Đề xuất**:
1. Branch switcher trên header, luôn hiển thị tên chi nhánh hiện tại + màu badge phân biệt.
2. Combobox có **search theo tên/mã**, group theo khu vực (khi có F-08), section "Gần đây" (3 mục, lưu localStorage).
3. Chế độ "Tất cả chi nhánh" (gửi `X-Branch-Id: all`) chỉ hiện với user có `branch.cross_view`, và chỉ cho màn hình **chỉ đọc** (báo cáo, tra cứu); màn hình ghi phải chọn chi nhánh cụ thể (backend đã định nghĩa `BRANCH_REQUIRED`).
4. Interceptor axios/fetch tự gắn `X-Branch-Id` toàn cục + invalidate toàn bộ cache TanStack Query khi đổi chi nhánh (nếu quên sẽ hiển thị dữ liệu chi nhánh cũ — bug nguy hiểm).

**Ưu tiên: P0 — Độ phức tạp: Vừa** (backend đã sẵn sàng, chủ yếu là FE)

---

### F-02. Vai trò quản lý vùng / khu vực (Region Manager)
**Hiện trạng**: Phân quyền chỉ có 2 nấc — hoặc thấy **1 chi nhánh** (branch context), hoặc thấy **toàn bộ tenant** (`branch.cross_view`). Không có nấc giữa. Không có bảng/entity nào cho vùng (verify: grep `region`/`area` = 0 kết quả).

**Vấn đề khi scale**: Với 30 chi nhánh, giám đốc khu vực miền Bắc phải được xem 10 chi nhánh của mình — **không quá 1, không phải cả 30**. Hiện chỉ có cách cấp `branch.cross_view` cho họ → **rò rỉ dữ liệu doanh thu/bệnh nhân toàn hệ thống**. Đây là lỗ hổng phân quyền thật, không phải nice-to-have.

**Đề xuất**:
- Bảng `diab_his_sys_branch_groups` (vùng/khu vực) + `branches.group_id`.
- Permission mới `branch.group_view` — cross-view **giới hạn trong tập chi nhánh của user** (`AllowedBranchIds`).
- Sửa `BranchScopeMiddleware` + `IBranchProvider`: thêm trạng thái thứ 3 `ScopeMode = Single | Group | All`. Query filter dùng `branch_id IN (@allowedIds)` thay vì bỏ hẳn filter.
- Role mới `quan_ly_vung`.

> Lưu ý: nếu chọn làm F-08 (hierarchy) thì "vùng" nên là chính cấp trung gian đó, tránh làm 2 lần.

**Ưu tiên: P1 (P0 nếu user xác nhận có mô hình quản lý vùng) — Độ phức tạp: Vừa**

---

### F-03. Điều chuyển kho liên chi nhánh (Stock Transfer)
**Hiện trạng**: Chưa làm (quyết định số 2 trong SESSION-SUMMARY — hoãn có chủ đích). Verify: không tồn tại bảng/handler nào. Tồn kho đã tách theo `branch_id` trên `pha_stock` + `pha_stock_movements`.

**Vấn đề khi scale**: Với 2–3 chi nhánh, dược sĩ có thể "xuất huỷ + nhập tay" để lách. Với hàng chục chi nhánh, đây trở thành **nghiệp vụ hàng ngày**:
- Chi nhánh A sắp hết Insulin, chi nhánh B tồn dư sắp hết hạn → hiện không có đường hợp lệ để chuyển.
- Lách bằng xuất/nhập tay làm **mất truy vết lô/HSD** → vi phạm quy định quản lý dược, không đối chiếu được khi thanh tra.
- Không có báo cáo "hàng cận date toàn hệ thống" để điều phối.

**Đề xuất**:
- Bảng `pha_stock_transfers` + `pha_stock_transfer_items` (giữ nguyên `lot_no`, `expiry_date`, `from_branch_id`, `to_branch_id`).
- Luồng 2 bước bắt buộc: **Chi nhánh gửi tạo phiếu → chi nhánh nhận xác nhận** (trạng thái `DRAFT → SENT → RECEIVED / REJECTED`). Tồn kho chỉ trừ khi SENT, chỉ cộng khi RECEIVED; chênh lệch nằm ở "hàng đang đi đường".
- Đây là ngoại lệ hợp lệ đầu tiên của quy tắc branch filter → phải cho phép user chi nhánh A **nhìn** phiếu tới chi nhánh B. Cần xử lý cẩn thận ở query filter, dễ sinh lỗ hổng.
- Permission mới: `pharmacy.transfer_create`, `pharmacy.transfer_receive`.

**Ưu tiên: P1 (P0 nếu số chi nhánh > 5) — Độ phức tạp: Lớn**

---

### F-04. Báo cáo & BI theo chi nhánh / so sánh chi nhánh
**Hiện trạng**: Bảng cache report đã có `branch_id` (migration 9084, unique key sửa ở 9087). Nhưng tầng ứng dụng: toàn module `Reports/` chỉ có **14 lần xuất hiện "branch"** trên 3 file; chỉ descriptor `bil_cash_out` được lọc. Không có báo cáo nào **group theo branch**, không có màn so sánh chi nhánh.

**Vấn đề khi scale**: Chủ chuỗi/giám đốc điều hành cần đúng 3 thứ mà hiện **không có cái nào**:
1. Bảng xếp hạng chi nhánh (doanh thu, lượt khám, doanh thu/lượt, tỷ lệ tái khám).
2. Tổng hợp toàn hệ thống có breakdown theo chi nhánh, drill-down từ tổng → vùng → chi nhánh → bác sĩ.
3. So sánh cùng kỳ giữa các chi nhánh.

Đồng thời, các báo cáo **chưa lọc branch** đang là **rò rỉ dữ liệu tiềm ẩn**: user chi nhánh A có thể thấy số liệu chi nhánh B.

**Đề xuất**:
- **Trước tiên (P0, coi là bug bảo mật)**: rà toàn bộ report descriptor + ~10 Dapper handler còn nợ, bổ sung `BranchSql.Condition`. Viết test tự động chặn regression (test: user branch A gọi mọi endpoint list → không có dòng nào của branch B).
- Thêm dimension chuẩn `branch` vào `ReportDescriptor.GroupByKey`; mọi báo cáo hỗ trợ tham số `branchIds[]` (nhiều chi nhánh) chứ không chỉ 1.
- Dashboard mới "Tổng quan chuỗi": bảng xếp hạng + heatmap + drill-down. Chỉ hiện với `branch.cross_view` / `branch.group_view`.
- Lưu ý kỹ thuật cho architect: với hàng chục chi nhánh × nhiều ngày, cache report phải pre-aggregate theo `(tenant, branch, ngày)` — query realtime sẽ chậm.

**Ưu tiên: P0 (phần vá lọc branch) / P1 (phần dashboard chuỗi) — Độ phức tạp: Vừa → Lớn**

---

### F-05. Giá dịch vụ & ký hiệu hoá đơn theo cơ sở
**Hiện trạng**: Bảng giá `bil_services`, `bil_service_packages` và ký hiệu HĐĐT **dùng chung toàn tenant** (quyết định số 3 trong SESSION-SUMMARY). Thiết kế ERD đã dự phòng đúng hướng: nếu cần thì tạo bảng phụ `bil_service_branch_prices`, KHÔNG thêm `branch_id` vào bảng giá gốc.

**Vấn đề khi scale**:
- Chi nhánh Quận 1 (TP.HCM) và chi nhánh tỉnh **chắc chắn không thể cùng giá khám** — khác mặt bằng, khác thu nhập dân cư. Đây là thực tế thị trường, không phải giả định.
- Nếu là mô hình nhiều bệnh viện khác pháp nhân: **bắt buộc** khác ký hiệu hoá đơn, khác mã số thuế, khác dải số hoá đơn — hiện dùng chung là **sai quy định thuế**.
- Bộ đếm số phiếu `bil_counters` đã tách theo branch (tốt), nhưng ký hiệu hoá đơn thì chưa.

**Đề xuất**:
- Bảng `diab_his_bil_service_branch_prices (tenant_id, branch_id, service_id, price, effective_from, effective_to)`. Logic lấy giá: **override theo branch nếu có, không thì fallback giá gốc**. Đây là mô hình an toàn nhất — không phá dữ liệu cũ.
- Phải lưu **giá tại thời điểm phát sinh** vào `bil_billing_items` (nếu chưa có) — nếu chỉ join sang bảng giá thì sửa giá sẽ làm sai lệch hoá đơn lịch sử. Cần architect verify điểm này.
- Cấu hình HĐĐT (mẫu số, ký hiệu, MST) tách theo branch — **P0 nếu mô hình đa pháp nhân**, P2 nếu 1 pháp nhân.
- Quyền `service.price_override` riêng, chỉ Admin chuỗi được sửa giá chi nhánh.

**Ưu tiên: P1 (giá) / P0-hoặc-P2 (HĐĐT, tuỳ mô hình pháp nhân — CẦN USER CHỐT) — Độ phức tạp: Vừa**

---

### F-06. Bệnh nhân xuyên chi nhánh — UX & quyền riêng tư
**Hiện trạng**: Thiết kế đúng (D4: bệnh nhân toàn cục theo tenant, không có `branch_id`). Mã BN unique theo tenant. Đã có dedup theo CCCD (migration 9088).

**Vấn đề khi scale**:
- **Chưa hiển thị "bệnh nhân này từng khám ở chi nhánh nào"**. Với 30 chi nhánh, bác sĩ mở hồ sơ thấy 50 lượt khám lẫn lộn, không biết cái nào của cơ sở mình → cần cột/filter chi nhánh trong lịch sử khám.
- **Vấn đề quyền riêng tư chưa được đặt ra**: hiện MỌI user thấy MỌI bệnh nhân của tenant. Với chuỗi lớn, một lễ tân ở chi nhánh nhỏ tra được hồ sơ của bất kỳ ai trong toàn hệ thống — rủi ro pháp lý (dữ liệu sức khoẻ là dữ liệu cá nhân nhạy cảm theo NĐ 13/2023). Cần "break-glass": tra cứu ngoài chi nhánh thì được, nhưng **ghi audit log bắt buộc** và có thể yêu cầu nêu lý do.
- Chưa có màn "chuyển tuyến/chuyển cơ sở" nội bộ (bệnh nhân được giới thiệu từ chi nhánh A sang B).

**Đề xuất**:
1. Lịch sử khám: thêm cột "Chi nhánh" + filter. Header hồ sơ hiển thị "Chi nhánh gần nhất" (derive từ `MAX(encounters.started_at)`, KHÔNG thêm cột — theo đúng thiết kế đã chốt). **P0, Nhỏ.**
2. Audit log khi user xem hồ sơ bệnh nhân chưa từng khám tại chi nhánh của mình (`action = 'PATIENT_CROSS_BRANCH_VIEW'`). **P1, Nhỏ.**
3. Giới thiệu/chuyển cơ sở nội bộ có ghi nhận. **P2, Vừa.**

---

### F-07. Cấu hình & vận hành theo chi nhánh (phòng, lịch trực, danh mục, tích hợp)
**Hiện trạng**: `sys_rooms` và lịch trực đã có `branch_id`. Credential ĐTQG đã tách theo `(tenant_id, branch_id)`. Nhưng danh mục thuốc, mẫu bệnh án, CDSS dùng chung toàn tenant.

**Vấn đề khi scale**:
- Hàng chục chi nhánh → **khai báo thủ công cực nặng**: mỗi chi nhánh mới phải tạo lại phòng, quầy, ca thu ngân, gán user từ đầu. Không có "nhân bản cấu hình từ chi nhánh mẫu".
- Danh mục thuốc dùng chung: chi nhánh nhỏ không có 800 loại thuốc của chi nhánh lớn nhưng vẫn thấy hết trong dropdown kê đơn → bác sĩ kê thuốc **cơ sở không có** → đơn không cấp phát được. Cần cờ "thuốc lưu hành tại chi nhánh" (derive từ tồn kho, hoặc bảng whitelist).
- Mỗi branch có `cskcb_code` riêng và credential ĐTQG riêng → **onboarding 30 chi nhánh = 30 lần cấu hình tích hợp thủ công**, chưa có màn quản lý tập trung + trạng thái kết nối.

**Đề xuất**:
- Chức năng **"Tạo chi nhánh từ mẫu"** (clone phòng/quầy/lịch mẫu/role assignment). **P1, Vừa.**
- Kê đơn: mặc định lọc thuốc theo tồn kho chi nhánh hiện tại, có toggle "hiện tất cả". **P1, Nhỏ.**
- Màn hình "Trạng thái tích hợp theo chi nhánh" (ĐTQG/BHYT: đã cấu hình chưa, token còn hạn không, số đơn đẩy lỗi). **P1, Vừa.**
- Bulk import chi nhánh + bulk gán user vào chi nhánh (Excel). **P2, Vừa.**

---

### F-08. Cấu trúc phân cấp: Tập đoàn > Bệnh viện > Chi nhánh — VẤN ĐỀ KIẾN TRÚC LỚN NHẤT
**Hiện trạng**: Chỉ có 2 cấp `Tenant → Branch` (phẳng). Tầng `sys_clinics` đã bị **cố ý deprecate** (quyết định D2 trong ERD) — tức là cấp trung gian từng tồn tại và đã bị gỡ bỏ.

**Vấn đề khi scale**: Đây chính là câu hỏi gốc của user. Khi có "nhiều bệnh viện, mỗi bệnh viện nhiều chi nhánh", hệ thống hiện tại buộc phải chọn 1 trong 2 phương án **đều sai**:

| Phương án ép dùng | Hậu quả |
|---|---|
| Mỗi bệnh viện = 1 tenant | Không tổng hợp báo cáo tập đoàn; bệnh nhân **không** dùng chung giữa các bệnh viện; danh mục thuốc/dịch vụ phải khai lại từng tenant; user không dùng 1 tài khoản cho nhiều bệnh viện |
| Tất cả = 1 tenant, branch phẳng | Không phân quyền "giám đốc bệnh viện A chỉ thấy các cơ sở của A"; báo cáo không group được theo bệnh viện; giá/hoá đơn/pháp nhân buộc dùng chung → sai quy định thuế nếu khác MST |

**Đề xuất** — 3 phương án, cần user chốt:

- **PA-1 (khuyến nghị nếu chưa chắc mô hình): thêm 1 cấp `branch_group` linh hoạt.** Bảng `diab_his_sys_branch_groups(id, tenant_id, code, name, parent_id NULL, type)` + `branches.group_id`. `type` = `REGION` | `HOSPITAL`. Cho phép cây nhiều cấp mà không phá schema. Giải quyết luôn F-02 (quản lý vùng) và F-04 (báo cáo drill-down).
  → **Chi phí: Vừa. Không breaking** (cột nullable, dữ liệu cũ vẫn chạy).
- **PA-2: hồi sinh `sys_clinics` làm cấp bệnh viện.** Bảng còn nguyên trong DB. Nhược: phải revert quyết định D2, sửa lại thiết kế đã chốt, dữ liệu legacy lẫn lộn.
  → **Chi phí: Vừa. Rủi ro nhầm lẫn cao.**
- **PA-3: giữ nguyên 2 cấp, mỗi bệnh viện 1 tenant, thêm khái niệm "Tenant Group" phía trên.** Phù hợp nếu các bệnh viện **khác pháp nhân, khác MST, dữ liệu bệnh nhân KHÔNG được dùng chung** (đúng luật hơn về bảo vệ dữ liệu). Nhược: phức tạp nhất ở tầng auth (user đa tenant), báo cáo phải aggregate cross-tenant.
  → **Chi phí: Lớn.**

**Ưu tiên: P0 nếu user xác nhận mô hình đa bệnh viện; P1 nếu chỉ là chuỗi phòng khám — Độ phức tạp: Vừa (PA-1) → Lớn (PA-3)**

> **Lời khuyên PO**: dù chọn gì cũng nên làm **PA-1 sớm**, vì nó rẻ khi dữ liệu còn ít và giải quyết được 3 vấn đề cùng lúc (F-02, F-04, F-08). Càng nhiều chi nhánh đi vào vận hành, chi phí chèn 1 cấp càng tăng theo cấp số.

---

### F-09. Nợ kỹ thuật chặn go-live đa chi nhánh
**Hiện trạng & vấn đề**:

| # | Vấn đề | Mức |
|---|---|---|
| a | **Trùng số migration `9080`/`9081`/`9082`** (2 bộ file khác nhau cùng số). Thứ tự chạy không xác định → có thể `add_branch_col` được gọi trước khi helper được tạo → migration fail hoặc bỏ sót cột trên môi trường mới. | **P0, Nhỏ** — phải đánh số lại ngay |
| b | ~10 Dapper handler chưa lọc branch (dispensing, DTQG, BHYT export) → **rò rỉ dữ liệu chéo chi nhánh** | P0, Vừa |
| c | Query filter còn điều khoản `branch_id IS NULL` = "luôn thấy" (giai đoạn migrate). Nếu quên gỡ sau khi backfill, mọi bản ghi bug NULL sẽ **rò rỉ sang mọi chi nhánh** | P1, Nhỏ (cần task có deadline rõ) |
| d | Chưa có test đa chi nhánh thật: QC mới test 1 tenant / 1 branch (`branch_id=1`). Chưa có kịch bản 2+ chi nhánh, user cross-branch, đổi chi nhánh giữa chừng | P0, Vừa |
| e | JWT nhồi `branch_ids` dạng CSV. Với user được gán 40 chi nhánh → chuỗi dài, cộng permissions → **nguy cơ lặp lại bug token > 4KB đã từng xảy ra** với super admin | P1, Nhỏ — nên bỏ `branch_ids` khỏi JWT khi user có cross_view, hoặc tra DB/Redis |
| f | Chưa chạy migration `9080`-`9098` thật 2 lần liên tiếp để verify idempotency (tự khai trong SESSION-SUMMARY) | P0, Nhỏ |

---

## 4. Câu hỏi cần user quyết định (chặn bước tiếp theo)

1. **Mô hình đích là gì?** (A) chuỗi phòng khám 1 pháp nhân nhiều cơ sở / (B) tập đoàn nhiều bệnh viện, mỗi bệnh viện nhiều cơ sở / (C) cả hai. → quyết định F-08.
2. **Quy mô tối đa cần hỗ trợ**: bao nhiêu chi nhánh trên 1 tenant? (≤5 / 5–20 / >20) → quyết định độ ưu tiên F-01, F-03, F-04.
3. **Các cơ sở có khác pháp nhân/MST không?** → quyết định F-05 (HĐĐT) là P0 hay P2.
4. **Có vai trò "giám đốc khu vực" trong tổ chức thật không?** → quyết định F-02.
5. **Giá dịch vụ có khác nhau giữa các cơ sở không?** → quyết định F-05.
6. **Dược có nhu cầu điều chuyển thuốc giữa cơ sở không?** → quyết định F-03.

---

## 5. Đề xuất thứ tự triển khai (nếu user chưa trả lời được ngay)

| Đợt | Nội dung | Lý do |
|---|---|---|
| **Đợt 0 (làm ngay, không cần chờ quyết định)** | F-09a (đánh số lại migration), F-09b (vá lọc branch), F-09d (test đa chi nhánh), F-09f (chạy migration thật) | Đây là **bug/rò rỉ dữ liệu**, không phải tính năng mới. Không phụ thuộc mô hình đích. |
| **Đợt 1** | F-01 (branch switcher + màn quản lý chi nhánh), F-06.1 (cột chi nhánh trong lịch sử khám) | Không có UI thì toàn bộ backend đã làm là vô dụng |
| **Đợt 2** | F-08 PA-1 (thêm cấp `branch_group`) + F-02 (quản lý vùng) + F-04 (dashboard chuỗi) | Làm gộp 1 lần, rẻ hơn tách lẻ |
| **Đợt 3** | F-03 (điều chuyển kho), F-05 (giá theo cơ sở), F-07 (clone cấu hình) | Nghiệp vụ nâng cao, chỉ cấp thiết khi số chi nhánh thực sự tăng |

---

*Tài liệu đánh giá nhanh — mọi con số/nhận định về code đã được verify trực tiếp trên `develop` ngày 2026-08-29. Các mục có ghi "cần architect verify" chưa được kiểm chứng sâu.*
