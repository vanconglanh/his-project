#!/usr/bin/env python3
"""
fix_visit_completeness_20261017.py
====================================
Bổ sung hoàn chỉnh dữ liệu lượt khám LEG-* trên pro-his-db (tenant 1).
Chỉ đọc pro-his-db; KHÔNG đụng pro-db-my8/SUNS/staging.
Idempotent. Backup DB trước khi chạy (xem --skip-backup).

Các bước:
  1. Fix chẩn đoán R69: ghép ICD10 từ result_value của fake-lab + dict lookup
  2. Bổ sung sinh hiệu còn thiếu từ SinhHieu trong structured_values_json
  3. Soft-delete 300 fake lab results (test_code là dịch vụ khám)
  4. Tạo lịch hẹn từ HenTaiKham
  5. Tạo hóa đơn lịch sử từ ChiPhi
  6. Báo cáo TapTin
  7. Cập nhật EMR thêm section "Dữ liệu gốc SUNS"

Chạy: python3 fix_visit_completeness_20261017.py [--dry-run] [--skip-backup]
"""
import sys, json, re, uuid, unicodedata, subprocess, os, argparse
from datetime import datetime, date

import pymysql
import pymysql.cursors

# ─── Cấu hình ─────────────────────────────────────────────────────────────────
DST_HOST = os.environ.get("HIS_DST_HOST", "172.20.0.3")
DST_PORT = int(os.environ.get("HIS_DST_PORT", "3306"))
DST_USER = "root"
DST_PASS = os.environ.get("HIS_DST_PASS", "")
DST_DB   = "prodiab_his"
TENANT   = 1

# ─── Helpers ──────────────────────────────────────────────────────────────────
def uid(): return str(uuid.uuid4())

def normalize(s: str) -> str:
    """Chuẩn hoá: bỏ dấu, lowercase, bỏ khoảng trắng thừa."""
    if not s: return ""
    nfkd = unicodedata.normalize("NFKD", s)
    ascii_ = "".join(c for c in nfkd if not unicodedata.combining(c))
    return re.sub(r'\s+', ' ', ascii_.lower().strip())

_ICD_IN_PARENS = re.compile(r'\(([A-Z]\d{2}(?:\.[0-9A-Z]+)?)\)')
_FALLBACK_ICD  = "R69"

def parse_diagnoses(raw: str):
    """
    Tách list (name, icd10_code) từ chuỗi chẩn đoán.
    VD: "Bệnh đái tháo đường không phụ thuộc insulin(E11); Tăng lipid máu hỗn hợp(E78.2);"
    VD: "Rối loạn lipid máu - Thừa cân"  (không có mã → R69)
    """
    out = []
    for part in re.split(r';|—', raw):
        part = part.strip().rstrip(',').strip()
        if not part: continue
        codes = _ICD_IN_PARENS.findall(part)
        name  = _ICD_IN_PARENS.sub('', part).strip(" -")
        if not name: name = part[:255]
        if codes:
            for code in codes:
                out.append((name[:255], code.upper()))
        else:
            out.append((name[:255], _FALLBACK_ICD))
    return out

def connect_dst():
    return pymysql.connect(
        host=DST_HOST, port=DST_PORT, user=DST_USER, password=DST_PASS,
        db=DST_DB, charset="utf8mb4", cursorclass=pymysql.cursors.DictCursor,
        autocommit=False,
    )

# ─── Backup ───────────────────────────────────────────────────────────────────
def do_backup():
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    path = f"/home/stackops/backups/pro-his/prodiab_his_{ts}_completeness.sql.gz"
    os.makedirs(os.path.dirname(path), exist_ok=True)
    cmd = (
        f'docker exec -e MYSQL_ROOT_PASSWORD="{DST_PASS}" pro-his-db '
        f'mysqldump -uroot --default-character-set=utf8mb4 --single-transaction '
        f'--quick {DST_DB} | gzip > {path}'
    )
    print(f"[backup] Đang backup → {path} ...")
    r = subprocess.run(cmd, shell=True)
    if r.returncode != 0:
        raise RuntimeError("Backup thất bại! Dừng lại.")
    sz = os.path.getsize(path) // 1024
    print(f"[backup] OK — {sz} KB tại {path}")
    return path

# ─── Bước 1+3: Fix chẩn đoán từ fake-lab + ICD dict lookup ───────────────────
def step1_fix_diagnoses(conn, dry) -> dict:
    """
    1a. Đọc fake lab results (test_code = dịch vụ khám) → parse ICD từ result_value
        → upsert vào diab_his_enc_diagnoses → soft-delete fake lab row (bước 3 gộp chung)
    1b. Với diagnoses còn R69 sau 1a → thử lookup trong diab_his_dict_icd10 (name_vi)
    Trả về stats dict.
    """
    # Tải ICD10 dict vào memory
    with conn.cursor() as c:
        c.execute("SELECT code, name_vi FROM diab_his_dict_icd10 WHERE is_active=1")
        icd_rows = c.fetchall()
    icd_by_code = {r["code"]: r["name_vi"] for r in icd_rows}
    icd_norm_to_code = {}
    for r in icd_rows:
        icd_norm_to_code[normalize(r["name_vi"])] = r["code"]

    # Từ điển thêm alias hay gặp trong DiaB
    ALIASES = {
        "dai thao duong tip 2": "E11",
        "dai thao duong type 2": "E11",
        "tieu duong tip 2": "E11",
        "roi loan lipid mau": "E78.5",
        "tang lipid mau": "E78.5",
        "tang huyet ap": "I10",
        "huyet ap cao": "I10",
        "beo phi": "E66",
        "thua can": "E66.9",
        "tien dai thao duong": "R73.09",
        "tien dtd": "R73.09",
        "khang insulin": "E11.9",
        "de khang insulin": "E11.9",
        "tang acid uric": "E79.0",
        "tang men gan": "K76.0",
        "thieu vitamin d": "E55.9",
        "tang canxi mau": "E83.52",
        "thieu canxi": "E55.9",
        "roi loan chuc nang tuyen giap": "E07.9",
        "cuong giap": "E05.9",
        "suy giap": "E03.9",
        "nhiet doc giap": "E05.9",
        "nhan giap": "E04.1",
        "lam sang beo phi": "E66",
        "roi loan lipid mau hon hop": "E78.2",
        "tang cholesterol mau": "E78.0",
        "tang triglyceride": "E78.1",
        "tieu duong": "E14",
    }

    def lookup_icd(name: str) -> str | None:
        """Trả mã ICD10 nếu tìm được, else None."""
        n = normalize(name)
        if n in icd_norm_to_code:
            return icd_norm_to_code[n]
        # alias
        for alias, code in ALIASES.items():
            if alias in n:
                return code
        # partial: tìm icd_norm key nào là substring của n (dài >= 10 ký tự)
        best = None
        for icd_norm, code in icd_norm_to_code.items():
            if len(icd_norm) >= 10 and icd_norm in n:
                if best is None or len(icd_norm) > len(best[0]):
                    best = (icd_norm, code)
        if best:
            return best[1]
        return None

    stats = {"diag_from_fake_lab": 0, "diag_r69_fixed": 0, "diag_r69_kept": 0,
             "fake_lab_deleted": 0, "fake_lab_empty_val": 0}

    # ── 1a: Fake lab results → diagnoses ───────────────────────────────────────
    FAKE_CODES = ('Khám', 'Khám chuyên gia', 'Tư vấn',
                  'Chương trình thay đổi lối sống - 12 tháng',
                  'Tư vấn chuyên gia', 'Tư vấn dinh dưỡng')
    with conn.cursor() as c:
        fmt = ','.join(['%s'] * len(FAKE_CODES))
        c.execute(f"""
            SELECT id, encounter_id, test_code, result_value, note
            FROM diab_his_lab_results
            WHERE tenant_id=%s AND deleted_at IS NULL
              AND test_code IN ({fmt})
        """, (TENANT, *FAKE_CODES))
        fake_labs = c.fetchall()

    print(f"[step1a] Tìm thấy {len(fake_labs)} fake lab results.")

    for row in fake_labs:
        enc_id = row["encounter_id"]
        val    = (row["result_value"] or "").strip()

        if val:
            diag_list = parse_diagnoses(val)
        else:
            stats["fake_lab_empty_val"] += 1
            diag_list = []

        for sort_i, (name, code) in enumerate(diag_list):
            # Thử lookup nếu R69
            if code == _FALLBACK_ICD:
                found = lookup_icd(name)
                if found:
                    code = found

            if not dry:
                with conn.cursor() as c2:
                    c2.execute("""
                        INSERT INTO diab_his_enc_diagnoses
                            (id, tenant_id, encounter_id, icd10_code, name, type,
                             note, sort_order, created_at, updated_at)
                        SELECT UUID(), %s, %s, %s, %s,
                               IF(%s=0, 'PRIMARY', 'SECONDARY'), NULL, %s, NOW(), NOW()
                        WHERE NOT EXISTS (
                            SELECT 1 FROM diab_his_enc_diagnoses
                            WHERE encounter_id=%s AND name=%s AND deleted_at IS NULL
                        )
                    """, (TENANT, enc_id, code, name[:255], sort_i, sort_i,
                          enc_id, name[:255]))
                    stats["diag_from_fake_lab"] += c2.rowcount
            else:
                stats["diag_from_fake_lab"] += 1

        # Soft-delete fake lab row
        if not dry:
            with conn.cursor() as c2:
                c2.execute("""
                    UPDATE diab_his_lab_results
                    SET deleted_at=NOW(), note=CONCAT(IFNULL(note,''),' [soft-deleted: dich vu kham, khong phai XN that]')
                    WHERE id=%s AND deleted_at IS NULL
                """, (row["id"],))
                stats["fake_lab_deleted"] += c2.rowcount
        else:
            stats["fake_lab_deleted"] += 1

    if not dry:
        conn.commit()

    # ── 1b: Các diagnoses còn R69 → thử lookup ────────────────────────────────
    with conn.cursor() as c:
        c.execute("""
            SELECT id, name, type FROM diab_his_enc_diagnoses
            WHERE tenant_id=%s AND icd10_code=%s AND deleted_at IS NULL
        """, (TENANT, _FALLBACK_ICD))
        r69_rows = c.fetchall()

    print(f"[step1b] {len(r69_rows)} diagnoses còn R69, thử lookup ICD10...")

    for row in r69_rows:
        found = lookup_icd(row["name"])
        if found:
            if not dry:
                with conn.cursor() as c2:
                    c2.execute("""
                        UPDATE diab_his_enc_diagnoses
                        SET icd10_code=%s,
                            note=CONCAT(IFNULL(note,''),' [ICD gán bằng dict lookup 2026-10-17]'),
                            updated_at=NOW()
                        WHERE id=%s AND icd10_code=%s
                    """, (found, row["id"], _FALLBACK_ICD))
                    stats["diag_r69_fixed"] += c2.rowcount
            else:
                stats["diag_r69_fixed"] += 1
        else:
            stats["diag_r69_kept"] += 1

    if not dry:
        conn.commit()

    return stats

# ─── Bước 2: Bổ sung sinh hiệu từ SinhHieu JSON ──────────────────────────────
def step2_vital_signs(conn, dry) -> dict:
    """Với mỗi LEG-* encounter, đọc SinhHieu từ JSON → UPDATE null cols."""
    SH_MAP = [
        # (json_key, db_col, converter)
        ("Mach",    "heart_rate_bpm",   lambda v: int(float(v)) if v else None),
        ("NhietDo", "temperature_c",    lambda v: float(v) if v else None),
        ("NhipTho", "respiratory_rate", lambda v: int(float(v)) if v else None),
        ("SPO2",    "spo2_percent",      lambda v: int(float(v)) if v else None),
        ("CanNang", "weight_kg",         lambda v: float(v) if v else None),
        ("ChieuCao","height_cm",         lambda v: float(v) if v else None),
        # BMI & VongNguc: không có cột trong entity → bỏ qua
    ]

    def parse_bp(huyet_ap):
        """'120/80' → (120, 80)"""
        if not huyet_ap or '/' not in huyet_ap:
            return None, None
        parts = huyet_ap.split('/')
        try:
            return int(float(parts[0])), int(float(parts[1]))
        except Exception:
            return None, None

    with conn.cursor() as c:
        c.execute("""
            SELECT em.encounter_id, em.structured_values_json,
                   vs.id vs_id,
                   vs.heart_rate_bpm, vs.temperature_c, vs.respiratory_rate,
                   vs.bp_systolic, vs.bp_diastolic, vs.spo2_percent,
                   vs.weight_kg, vs.height_cm
            FROM diab_his_enc_emr_contents em
            JOIN diab_his_enc_encounters enc ON enc.id = em.encounter_id AND enc.tenant_id=%s
            LEFT JOIN diab_his_enc_vital_signs vs ON vs.encounter_id = em.encounter_id AND vs.tenant_id=%s
            WHERE em.tenant_id=%s AND enc.encounter_no LIKE 'LEG-%%'
              AND em.structured_values_json IS NOT NULL
        """, (TENANT, TENANT, TENANT))
        rows = c.fetchall()

    stats = {"vs_updated": 0, "vs_inserted": 0, "vs_skipped_empty": 0}
    now_ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    for row in rows:
        try:
            legacy = json.loads(row["structured_values_json"])
        except Exception:
            continue
        sh = legacy.get("SinhHieu") or {}

        # Lấy giá trị có thực (không rỗng)
        vals = {}
        for json_key, db_col, conv in SH_MAP:
            raw = str(sh.get(json_key) or "").strip()
            if raw and raw != "/":
                try:
                    v = conv(raw)
                    if v is not None:
                        vals[db_col] = v
                except Exception:
                    pass

        sys_v, dia_v = parse_bp(str(sh.get("HuyetAp") or "").strip())
        if sys_v:
            vals["bp_systolic"]  = sys_v
            vals["bp_diastolic"] = dia_v

        if not vals:
            stats["vs_skipped_empty"] += 1
            continue

        vs_id = row["vs_id"]

        if vs_id:
            # UPDATE: chỉ set col nếu hiện tại NULL
            set_parts = []
            set_vals  = []
            for col, v in vals.items():
                if row.get(col) is None:
                    set_parts.append(f"{col}=%s")
                    set_vals.append(v)
            if not set_parts:
                stats["vs_skipped_empty"] += 1
                continue
            if not dry:
                with conn.cursor() as c2:
                    c2.execute(
                        f"UPDATE diab_his_enc_vital_signs SET {', '.join(set_parts)}, updated_at=NOW() WHERE id=%s",
                        (*set_vals, vs_id)
                    )
                    stats["vs_updated"] += c2.rowcount
            else:
                stats["vs_updated"] += 1
        else:
            # INSERT mới
            cols = ["id", "tenant_id", "encounter_id", "patient_id",
                    "recorded_at", "record_sequence", "created_at", "updated_at"]
            vlist = [uid(), TENANT, row["encounter_id"],
                     # patient_id: lấy từ encounter
                     None,  # placeholder, cần join
                     now_ts, 1, now_ts, now_ts]
            # Lấy patient_id
            with conn.cursor() as c2:
                c2.execute("SELECT patient_id FROM diab_his_enc_encounters WHERE id=%s", (row["encounter_id"],))
                enc_r = c2.fetchone()
            if not enc_r:
                continue
            vlist[3] = enc_r["patient_id"]
            for col, v in vals.items():
                cols.append(col)
                vlist.append(v)
            pholds = ','.join(['%s'] * len(cols))
            col_str = ','.join(cols)
            if not dry:
                with conn.cursor() as c2:
                    c2.execute(
                        f"INSERT INTO diab_his_enc_vital_signs ({col_str}) VALUES ({pholds})",
                        vlist
                    )
                    stats["vs_inserted"] += 1
            else:
                stats["vs_inserted"] += 1

    if not dry:
        conn.commit()
    return stats

# ─── Bước 4: Tạo lịch hẹn từ HenTaiKham ─────────────────────────────────────
def step4_appointments(conn, dry) -> dict:
    """Đọc HenTaiKham từ structured_values_json → insert diab_his_sch_appointments."""
    TODAY = date.today()

    with conn.cursor() as c:
        c.execute("""
            SELECT em.encounter_id, em.structured_values_json,
                   enc.patient_id, enc.started_at
            FROM diab_his_enc_emr_contents em
            JOIN diab_his_enc_encounters enc ON enc.id = em.encounter_id AND enc.tenant_id=%s
            WHERE em.tenant_id=%s AND enc.encounter_no LIKE 'LEG-%%'
              AND em.structured_values_json IS NOT NULL
        """, (TENANT, TENANT))
        rows = c.fetchall()

    stats = {"appt_inserted": 0, "appt_skipped_exists": 0,
             "appt_skipped_no_date": 0, "appt_no_show": 0, "appt_pending": 0}

    for row in rows:
        try:
            legacy = json.loads(row["structured_values_json"])
        except Exception:
            continue
        hen_list = legacy.get("HenTaiKham") or []
        for hen in hen_list:
            ngay = (hen.get("NgayHenKham") or "").strip()
            gio  = (hen.get("ThoiGian") or "08:00").strip()
            noi  = (hen.get("NoiDung") or "").strip()
            if not ngay:
                stats["appt_skipped_no_date"] += 1
                continue
            # Parse date
            try:
                appt_dt = datetime.strptime(f"{ngay} {gio}", "%d/%m/%Y %H:%M")
            except Exception:
                try:
                    appt_dt = datetime.strptime(ngay, "%d/%m/%Y")
                except Exception:
                    stats["appt_skipped_no_date"] += 1
                    continue

            note = f"[HenTaiKham legacy SUNS]{' — ' + noi if noi else ''}"

            # Idempotent: kiểm tra đã có chưa (patient_ref + appointment_at gần ngày đó)
            with conn.cursor() as c2:
                c2.execute("""
                    SELECT id FROM diab_his_sch_appointments
                    WHERE tenant_id=%s AND patient_ref=%s
                      AND DATE(appointment_at)=DATE(%s)
                      AND note LIKE '%%HenTaiKham legacy%%'
                      AND deleted_at IS NULL
                """, (TENANT, row["patient_id"], appt_dt))
                if c2.fetchone():
                    stats["appt_skipped_exists"] += 1
                    continue

            status = "PENDING" if appt_dt.date() > TODAY else "NO_SHOW"
            if status == "NO_SHOW":
                stats["appt_no_show"] += 1
            else:
                stats["appt_pending"] += 1

            if not dry:
                with conn.cursor() as c2:
                    c2.execute("""
                        INSERT INTO diab_his_sch_appointments
                            (tenant_id, patient_ref, appointment_at, duration_minutes,
                             status, source, note, created_at, updated_at)
                        VALUES (%s, %s, %s, 30, %s, 'APP', %s, NOW(), NOW())
                    """, (TENANT, row["patient_id"], appt_dt, status, note[:500]))
                    stats["appt_inserted"] += 1
            else:
                stats["appt_inserted"] += 1

    if not dry:
        conn.commit()
    return stats

# ─── Bước 5: Tạo hóa đơn lịch sử từ ChiPhi ──────────────────────────────────
def step5_billing(conn, dry) -> dict:
    """Đọc ChiPhi từ structured_values_json → insert diab_his_bil_billing + items."""
    stats = {"billing_inserted": 0, "billing_items": 0, "billing_skipped": 0}

    def item_type(ma_dv: str) -> str:
        ma = (ma_dv or "").upper()
        if ma.startswith("KB"):      return "SERVICE"
        if ma.startswith("THUOC"):   return "DRUG"
        if ma.startswith("XN"):      return "LAB"
        if ma.startswith("CDHA"):    return "RADIOLOGY"
        return "SERVICE"

    with conn.cursor() as c:
        c.execute("""
            SELECT em.encounter_id, em.structured_values_json,
                   enc.patient_id, enc.started_at, enc.encounter_no
            FROM diab_his_enc_emr_contents em
            JOIN diab_his_enc_encounters enc ON enc.id = em.encounter_id AND enc.tenant_id=%s
            WHERE em.tenant_id=%s AND enc.encounter_no LIKE 'LEG-%%'
              AND em.structured_values_json IS NOT NULL
        """, (TENANT, TENANT))
        rows = c.fetchall()

    for row in rows:
        try:
            legacy = json.loads(row["structured_values_json"])
        except Exception:
            continue
        chi_phi = legacy.get("ChiPhi") or []
        if not chi_phi:
            continue

        enc_id = row["encounter_id"]
        bill_no = f"LEG-{row['encounter_no'].replace('LEG-', '')}"

        # Idempotent: kiểm tra đã có billing cho encounter này chưa
        with conn.cursor() as c2:
            c2.execute("""
                SELECT id FROM diab_his_bil_billing
                WHERE tenant_id=%s AND encounter_id=%s AND deleted_at IS NULL
                  AND (note LIKE '%%SUNS legacy%%' OR bill_no LIKE 'LEG-%%')
            """, (TENANT, enc_id))
            if c2.fetchone():
                stats["billing_skipped"] += 1
                continue

        # Tính tổng
        subtotal = sum(
            float(item.get("ThanhTien") or item.get("TongCP") or 0)
            for item in chi_phi
        )
        has_bhyt = any(item.get("TinhBHYT") for item in chi_phi)
        payer    = "BHYT" if has_bhyt else "SELF"
        finalized_at = row["started_at"]

        billing_id = uid()
        if not dry:
            with conn.cursor() as c2:
                c2.execute("""
                    INSERT INTO diab_his_bil_billing
                        (id, tenant_id, patient_id, encounter_id, bill_no,
                         payer, subtotal, vat_total, discount_amount, bhyt_amount,
                         patient_payable, paid_amount, balance,
                         status, note, finalized_at, created_at, updated_at)
                    VALUES (%s,%s,%s,%s,%s,
                            %s,%s,0,0,0,
                            %s,%s,0,
                            'PAID',%s,%s,NOW(),NOW())
                """, (billing_id, TENANT, row["patient_id"], enc_id, bill_no,
                      payer, subtotal,
                      subtotal, subtotal,
                      "[Lịch sử SUNS legacy — thanh toán đã hoàn tất]",
                      finalized_at))
                stats["billing_inserted"] += 1

            # Billing items
            for item in chi_phi:
                ma_dv   = (item.get("MaDV") or "")[:50]
                ten_dv  = (item.get("TenDV") or "")[:255]
                so_luong = float(item.get("SoLuong") or 1)
                don_gia  = float(item.get("DonGia") or 0)
                thanh_tien = float(item.get("ThanhTien") or item.get("TongCP") or 0)
                bhyt_ap = bool(item.get("TinhBHYT"))
                bhyt_amt= float(item.get("BHChi") or 0)

                with conn.cursor() as c2:
                    c2.execute("""
                        INSERT INTO diab_his_bil_billing_items
                            (billing_id, tenant_id, item_type, code, name,
                             quantity, unit_price, vat_rate, discount_percent,
                             line_total, bhyt_applicable, bhyt_amount, created_at)
                        VALUES (%s,%s,%s,%s,%s,
                                %s,%s,0,0,
                                %s,%s,%s,NOW())
                    """, (billing_id, TENANT, item_type(ma_dv), ma_dv, ten_dv,
                          so_luong, don_gia,
                          thanh_tien, 1 if bhyt_ap else 0, bhyt_amt))
                    stats["billing_items"] += c2.rowcount
        else:
            stats["billing_inserted"] += 1
            stats["billing_items"] += len(chi_phi)

    if not dry:
        conn.commit()
    return stats

# ─── Bước 6: Báo cáo TapTin ──────────────────────────────────────────────────
def step6_taptin(conn) -> dict:
    """Đếm TapTin trong JSON và so với fil_files đã migrate."""
    with conn.cursor() as c:
        c.execute("""
            SELECT COUNT(*) AS n,
                   SUM(JSON_LENGTH(JSON_EXTRACT(structured_values_json,'$.TapTin'))) AS total_files
            FROM diab_his_enc_emr_contents
            WHERE tenant_id=%s AND structured_values_json IS NOT NULL
              AND JSON_LENGTH(JSON_EXTRACT(structured_values_json,'$.TapTin')) > 0
        """, (TENANT,))
        r = c.fetchone()
    encs_with_files = r["n"] or 0
    total_files     = r["total_files"] or 0

    with conn.cursor() as c:
        c.execute("SELECT COUNT(*) AS n FROM fil_files WHERE tenant_id=%s AND deleted_at IS NULL", (TENANT,))
        r2 = c.fetchone()
    fil_count = r2["n"] or 0

    return {
        "taptin_encounters": encs_with_files,
        "taptin_total_refs": int(total_files),
        "fil_files_migrated": fil_count,
        "note": "File thật ở VStorage SUNS cũ; chỉ metadata đã migrate vào fil_files. Không tải lại.",
    }

# ─── Bước 7: Cập nhật EMR thêm section "Dữ liệu gốc SUNS" ───────────────────
def step7_update_emr(conn, dry) -> dict:
    """Thêm section cuối vào tiptap doc: KetQuaDichVu, VatTu, PhieuDieuTri."""
    def _text(s): return [{"type": "text", "text": s}] if s else []
    def _p(s): return {"type": "paragraph", "content": _text(s)} if s else {"type": "paragraph"}
    def _h2(s): return {"type": "heading", "attrs": {"level": 2}, "content": _text(s)}
    def _bullet(items):
        return {"type": "bulletList", "content": [
            {"type": "listItem", "content": [_p(it)]} for it in items
        ]}

    stats = {"emr_updated": 0, "emr_skipped_no_data": 0, "emr_skipped_exists": 0}

    with conn.cursor() as c:
        c.execute("""
            SELECT id, encounter_id, content_json, structured_values_json
            FROM diab_his_enc_emr_contents
            WHERE tenant_id=%s AND structured_values_json IS NOT NULL
        """, (TENANT,))
        rows = c.fetchall()

    for row in rows:
        try:
            legacy = json.loads(row["structured_values_json"])
        except Exception:
            continue

        # Kiểm tra đã có section này chưa
        try:
            doc = json.loads(row["content_json"] or '{"type":"doc","content":[]}')
        except Exception:
            continue

        existing_texts = json.dumps(doc)
        if "Dữ liệu gốc SUNS" in existing_texts:
            stats["emr_skipped_exists"] += 1
            continue

        ket_qua_dv  = legacy.get("KetQuaDichVu") or []
        vat_tu      = legacy.get("VatTu") or []
        phieu_dt    = legacy.get("PhieuDieuTri") or []

        new_nodes = []

        if ket_qua_dv:
            new_nodes.append(_h2("Kết quả dịch vụ (nguồn SUNS)"))
            items = []
            for kq in ket_qua_dv:
                ten  = kq.get("TenDV") or kq.get("MaDV") or ""
                ketq = kq.get("KetQua") or ""
                items.append(f"{ten}: {ketq}" if ketq else ten)
            if items:
                new_nodes.append(_bullet(items))

        if vat_tu:
            new_nodes.append(_h2("Vật tư (nguồn SUNS)"))
            items = [f"{v.get('TenVT','')}: SL {v.get('SoLuong','')}" for v in vat_tu]
            if items:
                new_nodes.append(_bullet(items))

        if phieu_dt:
            new_nodes.append(_h2("Phiếu điều trị (nguồn SUNS)"))
            items = [json.dumps(p, ensure_ascii=False)[:200] for p in phieu_dt]
            if items:
                new_nodes.append(_bullet(items))

        if not new_nodes:
            stats["emr_skipped_no_data"] += 1
            continue

        # Thêm header "Dữ liệu gốc SUNS"
        suns_section = [_h2("Dữ liệu gốc SUNS")] + new_nodes

        doc.setdefault("content", [])
        doc["content"].extend(suns_section)
        new_json = json.dumps(doc, ensure_ascii=False)

        if not dry:
            with conn.cursor() as c2:
                c2.execute("""
                    UPDATE diab_his_enc_emr_contents
                    SET content_json=%s, updated_at=NOW()
                    WHERE id=%s
                """, (new_json, row["id"]))
                stats["emr_updated"] += c2.rowcount
        else:
            stats["emr_updated"] += 1

    if not dry:
        conn.commit()
    return stats

# ─── Verify ───────────────────────────────────────────────────────────────────
def do_verify(conn) -> dict:
    """Kiểm tra nhanh sau khi chạy."""
    with conn.cursor() as c:
        def q(sql, *args):
            c.execute(sql, args)
            r = c.fetchone()
            return list(r.values())[0] if r else 0

        return {
            "patients_active": q("SELECT COUNT(*) FROM diab_his_pat_patients WHERE tenant_id=%s AND deleted_at IS NULL", TENANT),
            "encounters_leg":  q("SELECT COUNT(*) FROM diab_his_enc_encounters WHERE tenant_id=%s AND encounter_no LIKE 'LEG-%%'", TENANT),
            "diagnoses_total": q("SELECT COUNT(*) FROM diab_his_enc_diagnoses WHERE tenant_id=%s AND deleted_at IS NULL", TENANT),
            "diagnoses_r69":   q("SELECT COUNT(*) FROM diab_his_enc_diagnoses WHERE tenant_id=%s AND icd10_code='R69' AND deleted_at IS NULL", TENANT),
            "vitals_total":    q("SELECT COUNT(*) FROM diab_his_enc_vital_signs WHERE tenant_id=%s", TENANT),
            "lab_active":      q("SELECT COUNT(*) FROM diab_his_lab_results WHERE tenant_id=%s AND deleted_at IS NULL", TENANT),
            "lab_deleted":     q("SELECT COUNT(*) FROM diab_his_lab_results WHERE tenant_id=%s AND deleted_at IS NOT NULL", TENANT),
            "appointments":    q("SELECT COUNT(*) FROM diab_his_sch_appointments WHERE tenant_id=%s AND deleted_at IS NULL", TENANT),
            "billing":         q("SELECT COUNT(*) FROM diab_his_bil_billing WHERE tenant_id=%s AND deleted_at IS NULL", TENANT),
            "billing_items":   q("SELECT COUNT(*) FROM diab_his_bil_billing_items WHERE tenant_id=%s", TENANT),
        }

# ─── Verify chi tiết LEG-518 ──────────────────────────────────────────────────
def verify_leg518(conn) -> dict:
    """Đối chiếu nguồn ↔ đích cho LEG-518."""
    with conn.cursor() as c:
        c.execute("""
            SELECT enc.id enc_id, enc.encounter_no, enc.status, enc.started_at,
                   enc.patient_id,
                   pat.full_name, pat.code pat_code
            FROM diab_his_enc_encounters enc
            JOIN diab_his_pat_patients pat ON pat.id=enc.patient_id
            WHERE enc.tenant_id=%s AND enc.encounter_no='LEG-518'
        """, (TENANT,))
        enc = c.fetchone() or {}

    enc_id = enc.get("enc_id")
    pat_id = enc.get("patient_id")
    if not enc_id:
        return {"error": "LEG-518 not found"}

    with conn.cursor() as c:
        c.execute("SELECT icd10_code, name, type FROM diab_his_enc_diagnoses WHERE encounter_id=%s AND deleted_at IS NULL", (enc_id,))
        diags = c.fetchall()
    with conn.cursor() as c:
        c.execute("SELECT weight_kg, height_cm, heart_rate_bpm, temperature_c, spo2_percent FROM diab_his_enc_vital_signs WHERE encounter_id=%s", (enc_id,))
        vitals = c.fetchall()
    with conn.cursor() as c:
        c.execute("SELECT bill_no, subtotal, status FROM diab_his_bil_billing WHERE encounter_id=%s AND deleted_at IS NULL", (enc_id,))
        bills = c.fetchall()
    with conn.cursor() as c:
        c.execute("SELECT DATE(appointment_at) appt_date, status FROM diab_his_sch_appointments WHERE patient_ref=%s AND note LIKE '%%HenTaiKham%%' AND deleted_at IS NULL", (pat_id,))
        appts = c.fetchall()
    with conn.cursor() as c:
        c.execute("SELECT COUNT(*) AS n FROM diab_his_lab_results WHERE encounter_id=%s AND deleted_at IS NULL", (enc_id,))
        lab_active = c.fetchone()["n"]

    return {
        "encounter": enc,
        "diagnoses": diags,
        "vitals": vitals,
        "billing": bills,
        "appointments": appts,
        "lab_active": lab_active,
    }

# ─── Main ─────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--skip-backup", action="store_true")
    parser.add_argument("--step", type=int, default=0, help="Chỉ chạy bước cụ thể (0=tất cả)")
    args = parser.parse_args()

    dry = args.dry_run
    if dry:
        print("=== DRY RUN — không ghi gì vào DB ===")

    # Lấy password từ env hoặc docker inspect
    global DST_PASS
    if not DST_PASS:
        r = subprocess.run(
            "docker inspect pro-his-db --format '{{range .Config.Env}}{{println .}}{{end}}'",
            shell=True, capture_output=True, text=True
        )
        for line in r.stdout.splitlines():
            if line.startswith("MYSQL_ROOT_PASSWORD="):
                DST_PASS = line.split("=", 1)[1]
                break
    if not DST_PASS:
        sys.exit("Không lấy được MYSQL_ROOT_PASSWORD từ pro-his-db!")

    # Backup trước khi ghi
    backup_path = ""
    if not dry and not args.skip_backup:
        backup_path = do_backup()

    conn = connect_dst()

    all_stats = {"backup": backup_path, "dry_run": dry}

    try:
        run_all = (args.step == 0)

        if run_all or args.step in (1, 3):
            print("\n=== Bước 1+3: Fix chẩn đoán + soft-delete fake lab ===")
            s = step1_fix_diagnoses(conn, dry)
            all_stats["step1_3"] = s
            print(json.dumps(s, ensure_ascii=False))

        if run_all or args.step == 2:
            print("\n=== Bước 2: Bổ sung sinh hiệu ===")
            s = step2_vital_signs(conn, dry)
            all_stats["step2"] = s
            print(json.dumps(s, ensure_ascii=False))

        if run_all or args.step == 4:
            print("\n=== Bước 4: Tạo lịch hẹn ===")
            s = step4_appointments(conn, dry)
            all_stats["step4"] = s
            print(json.dumps(s, ensure_ascii=False))

        if run_all or args.step == 5:
            print("\n=== Bước 5: Tạo hóa đơn lịch sử ===")
            s = step5_billing(conn, dry)
            all_stats["step5"] = s
            print(json.dumps(s, ensure_ascii=False))

        if run_all or args.step == 6:
            print("\n=== Bước 6: Báo cáo TapTin ===")
            s = step6_taptin(conn)
            all_stats["step6"] = s
            print(json.dumps(s, ensure_ascii=False))

        if run_all or args.step == 7:
            print("\n=== Bước 7: Cập nhật EMR section SUNS ===")
            s = step7_update_emr(conn, dry)
            all_stats["step7"] = s
            print(json.dumps(s, ensure_ascii=False))

        print("\n=== Verify tổng quát ===")
        v = do_verify(conn)
        all_stats["verify"] = v
        print(json.dumps(v, ensure_ascii=False))

        print("\n=== Verify LEG-518 ===")
        v518 = verify_leg518(conn)
        all_stats["verify_leg518"] = v518
        print(json.dumps(v518, ensure_ascii=False, default=str))

    finally:
        conn.close()

    # Lưu stats
    out_path = f"/tmp/completeness_stats_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(all_stats, f, ensure_ascii=False, indent=2, default=str)
    print(f"\n[done] Stats → {out_path}")
    print(json.dumps(all_stats, ensure_ascii=False, indent=2, default=str))

if __name__ == "__main__":
    main()
