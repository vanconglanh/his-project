# PRD — Trình tạo Báo cáo tự phục vụ (Report Builder)

> Module: **Report/BI** (mở rộng) · Mục tiêu: cho người dùng có quyền **tự tạo/sửa báo cáo** qua giao diện, KHÔNG cần lập trình viên sửa code.
> Nền tảng kế thừa: **Report Engine config-driven** đã có (`ReportDescriptor` + `GenericReportDataService` + `GenericReportPdfExporter` + Excel + màn `/reports`). 43 báo cáo hiện tại là descriptor **khai báo trong code**. PRD này bổ sung lớp **descriptor lưu trong DB, tạo qua UI**.

---

## 1. Vấn đề & Mục tiêu

**Hiện tại:** thêm/sửa báo cáo = lập trình viên viết descriptor (cột + SQL) trong `ReportRegistry.cs` rồi deploy. Người dùng chỉ chọn báo cáo có sẵn + lọc + xuất.

**Mục tiêu:** người dùng (Quản trị / power user) tự **kéo-thả tạo báo cáo mới** (chọn nguồn dữ liệu, cột, bộ lọc, gộp nhóm, sắp xếp) → lưu lại → xuất hiện ngay trong danh mục `/reports`, in PDF/xuất Excel như báo cáo hệ thống — **không cần code, không cần deploy**.

**Nguyên tắc bất di bất dịch:**
- ❌ KHÔNG cho người dùng gõ SQL tự do (rủi ro injection + rò rỉ dữ liệu chéo tenant).
- ✅ Người dùng chỉ chọn từ **danh sách trường được whitelmht** (Dataset). Backend **tự sinh SQL** an toàn, luôn ép `WHERE tenant_id = @tenantId`.

---

## 2. Ý tưởng kiến trúc cốt lõi: "Dataset" (nguồn dữ liệu an toàn)

Đây là mấu chốt để vừa linh hoạt vừa an toàn.

**Dataset** = một "khung dữ liệu" do lập trình viên định nghĩa sẵn (whitelist), mô tả:
- **Base + joins**: bảng gốc + các join cố định (vd Billing ⋈ Payments ⋈ Patient).
- **Fields**: danh sách trường cho phép dùng, mỗi trường gồm:
  - `key`, `label` (nhãn tiếng Việt)
  - `role`: **Dimension** (chiều: ngày/bác sĩ/nhóm...) hoặc **Measure** (số đo: tiền/số lượng — cộng/đếm/trung bình được)
  - `sqlExpr`: biểu thức SQL nội bộ (người dùng KHÔNG thấy/không sửa)
  - `dataType`: Text/Money/Number/Date/DateTime/Enum
  - `aggregations`: các phép gộp cho phép (SUM/COUNT/AVG/MIN/MAX)
- **Bắt buộc**: mọi dataset đã gắn sẵn điều kiện `tenant_id = @tenantId` + `deleted_at IS NULL`.

**Người dùng thao tác trên Dataset**, không đụng bảng/SQL. Backend nhận **định nghĩa báo cáo (JSON)** → sinh SQL tham số hoá:
```
SELECT <dims>, <agg(measures)>
FROM <dataset base+joins>
WHERE tenant_id=@tenantId AND <filters người dùng chọn>
GROUP BY <dims>
ORDER BY <sort>
```

**Dataset khởi đầu (Phase 1)** — bám dữ liệu đã có:
| Dataset | Nguồn | Dimension tiêu biểu | Measure tiêu biểu |
|---|---|---|---|
| Thu ngân / Thanh toán | bil_billing ⋈ payments ⋈ patient | Ngày, Người thu, Quầy, Phương thức, BN | Tổng thu, Số phiếu, Giảm, Hoàn |
| Lượt khám | enc_encounters ⋈ diagnoses ⋈ doctor ⋈ room | Ngày, Bác sĩ, Phòng, ICD-10, Giờ | Số lượt, Số BN |
| Kho dược | pha_stock ⋈ drugs | Thuốc, Lô, HSD, Nhóm | SL tồn, Giá trị tồn |
| Đơn thuốc | prescriptions ⋈ items ⋈ drugs | Thuốc, Bác sĩ, Ngày | Số lần kê, Tổng SL |

> Thêm dataset mới vẫn cần lập trình viên (1 lần), nhưng **sau đó người dùng tạo bao nhiêu báo cáo tuỳ ý** trên các dataset đó.

---

## 3. Data model (bảng mới)

```
diab_his_rep_definitions        -- báo cáo do người dùng tạo
  id CHAR(36), tenant_id INT, code VARCHAR(60) UNIQUE(tenant),
  title VARCHAR(200), dataset_key VARCHAR(50),
  definition_json JSON,         -- {columns[], filters[], groupBy[], sort[], kpis[]}
  orientation ENUM('AUTO','PORTRAIT','LANDSCAPE') DEFAULT 'AUTO',
  visibility ENUM('PRIVATE','TENANT') DEFAULT 'TENANT',  -- chỉ mình / cả phòng khám
  is_active TINYINT, created_by, created_at, updated_by, updated_at, deleted_at

diab_his_rep_definition_shares  -- (tuỳ chọn Phase 2) chia sẻ theo role/user
```

`definition_json` ví dụ:
```json
{
  "columns": [
    {"field":"paid_date","label":"Ngày","agg":null},
    {"field":"collector","label":"Người thu","agg":null},
    {"field":"amount","label":"Thực thu","agg":"SUM","isSubtotal":true}
  ],
  "filters": [{"field":"method","op":"in","value":["CASH"]}],
  "groupBy": ["collector"],
  "sort": [{"field":"amount","dir":"desc"}],
  "kpis": [{"label":"Tổng thu","field":"amount","agg":"SUM"}]
}
```

---

## 4. Backend

1. **IDatasetRegistry** (code-defined, whitelist): trả danh sách Dataset + fields. Tương tự `IReportRegistry` hiện tại.
2. **Dynamic descriptor**: `IReportRegistry.GetByCode` mở rộng → nếu code không thuộc registry code-defined thì tra `diab_his_rep_definitions` (theo tenant) → dựng `ReportDescriptor` **động** từ `definition_json`.
3. **SafeQueryBuilder**: nhận (dataset, definition) → sinh SQL tham số hoá. Chống injection tuyệt đối:
   - Field chỉ nhận từ whitelist của dataset (map sang `sqlExpr` nội bộ) — KHÔNG bao giờ nội suy chuỗi người dùng vào SQL.
   - Toán tử filter từ tập cố định (`=,<>,in,between,like,>,<`), giá trị luôn qua tham số Dapper.
   - Luôn append `tenant_id=@tenantId`.
   - Giới hạn: tối đa N cột, N filter, khoảng ngày ≤366, LIMIT an toàn.
4. **Endpoint mới** (thêm vào ReportsController, không phá cũ):
   - `GET /reports/datasets` — danh sách dataset + fields (cho builder).
   - `POST /reports/definitions` — tạo báo cáo (validate definition hợp lệ với dataset).
   - `PUT/DELETE /reports/definitions/{id}` — sửa/xoá (chỉ chủ sở hữu hoặc admin).
   - `GET /reports/definitions` — liệt kê báo cáo do người dùng tạo (của tenant).
   - `POST /reports/preview` — chạy thử definition chưa lưu (LIMIT nhỏ) để xem trước.
   - **Tái dùng 100%**: `/reports/catalog` (gộp cả code-defined + user-defined), `/reports/{code}/data`, `/reports/{code}/export`.
5. **Quyền**: `report.build` (tạo/sửa báo cáo tự phục vụ) — tách khỏi `report.read/export`. Mặc định cấp cho Admin (+ tuỳ chọn power user).

---

## 5. Frontend

1. **Màn "Trình tạo báo cáo"** (`/reports/builder`):
   - Bước 1: chọn **Dataset**.
   - Bước 2: kéo-thả/tick **Cột** (chia 2 khay Dimension | Measure), chọn phép gộp cho measure.
   - Bước 3: **Bộ lọc** (chọn trường + toán tử + giá trị; giá trị dạng picker theo kiểu dữ liệu).
   - Bước 4: **Gộp nhóm** + **Sắp xếp** + đặt **KPI**.
   - **Xem trước** (bảng preview realtime, dùng `/reports/preview`).
   - Đặt **tên** + phạm vi (Riêng tôi / Cả phòng khám) → **Lưu**.
2. **Quản lý báo cáo của tôi**: danh sách user-defined (sửa/xoá/nhân bản).
3. **Tích hợp sidebar `/reports`**: báo cáo tự tạo hiện trong nhóm **"Báo cáo của tôi / phòng khám"**; chạy/lọc/in/xuất y hệt báo cáo hệ thống (tái dùng ReportRunner đã có).

---

## 6. Bảo mật & Multi-tenant (bắt buộc)

- **Tenant isolation**: SafeQueryBuilder luôn ép `tenant_id`; `rep_definitions` cũng tenant-scoped → không thấy báo cáo tenant khác.
- **Không SQL tự do**: người dùng chỉ chọn field whitelist. Đây là ranh giới an toàn quan trọng nhất.
- **RBAC**: chỉ role có `report.build` mới tạo; báo cáo `PRIVATE` chỉ chủ thấy, `TENANT` cả phòng khám thấy (đọc cần `report.read`).
- **Giới hạn tài nguyên**: cap số cột/filter/nhóm, timeout query, LIMIT — tránh báo cáo "nặng" làm chậm DB.
- **Audit**: log tạo/sửa/xoá definition (bảng audit sẵn có).

---

## 7. Lộ trình & ước lượng

| Phase | Nội dung | Ước lượng |
|---|---|---|
| **P1 — MVP (báo cáo bảng tự tạo)** | Dataset registry (3–4 dataset) + bảng `rep_definitions` + SafeQueryBuilder + endpoint datasets/CRUD/preview + màn Builder (bảng) + tích hợp catalog + quyền `report.build`. Tái dùng grid/PDF/Excel. | **~2–3 tuần** |
| **P2 — Dashboard & biểu đồ** | Thêm loại "chart" (cột/đường/tròn — Recharts/Tremor), ghim báo cáo vào Dashboard tuỳ biến, thêm dataset. | ~2 tuần |
| **P3 — Nâng cao** | Trường tính toán (công thức an toàn), lịch gửi email/xuất định kỳ, chia sẻ theo role, drill-through. | ~2–3 tuần |

> Khuyến nghị: làm **P1 trọn vẹn** trước (đã đủ "người dùng tự tạo báo cáo" — giá trị lớn nhất). P2/P3 làm sau theo nhu cầu.

---

## 8. So sánh nhanh với Power BI (kỳ vọng đúng)

| Tiêu chí | Report Builder (kế hoạch này) | Power BI |
|---|---|---|
| Người dùng tự tạo báo cáo bảng | ✅ (trên Dataset whitelist) | ✅ (tự do hơn) |
| Kéo-thả cột/gộp/lọc | ✅ | ✅ |
| Biểu đồ/dashboard tương tác | P2 | ✅ mạnh |
| Kết nối nguồn dữ liệu tuỳ ý, data model, DAX | ❌ (giới hạn trong Dataset định sẵn) | ✅ |
| Nhúng trong app, đồng bộ RBAC + tenant sẵn có | ✅ (native) | Cần Power BI Embedded + cấu hình bảo mật |
| Chi phí license | 0 (tự xây) | Có phí |

→ Đây là **"self-service report trong tầm kiểm soát"**: linh hoạt cho người dùng nhưng an toàn tenant, không phụ thuộc license. Nếu cần sức mạnh dashboard/khám phá dữ liệu như Power BI thật → cân nhắc **hướng 3** (nhúng Metabase/Superset/Power BI Embedded) song song.

---

## 9. Việc cần chốt trước khi code

1. **Ai được `report.build`?** Chỉ Admin, hay cả Bác sĩ/Kế toán trưởng (power user)?
2. **Danh sách Dataset P1**: chốt 3–4 dataset ưu tiên (đề xuất: Thu ngân, Lượt khám, Kho, Đơn thuốc).
3. **Có cần biểu đồ ngay ở P1** hay để P2? (đề xuất: P1 chỉ bảng cho gọn.)
4. **Phạm vi chia sẻ**: chỉ Riêng tôi / Cả phòng khám ở P1, hay cần chia sẻ theo role ngay?
5. **Giới hạn tài nguyên**: cap số cột/filter, LIMIT mặc định, timeout — chốt ngưỡng an toàn.
