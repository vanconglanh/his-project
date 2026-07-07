# 単体テスト仕様書 (UTC) — Màn hình **ICD-10** (Tra cứu chẩn đoán)

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `Icd10PageClient.tsx` · BE `Icd10Handlers.cs`/`Icd10Controller.cs` · DB `0028_seed_icd10.sql`.

| Mục | Nội dung |
|---|---|
| 機能ID | `ICD10-LOOKUP-001` |
| Màn hình | ICD-10 (tra cứu) |
| Route FE | `/icd10` |
| API base | `/api/v1/icd10` |
| Bảng DB | `diab_his_dict_icd10` (PK `code` VARCHAR(10)) · FULLTEXT `(name_vi,name_en)` |
| Permission | `icd10.read` (grant BACSI/DIEUDUONG/ADMIN) |
| ⚠️ Loại | **READ-ONLY / TRA CỨU** — KHÔNG có Create/Update/Delete. UTC phủ **tìm kiếm + hiển thị**, không có insert/update. |

## 1. Field hiển thị (không có form nhập)

| Field hiển thị | Cột DB | Type | Null | Ghi chú |
|---|---|---|---|---|
| Mã ICD-10 | `code` | VARCHAR(10) | NOT NULL (PK) | |
| Tên tiếng Việt | `name_vi` | VARCHAR(500) | NOT NULL | |
| (name_en ẩn) | `name_en` | VARCHAR(500) | NOT NULL | dùng FULLTEXT |
| Nhóm | `category` | VARCHAR(20) | NULL | |
| Thanh toán | `is_billable` | TINYINT(1) | NOT NULL DEF 1 | badge Có/Không |

Dữ liệu: seed `0028_seed_icd10.sql` (~58 mã) + LOAD CSV thủ công (DBA). Không có cột audit.

## 2. Test cases (tra cứu)

### A — Load ban đầu
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| A01 | Load màn | Mở `/icd10` | Tiêu đề "Tra cứu ICD-10"; ô tìm kiếm; bảng rỗng/empty-state | |
| A02 | Không có nút CRUD | Read-only | Quan sát | KHÔNG có nút Tạo/Sửa/Xóa | |
| A03 | Quyền | Authz | User ko `icd10.read` | 403 / không vào được | |

### B/C — Tìm kiếm (入力チェック cho ô search)
| No | 中項目 | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| C01 | Search rỗng | Biên | q=`` (0 ký tự) | Không gọi API; hiển thị empty-state | | enabled khi `q.length>=1` |
| C02 | Search 1 ký tự | Biên min | q=`E` | Gọi API, trả kết quả (limit 50) | | |
| C03 | Tìm theo tên VN | Fulltext | `tiểu đường` | Trả các mã liên quan (E10, E11…), có dấu | | |
| C04 | Tìm theo mã | Prefix | `E11` | Trả mã bắt đầu E11 | | |
| C05 | Không kết quả | Empty | `zzzzz` | Hiển thị "không tìm thấy", không lỗi | | |
| C06 | Ký tự đặc biệt | Robustness | `E11'--` | Không lỗi (param hóa), trả rỗng/hợp lý | | J-SQLi |
| C07 | Tiếng Việt có dấu | Encoding | `đái tháo đường` | Match đúng (FULLTEXT ngram utf8mb4) | | |
| C08 | Giới hạn kết quả | Limit | từ khóa nhiều kết quả | Tối đa 50 (FE) / server cap 100 | | |

### D — Hiển thị field
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| D01 | Cột đầy đủ | Display | Xem 1 dòng kết quả | Mã, Tên VN, Nhóm, badge Thanh toán đúng | |
| D02 | is_billable | Badge | Chọn mã lá (billable=1) vs mã cha (=0) | Badge "Có" / "Không" đúng | |
| D03 | GET by code | API | GET `/icd10/{code}` mã tồn tại/không | 200 data / 404 `ICD10_NOT_FOUND` | |

## 3. Defect / lưu ý
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | Thấp | FE không truyền `type/category/billable_only` dù API hỗ trợ → chỉ test filter qua API trực tiếp | `icd10.ts:11` |
| #2 | TB | Dữ liệu seed chỉ ~58 mã; full ICD-10 phải LOAD CSV thủ công → môi trường test có thể thiếu mã | `0028:109` |
| #3 | TB | 2 migration cùng tạo bảng (`0018`/`0028`); cột `is_billable`+FULLTEXT chỉ có ở `0028` → nếu thiếu 0028, handler SELECT `is_billable` có thể lỗi | 0018/0028 |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A Load | 3 | | | |
| B/C Search | 8 | | | |
| D Hiển thị | 3 | | | |
| **TỔNG** | **14** | | | |
