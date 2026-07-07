# 単体テスト仕様書 (UTC) — **Nhóm thuốc** (Drug Categories)

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: BE `DrugHandlers.cs:337-377`/`DrugDtos.cs:46` · DB `0036_drug_master_extensions.sql:45`.

| Mục | Nội dung |
|---|---|
| 機能ID | `DRUGCAT-CRUD-001` |
| Route FE | (⚠️ **KHÔNG có form FE**) |
| API base | `GET/POST /api/v1/drugs/categories` |
| Bảng DB | `diab_his_pha_drug_categories` (PK `id` CHAR(36)) · UNIQUE `(tenant_id, code)` |
| Permission | Xem `drug.read` · Ghi `drug.write` |
| ⚠️ Tình trạng | Backend **chỉ có List + Create** (KHÔNG Update/Delete); **không có FE form**. UTC test ở **tầng API**, phạm vi hẹp. |

## 1. Field matrix (3 tầng)

| Field | Nhãn | Control FE | BE validator | DB: type/null/default | GAP |
|---|---|---|---|---|---|
| code | Mã nhóm | ❌ ko có | ❌ ko validator | VARCHAR(50) NOT NULL, UNIQUE | |
| name | Tên nhóm | ❌ | ❌ | VARCHAR(255) NOT NULL | |
| parent_id | Nhóm cha | ❌ | ❌ | CHAR(36) NULL (self-ref) | phân cấp |

**Bắt buộc (DB NOT NULL):** `code`, `name` (+ tenant_id). `parent_id` optional.

## 2. Test cases (tầng API)

| No | 中項目 | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| A01 | Gap FE | UI | Tìm form tạo nhóm thuốc | ⚠️ **Không có** UI (endpoint chỉ tham chiếu khi tạo thuốc) | | **Defect#2** |
| C01 | code rỗng | Mandatory | POST `{code:"",name:"N"}` | Kỳ vọng 400; thực tế lưu (ko validator) | | **Defect#3** |
| C02 | name rỗng | Mandatory | `{code:"C1",name:""}` | Kỳ vọng chặn | | |
| C03 | code > 50 | 境界値 | 51 ký tự | Kỳ vọng chặn; lỗi DB | | |
| D01 | Mã trùng | Unique | POST trùng code | ⚠️ Kỳ vọng 409; thực tế **lỗi DB thô** (chỉ dựa UNIQUE key) | | **Defect#3** |
| E01 | Insert | Ghi DB | POST hợp lệ | 1 row `diab_his_pha_drug_categories`; id CHAR(36); tenant JWT | | |
| E02 | parent_id | Phân cấp | POST kèm parent_id hợp lệ | Lưu quan hệ cha-con | | |
| E03 | audit | Ghi | Sau E01 | ⚠️ created_by/updated_by **không** được set (dù cột tồn tại) | | **Defect#4** |
| F01 | List | Read | GET `/drugs/categories` | Nhóm mới xuất hiện | | |
| G01 | Update/Delete | Thiếu chức năng | PUT/DELETE nhóm | ⚠️ **Không có endpoint** — không sửa/xóa được | | **Defect#1** |
| I01 | Thiếu quyền | Authz | POST ko `drug.write` | 403 | | |
| I02 | Cách ly tenant | Multi-tenant | GET nhóm tenant khác | Ko thấy | | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | Thiếu Update/Delete ở backend — chỉ List+Create; soft-delete `deleted_at` có nhưng không dùng được qua API | `DrugHandlers.cs:337-377` |
| #2 | Cao | Không có FE form tạo nhóm thuốc — endpoint tồn tại nhưng không màn hình nào gọi | `DrugsPageClient.tsx` (grep `categor`=0) |
| #3 | TB | Không validate code trùng ở app-layer (lỗi DB thô); không chặn rỗng/maxLen | |
| #4 | Thấp | Create không insert `created_by`/`updated_by` dù cột tồn tại | `9012` |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/C | 4 | | | |
| D/E/F/G/I | 8 | | | |
| **TỔNG** | **12** | | | |

---
> **Không cần UTC riêng** (đã xác nhận là enum cứng / config / read-only): **Nhóm dịch vụ** (mảng cứng 6 giá trị), **DTQG** (config token 1 bản ghi/tenant), **E-Invoice** (mock FE, chưa có backend lưu cấu hình), **Notifications/VAPID** (config 1 bản ghi/tenant). Nếu cần, có thể viết "UTC config" riêng cho DTQG & VAPID (upsert + test connection) — báo nếu bạn muốn.
