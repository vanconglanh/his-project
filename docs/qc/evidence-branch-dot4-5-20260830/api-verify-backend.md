# Evidence API Đợt 4+5 đa chi nhánh — verify LIVE trên stack local (2026-08-30 10:59)

Backend port 5000 (image rebuild sau commit Đợt 4+5), MySQL thật, JWT admin (qc.admin@prodiab.test).

## GET /dashboard/branch-ranking?from=2026-08-01&to=2026-08-30
```json
{"data":[{"branch_id":1,"branch_name":"Phòng khám Đái tháo đường DiaBetis HCM","revenue":983425.00,"encounter_count":33,"revenue_per_encounter":29800.76,"new_patient_count":22,"cancel_rate":0,"pct_change_revenue":2358.56},{"branch_id":2,"branch_name":"Chi nhánh Quận 7 (test UTE)","revenue":0.00,"encounter_count":0,"revenue_per_encounter":0,"new_patient_count":0,"cancel_rate":0,"pct_change_revenue":null}],"meta":{"included_branch_count":2,"total_branch_count":2,"included_branch_names":["Phòng khám Đái tháo đường DiaBetis HCM","Chi nhánh Quận 7 (test UTE)"]}}
```
## GET /dashboard/branch/1/detail (drill-down bác sĩ)
```json
{"data":{"branch_id":1,"branch_name":"Phòng khám Đái tháo đường DiaBetis HCM","doctors":[{"doctor_id":"a0000000-0000-0000-0000-000000000002","doctor_name":"BS. Nguyễn Văn An","revenue":910000.00,"encounter_count":19,"revenue_per_encounter":47894.74},{"doctor_id":"e210a28b-062d-4d90-98f9-693936cbcc5d","doctor_name":"BS. Test Demo","revenue":4200.00,"encounter_count":1,"revenue_per_encounter":4200.00},{"doctor_id":"14ab91a9-a65d-4279-8886-5c331e925c55","doctor_name":"QC Admin Test","revenue":0.00,"encounter_count":2,"revenue_per_encounter":0.00}]}}
```
## GET /branches/bhyt-compliance (BR-107)
```json
{"data":[{"branch_id":2,"name":"Chi nhánh Quận 7 (test UTE)","has_cskcb":false,"bhyt_enabled":false,"bhyt_contract_valid":false,"dtqg_connected":false,"dtqg_token_valid":false,"last_bhyt_export_period":null},{"branch_id":1,"name":"Phòng khám Đái tháo đường DiaBetis HCM","has_cskcb":true,"bhyt_enabled":false,"bhyt_contract_valid":false,"dtqg_connected":false,"dtqg_token_valid":false,"last_bhyt_export_period":null}]}
```
## GET /branches/1/readiness (checklist go-live BR-112)
```json
{"data":{"branch_id":1,"all_passed":false,"items":[{"key":"room_exam","label":"Co it nhat 1 phong kham (EXAM)","passed":true,"detail":"Da co 2 phong kham"},{"key":"warehouse","label":"Co it nhat 1 kho thuoc","passed":true,"detail":"Da co 2 kho"},{"key":"staff","label":"Co it nhat 1 bac si va 1 le tan duoc gan vao chi nhanh","passed":true,"detail":"Bac si: 2, le tan: 1"},{"key":"schedule","label":"Co it nhat 1 ca truc trong 7 ngay toi","passed":false,"detail":"Chua co lich truc nao trong 7 ngay toi"},{"key":"counter","label":"Da co bo dem so phieu","passed":true,"detail":"Da co 3 bo dem"},{"key":"einvoice","label":"Hoa don dien tu","passed":true,"detail":"Khong ap dung — bo theo quyet dinh Q3 (khong lam HDDT theo Facility)"}]}}
```
## GET /inter-branch-debts (BR-85 ledger)
```json
{"data":[],"meta":{"page":1,"page_size":20,"total":0}}
```

## BR-85 tra no cheo chi nhanh — verify E2E (LIVE, sau fix IgnoreQueryFilters)

Kich ban: admin (S3 cross_view) thu 10.000d tai CN2 (X-Branch-Id:2) cho hoa don cua CN1.
```
Truoc:  HD b0000001..010  branch=1  balance=50000
POST /api/v1/payments  (X-Branch-Id:2, body snake_case: billing_id/amount/method)
  -> payment.id=caf724f5..., payment.branch_id=2, status=COMPLETED
Sau:    HD balance=40000, paid_amount=10000  (khoan phai thu CN1 giam)
But toan cong no noi bo sinh dung:
  debtor_branch_id=2 (CN thu tien) / creditor_branch_id=1 (CN phat sinh HD)
  amount=10000  source_type=CROSS_BRANCH_PAYMENT  status=OPEN
Doanh thu CN1 KHONG doi (ghi nhan theo billing.branch_id=1, BR-86).
```

> LUU Y CASING: API request body dung snake_case (JsonNamingPolicy.SnakeCaseLower)
> — vd billing_id, source_branch_id. FE da dung dung snake_case.

## BR-112 activate chan + BR-111 clone + BR-29 referral — verify E2E (LIVE)
```
POST /branches/1/activate -> 400 BRANCH_NOT_READY, failed_items=[schedule] (thieu ca truc 7 ngay)
POST /internal-referrals (CN1->CN2) -> tao OK, resolve ten BN + ten 2 chi nhanh
GET  /internal-referrals/incoming (X-Branch-Id:2) -> thay referral CN1->CN2
POST /branches/1/clone {code:CN-CLONE-TEST} -> CN moi id=4 status=DRAFT cskcb_code=NULL
  copy: rooms=4, warehouses=2, counters=3 (code room/warehouse suffix -B4 tranh trung unique tenant)
  KHONG copy: staff=0 (nhan su), cskcb rong (AC-8.1.1)
```
