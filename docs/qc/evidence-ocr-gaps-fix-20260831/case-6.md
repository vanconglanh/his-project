# CASE 6 — Migration 9190: Reference ranges được seed đúng, flag XN bất thường hoạt động

## Mô tả
Migration 9190 seed khoảng tham chiếu người lớn chuẩn cho 7 xét nghiệm thường quy Nội tiết/Tiểu đường vào bảng `diab_his_dict_lab_tests`. Trước đây các cột này là NULL → flag luôn là NORMAL. Case này kiểm tra:
1. Các reference ranges đã được seed đúng giá trị
2. Kết quả XN tạo mới với giá trị ngoài khoảng → flag được tính đúng (H/L/HH/LL)

## DB Check — Reference Ranges

```sql
SELECT code, reference_range_low, reference_range_high 
FROM diab_his_dict_lab_tests 
WHERE code IN ('GLU_F','GLU_PP','GLU_R','HBA1C','TSH','ALT','AST')
ORDER BY code;
```

Kết quả:
```
ALT    | 0.0000 | 41.0000
AST    | 0.0000 | 40.0000
GLU_F  | 3.9000 |  5.5000
GLU_PP | 3.9000 |  7.8000
GLU_R  | 3.9000 |  7.8000
HBA1C  | 4.0000 |  5.6000
TSH    | 0.4000 |  4.0000
```

Tất cả 7 xét nghiệm có reference ranges khác NULL — migration 9190 đã chạy đúng ✓

## API Check — GET /api/v1/lab-results/abnormal

```
GET http://localhost:5099/api/v1/lab-results/abnormal?severity=ALL
Authorization: Bearer <ktv.test@prodiab.test token>
```

Response HTTP 200:
```json
{
  "data": [
    {"test_code": "GLU_F", "flag": "H", "value": "5.9", "reference_range_low": 3.9, "reference_range_high": 5.5},
    ...
  ]
}
```
Total: 43 kết quả bất thường

## Kiểm tra flag tính đúng (từ CASE 3)

Lab result tạo mới với GLU_F = 5.9 mmol/L, ref range [3.9, 5.5]:
- 5.9 > 5.5 → flag = "H" (High) ✓
- Trước migration 9190: ref range = NULL → flag = NORMAL (sai, bug đã fix)

## Result: PASS

- Migration 9190 seed đúng 7 khoảng tham chiếu (tất cả khác NULL ✓)
- Endpoint /lab-results/abnormal trả về HTTP 200 với danh sách đúng
- Flag "H" được tính đúng khi giá trị vượt reference_range_high
- Chức năng cảnh báo giá trị bất thường hoạt động thực sau migration 9190 ✓
