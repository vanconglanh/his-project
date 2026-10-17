#!/usr/bin/env python3
"""
fix_compound_dedup_doctor_20261017.py
======================================
Ba nhiệm vụ bổ sung cho pro-his-db (tenant 1):

A. Tách chẩn đoán ghép: "A - B" → PRIMARY A + SECONDARY B (áp dụng lại toàn bộ LEG-*).
   - Ưu tiên alias dài nhất cho từng phần.
B. Dedupe dữ liệu legacy: prescription_items, lab_results, vital_signs, diagnoses.
C. Map doctor_id: ToaThuoc.BacSiChiDinh → user UUID → cập nhật encounter + prescription.

Idempotent. Chạy: python3 fix_compound_dedup_doctor_20261017.py [--dry-run] [--skip-backup]
"""
import sys, json, re, uuid, unicodedata, subprocess, os, argparse
from datetime import datetime

import pymysql, pymysql.cursors

DST_HOST = os.environ.get("HIS_DST_HOST", "172.20.0.3")
DST_PORT = int(os.environ.get("HIS_DST_PORT", "3306"))
DST_USER = "root"
DST_PASS = os.environ.get("HIS_DST_PASS", "")
DST_DB   = "prodiab_his"
TENANT   = 1

def uid(): return str(uuid.uuid4())

_VIET_MAP = {"đ":"d","Đ":"d","ư":"u","Ư":"u","ơ":"o","Ơ":"o","ă":"a","Ă":"a"}
def normalize(s: str) -> str:
    if not s: return ""
    s = "".join(_VIET_MAP.get(c,c) for c in s)
    nfkd = unicodedata.normalize("NFKD", s)
    ascii_ = "".join(c for c in nfkd if not unicodedata.combining(c))
    return re.sub(r'\s+', ' ', ascii_.lower().strip())

_ICD_IN_PARENS = re.compile(r'\(([A-Z]\d{2}(?:\.[0-9A-Z]+)?)\)')
_FALLBACK = "R69"

# Alias dict — theo thứ tự ưu tiên (dài hơn → cụ thể hơn).
# Lookup dùng longest-match, không phải first-match.
ALIASES = {
    "dai thao duong khong phu thuoc insulin":  "E11",
    "benh dai thao duong khong phu thuoc insulin": "E11",
    "benh dai thao duong type 2":              "E11",
    "dai thao duong type 2":                   "E11",
    "dai thao duong tip 2":                    "E11",
    "dai thao duong t2":                       "E11",
    "tieu duong tip 2":                        "E11",
    "tang lipid mau hon hop":                  "E78.2",
    "roi loan lipid mau hon hop":              "E78.2",
    "tang cholesterol mau":                    "E78.0",
    "tang triglyceride mau":                   "E78.1",
    "roi loan lipid mau":                      "E78.5",
    "tang lipid mau":                          "E78.5",
    "roi loan chuyen hoa lipoprotein":         "E78",
    "tang huyet ap nguyen phat":               "I10",
    "tang huyet ap vo can":                    "I10",
    "tang huyet ap":                           "I10",
    "huyet ap cao":                            "I10",
    "beo phi do 3":                            "E66.09",
    "beo phi do 2":                            "E66.09",
    "beo phi do 1":                            "E66.09",
    "beo phi khu tru":                         "E65",
    "lam sang beo phi":                        "E66",
    "benh thua can beo phi":                   "E66",
    "benh beo phi":                            "E66",
    "beo phi":                                 "E66",
    "thua can":                                "E66.9",
    "tien dai thao duong":                     "R73.09",
    "tien dtd":                                "R73.09",
    "tang duong huyet":                        "R73.9",
    "khang insulin":                           "E11.9",
    "de khang insulin":                        "E11.9",
    "tang acid uric mau":                      "E79.0",
    "tang acid uric":                          "E79.0",
    "tang men gan":                            "K76.0",
    "gan nhiem mo":                            "K76.0",
    "thieu vitamin d":                         "E55.9",
    "thieu calci do che do an":                "E58",
    "thieu calci":                             "E55.9",
    "tang canxi mau":                          "E83.52",
    "roi loan chuc nang tuyen giap":           "E07.9",
    "cuong giap":                              "E05.9",
    "nhiet doc giap":                          "E05.9",
    "suy giap":                                "E03.9",
    "nhan giap":                               "E04.1",
    "loang xuong":                             "M81.0",
    "thieu mau":                               "D64.9",
    "tieu duong":                              "E14",
    "dai thao duong":                          "E14",
}

def connect_dst():
    return pymysql.connect(
        host=DST_HOST, port=DST_PORT, user=DST_USER, password=DST_PASS,
        database=DST_DB, charset="utf8mb4",
        cursorclass=pymysql.cursors.DictCursor, autocommit=False,
    )

def do_backup():
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    path = f"/home/stackops/backups/pro-his/prodiab_his_{ts}_cdd.sql.gz"
    os.makedirs(os.path.dirname(path), exist_ok=True)
    r = subprocess.run(
        f'docker exec pro-his-db bash -c "mysqldump -uroot -p\\"$MYSQL_ROOT_PASSWORD\\" '
        f'--default-character-set=utf8mb4 --single-transaction --quick {DST_DB}" | gzip > {path}',
        shell=True
    )
    if r.returncode != 0:
        raise RuntimeError("Backup thất bại!")
    sz = os.path.getsize(path) // 1024
    print(f"[backup] OK — {sz} KB → {path}")
    return path

# ─── Load ICD10 dict ──────────────────────────────────────────────────────────
def load_icd_dict(conn):
    with conn.cursor() as c:
        c.execute("SELECT code, name_vi FROM diab_his_dict_icd10 WHERE is_active=1")
        rows = c.fetchall()
    norm_to_code = {normalize(r["name_vi"]): r["code"] for r in rows}
    return norm_to_code

def lookup_icd_best(name: str, norm_to_code: dict) -> str | None:
    """Tìm ICD code khớp tốt nhất (dài nhất) cho tên bệnh."""
    n = normalize(name)
    if not n: return None
    # 1. ICD trong ngoặc đơn
    m = _ICD_IN_PARENS.search(name)
    if m: return m.group(1).upper()
    # 2. Exact dict
    if n in norm_to_code: return norm_to_code[n]
    # 3. Alias longest-match
    best_len, best_code = 0, None
    for alias, code in ALIASES.items():
        if alias in n and len(alias) > best_len:
            best_len, best_code = len(alias), code
    if best_code: return best_code
    # 4. Dict partial longest-match (icd name ∈ n)
    best_len, best_code = 0, None
    for icd_norm, code in norm_to_code.items():
        if len(icd_norm) >= 8 and icd_norm in n and len(icd_norm) > best_len:
            best_len, best_code = len(icd_norm), code
    return best_code  # None nếu không tìm được

# Tách compound name thành danh sách phần
_SEP = re.compile(r'\s*;\s*|\s+[-–_]\s+|\s*,\s+|\s+và\s+|\s+\+\s+')

def split_parts(name: str) -> list[str]:
    """Split compound diagnosis into list of parts."""
    parts = [p.strip().rstrip('.,').strip() for p in _SEP.split(name)]
    return [p for p in parts if len(normalize(p)) >= 4]

# ─── A. Tách chẩn đoán ghép ──────────────────────────────────────────────────
def step_a_compound(conn, dry, norm_to_code) -> dict:
    """
    Với mỗi diagnosis legacy có compound name:
    - Split thành N phần
    - UPDATE bản ghi gốc: name=part[0], icd=best_icd, type=PRIMARY (giữ type cũ nếu không phải PRIMARY ghép)
    - INSERT bản ghi mới cho part[1..N] là SECONDARY (idempotent WHERE NOT EXISTS)
    """
    # Lấy tất cả diagnoses legacy (có thể compound)
    with conn.cursor() as c:
        c.execute("""
            SELECT d.id, d.encounter_id, d.name, d.icd10_code, d.type, d.note, d.sort_order
            FROM diab_his_enc_diagnoses d
            JOIN diab_his_enc_encounters e ON e.id=d.encounter_id AND e.tenant_id=%s
            WHERE d.tenant_id=%s AND d.deleted_at IS NULL
              AND e.encounter_no LIKE 'LEG-%%'
        """, (TENANT, TENANT))
        rows = c.fetchall()

    stats = {"compound_split": 0, "compound_inserted": 0, "compound_updated": 0, "single_updated": 0}

    for row in rows:
        name = row["name"]
        parts = split_parts(name)

        if len(parts) <= 1:
            # Đơn lẻ — chỉ update ICD nếu đang R69
            if row["icd10_code"] == _FALLBACK:
                icd = lookup_icd_best(name, norm_to_code)
                if icd and icd != _FALLBACK:
                    if not dry:
                        with conn.cursor() as c2:
                            c2.execute("""
                                UPDATE diab_his_enc_diagnoses
                                SET icd10_code=%s, note=CONCAT(IFNULL(note,''),' [ICD gán lại cdd-20261017]'), updated_at=NOW()
                                WHERE id=%s AND icd10_code=%s
                            """, (icd, row["id"], _FALLBACK))
                            stats["single_updated"] += c2.rowcount
                    else:
                        stats["single_updated"] += 1
            continue

        # Compound — split
        stats["compound_split"] += 1
        icds = []
        for p in parts:
            icd = lookup_icd_best(p, norm_to_code)
            icds.append(icd or _FALLBACK)

        # UPDATE bản ghi gốc → phần đầu + ICD[0] + type PRIMARY
        new_name0 = parts[0][:255]
        new_icd0  = icds[0]
        note0 = (row["note"] or "") + f" [tach compound 2026-10-17 tu: {name[:100]}]"
        if not dry:
            with conn.cursor() as c2:
                c2.execute("""
                    UPDATE diab_his_enc_diagnoses
                    SET name=%s, icd10_code=%s, type='PRIMARY', note=%s, updated_at=NOW()
                    WHERE id=%s
                """, (new_name0, new_icd0, note0[:500], row["id"]))
                stats["compound_updated"] += c2.rowcount
        else:
            stats["compound_updated"] += 1

        # INSERT phần còn lại là SECONDARY
        for i, (part_name, part_icd) in enumerate(zip(parts[1:], icds[1:]), start=1):
            if not dry:
                with conn.cursor() as c2:
                    c2.execute("""
                        INSERT INTO diab_his_enc_diagnoses
                            (id, tenant_id, encounter_id, icd10_code, name, type, note, sort_order, created_at, updated_at)
                        SELECT UUID(), %s, %s, %s, %s, 'SECONDARY',
                               %s, %s, NOW(), NOW()
                        WHERE NOT EXISTS (
                            SELECT 1 FROM diab_his_enc_diagnoses
                            WHERE encounter_id=%s AND name=%s AND deleted_at IS NULL
                        )
                    """, (
                        TENANT, row["encounter_id"], part_icd, part_name[:255],
                        f"[tach compound 2026-10-17]", row["sort_order"] + i,
                        row["encounter_id"], part_name[:255],
                    ))
                    stats["compound_inserted"] += c2.rowcount
            else:
                stats["compound_inserted"] += 1

    if not dry:
        conn.commit()
    return stats

# ─── B. Dedupe ────────────────────────────────────────────────────────────────
def step_b_dedup(conn, dry) -> dict:
    """Soft-delete trùng lặp trong prescription_items, lab_results, vital_signs."""
    stats = {"rx_deduped": 0, "lab_deduped": 0, "vs_deduped": 0}

    # B1: Prescription items — key: (prescription_id, drug_name, quantity, dosage)
    with conn.cursor() as c:
        c.execute("""
            SELECT pi.id, pi.prescription_id, pi.drug_name, pi.quantity, pi.dosage,
                   pi.created_at
            FROM diab_his_pha_prescription_items pi
            JOIN diab_his_pha_prescriptions pr ON pr.id=pi.prescription_id
            WHERE pr.tenant_id=%s AND pi.deleted_at IS NULL
              AND pr.encounter_id IN (
                SELECT id FROM diab_his_enc_encounters WHERE encounter_no LIKE 'LEG-%%' AND tenant_id=%s
              )
            ORDER BY pi.prescription_id, pi.drug_name, pi.quantity, pi.dosage, pi.created_at
        """, (TENANT, TENANT))
        items = c.fetchall()

    seen = {}  # key → first id
    dups = []
    for row in items:
        key = (row["prescription_id"], (row["drug_name"] or "").lower()[:50],
               str(row["quantity"]), (row["dosage"] or "")[:20])
        if key in seen:
            dups.append(row["id"])
        else:
            seen[key] = row["id"]

    if dups and not dry:
        for dup_id in dups:
            with conn.cursor() as c2:
                c2.execute("""
                    UPDATE diab_his_pha_prescription_items
                    SET deleted_at=NOW(), note=CONCAT(IFNULL(note,''),' [dedup legacy 2026-10-17]')
                    WHERE id=%s AND deleted_at IS NULL
                """, (dup_id,))
                stats["rx_deduped"] += c2.rowcount
        conn.commit()
    else:
        stats["rx_deduped"] = len(dups)

    # B2: Lab results — key: (encounter_id, test_code, result_value trimmed)
    with conn.cursor() as c:
        c.execute("""
            SELECT id, encounter_id, test_code, LEFT(TRIM(result_value),100) AS rval, created_at
            FROM diab_his_lab_results
            WHERE tenant_id=%s AND deleted_at IS NULL
              AND encounter_id IN (
                SELECT id FROM diab_his_enc_encounters WHERE encounter_no LIKE 'LEG-%%' AND tenant_id=%s
              )
            ORDER BY encounter_id, test_code, rval, created_at
        """, (TENANT, TENANT))
        lr_rows = c.fetchall()

    seen = {}
    lab_dups = []
    for row in lr_rows:
        key = (row["encounter_id"], (row["test_code"] or "")[:40], (row["rval"] or "")[:80])
        if key in seen:
            lab_dups.append(row["id"])
        else:
            seen[key] = row["id"]

    if lab_dups and not dry:
        for dup_id in lab_dups:
            with conn.cursor() as c2:
                c2.execute("""
                    UPDATE diab_his_lab_results
                    SET deleted_at=NOW(), note=CONCAT(IFNULL(note,''),' [dedup legacy 2026-10-17]')
                    WHERE id=%s AND deleted_at IS NULL
                """, (dup_id,))
                stats["lab_deduped"] += c2.rowcount
        conn.commit()
    else:
        stats["lab_deduped"] = len(lab_dups)

    # B3: Vital signs — key: (encounter_id, DATE(recorded_at))
    with conn.cursor() as c:
        c.execute("""
            SELECT id, encounter_id, DATE(recorded_at) AS rec_date, created_at
            FROM diab_his_enc_vital_signs
            WHERE tenant_id=%s
              AND encounter_id IN (
                SELECT id FROM diab_his_enc_encounters WHERE encounter_no LIKE 'LEG-%%' AND tenant_id=%s
              )
            ORDER BY encounter_id, rec_date, created_at
        """, (TENANT, TENANT))
        vs_rows = c.fetchall()

    seen = {}
    vs_dups = []
    for row in vs_rows:
        key = (row["encounter_id"], str(row["rec_date"]))
        if key in seen:
            vs_dups.append(row["id"])
        else:
            seen[key] = row["id"]

    if vs_dups and not dry:
        for dup_id in vs_dups:
            with conn.cursor() as c2:
                c2.execute("""
                    UPDATE diab_his_enc_vital_signs SET deleted_at=NOW()
                    WHERE id=%s AND deleted_at IS NULL
                """, (dup_id,))
                stats["vs_deduped"] += c2.rowcount
        conn.commit()
    else:
        stats["vs_deduped"] = len(vs_dups)

    return stats

# ─── C. Map doctor_id ─────────────────────────────────────────────────────────
def step_c_doctor(conn, dry, nhan_su_list) -> dict:
    """
    1. Đọc diab_his_sec_users: id, email, suns_code → map NV_CODE → user_id
    2. Đọc structured_values_json → ToaThuoc[0].BacSiChiDinh (tên BS)
       + map tên → user_id qua suns_code
    3. UPDATE encounters + prescriptions doctor_id
    """
    # Build map: suns_code → user_id (từ HIS users có suns_code)
    with conn.cursor() as c:
        c.execute("""
            SELECT id, email, full_name, suns_code
            FROM diab_his_sec_users
            WHERE tenant_id=%s AND deleted_at IS NULL AND suns_code IS NOT NULL
        """, (TENANT,))
        users = c.fetchall()

    suns_to_uid = {u["suns_code"].strip().upper(): u["id"] for u in users if u["suns_code"]}
    # Cũng map theo tên (normalized) từ nhan_su → user email → user id
    name_to_uid  = {}
    for ns in nhan_su_list:
        # tìm user theo email prefix
        user_email = (ns.get("user") or "") + "@diab.com.vn"
        for u in users:
            if u["email"].lower() == user_email.lower():
                name_to_uid[normalize(ns["ten"])] = u["id"]
                suns_to_uid[ns["ma"].strip().upper()] = u["id"]
                break

    stats = {"enc_doctor_mapped": 0, "rx_doctor_mapped": 0, "enc_no_match": 0}

    # Đọc tất cả EMR LEG-* có structured_values_json
    with conn.cursor() as c:
        c.execute("""
            SELECT em.encounter_id, em.structured_values_json,
                   enc.doctor_id enc_doctor_id
            FROM diab_his_enc_emr_contents em
            JOIN diab_his_enc_encounters enc ON enc.id=em.encounter_id AND enc.tenant_id=%s
            WHERE em.tenant_id=%s AND enc.encounter_no LIKE 'LEG-%%'
              AND em.structured_values_json IS NOT NULL
        """, (TENANT, TENANT))
        rows = c.fetchall()

    for row in rows:
        try:
            legacy = json.loads(row["structured_values_json"])
        except Exception:
            continue

        doctor_uid = None

        # Nguồn 1: ToaThuoc.BacSiChiDinh (tên bác sĩ)
        toa_list = legacy.get("ToaThuoc") or []
        for toa in toa_list:
            bs_name = (toa.get("BacSiChiDinh") or toa.get("BacSi") or "").strip()
            if bs_name:
                norm_bs = normalize(bs_name)
                if norm_bs in name_to_uid:
                    doctor_uid = name_to_uid[norm_bs]
                    break
                # Tìm partial: lấy mục nhan_su nào là substring của bs_name
                for norm_name, uid_ in name_to_uid.items():
                    if len(norm_name) >= 8 and norm_name in norm_bs:
                        doctor_uid = uid_
                        break
            if doctor_uid:
                break

        # Nguồn 2: KetQuaDichVu (không có tên BS thường, bỏ qua)

        if not doctor_uid:
            stats["enc_no_match"] += 1
            continue

        enc_id = row["encounter_id"]
        current_doctor = row["enc_doctor_id"]

        # UPDATE encounter doctor_id (chỉ nếu NULL hoặc rỗng)
        if not current_doctor or current_doctor == '':
            if not dry:
                with conn.cursor() as c2:
                    c2.execute("""
                        UPDATE diab_his_enc_encounters
                        SET doctor_id=%s, updated_at=NOW()
                        WHERE id=%s AND (doctor_id IS NULL OR doctor_id='')
                    """, (doctor_uid, enc_id))
                    stats["enc_doctor_mapped"] += c2.rowcount
            else:
                stats["enc_doctor_mapped"] += 1

        # UPDATE prescription doctor_id
        if not dry:
            with conn.cursor() as c2:
                c2.execute("""
                    UPDATE diab_his_pha_prescriptions
                    SET doctor_id=%s, updated_at=NOW()
                    WHERE encounter_id=%s AND (doctor_id IS NULL OR doctor_id='')
                      AND tenant_id=%s
                """, (doctor_uid, enc_id, TENANT))
                stats["rx_doctor_mapped"] += c2.rowcount
        else:
            with conn.cursor() as c2:
                c2.execute("""
                    SELECT COUNT(*) AS n FROM diab_his_pha_prescriptions
                    WHERE encounter_id=%s AND (doctor_id IS NULL OR doctor_id='') AND tenant_id=%s
                """, (enc_id, TENANT))
                stats["rx_doctor_mapped"] += c2.fetchone()["n"]

    if not dry:
        conn.commit()
    return stats

# ─── Verify ───────────────────────────────────────────────────────────────────
def verify(conn) -> dict:
    with conn.cursor() as c:
        def q(sql, *args):
            c.execute(sql, args)
            r = c.fetchone()
            return list(r.values())[0] if r else 0
        return {
            "patients_active":    q("SELECT COUNT(*) FROM diab_his_pat_patients WHERE tenant_id=%s AND deleted_at IS NULL", TENANT),
            "diagnoses_total":    q("SELECT COUNT(*) FROM diab_his_enc_diagnoses WHERE tenant_id=%s AND deleted_at IS NULL", TENANT),
            "diagnoses_r69":      q("SELECT COUNT(*) FROM diab_his_enc_diagnoses WHERE tenant_id=%s AND icd10_code='R69' AND deleted_at IS NULL", TENANT),
            "rx_items_active":    q("SELECT COUNT(*) FROM diab_his_pha_prescription_items WHERE deleted_at IS NULL"),
            "lab_active":         q("SELECT COUNT(*) FROM diab_his_lab_results WHERE tenant_id=%s AND deleted_at IS NULL", TENANT),
            "enc_with_doctor":    q("SELECT COUNT(*) FROM diab_his_enc_encounters WHERE tenant_id=%s AND doctor_id IS NOT NULL AND doctor_id<>'' AND encounter_no LIKE 'LEG-%%'", TENANT),
        }

def verify_leg518(conn) -> dict:
    with conn.cursor() as c:
        c.execute("""
            SELECT enc.id enc_id, enc.doctor_id, enc.patient_id,
                   u.full_name doctor_name
            FROM diab_his_enc_encounters enc
            LEFT JOIN diab_his_sec_users u ON u.id=enc.doctor_id
            WHERE enc.tenant_id=%s AND enc.encounter_no='LEG-518'
        """, (TENANT,))
        enc = c.fetchone() or {}

    enc_id = enc.get("enc_id")
    if not enc_id: return {"error": "not found"}

    with conn.cursor() as c:
        c.execute("SELECT icd10_code, name, type FROM diab_his_enc_diagnoses WHERE encounter_id=%s AND deleted_at IS NULL ORDER BY type, sort_order", (enc_id,))
        diags = c.fetchall()

    with conn.cursor() as c:
        c.execute("""
            SELECT pi.drug_name, pi.quantity, pi.dosage FROM diab_his_pha_prescription_items pi
            JOIN diab_his_pha_prescriptions pr ON pr.id=pi.prescription_id
            WHERE pr.encounter_id=%s AND pi.deleted_at IS NULL
            ORDER BY pi.drug_name
        """, (enc_id,))
        rx = c.fetchall()

    with conn.cursor() as c:
        c.execute("""
            SELECT pr.doctor_id, u.full_name doctor_name
            FROM diab_his_pha_prescriptions pr
            LEFT JOIN diab_his_sec_users u ON u.id=pr.doctor_id
            WHERE pr.encounter_id=%s AND pr.deleted_at IS NULL
        """, (enc_id,))
        prs = c.fetchall()

    return {
        "doctor_name": enc.get("doctor_name"),
        "diagnoses": diags,
        "rx_items": rx,
        "prescriptions": prs,
    }

# ─── Main ─────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run",     action="store_true")
    parser.add_argument("--skip-backup", action="store_true")
    args = parser.parse_args()
    dry = args.dry_run
    if dry: print("=== DRY RUN ===")

    global DST_PASS
    if not DST_PASS:
        r = subprocess.run(
            "docker inspect pro-his-db --format '{{range .Config.Env}}{{println .}}{{end}}'",
            shell=True, capture_output=True, text=True
        )
        for line in r.stdout.splitlines():
            if line.startswith("MYSQL_ROOT_PASSWORD="):
                DST_PASS = line.split("=", 1)[1]; break

    backup_path = ""
    if not dry and not args.skip_backup:
        backup_path = do_backup()

    conn = connect_dst()
    norm_to_code = load_icd_dict(conn)

    # Load nhan_su từ env hoặc hardcode (đã có trên server từ scp trước đó)
    nhan_su_path = "/home/stackops/suns-master-20261016.json"
    nhan_su_list = []
    if os.path.exists(nhan_su_path):
        with open(nhan_su_path, encoding="utf-8") as f:
            nhan_su_list = json.load(f).get("nhan_su", [])
    else:
        # Inline fallback (31 nhân sự từ file)
        nhan_su_list = [
            {"ma":"NV001","ten":"BSCKI. Trương Lan Đài","user":"bsdai"},
            {"ma":"NV002","ten":"TS BS. Lý Đại Lương","user":"bsluong"},
            {"ma":"NV003","ten":"BSCKI. Trần Vũ Lan Hương","user":"bshuong"},
            {"ma":"NV004","ten":"BSCKI. Châu Hồ Minh Quân","user":"bsquan"},
            {"ma":"NV005","ten":"BSCKII. Dương Thị Mỹ Dung","user":"bsdung"},
            {"ma":"NV006","ten":"ThS BS. Nguyễn Kim Chi","user":"bschi"},
            {"ma":"NV007","ten":"ThS BSCKI. Nguyễn Vũ Nhật Phong","user":"bsphong"},
            {"ma":"NV008","ten":"BS. Đinh Nguyễn Phương Khanh","user":"bskhanh"},
            {"ma":"NV009","ten":"BS. Cao Phúc Hậu","user":"bshau"},
            {"ma":"NV010","ten":"BS. Trương Như Ý","user":"bsy"},
            {"ma":"NV023","ten":"BSCKI. Trần Thị Ngọc Mai","user":"bsmai"},
            {"ma":"NV025","ten":"BSCKI. Bùi Thị Huỳnh Anh","user":"bsanh"},
        ]

    all_stats = {"backup": backup_path, "dry_run": dry}

    try:
        print("\n=== A. Tách chẩn đoán ghép ===")
        s = step_a_compound(conn, dry, norm_to_code)
        all_stats["step_a"] = s
        print(json.dumps(s, ensure_ascii=False))

        print("\n=== B. Dedupe dữ liệu legacy ===")
        s = step_b_dedup(conn, dry)
        all_stats["step_b"] = s
        print(json.dumps(s, ensure_ascii=False))

        print("\n=== C. Map doctor_id ===")
        s = step_c_doctor(conn, dry, nhan_su_list)
        all_stats["step_c"] = s
        print(json.dumps(s, ensure_ascii=False))

        print("\n=== Verify tổng quát ===")
        v = verify(conn)
        all_stats["verify"] = v
        print(json.dumps(v, ensure_ascii=False))

        print("\n=== Verify LEG-518 ===")
        v518 = verify_leg518(conn)
        all_stats["verify_leg518"] = v518
        print(json.dumps(v518, ensure_ascii=False, default=str))

        # 10 lượt ghép ngẫu nhiên đã tách
        with conn.cursor() as c:
            c.execute("""
                SELECT LEFT(d.name,60) name, d.icd10_code, d.type, enc.encounter_no
                FROM diab_his_enc_diagnoses d
                JOIN diab_his_enc_encounters enc ON enc.id=d.encounter_id
                WHERE d.tenant_id=%s AND d.deleted_at IS NULL AND d.note LIKE '%%tach compound%%'
                ORDER BY RAND() LIMIT 20
            """, (TENANT,))
            samples = c.fetchall()
        all_stats["sample_compound_split"] = samples
        print("\n=== Sample 10 chẩn đoán đã tách ===")
        for s2 in samples[:10]:
            print(json.dumps(s2, ensure_ascii=False, default=str))

    finally:
        conn.close()

    out = f"/tmp/cdd_stats_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
    with open(out, "w", encoding="utf-8") as f:
        json.dump(all_stats, f, ensure_ascii=False, indent=2, default=str)
    print(f"\n[done] → {out}")

if __name__ == "__main__":
    main()
