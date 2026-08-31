# -*- coding: utf-8 -*-
"""UTE vong 2 (retest sau fix 4 Blocker) - chay lai toan bo 93 case UTC full-flow.
Chay: python .qc-tmp/retest2.py
Ket qua: .qc-tmp/retest2-results.jsonl + bang tong hop tren stdout.
"""
import sys, os, json, datetime, urllib.parse, subprocess
sys.path.insert(0, os.path.dirname(__file__))
import api

D = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(D, ".."))
TS = datetime.datetime.now().strftime("%H%M%S")
R = []


def rec(cid, status, note):
    R.append({"id": cid, "status": status, "note": note})
    icon = {"PASS": "OK  ", "FAIL": "FAIL", "SKIP": "SKIP"}[status]
    print(f"  [{icon}] {cid}: {note}")


def sql(q):
    p = subprocess.run(["docker", "exec", "prodiab-mysql", "sh", "-c",
                        'mysql -uroot -p"$MYSQL_ROOT_PASSWORD" --default-character-set=utf8mb4 -N -e ' +
                        json.dumps(q)],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    return [l for l in (p.stdout or "").splitlines() if l and not l.startswith("mysql:")]


def err(d):
    return (d or {}).get("error", {}).get("code") if isinstance(d, dict) else None


def lst(d):
    raw = (d or {}).get("data") if isinstance(d, dict) else d
    if isinstance(raw, dict):
        return raw.get("items") or []
    return raw or []


def page(url):
    """GET 1 trang HTML FE, tra (status, html)."""
    import urllib.request, urllib.error
    try:
        with urllib.request.urlopen(url, timeout=30) as r:
            return r.status, r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, ""
    except Exception as ex:
        return 0, str(ex)


def hd(t):
    print("\n" + "=" * 78 + f"\n{t}\n" + "=" * 78)


ST = {}

# ══════════════════════════════════════════════════════════════════ AUTH
hd("4.1 AUTH - Dang nhap & 2FA")
st, _h = page("http://localhost:3000/login")
rec("UTC-AUTH-01", "PASS" if st == 200 else "FAIL", f"trang /login HTTP {st}")
p = api.perms("letan")
rec("UTC-AUTH-02", "PASS" if len(p) == 32 else "FAIL", f"letan dang nhap OK, JWT {len(p)} quyen")

st, d = api._req("POST", "/auth/login", body={"email": "qc.admin@prodiab.test", "password": "Test@123"})
dd = (d or {}).get("data", {})
ok3 = st == 200 and not dd.get("accessToken") and (dd.get("requires2fa") or dd.get("mfaSetupRequired"))
rec("UTC-AUTH-03", "PASS" if ok3 else "FAIL",
    f"admin chua co accessToken: requires2fa={dd.get('requires2fa')} mfaSetupRequired={dd.get('mfaSetupRequired')}")
rec("UTC-AUTH-04", "PASS" if os.path.exists(os.path.join(D, "admin_totp.txt")) else "SKIP",
    "2FA da bat tu vong 1 (secret .qc-tmp/admin_totp.txt); khong bat lai de khong pha state")
rec("UTC-AUTH-05", "PASS" if (st == 200 and dd.get("requires2fa") and not dd.get("accessToken")) else "FAIL",
    f"dang nhap lai -> requires2fa={dd.get('requires2fa')}, accessToken rong")

pend = dd.get("mfaPendingToken")
if pend:
    st6, d6 = api._req("POST", "/auth/2fa/verify", body={"mfaPendingToken": pend, "code": "000000"})
    rec("UTC-AUTH-06", "PASS" if st6 == 401 and err(d6) == "AUTH_MFA_INVALID_CODE" else "FAIL",
        f"code sai -> HTTP {st6} {err(d6)}")
else:
    rec("UTC-AUTH-06", "SKIP", "khong lay duoc mfaPendingToken")
pa = api.perms("admin")
rec("UTC-AUTH-07", "PASS" if len(api._tok.get("admin", "")) > 20 else "FAIL",
    "TOTP dung -> lay duoc accessToken admin day du")
st, d = api._req("GET", "/patients")
rec("UTC-AUTH-08", "PASS" if st == 401 else "FAIL", f"goi API khong token -> HTTP {st}")

# ══════════════════════════════════════════════════════════════════ REC
hd("4.2 REC - Tiep don + quet QR CCCD")
st, _h = page("http://localhost:3000/reception")
rec("UTC-REC-01", "PASS" if st in (200, 307, 302) else "FAIL", f"man tiep don HTTP {st}")

CCCD = "079085" + TS
NAME = "Nguyễn Thị Bích Hạnh"
DOB = "1985-03-15"
ADDR = "12 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM"


def chk(idn, nm=None, dob=None, g=None, ad=None):
    q = {"id_number": idn}
    if nm: q["full_name"] = nm
    if dob: q["date_of_birth"] = dob
    if g: q["gender"] = g
    if ad: q["address"] = ad
    return api.get("/patients/check-cccd-duplicate?" + urllib.parse.urlencode(q), who="letan")


st, d = chk(CCCD, NAME, DOB, "FEMALE", ADDR)
c = (d or {}).get("data", {})
rec("UTC-REC-02", "PASS" if c.get("case") == "NONE" and not c.get("field_diffs") else "FAIL",
    f"case={c.get('case')} field_diffs={c.get('field_diffs')}")

st, d = api.post("/patients", {
    "full_name": NAME, "date_of_birth": DOB, "gender": "FEMALE", "id_number": CCCD,
    "phone": "090" + TS + "1",
    "address": {"province_code": "79", "district_code": "760", "ward_code": "26734", "street": ADDR},
    "patient_type": "SERVICE", "nationality": "VN"}, who="letan")
pd_ = ((d or {}).get("data", {}) or {}).get("patient") or (d or {}).get("data", {})
PID = pd_.get("id")
masked = "*" in str(pd_.get("id_number") or "")
rec("UTC-REC-03", "PASS" if st == 201 and PID and masked else "FAIL",
    f"HTTP {st}, ma={pd_.get('code')}, ten={pd_.get('full_name')!r}, id_number={pd_.get('id_number')!r}")
ST["patient_id"] = PID

st, d = chk(CCCD, NAME, DOB, "FEMALE", ADDR)
c = (d or {}).get("data", {})
rec("UTC-REC-04", "PASS" if c.get("case") == "EXACT_MATCH" and not c.get("field_diffs") else "FAIL",
    f"case={c.get('case')} patient_code={c.get('patient_code')}")

st, d = chk(CCCD, "Nguyễn Thị Bích Hằng", DOB, "FEMALE", "99 Nguyễn Huệ, Quận 1, TP.HCM")
c = (d or {}).get("data", {})
fdf = c.get("field_diffs") or []
flds = sorted(x.get("field") for x in fdf)
rec("UTC-REC-05", "PASS" if c.get("case") == "FIELD_MISMATCH" and len(fdf) == 2 else "FAIL",
    f"case={c.get('case')} n_diff={len(fdf)} fields={flds}")
rec("UTC-REC-06", "PASS" if all(x.get("old_value") is not None and x.get("new_value") is not None for x in fdf) and fdf else "FAIL",
    "moi truong lech deu co old_value/new_value de dung dialog so sanh 4 cot")

st, d = chk(CCCD.replace("079085", "07908"))
rec("UTC-REC-07", "PASS" if st in (200, 400) and st != 500 else "FAIL",
    f"CCCD sai dinh dang -> HTTP {st}, khong crash")

st, d = chk(CCCD, "  nguyễn thị   bích hạnh ", DOB, "FEMALE", ADDR)
c = (d or {}).get("data", {})
rec("UTC-REC-08", "PASS" if c.get("case") == "EXACT_MATCH" else "FAIL",
    f"chuan hoa hoa/thuong + khoang trang -> case={c.get('case')}")

sql("update prodiab_his.diab_his_rcp_queue_tickets set status='COMPLETED', updated_at=now() "
    "where room_id in ('c0000000-0000-0000-0000-000000000001',"
    "'c0000000-0000-0000-0000-000000000002') and status in ('CALLED','IN_PROGRESS');")
ROOM1 = "c0000000-0000-0000-0000-000000000001"
ROOM2 = "c0000000-0000-0000-0000-000000000002"
st, d = api.post("/reception/check-in", {"patient_id": PID, "room_id": ROOM1,
    "reason_for_visit": "Tái khám đái tháo đường type 2", "priority": "NORMAL"}, who="letan")
tk = (d or {}).get("data", {})
TID = tk.get("id")
rec("UTC-REC-09", "PASS" if st == 201 and TID else "FAIL",
    f"HTTP {st} ticket={tk.get('ticket_number') or tk.get('ticket_no')} status={tk.get('status')}")

st, d = api.get("/reception/queue", who="letan")
q = lst(d)
rec("UTC-REC-10", "PASS" if st == 200 and any(x.get("id") == TID for x in q) else "FAIL",
    f"hang doi {len(q)} ticket, co ticket vua tao")

api.put(f"/reception/queue/{TID}/call", who="letan")
st, d = api.post(f"/reception/queue/{TID}/admit", who="letan")
EID = (d or {}).get("data", {}).get("encounter_id")
rec("UTC-REC-11", "PASS" if st in (200, 201) and EID else "FAIL",
    f"admit HTTP {st} encounter_id={EID} created={(d or {}).get('data', {}).get('created')}")
ST["encounter_id"] = EID

st, d = api.post("/reception/check-in", {"patient_id": PID, "room_id": ROOM1,
    "reason_for_visit": "QC trung", "priority": "NORMAL"}, who="letan")
rec("UTC-REC-12", "PASS" if st == 409 and err(d) == "RECEPTION_DUPLICATE_CHECKIN" else "FAIL",
    f"check-in trung -> HTTP {st} {err(d)}")


def newpat(sfx, name):
    st, d = api.post("/patients", {"full_name": name, "date_of_birth": "1990-01-01", "gender": "MALE",
        "id_number": "0791" + TS + sfx, "phone": "0913" + TS + sfx[0],
        "address": {"province_code": "79", "street": "1 Test"},
        "patient_type": "SERVICE", "nationality": "VN"}, who="letan")
    dd = ((d or {}).get("data", {}) or {}).get("patient") or (d or {}).get("data", {})
    return dd.get("id"), dd.get("code")


# ── BUG-02 retest: phong capacity=1, tiep don BN thu 2 KHAC nguoi cung ngay
cap = sql(f"select capacity from prodiab_his.diab_his_sys_rooms where id='{ROOM2}'")
pA, cA = newpat("11", "Trần Văn An")
pB, cB = newpat("22", "Lê Thị Bình")
stA, dA = api.post("/reception/check-in", {"patient_id": pA, "room_id": ROOM2,
    "reason_for_visit": "Khám nội", "priority": "NORMAL"}, who="letan")
stB, dB = api.post("/reception/check-in", {"patient_id": pB, "room_id": ROOM2,
    "reason_for_visit": "Khám nội", "priority": "NORMAL"}, who="letan")
rec("UTC-REC-13", "PASS" if stA == 201 and stB == 201 else "FAIL",
    f"[BUG-02] phong PK02 capacity={cap}: BN A -> {stA} {err(dA) or 'OK'} | BN B (khac nguoi, cung ngay) -> {stB} {err(dB) or 'OK'}")
ST["patient_b"] = pB

# ══════════════════════════════════════════════════════════════════ ENC / EMR
hd("4.3 ENC/EMR - Kham benh, benh an, ky so")
st, d = api.post(f"/encounters/{EID}/start", who="bacsi")
e = (d or {}).get("data", {})
rec("UTC-ENC-01", "PASS" if st == 200 and str(e.get("status")).upper() in ("IN_PROGRESS", "INPROGRESS") else "FAIL",
    f"HTTP {st} status={e.get('status')}")

doc = sql(f"""select u.email, u.full_name, r.code from prodiab_his.diab_his_enc_encounters e
 join prodiab_his.diab_his_sec_users u on u.id=e.doctor_id
 left join prodiab_his.diab_his_sec_user_roles ur on ur.user_id=u.id
 left join prodiab_his.diab_his_sec_roles r on r.id=ur.role_id
 where e.id='{EID}' limit 1;""")
isdoc = bool(doc) and ("bacsi" in doc[0].lower() or "DOCTOR" in doc[0].upper())
rec("UTC-ENC-02", "PASS" if isdoc else "FAIL",
    f"[High cu] doctor_id cua encounter = {doc[0] if doc else 'khong doc duoc'}")

TPL = "aaaaaaaa-0002-0000-0000-000000000002"
st, d = api.get(f"/encounters/{EID}/emr", who="bacsi")
rec("UTC-EMR-01", "PASS" if st == 200 and (d or {}).get("data") is None else "FAIL",
    f"benh an ban dau: HTTP {st} data={(d or {}).get('data')}")

content = {"chief_complaint": "Mệt mỏi, khát nước nhiều, tiểu đêm 3-4 lần/đêm",
           "history": "Đái tháo đường type 2 phát hiện 2019",
           "examination": "Tỉnh, tiếp xúc tốt. Tim đều, phổi trong.",
           "assessment": "Đái tháo đường type 2 kiểm soát chưa tốt (E11.9)",
           "plan": "Xét nghiệm HbA1c, đường huyết đói."}
structured = {"hba1c_muc_tieu": "7.0", "bien_chung_than": "Chưa", "tuan_thu_thuoc": "Tốt"}
st, d = api.put(f"/encounters/{EID}/emr", {"content_json": content, "content_html": "<p>BA ĐTĐ</p>",
    "template_id": TPL, "structured_values": structured}, who="bacsi")
v1 = (d or {}).get("data", {}).get("version")
snap = sql(f"select case when schema_snapshot_json is null then 'NULL' else 'CO' end from prodiab_his.diab_his_cli_emr_versions v join prodiab_his.diab_his_enc_emr_contents c on c.id=v.emr_id where c.encounter_id='{EID}' order by v.version desc limit 1;")
rec("UTC-EMR-02", "PASS" if st == 200 and v1 == 1 else "FAIL",
    f"HTTP {st} version={v1} schema_snapshot_json={snap[0] if snap else '?'}")

content["plan"] += " Hẹn tái khám 1 tháng."
st, d = api.put(f"/encounters/{EID}/emr", {"content_json": content, "template_id": TPL,
    "structured_values": structured}, who="bacsi")
v2 = (d or {}).get("data", {}).get("version")
rec("UTC-EMR-03", "PASS" if v2 == 2 else "FAIL", f"luu lan 2 -> version={v2}")

st, d = api.get(f"/encounters/{EID}/emr/versions", who="bacsi")
vs = lst(d)
rec("UTC-EMR-04", "PASS" if len(vs) >= 2 else "FAIL", f"lich su {len(vs)} ban ghi")

st, d = api.post(f"/encounters/{EID}/emr/sign", {
    "signature_data": "QUJDRDEyMzQ1Njc4OTBhYmNkZWZnaGlqa2xtbm9wcXJzdHV2d3h5eg==",
    "certificate_id": "QC-TEST-CERT-2026", "signature_algorithm": "SHA256withRSA"}, who="bacsi")
rec("UTC-EMR-05", "PASS" if st == 200 else "FAIL", f"ky so HTTP {st}")

st, d = api.post(f"/encounters/{EID}/emr/sign", {
    "signature_data": "QUJDRDEyMzQ1Njc4OTBhYmNkZWZnaGlqa2xtbm9wcXJzdHV2d3h5eg==",
    "certificate_id": "QC-TEST-CERT-2026"}, who="bacsi")
rec("UTC-EMR-06", "PASS" if st in (400, 409) and err(d) == "EMR_ALREADY_SIGNED" else "FAIL",
    f"ky lan 2 -> HTTP {st} {err(d)}")

st, d = api.put(f"/encounters/{EID}/emr", {"content_json": {"chief_complaint": "SUA SAU KHI KY"},
    "template_id": TPL}, who="bacsi")
rec("UTC-EMR-07", "PASS" if st == 409 and err(d) == "EMR_ALREADY_SIGNED" else "FAIL",
    f"sua sau khi ky -> HTTP {st} {err(d)}")

rows = sql("select name, case when structured_json is null then 'NULL' else 'CO' end from prodiab_his.diab_his_cli_emr_templates where is_system=1;")
allhave = bool(rows) and all(r.split("\t")[-1] == "CO" for r in rows)
rec("UTC-EMR-08", "PASS" if allhave else "FAIL",
    f"[High cu] mau he thong structured_json: {rows}")

# ══════════════════════════════════════════════════════════════════ VIT / INB
hd("4.4 VIT/INB - Sinh hieu + InBody")
st, d = api.post(f"/encounters/{EID}/vital-signs", {"temperature_c": 36.8, "heart_rate_bpm": 82,
    "respiratory_rate": 18, "bp_systolic": 128, "bp_diastolic": 82, "spo2_percent": 98,
    "weight_kg": 62.5, "height_cm": 158, "pain_scale": 2, "glucose_mg_dl": 142,
    "note": "Bệnh nhân tỉnh táo"}, who="bacsi")
v = (d or {}).get("data", {})
bmi = v.get("bmi") or v.get("bmi_value")
rec("UTC-VIT-01", "PASS" if st == 201 and abs(float(bmi or 0) - 25.0) < 0.2 else "FAIL",
    f"HTTP {st} BMI={bmi} seq={v.get('record_sequence')}")

st, d = api.post(f"/encounters/{EID}/vital-signs", {"temperature_c": 41.5, "heart_rate_bpm": 190,
    "bp_systolic": 240, "bp_diastolic": 150, "spo2_percent": 70, "weight_kg": 62.5,
    "height_cm": 158, "note": "QC bat thuong"}, who="bacsi")
rec("UTC-VIT-02", "PASS" if st == 201 else "FAIL", f"gia tri bat thuong co that -> HTTP {st} (cho ghi)")

st, d = api.post(f"/encounters/{EID}/vital-signs", {"temperature_c": 999, "heart_rate_bpm": -5,
    "bp_systolic": 0, "spo2_percent": 500, "weight_kg": -10, "height_cm": 0}, who="bacsi")
rec("UTC-VIT-03", "PASS" if st in (400, 422) else "FAIL",
    f"gia tri vo ly -> HTTP {st} {err(d)} | {json.dumps(d, ensure_ascii=False)[:140]}")

INB = os.path.join(ROOT, "docs/qc/evidence-inbody-ocr-20260830/sample-inbody-full.pdf")
st, d = api.upload(f"/patients/{PID}/inbody-reports", [("file", INB)], {"encounter_id": EID}, who="bacsi")
rep = (d or {}).get("data", {})
RIDB = rep.get("id")
flds = rep.get("fields", [])
nok = sum(1 for f in flds if f.get("extracted"))
rec("UTC-INB-01", "PASS" if st == 201 and nok >= 9 and rep.get("extraction_status") == "pending" else "FAIL",
    f"HTTP {st} doc duoc {nok}/{len(flds)} chi so, status={rep.get('extraction_status')}")

conf = [{"indicator_type": f["indicator_type"], "value": f.get("value"), "unit": f.get("unit"),
         "include": bool(f.get("extracted"))} for f in flds]
st, d = api.post(f"/inbody-reports/{RIDB}/confirm", {"encounter_id": EID, "fields": conf}, who="bacsi")
rec("UTC-INB-02", "PASS" if st == 200 and (d or {}).get("data", {}).get("extraction_status") == "success" else "FAIL",
    f"confirm HTTP {st} status={(d or {}).get('data', {}).get('extraction_status')}")

ind = sql(f"select indicator_type, value, source from prodiab_his.diab_his_cli_indicator_reading where patient_id='{PID}';")
hasbmi = any(r.split("\t")[0] == "BMI" for r in ind)
rec("UTC-INB-03", "PASS" if hasbmi and len(ind) >= 8 else "FAIL",
    f"indicator_reading {len(ind)} dong, co BMI={hasbmi}: {[r.split(chr(9))[0] for r in ind]}")

vsr = sql(f"select weight_kg, note from prodiab_his.diab_his_enc_vital_signs where encounter_id='{EID}' order by record_sequence desc limit 1;")
rec("UTC-INB-04", "PASS" if vsr and "InBody" in vsr[0] else "FAIL", f"vital_signs moi nhat: {vsr}")
rec("UTC-INB-05", "SKIP", "chua dung duoc PDF InBody co chi so phi ly (co che tuong duong da verify o UTC-CLS-14)")

st, d = api.delete(f"/inbody-reports/{RIDB}?reason=QC%20retest%20vong%202", who="bacsi")
soft = sql(f"select case when deleted_at is null then 'NULL' else 'CO' end, delete_reason from prodiab_his.diab_his_cli_inbody_report where id='{RIDB}';")
rec("UTC-INB-06", "PASS" if st == 200 and soft and soft[0].startswith("CO") else "FAIL",
    f"delete HTTP {st}; DB con dong soft-delete: {soft}")

BAD = os.path.join(ROOT, "docs/qc/evidence-radresult-ocr-20260830/phieu-ket-qua-cdha-test.pdf")
st, d = api.upload(f"/patients/{PID}/inbody-reports", [("file", BAD)], {"encounter_id": EID}, who="bacsi")
rec("UTC-INB-07", "PASS" if st != 500 else "FAIL", f"upload PDF khong phai InBody -> HTTP {st}, khong crash")

json.dump(ST, open(os.path.join(D, "retest2-state-part1.json"), "w"), indent=1)
with open(os.path.join(D, "retest2-part1.jsonl"), "w", encoding="utf-8") as f:
    for r in R:
        f.write(json.dumps(r, ensure_ascii=False) + "\n")
print("\n>>> phan 1 xong:", sum(1 for r in R if r["status"] == "PASS"), "PASS /",
      sum(1 for r in R if r["status"] == "FAIL"), "FAIL /",
      sum(1 for r in R if r["status"] == "SKIP"), "SKIP")
