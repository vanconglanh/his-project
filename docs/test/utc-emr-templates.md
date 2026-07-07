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
| ⚠️ Tình trạng | **FE chưa có form Tạo/Sửa** (nút "Tạo mẫu mới" không có onClick; không có editor); **BE có CRUD** nhưng **không validator**. UTC test chủ yếu ở **tầng API**. |

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

### A — Load ban đầu
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| A01 | List | Mở `/admin/emr-templates` | Chia "Mẫu hệ thống" / "Mẫu tùy chỉnh"; badge is_system | | |
| A02 | Nút Tạo | UI gap | Bấm "Tạo mẫu mới" | ⚠️ **Kỳ vọng:** mở form. Thực tế: **không có gì xảy ra** (nút chết) | | **Defect#1** |
| A03 | Lọc | Filter | Lọc theo speciality / is_system | Danh sách lọc đúng | | |

### C — Validate (chủ yếu tầng API, do FE thiếu form)
| No | 観点 | データ (API) | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| C01 | name rỗng | POST `{name:"", content_json:{}, speciality:"GENERAL"}` | **Kỳ vọng 400**; thực tế **201** (ko validator) | | **Defect#2** |
| C02 | name > 200 | 201 ký tự | Kỳ vọng chặn; thực tế lỗi/truncate DB | | **Defect#7** |
| C03 | content_json null | `{content_json:null}` | **Kỳ vọng 400**; thực tế lưu chuỗi `"null"` | | **Defect#3** |
| C04 | speciality lạ | `speciality:"XYZ123"` | Kỳ vọng chặn enum; thực tế lưu nguyên | | **Defect#4** |
| C05 | speciality > 50 | 51 ký tự | Kỳ vọng chặn; lỗi DB | | |

### D/E — Business + DB
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| D01 | Tên trùng | Unique | Tạo 2 mẫu cùng name | ⚠️ **Cả 2 thành công** (ko unique) | | **Defect#5** |
| E01 | Insert | Ghi DB | POST hợp lệ | 1 row `diab_his_cli_emr_templates`; HTTP 201 | | |
| E02 | is_system ép false | Business | POST | DB `is_system=0`; tenant_id=JWT | | |
| E03 | content_json lưu đúng | Ghi | POST JSON Tiptap | DB `content_json` = JSON serialize đúng | | |

### F/G/H/I
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| F01 | List sau tạo | reload | Mẫu mới ở "Mẫu tùy chỉnh" | | |
| G01 | Update | PUT | Cập nhật name/content/speciality | | |
| G02 | Sửa mẫu hệ thống | Bảo vệ | PUT mẫu is_system | ⚠️ **Kỳ vọng 422/403**; thực tế **Update KHÔNG chặn** (chỉ Delete chặn) | | **Defect#6** |
| H01 | Xóa mẫu tùy chỉnh | Soft delete | DELETE | `deleted_at` set; 204 | | |
| H02 | Xóa mẫu hệ thống | Bảo vệ | DELETE mẫu is_system | 422 `TEMPLATE_SYSTEM` | | |
| I01 | Thiếu quyền | Authz | POST ko `emr_template.write` | 403 | | |
| I02 | Cách ly tenant | Multi-tenant | Mẫu tenant khác | Ko sửa/xóa (mẫu system tenant_id null dùng chung) | | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | FE thiếu toàn bộ form Tạo/Sửa: nút "Tạo mẫu mới" không có `onClick`; không editor content; không select speciality → **không tạo/sửa được qua UI** | `EmrTemplatesPageClient.tsx:27` |
| #2 | **Cao** | BE không validator → name rỗng/quá dài, speciality rác đều lưu được | `EmrHandlers.cs:447+` |
| #3 | TB | `content_json` kiểu `object` ko kiểm null → `Serialize(null)` lưu `"null"` vào cột NOT NULL | `EmrHandlers.cs:463` |
| #4 | TB | speciality không enum-check ở cả 3 tầng (chỉ COMMENT) | |
| #5 | TB | Không UNIQUE trên `name` → mẫu trùng tên không bị chặn | `0026` |
| #6 | TB | Update không chặn sửa mẫu hệ thống (chỉ Delete chặn) → PUT có thể sửa mẫu system | `EmrHandlers.cs:486` |
| #7 | Thấp | maxLen name(200)/speciality(50) không áp ở FE/BE | |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A Load | 3 | | | |
| C Validate (API) | 5 | | | |
| D/E | 4 | | | |
| F/G/H/I | 7 | | | |
| **TỔNG** | **19** | | | |
