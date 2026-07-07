# 単体テスト仕様書 (UTC) — Màn hình **Kho thuốc / Warehouse**

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: BE `WarehouseHandlers.cs`/`WarehouseDtos.cs` · DB `9026_create_pha_warehouses.sql`.

| Mục | Nội dung |
|---|---|
| 機能ID | `WH-CRUD-001` |
| Route FE | (⚠️ **KHÔNG có form FE** — chỉ dùng làm dropdown) |
| API base | `/api/v1/pharmacy/warehouses` |
| Bảng DB | `pha_warehouses` (PK `id` INT AUTO_INCREMENT) · UNIQUE `(tenant_id, code)` |
| Permission | Xem `warehouse.read` · Ghi `warehouse.write` |
| ⚠️ Tình trạng | Backend CRUD đầy đủ + hooks FE tồn tại **nhưng không nối UI**. UTC test ở **tầng API**. |

## 1. Field matrix (3 tầng)

| Field | Nhãn | Control FE | FE rule | BE validator | DB: type/null/default | GAP |
|---|---|---|---|---|---|---|
| code | Mã kho | ❌ ko có | ❌ | ❌ ko validator | VARCHAR(30) NOT NULL, UNIQUE | |
| name | Tên kho | ❌ | ❌ | ❌ | VARCHAR(255) NOT NULL | |
| type | Loại kho | ❌ | ❌ | ❌ (DTO `string` non-null nhưng ko check) | VARCHAR(20) **NULL** | ⚠️ DTO non-null vs DB null → null-ref khi map |
| address | Địa chỉ | ❌ | ❌ | ❌ | TEXT NULL | |
| manager_user_id | Thủ kho | ❌ | ❌ | ❌ | INT NULL | |
| is_active | — | — | — | — | ⚠️ **KHÔNG có cột** (dùng `deleted_at`) | |

**Bắt buộc (DB NOT NULL):** `code`, `name`, `tenant_id`.

## 2. Test cases (tầng API)

### A/C — Tạo & Validate
| No | 観点 | データ (API) | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| A01 | (Gap FE) | Mở màn quản lý kho | ⚠️ **Không có màn hình CRUD kho** (chỉ dropdown) | | **Defect#1** |
| C01 | code rỗng | POST `{code:"",name:"K"}` | **Kỳ vọng 400**; thực tế lưu "" (DB NOT NULL ko chặn chuỗi rỗng) | | **Defect#3** |
| C02 | name rỗng | `{code:"K01",name:""}` | Kỳ vọng chặn | | |
| C03 | code > 30 | 31 ký tự | Kỳ vọng chặn; thực tế lỗi DB | | **Defect#3** |
| C04 | type null | `{code,name, type:null}` | ⚠️ map response `string Type` từ cột null → **null-ref/lỗi** | | **Defect#4** |

### D/E/F — Business + DB
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| D01 | Mã trùng | POST trùng code | ⚠️ Kỳ vọng 409; thực tế lỗi DB (ko check) | |
| E01 | Insert | POST hợp lệ | 1 row `pha_warehouses`; tenant_id từ JWT; id INT | |
| F01 | Read/List | GET | Kho mới xuất hiện | |
| G01 | Update | PUT `/{id}` | Cập nhật name/type/address | |
| H01 | Xóa mềm | DELETE `/{id}` | `deleted_at` set (ko có is_active) | |
| I01 | Thiếu quyền | POST ko `warehouse.write` | 403 | |
| I02 | Cách ly tenant | GET kho tenant khác | 404/ko thấy | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | Không có form FE tạo/sửa kho dù hooks `useCreateWarehouse/...` tồn tại — chỉ dùng dropdown | `WarehouseTab.tsx` |
| #2 | Cao | Không validator BE → code/name rỗng, quá dài lọt tới DB | `WarehouseHandlers.cs` |
| #3 | TB | Không check trùng code ở app → lỗi DB thô | |
| #4 | TB | `type` DTO non-null `string` vs DB NULL → null-ref khi map response | `Handlers.cs:47` |
| #5 | Thấp | Bảng thiếu `is_active` (mềm hóa bằng `deleted_at`) — field "is_active" trong yêu cầu không có chỗ lưu | 9026 |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/C | 5 | | | |
| D/E/F/G/H/I | 7 | | | |
| **TỔNG** | **12** | | | |
