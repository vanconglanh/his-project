# -*- coding: utf-8 -*-
"""UTE vong 2 - phan 2: CLS / DOC / RX / BIL / DIS / APM / BRN / SEC."""
import sys, os, json, datetime, urllib.parse, subprocess
sys.path.insert(0, os.path.dirname(__file__))
import api
from retest_util import rec, sql, err, lst, page, hd, dump, R

D = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(D, ".."))
TS = datetime.datetime.now().strftime("%H%M%S")
S = json.load(open(os.path.join(D, "retest2-state.json")))
PID, EID = S["patient_id"], S["encounter_id"]
print("BN:", PID, "| Encounter:", EID)

LAB_PDF = os.path.join(ROOT, "docs/qc/evidence-lab-result-ocr-20260830/phieu-ket-qua-xn-test.pdf")
RAD_PDF = os.path.join(ROOT, "docs/qc/evidence-radresult-ocr-20260830/phieu-ket-qua-cdha-test.pdf")
INB_PDF = os.path.join(ROOT, "docs/qc/evidence-inbody-ocr-20260830/sample-inbody-full.pdf")
GAP3_PDF = os.path.join(ROOT, "docs/qc/evidence-full-flow-20260831/fixture-xn-gap3-ngoai-nguong.pdf")

# ══════════════════════════════════════════════════════════════ CLS
hd("4.5 CLS - Chi dinh + OCR ket qua")
st, d = api.post(f"/encounters/{EID}/cls-rounds", {
    "note": "Chỉ định thường quy tái khám ĐTĐ",
    "lab_tests": [{"test_code": "GLU_F", "sample_type": "BLOOD", "priority": "NORMAL"},
                  {"test_code": "HBA1C", "sample_type": "BLOOD", "priority": "NORMAL"}],
    "rad_orders": [{"modality": "US", "body_part": "ABDOMEN", "contrast": False,
                    "procedure_code": "US_ABD", "priority": "NORMAL"}]}, who="bacsi")
rd = (d or {}).get("data", {})
RID = rd.get("id")
tot = rd.get("total_amount")
rec("UTC-CLS-01", "PASS" if st == 201 and rd.get("status") == "OPEN" and float(tot or 0) == 335000 else "FAIL",
    f"HTTP {st} status={rd.get('status')} payment={rd.get('payment_status')} tong={tot}")

st, d = api.post(f"/cls-rounds/{RID}/submit", who="bacsi")
rec("UTC-CLS-02", "PASS" if st == 200 and (d or {}).get("data", {}).get("status") == "SUBMITTED" else "FAIL",
    f"HTTP {st} status={(d or {}).get('data', {}).get('status')}")

st, d = api.upload("/lab-results/ocr-extract", [("file", LAB_PDF)], {"encounter_id": EID}, who="ktv")
ex = (d or {}).get("data", {})
sfid = ex.get("source_file_id")
fs = ex.get("fields", [])
hba = [f for f in fs if "HBA1C" in str(f.get("test_code", "")).upper()]
rec("UTC-CLS-03", "PASS" if st == 200 and sfid and hba and abs(float(hba[0].get("value_numeric") or 0) - 8.1) < 0.01 else "FAIL",
    f"HTTP {st} source_file_id={'CO' if sfid else 'NULL'} HbA1c={hba[0].get('value_numeric') if hba else None}")

items = [{"lab_order_item_id": f.get("lab_order_item_id"), "value": str(f.get("value_numeric")),
          "value_numeric": f.get("value_numeric"), "unit": f.get("unit"), "include": True,
          "ocr_raw_value": f.get("ocr_raw_value") or str(f.get("value_numeric"))}
         for f in fs if f.get("value_numeric") is not None and f.get("lab_order_item_id")]
now = datetime.datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
st, d = api.post("/lab-results/ocr-confirm", {"performed_at": now, "items": items,
                                              "source_file_id": sfid}, who="ktv")
_e = json.dumps((d or {}).get("data", {}), ensure_ascii=False)
_blocked = "CLS_ORDER_UNPAID" in _e or err(d) == "CLS_ORDER_UNPAID"
_saved = (d or {}).get("data", {}).get("created_count") or 0
rec("UTC-CLS-04", "PASS" if _blocked and _saved == 0 else "FAIL",
    f"[cong G02] chua thanh toan ma luu KQ -> HTTP {st}, created_count={_saved}, chan={_blocked}: {_e[:160]}")

st, d = api.post(f"/cls-rounds/{RID}/pay", {"amount": 335000, "method": "CASH"}, who="ketoan")
paid = sql(f"select payment_status from prodiab_his.diab_his_cls_order_rounds where id='{RID}';")
rec("UTC-CLS-05", "PASS" if paid and paid[0] == "PAID" else "FAIL",
    f"thu tien dot CLS -> HTTP {st}, payment_status={paid}")

st, d = api.post("/lab-results/ocr-confirm", {"performed_at": now, "items": items,
                                              "source_file_id": sfid}, who="ktv")
cc = (d or {}).get("data", {}).get("created_count")
rec("UTC-CLS-06", "PASS" if st in (200, 201) and cc and cc >= 1 else "FAIL",
    f"HTTP {st} created_count={cc}")

rows = sql(f"""select test_code, value_numeric, flag, reference_range_low, reference_range_high,
 case when source_file_id is null then 'NULL' else 'CO' end, ifnull(ocr_raw_value,'NULL')
 from prodiab_his.diab_his_lab_results where encounter_id='{EID}';""")
h = [r.split("\t") for r in rows if r.split("\t")[0] == "HBA1C"]
ok7 = bool(h) and h[0][2] not in ("NORMAL", "NULL", "") and h[0][3] not in ("NULL", "")
rec("UTC-CLS-07", "PASS" if ok7 else "FAIL", f"[Bug A] HbA1c: {h[0] if h else 'khong co'}")
rec("UTC-CLS-08", "PASS" if h and h[0][5] == "CO" and h[0][6] != "NULL" else "FAIL",
    f"[GAP-8/GAP-2] source_file_id={h[0][5] if h else '?'} ocr_raw_value={h[0][6] if h else '?'}")


def flag_of(code, val):
    """Tao 1 dot CLS moi chi 1 XN, confirm gia tri val, doc flag trong DB."""
    st, d = api.post(f"/encounters/{EID}/cls-rounds", {"note": f"QC flag {code} {val}",
        "lab_tests": [{"test_code": code, "sample_type": "BLOOD", "priority": "NORMAL"}]}, who="bacsi")
    r = (d or {}).get("data", {})
    rid = r.get("id")
    api.post(f"/cls-rounds/{rid}/submit", who="bacsi")
    stp, dp = api.post(f"/cls-rounds/{rid}/pay", {"amount": r.get("total_amount") or 0,
                                                  "method": "CASH"}, who="ketoan")
    los = r.get("lab_orders") or []
    if not los:
        return None, f"round {rid} khong co lab_order"
    oid = los[0]["id"]
    st, d = api.post("/lab-results/ocr-confirm", {"performed_at": now, "items": [
        {"lab_order_item_id": oid, "value": str(val), "value_numeric": val,
         "include": True, "ocr_raw_value": str(val)}]}, who="ktv")
    body = json.dumps((d or {}).get("data", {}), ensure_ascii=False)
    if st not in (200, 201) or (d or {}).get("data", {}).get("created_count") != 1:
        return None, f"confirm HTTP {st} pay={stp} | {body[:150]}"
    f = sql(f"select flag from prodiab_his.diab_his_lab_results where lab_order_item_id='{oid}';")
    return (f[0] if f else None), f"HTTP {st}"


f9, n9 = flag_of("GLU_F", 5.9)
rec("UTC-CLS-09", "PASS" if f9 == "H" else "FAIL", f"GLU_F 5.9 -> flag={f9} ({n9})")
f10, n10 = flag_of("GLU_F", 5.0)
rec("UTC-CLS-10", "PASS" if f10 == "NORMAL" else "FAIL", f"GLU_F 5.0 -> flag={f10} ({n10})")
f11, n11 = flag_of("GLU_F", 2.0)
rec("UTC-CLS-11", "PASS" if f11 in ("L", "LL", "CRITICAL") else "FAIL", f"GLU_F 2.0 -> flag={f11} ({n11})")
f12, n12 = flag_of("CBC", 5.0)
rec("UTC-CLS-12", "PASS" if f12 == "NORMAL" or (f12 is None and "khong co" in n12) else "FAIL",
    f"CBC (khong co khoang tham chieu) -> flag={f12} ({n12})")
st, d = api.get("/lab-tests/KHONG_TON_TAI", who="ktv")
rec("UTC-CLS-13", "PASS" if st != 500 else "FAIL", f"ma XN khong ton tai -> HTTP {st}, khong 500")

if os.path.exists(GAP3_PDF):
    api.post(f"/encounters/{EID}/cls-rounds", {"note": "QC GAP-3",
        "lab_tests": [{"test_code": "HBA1C", "sample_type": "BLOOD", "priority": "NORMAL"}]}, who="bacsi")
    st, d = api.upload("/lab-results/ocr-extract", [("file", GAP3_PDF)], {"encounter_id": EID}, who="ktv")
    ff = (d or {}).get("data", {}).get("fields", [])
    oor = [f for f in ff if f.get("out_of_plausible_range")]
    rec("UTC-CLS-14", "PASS" if oor and oor[0].get("plausible_range_note") else "FAIL",
        f"[GAP-3] out_of_plausible_range={bool(oor)} note={(oor[0].get('plausible_range_note') if oor else None)}")
else:
    rec("UTC-CLS-14", "SKIP", "khong tim thay fixture GAP-3")

st, d = api.upload("/lab-results/ocr-extract", [("file", LAB_PDF)], {"encounter_id": EID}, who="ktv")
ff = (d or {}).get("data", {}).get("fields", [])
glu = [f for f in ff if str(f.get("test_code", "")).upper() == "GLU_F" and f.get("value_numeric") is not None]
rec("UTC-CLS-15", "PASS" if glu else "FAIL",
    f"[BUG-04 cu] phieu co dong 'Glucose (duong huyet)' -> doc duoc GLU_F: {glu[0].get('value_numeric') if glu else 'KHONG'}"
    f" | cac ma doc duoc: {[f.get('test_code') for f in ff]}")

st, d = api.upload("/rad-results/ocr-extract", [("file", RAD_PDF)], who="ktv")
rx_ = (d or {}).get("data", {})
has3 = all(rx_.get(k) for k in ("findings", "conclusion"))
viet = any("ộ" in str(rx_.get(k) or "") or "ầ" in str(rx_.get(k) or "") or "ả" in str(rx_.get(k) or "")
           for k in ("findings", "conclusion", "recommendations"))
rec("UTC-CLS-16", "PASS" if st == 200 and has3 else "FAIL",
    f"HTTP {st} findings={'CO' if rx_.get('findings') else 'NULL'} conclusion={'CO' if rx_.get('conclusion') else 'NULL'} giu_dau_TV={viet}")

st, d = api.get(f"/encounters/{EID}/rad-orders", who="ktv")
ro = lst(d)
if ro:
    st, d = api.post("/rad-results/ocr-confirm", {"rad_order_id": ro[0].get("id"),
        "findings": rx_.get("findings") or "Gan kích thước bình thường.",
        "impression": "Gan nhiễm mỡ độ I", "conclusion": rx_.get("conclusion") or "Gan nhiễm mỡ độ I.",
        "recommendations": rx_.get("recommendations"), "performed_at": now,
        "source_file_id": rx_.get("source_file_id"),
        "ocr_raw_text": (rx_.get("raw_text") or "")[:2000]}, who="ktv")
    rec("UTC-CLS-17", "PASS" if st in (200, 201) else "FAIL",
        f"HTTP {st} status={(d or {}).get('data', {}).get('status')}")
else:
    rec("UTC-CLS-17", "SKIP", "khong co rad_order")

# ══════════════════════════════════════════════════════════════ DOC
hd("4.6 DOC - Smart-upload tu nhan dien")
st, _h = page("http://localhost:3000/patients")
rec("UTC-DOC-01", "PASS" if st in (200, 307, 302) else "FAIL", f"man ho so BN HTTP {st} (dialog smart-upload da verify vong 1)")

files = [("files", INB_PDF), ("files", RAD_PDF), ("files", LAB_PDF)]
st, d = api.upload("/documents/smart-upload", files, {"patient_id": PID, "encounter_id": EID}, who="bacsi")
res = lst(d) or (d or {}).get("data", {}).get("results") or []
rec("UTC-DOC-02", "PASS" if st in (200, 201) and len(res) == 3 else "FAIL",
    f"HTTP {st} so ket qua rieng = {len(res)}")
def _cls(r):
    c = ((r.get("result") or {}).get("classification") or {})
    return (c.get("type"), c.get("confidence"))
kinds = {os.path.basename(r.get("file_name", "")): _cls(r) for r in res}
okk = any(v[0] == "InBody" for v in kinds.values()) and any(v[0] == "RadResult" for v in kinds.values())
rec("UTC-DOC-03", "PASS" if okk else "FAIL", f"phan loai: {kinds}")
lab = [v for k, v in kinds.items() if "xn" in k.lower()]
rec("UTC-DOC-04", "PASS" if lab and lab[0][0] == "LabResult" else "FAIL",
    f"[High cu] phieu KQ XN -> {lab[0] if lab else 'khong co'}")
rec("UTC-DOC-05", "SKIP", "chua dung 21 tep (ngoai 80/20, da ghi nhan vong 1)")
rec("UTC-DOC-06", "SKIP", "chua dung tep >20MB (ngoai 80/20, da ghi nhan vong 1)")
ZIP = os.path.join(ROOT, "docs/qc/evidence-smart-document-upload-20260830/batch-zip-3-files.zip")
if os.path.exists(ZIP):
    st, d = api.upload("/documents/smart-upload", [("files", ZIP)], {"patient_id": PID, "encounter_id": EID}, who="bacsi")
    rz = lst(d) or []
    rec("UTC-DOC-07", "PASS" if st in (200, 201) and len(rz) >= 2 else "FAIL", f"ZIP -> HTTP {st}, {len(rz)} tep")
else:
    rec("UTC-DOC-07", "SKIP", "khong tim thay fixture ZIP")

# ══════════════════════════════════════════════════════════════ RX
hd("4.7 RX - Ke don")
st, d = api.get("/drugs/search?q=Metformin", who="bacsi")
ds = lst(d)
nm = [(x.get("name") or x.get("nameVi") or x.get("name_vi") or "") for x in ds]
st2, d2 = api.get("/drugs?page=1&page_size=50", who="bacsi")
alld = lst(d2)
empty = [x for x in alld if not (x.get("name") or x.get("nameVi") or x.get("name_vi") or "").strip()]
ok1 = bool(nm) and any("Metformin" in n for n in nm) and len(empty) == 0
rec("UTC-RX-01", "PASS" if ok1 else "FAIL",
    f"[BUG-03] tim 'Metformin' -> {nm[:3]} | tong {len(alld)} thuoc, ten RONG = {len(empty)}")

MET = "d0000000-0000-0000-0000-000000000001"
GLI = "d0000000-0000-0000-0000-000000000007"
st, d = api.post("/prescriptions", {"encounter_id": EID, "patient_id": PID,
    "note": "Uống sau ăn", "items": [
        {"drug_id": MET, "dosage": "500mg", "frequency": "2 lần/ngày", "route": "ORAL",
         "duration_days": 30, "quantity": 60, "instructions": "Uống sau ăn sáng và tối"},
        {"drug_id": GLI, "dosage": "80mg", "frequency": "1 lần/ngày", "route": "ORAL",
         "duration_days": 30, "quantity": 30, "instructions": "Uống trước ăn sáng"}]}, who="bacsi")
RX = (d or {}).get("data", {}).get("id")
nit = sql(f"select count(*) from prodiab_his.diab_his_pha_prescription_items where prescription_id='{RX}';")
rec("UTC-RX-02", "PASS" if st == 201 and nit and nit[0] == "2" else "FAIL",
    f"HTTP {st} rx={RX} so dong DB={nit}")

st, d = api.get(f"/prescriptions/{RX}/ddi-check", who="bacsi")
rec("UTC-RX-03", "PASS" if st == 200 and "has_contraindicated" in json.dumps(d) else "FAIL",
    f"HTTP {st} {json.dumps(d, ensure_ascii=False)[:120]}")

st, d = api.post(f"/prescriptions/{RX}/sign", {"signature_data": "U0lHTkFUVVJFLVFDLTIwMjYwODMx",
    "certificate_thumbprint": "QC-THUMB-2026"}, who="bacsi")
rec("UTC-RX-04", "PASS" if st == 200 and (d or {}).get("data", {}).get("status") == "SIGNED" else "FAIL",
    f"HTTP {st} status={(d or {}).get('data', {}).get('status')}")

st, d = api.get(f"/prescriptions/{RX}/dtqg/status", who="bacsi")
rec("UTC-RX-05", "PASS" if st == 200 else "FAIL",
    f"[High cu] HTTP {st} {err(d)} {json.dumps(d, ensure_ascii=False)[:160]}")

st, d = api.get("/prescriptions/00000000-0000-0000-0000-000000000000/dtqg/status", who="bacsi")
rec("UTC-RX-06", "PASS" if st == 404 else "FAIL", f"don khong ton tai -> HTTP {st} {err(d)}")

st, d = api.get(f"/prescriptions/{RX}", who="bacsi")
ta = (d or {}).get("data", {}).get("total_amount")
rec("UTC-RX-07", "PASS" if ta and float(ta) > 0 else "FAIL", f"total_amount={ta} (ky vong > 0)")

dump('part2')
json.dump({**S, "rx": RX, "cls_round": RID}, open(os.path.join(D, "retest2-state.json"), "w"), indent=1)
print("\n>>> phan 2a xong")
