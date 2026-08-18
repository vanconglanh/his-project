# Phan cong trien khai 5 hang muc cai thien — 18/08/2026

Tech Lead: Khoa. Branch: develop. Khong push, khong deploy, khong merge main.

## Pham vi duoc duyet (dung 5 hang muc)

| # | Ma | Hang muc | Role chinh | Trang thai |
|---|----|----------|-----------|-----------|
| 1 | G06 | Chan doan chinh vs kem theo + map XML 4210 | architect -> backend -> frontend | Dang thiet ke |
| 2 | G01+G02 | Dot chi dinh CLS + gate thanh toan + WAITING_CLS | architect -> backend -> frontend | Dang thiet ke |
| 3 | G03 | Khoa benh an sau ket thuc kham + addendum | architect -> backend -> frontend | Dang thiet ke |
| 4 | G05 | Dieu phoi kham (doi BS/phong, chuyen phong giua ca) | architect -> backend -> frontend | Dang thiet ke |
| 5 | UI-G1 | Gom man kham ve 1 route nhieu tab, sidebar sticky | designer -> frontend | Dang thiet ke |

## Ngoai pham vi — KHONG lam
G04 goi kham rang buoc gia (cho user chot quy tac), master-detail 2 cot, toggle thuoc trong/ngoai kho, camera AI nhan dien khuon mat, ghi am buoi kham.

## Quy uoc
- Migration: db/migrations/9080-9089 (G06/G01/G02), 9090-9099 (G03/G05). Idempotent stored-procedure pattern, MySQL 8.
- Moi bang co tenant_id, moi query filter tenant_id.
- Commit tieng Viet khong dau, toi thieu 5 commit theo tung hang muc.
