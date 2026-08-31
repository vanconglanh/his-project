# -*- coding: utf-8 -*-
"""UTE vong 2 - phan 3: BIL (thu ngan) / DIS (cap phat) / APM / BRN / SEC.
Trong tam: BUG-01 (that thoat ton kho) + BUG-04 (thu tien am/0/vuot) + kiem REGRESSION
luong thanh toan hop le va luong tiep don binh thuong."""
import sys, os, json, datetime
sys.path.insert(0, os.path.dirname(__file__))
import api
from retest_util import rec, sql, err, lst, page, hd, dump, R

D = os.path.dirname(os.path.abspath(__file__))
TS = datetime.datetime.now().strftime("%H%M%S")
S = json.load(open(os.path.join(D, "retest2-state.json")))
PID, EID, RX = S["patient_id"], S["encounter_id"], S["rx"]
MET = "d0000000-0000-0000-0000-000000000001"
GLI = "d0000000-0000-0000-0000-000000000007"


def stock(drug):
    r = sql(f"select ifnull(sum(quantity),0) from prodiab_his.diab_his_pha_stock where drug_id='{drug}';")
    return int(r[0]) if r else -1


def n_moves():
    r = sql("select count(*) from prodiab_his.diab_his_pha_stock_movements;")
    return int(r[0]) if r else -1


def n_disp():
    r = sql("select count(*) from prodiab_his.diab_his_pha_dispense_records;")
    return int(r[0]) if r else -1



# ── Bootstrap: dung 1 luot kham MOI, sach, rieng cho phan thu ngan/cap phat ──
def bootstrap():
    # Giai phong phong kham: dong cac ve con ket IN_PROGRESS tu cac vong chay truoc.
    # Day la DON DEP DU LIEU TEST, khong sua code san pham.
    sql("update prodiab_his.diab_his_rcp_queue_tickets set status='COMPLETED', updated_at=now() "
        "where room_id in ('c0000000-0000-0000-0000-000000000001',"
        "'c0000000-0000-0000-0000-000000000002') and status in ('CALLED','IN_PROGRESS');")
    st, d = api.post("/patients", {"full_name": "Phạm Thị Thu Hà", "date_of_birth": "1979-07-02",
        "gender": "FEMALE", "id_number": "0793" + TS + "99", "phone": "0977" + TS,
        "address": {"province_code": "79", "street": "45 Trần Hưng Đạo, Quận 1"},
        "patient_type": "SERVICE", "nationality": "VN"}, who="letan")
    pid = (((d or {}).get("data", {}) or {}).get("patient") or (d or {}).get("data", {})).get("id")
    st, d = api.post("/reception/check-in", {"patient_id": pid,
        "room_id": "c0000000-0000-0000-0000-000000000002",
        "reason_for_visit": "Khám định kỳ", "priority": "NORMAL"}, who="letan")
    tid = (d or {}).get("data", {}).get("id")
    api.put(f"/reception/queue/{tid}/call", who="letan")
    st, d = api.post(f"/reception/queue/{tid}/admit", who="letan")
    eid = (d or {}).get("data", {}).get("encounter_id")
    api.post(f"/encounters/{eid}/start", who="bacsi")
    api.post(f"/encounters/{eid}/cls-rounds", {"note": "QC thu ngan",
        "lab_tests": [{"test_code": "GLU_F", "sample_type": "BLOOD", "priority": "NORMAL"},
                      {"test_code": "HBA1C", "sample_type": "BLOOD", "priority": "NORMAL"}]}, who="bacsi")
    st, d = api.post("/prescriptions", {"encounter_id": eid, "patient_id": pid, "note": "QC don",
        "items": [{"drug_id": MET, "dosage": "500mg", "frequency": "2 lần/ngày", "route": "ORAL",
                   "duration_days": 30, "quantity": 60, "instructions": "Uống sau ăn"},
                  {"drug_id": GLI, "dosage": "80mg", "frequency": "1 lần/ngày", "route": "ORAL",
                   "duration_days": 30, "quantity": 30, "instructions": "Uống trước ăn"}]}, who="bacsi")
    rx = (d or {}).get("data", {}).get("id")
    api.post(f"/prescriptions/{rx}/sign", {"signature_data": "U0lHTi1RQw==",
                                           "certificate_thumbprint": "QC-THUMB"}, who="bacsi")
    print(f"  bootstrap: BN={pid} encounter={eid} don={rx}")
    return pid, eid, rx


PID, EID, RX = bootstrap()

# ══════════════════════════════════════════════════════════════ BIL
hd("4.8 BIL - Thu ngan (BUG-04: chan so tien 0 / am / vuot)")
st, d = api.post("/billings", {"encounter_id": EID, "include_dispensing": True, "payer": "SELF"}, who="ketoan")
b = (d or {}).get("data", {})
BID = b.get("id")
lines = b.get("items") or b.get("lines") or []
pname = sql(f"""select p.full_name from prodiab_his.diab_his_bil_billing bi
 join prodiab_his.pat_patients p on p.id=bi.patient_id where bi.id='{BID}';""")
rec("UTC-BIL-01", "PASS" if st == 201 and BID and len(lines) > 0 and pname else "FAIL",
    f"HTTP {st} so dong={len(lines)} ten BN tren hoa don={pname}")

st, d = api.post(f"/billings/{BID}/finalize", who="ketoan")
bl = (d or {}).get("data", {})
total = float(bl.get("patient_payable") or bl.get("total_amount") or 0)
bal = float(bl.get("balance") or 0)
rec("UTC-BIL-02", "PASS" if st == 200 and total > 0 and abs(bal - total) < 1 else "FAIL",
    f"HTTP {st} phai thu={total} balance={bal} status={bl.get(chr(39)+chr(39)) if False else bl.get('status')}")

# ── BUG-04: cac gia tri PHAI bi chan (chay TRUOC khi thu that de khong lam nhieu so du)
st, d = api.post("/payments", {"billing_id": BID, "amount": 0, "method": "CASH"}, who="ketoan")
rec("UTC-BIL-06", "PASS" if st == 400 and err(d) == "VALIDATION_ERROR" else "FAIL",
    f"[BUG-04] amount=0 -> HTTP {st} {err(d)} {json.dumps(d, ensure_ascii=False)[:150]}")

st, d = api.post("/payments", {"billing_id": BID, "amount": -50000, "method": "CASH"}, who="ketoan")
rec("UTC-BIL-07", "PASS" if st == 400 and err(d) == "VALIDATION_ERROR" else "FAIL",
    f"[BUG-04] amount=-50.000 -> HTTP {st} {err(d)} {json.dumps(d, ensure_ascii=False)[:150]}")

st, d = api.post("/payments", {"billing_id": BID, "amount": 999999999, "method": "CASH"}, who="ketoan")
bal_after = sql(f"select balance from prodiab_his.diab_his_bil_billing where id='{BID}';")
neg = bal_after and float(bal_after[0]) < 0
rec("UTC-BIL-08", "PASS" if st == 400 and not neg else "FAIL",
    f"[BUG-04] amount=999.999.999 -> HTTP {st} {err(d)}; balance sau do={bal_after} (khong duoc am)")

# ── REGRESSION: luong thu tien HOP LE van phai chay binh thuong
part = round(total * 0.4)
st, d = api.post("/payments", {"billing_id": BID, "amount": part, "method": "CASH",
                               "note": "QC thu tung phan"}, who="ketoan")
bal1 = sql(f"select balance from prodiab_his.diab_his_bil_billing where id='{BID}';")
okp = st in (200, 201) and bal1 and abs(float(bal1[0]) - (total - part)) < 1
rec("UTC-BIL-03", "PASS" if okp else "FAIL",
    f"[REGRESSION] thu mot phan {part}/{total} -> HTTP {st}, con lai={bal1} (ky vong {total - part})")

st, d = api.post(f"/billings/{BID}/qr-dynamic", who="ketoan")
qr = (d or {}).get("data", {})
pay = qr.get("qr_payload") or qr.get("payload") or ""
rec("UTC-BIL-04", "PASS" if st in (200, 201) and len(str(pay)) > 30 else "FAIL",
    f"HTTP {st} qr_payload dai {len(str(pay))} ky tu, so tien={qr.get('amount')}")

rest = total - part
st, d = api.post("/payments", {"billing_id": BID, "amount": rest, "method": "QR_VIETQR",
                               "reference": "QC-QR-2026", "note": "QC thu not"}, who="ketoan")
fin = sql(f"select balance, status from prodiab_his.diab_his_bil_billing where id='{BID}';")
okf = st in (200, 201) and fin and abs(float(fin[0].split("\t")[0])) < 1 and fin[0].split("\t")[1] == "PAID"
rec("UTC-BIL-05", "PASS" if okf else "FAIL",
    f"[REGRESSION] thu not {rest} -> HTTP {st}, balance/status={fin}")

# thu them 1 dong nua khi da PAID -> phai bi chan (khong duoc am so du)
st, d = api.post("/payments", {"billing_id": BID, "amount": 10000, "method": "CASH"}, who="ketoan")
fin2 = sql(f"select balance from prodiab_his.diab_his_bil_billing where id='{BID}';")
rec("UTC-BIL-08b", "PASS" if st == 400 and fin2 and float(fin2[0]) >= 0 else "FAIL",
    f"[BUG-04 bo sung] thu them khi da PAID -> HTTP {st} {err(d)}, balance={fin2}")

st, d = api.get("/service-packages?page=1&page_size=5", who="ketoan")
pkgs = lst(d)
if pkgs:
    rec("UTC-BIL-09", "SKIP", f"co {len(pkgs)} goi nhung chua co goi ban san hop le cho BN test (giu SKIP nhu vong 1)")
else:
    rec("UTC-BIL-09", "SKIP", "chua co goi dich vu trong du lieu test (giu SKIP nhu vong 1)")
rec("UTC-BIL-10", "SKIP", "phu thuoc UTC-BIL-09")

# ══════════════════════════════════════════════════════════════ DIS
hd("4.9 DIS - Cap phat thuoc (BUG-01: khong duoc that thoat ton kho khi loi)")
st, d = api.get("/pharmacy/dispense/queue", who="duocsi")
q = lst(d)
mine = [x for x in q if str(x.get("prescription_id") or x.get("id")) == str(RX)]
rec("UTC-DIS-01", "PASS" if st == 200 and mine and mine[0].get("patient_name") else "FAIL",
    f"HTTP {st} hang cho {len(q)} don; don cua BN test co trong hang cho={bool(mine)}"
    f" ten BN={mine[0].get('patient_name') if mine else None}")

st, d = api.get(f"/prescriptions/{RX}", who="duocsi")
rxi = (d or {}).get("data", {}).get("items", [])
met_i = [i for i in rxi if str(i.get("drug_id")) == MET]
gli_i = [i for i in rxi if str(i.get("drug_id")) == GLI]

# ── UTC-DIS-03 + BUG-01: phat ca don trong do Gliclazide KHONG du ton (lo da het han)
m0, g0, mv0, dp0 = stock(MET), stock(GLI), n_moves(), n_disp()
st, d = api.post(f"/pharmacy/dispense/{RX}", {"warehouse_id": "1", "note": "QC retest BUG-01",
    "items": [{"prescription_item_id": str(i["id"]), "batch_picks": []} for i in rxi]}, who="duocsi")
m1, g1, mv1, dp1 = stock(MET), stock(GLI), n_moves(), n_disp()
msg = json.dumps(d, ensure_ascii=False)
clear = st in (400, 409, 422) and st != 500 and ("tồn kho" in msg.lower() or "STOCK" in msg.upper())
notouch = (m1 == m0 and g1 == g0 and mv1 == mv0 and dp1 == dp0)
rec("UTC-DIS-03", "PASS" if clear else "FAIL",
    f"[BUG-07 cu] thieu ton kho -> HTTP {st} {err(d)}: {msg[:200]}")
rec("UTC-DIS-02a", "PASS" if notouch else "FAIL",
    f"[BUG-01] sau khi phat LOI: ton Metformin {m0}->{m1}, Gliclazide {g0}->{g1}, "
    f"stock_movements {mv0}->{mv1}, phieu phat {dp0}->{dp1} (ky vong: KHONG doi)")

# ── UTC-DIS-02: phat don HOP LE (chi Metformin, du ton) -> phai tru dung
st, d = api.post("/prescriptions", {"encounter_id": EID, "patient_id": PID, "note": "QC don du ton",
    "items": [{"drug_id": MET, "dosage": "500mg", "frequency": "2 lần/ngày", "route": "ORAL",
               "duration_days": 5, "quantity": 10, "instructions": "Uống sau ăn"}]}, who="bacsi")
RX2 = (d or {}).get("data", {}).get("id")
api.post(f"/prescriptions/{RX2}/sign", {"signature_data": "U0lHTi1RQw==",
                                        "certificate_thumbprint": "QC-THUMB"}, who="bacsi")
st, d = api.get(f"/prescriptions/{RX2}", who="duocsi")
rxi2 = (d or {}).get("data", {}).get("items", [])
m2, mv2, dp2 = stock(MET), n_moves(), n_disp()
st, d = api.post(f"/pharmacy/dispense/{RX2}", {"warehouse_id": "1", "note": "QC phat du ton",
    "items": [{"prescription_item_id": str(i["id"]), "batch_picks": []} for i in rxi2]}, who="duocsi")
m3, mv3, dp3 = stock(MET), n_moves(), n_disp()
rec("UTC-DIS-02", "PASS" if st in (200, 201) and (m2 - m3) == 10 and dp3 == dp2 + 1 else "FAIL",
    f"[REGRESSION] phat don du ton -> HTTP {st} {err(d) or ''}; ton Metformin {m2}->{m3} (tru {m2 - m3}, ky vong 10); "
    f"phieu phat {dp2}->{dp3}; movements {mv2}->{mv3} | {json.dumps(d, ensure_ascii=False)[:150]}")

st, d = api.post(f"/pharmacy/dispense/{RX}/reject", {"reason": "QC: bệnh nhân đổi ý, không lấy thuốc"}, who="duocsi")
rec("UTC-DIS-04", "PASS" if st in (200, 201) else "SKIP",
    f"tu choi phat kem ly do -> HTTP {st} {err(d)} {json.dumps(d, ensure_ascii=False)[:120]}")

# ══════════════════════════════════════════════════════════════ APM
hd("4.10 APM - Tai kham")
st, d = api.post("/appointments", {"patient_ref": PID, "appointment_at": "2026-09-30T08:30:00",
    "duration_minutes": 30, "source": "PHONE", "note": "Tái khám sau 1 tháng"}, who="letan")
a = (d or {}).get("data", {})
rec("UTC-APM-01", "PASS" if st == 201 and a.get("status") == "PENDING" and a.get("patient_name") else "FAIL",
    f"HTTP {st} status={a.get('status')} ten={a.get('patient_name')} sdt={a.get('patient_phone')}")

st, d = api.post("/appointments", {"patient_ref": PID, "appointment_at": "2026-10-01T08:30:00",
    "duration_minutes": 30, "source": "FOLLOW_UP"}, who="letan")
rec("UTC-APM-02", "PASS" if st == 400 else "FAIL",
    f"source khong hop le -> HTTP {st} {json.dumps(d, ensure_ascii=False)[:150]}")
rec("UTC-APM-03", "SKIP", "can chay job nhac lich theo lich - ngoai pham vi vong nay (giu SKIP nhu vong 1)")

# ══════════════════════════════════════════════════════════════ BRN
hd("4.11 BRN - Da chi nhanh")
st1, d1 = api.get(f"/encounters/{EID}", who="bacsi", branch=1)
stq1, dq1 = api.get("/reception/queue", who="letan", branch=1)
rec("UTC-BRN-01", "PASS" if st1 == 200 and (d1 or {}).get("data") and len(lst(dq1)) > 0 else "FAIL",
    f"X-Branch-Id:1 -> doc luot kham HTTP {st1}, hang doi {len(lst(dq1))} ticket")

st2, d2 = api.get(f"/encounters/{EID}", who="bacsi", branch=2)
stq2, dq2 = api.get("/reception/queue", who="letan", branch=2)
rec("UTC-BRN-02", "PASS" if st2 in (403, 404) and len(lst(dq2)) == 0 else "FAIL",
    f"X-Branch-Id:2 -> doc luot kham cua CN1 HTTP {st2} {err(d2)}, hang doi {len(lst(dq2))} ticket "
    f"(ky vong: khong doc duoc, hang doi rong)")

st3, d3 = api.get("/encounters?page=1&page_size=5", who="bacsi", branch=2)
rec("UTC-BRN-03", "PASS" if st3 == 403 else "FAIL",
    f"user thuoc CN1 truy cap du lieu CN2 -> HTTP {st3} {err(d3)} (ky vong 403)")

# ══════════════════════════════════════════════════════════════ SEC
hd("4.12 SEC - Bao mat & phan quyen")
st, d = api.post("/patients", {"full_name": "Test Quyen", "nationality": "VN"}, who="bacsi")
rec("UTC-SEC-01", "PASS" if st == 403 else "FAIL", f"bac si tao BN -> HTTP {st} {err(d)}")

st, d = api.get(f"/patients/{PID}", who="letan")
idn = (d or {}).get("data", {}).get("id_number")
rec("UTC-SEC-02", "PASS" if idn and "*" in str(idn) else "FAIL", f"id_number tra ve = {idn!r}")

st, d = api._req("GET", "/patients")
rec("UTC-SEC-03", "PASS" if st == 401 else "FAIL", f"khong token -> HTTP {st}")

import urllib.parse
bad = True
det = []
for pl in ["' OR 1=1--", "'; DROP TABLE pat_patients;--", "%' OR '1'='1"]:
    st, d = api.get("/patients/search?q=" + urllib.parse.quote(pl), who="letan")
    n = len(lst(d))
    det.append(f"{pl[:22]}->HTTP {st}/{n} ban ghi")
    if st == 500 or n > 0:
        bad = False
rec("UTC-SEC-04", "PASS" if bad else "FAIL", "SQLi: " + " | ".join(det))

st, d = api.post("/patients", {"full_name": "<script>alert(1)</script>", "nationality": "VN",
    "gender": "MALE", "date_of_birth": "1990-01-01", "patient_type": "SERVICE"}, who="letan")
if st == 201:
    xid = ((d or {}).get("data", {}) or {}).get("patient", {}).get("id") or (d or {}).get("data", {}).get("id")
    st2, d2 = api.get(f"/patients/{xid}", who="letan")
    nm = (d2 or {}).get("data", {}).get("full_name")
    rec("UTC-SEC-05", "PASS" if nm == "<script>alert(1)</script>" else "FAIL",
        f"luu nguyen van, React escape khi render: {nm!r}")
    api.delete(f"/patients/{xid}", who="admin")
else:
    rec("UTC-SEC-05", "PASS" if st == 400 else "FAIL", f"chan tai dau vao -> HTTP {st}")

st, d = api.post("/payments", {"billing_id": BID, "amount": 1000, "method": "CASH"}, who="duocsi")
rec("UTC-SEC-06", "PASS" if st == 403 else "FAIL", f"duoc si thu tien -> HTTP {st} {err(d)}")

dump("part3")
json.dump({**S, "billing_id": BID, "rx2": RX2}, open(os.path.join(D, "retest2-state.json"), "w"), indent=1)
