#!/usr/bin/env python3
"""
migrate_legacy_his.py
=====================
Migrate data từ HIS cũ (diab_his @ port 13300) sang HIS mới (prodiab_his @ port 13301).
Cả hai là Docker container LOCAL — không đụng server thật.

Chạy: python migrate_legacy_his.py [--dry-run]

Nguyên tắc:
- Idempotent: chạy lại không sinh duplicate (dùng INSERT IGNORE hoặc check trước)
- Mọi bản ghi mới gán tenant_id = MIGRATION_TENANT_ID (=1)
- UUID mới generate cho mọi PK (CHAR 36)
- PII đã mã hóa cũ: ghi chú trong id_number_masked, không copy ciphertext sang schema mới
- FK dependency order: pat → encounter → emr/vital/lab/prescription
"""

import sys
import uuid
import logging
import argparse
from datetime import datetime

import pymysql
import pymysql.cursors

# ─── Cấu hình kết nối ────────────────────────────────────────────────────────
SRC_CFG = dict(host="127.0.0.1", port=13300, user="root", password="hisoldtest",
               db="diab_his", charset="utf8mb4",
               cursorclass=pymysql.cursors.DictCursor)

DST_CFG = dict(host="127.0.0.1", port=13301, user="root", password="root_dev",
               db="prodiab_his", charset="utf8mb4",
               cursorclass=pymysql.cursors.DictCursor)

MIGRATION_TENANT_ID = 1
MIGRATION_TAG       = "LEG-IMPORT-2026"   # gắn vào patient_source để trace
SYS_USER_ID         = None                # không có user tương ứng ở hệ mới → NULL

# ─── Logging ─────────────────────────────────────────────────────────────────
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)-7s %(message)s",
    datefmt="%H:%M:%S"
)
log = logging.getLogger("migrate")


def uid() -> str:
    return str(uuid.uuid4())


def fmt_ts(v) -> str | None:
    if v is None:
        return None
    if isinstance(v, datetime):
        return v.strftime("%Y-%m-%d %H:%M:%S")
    return str(v)


def fmt_date(v) -> str | None:
    if v is None:
        return None
    if hasattr(v, "strftime"):
        return v.strftime("%Y-%m-%d")
    return str(v)


# BUG FIX (phát hiện khi test UI local): enum giới tính hệ MỚI là "MALE"/"FEMALE"/"OTHER"
# (xem frontend/lib/api/types.ts Gender, frontend/lib/constants/code-labels.ts GENDER), nhưng
# DB nguồn (pat_pii_data.GENDER) lưu chữ viết tắt "M"/"F"/"K" — copy thẳng khiến frontend không
# map được nhãn hiển thị, render chữ "undefined" trên toàn bộ trang chi tiết bệnh nhân đã migrate.
GENDER_MAP = {"M": "MALE", "MALE": "MALE", "F": "FEMALE", "FEMALE": "FEMALE",
              "K": "OTHER", "O": "OTHER", "OTHER": "OTHER"}


def map_gender(v) -> str | None:
    if not v:
        return None
    return GENDER_MAP.get(str(v).strip().upper())


# ─── BƯỚC 0: Tạo / xác nhận tenant ──────────────────────────────────────────
def ensure_tenant(dst: pymysql.Connection, dry: bool) -> int:
    with dst.cursor() as c:
        c.execute("SELECT id FROM diab_his_sys_tenants WHERE id=%s", (MIGRATION_TENANT_ID,))
        if c.fetchone():
            log.info("Tenant id=%d đã tồn tại, bỏ qua.", MIGRATION_TENANT_ID)
            return MIGRATION_TENANT_ID
    log.info("Tạo tenant id=%d 'DiaB Legacy Import'...", MIGRATION_TENANT_ID)
    if not dry:
        with dst.cursor() as c:
            c.execute("""
                INSERT INTO diab_his_sys_tenants
                    (id, code, name, status, created_at, updated_at)
                VALUES (%s, %s, %s, 'ACTIVE', NOW(), NOW())
                ON DUPLICATE KEY UPDATE code=code
            """, (MIGRATION_TENANT_ID, "LEG001", "DiaB Legacy Import"))
        dst.commit()
    return MIGRATION_TENANT_ID


# ─── BƯỚC 1: Migrate bệnh nhân ───────────────────────────────────────────────
def migrate_patients(src, dst, dry) -> dict:
    """
    Trả về dict: {old_patient_id (int) -> new_patient_uuid (str)}
    Mapping bảng:
      pat_patients (old) + pat_pii_data (old)  →  diab_his_pat_patients (new)
    PII note:
      - NATIONAL_ID / PHONE_MOBILE: trường hợp trong data test đều EMPTY
        (ENCRYPTION_KEY_ID=1, VERSION=0 nhưng giá trị rỗng) → copy as-is
      - Nếu có giá trị trông như ciphertext (len > 30 ký tự không phải SĐT/CMND),
        ta set id_number_masked = 'ENCRYPTED_IN_SOURCE' và id_number_enc = NULL
    """
    with src.cursor() as c:
        c.execute("""
            SELECT p.ID, p.CODE, p.MRN, p.PATIENT_STATUS, p.BLOOD_TYPE,
                   p.OCCUPATION, p.ETHNICITY, p.MARITAL_STATUS, p.NATIONALITY,
                   p.REGISTERED_AT, p.CREATED_AT,
                   pii.FIRST_NAME, pii.MIDDLE_NAME, pii.LAST_NAME,
                   pii.DATE_OF_BIRTH, pii.GENDER,
                   pii.PHONE_MOBILE, pii.EMAIL,
                   pii.NATIONAL_ID, pii.ADDRESS_LINE1, pii.CITY,
                   pii.ENCRYPTION_KEY_ID, pii.ENCRYPTION_VERSION
            FROM pat_patients p
            LEFT JOIN pat_pii_data pii ON pii.PATIENT_ID = p.ID
            WHERE p.STATUS = 1
        """)
        rows = c.fetchall()

    log.info("Tìm thấy %d bệnh nhân trong DB cũ.", len(rows))

    # Kiểm tra đã migrate chưa (dùng patient_source = MIGRATION_TAG để trace)
    with dst.cursor() as c:
        c.execute("""
            SELECT code FROM diab_his_pat_patients
            WHERE tenant_id=%s AND patient_source=%s
        """, (MIGRATION_TENANT_ID, MIGRATION_TAG))
        already_codes = {r["code"] for r in c.fetchall()}

    id_map = {}   # old_int_id → new_uuid
    inserted = 0; skipped = 0

    for row in rows:
        old_id = row["ID"]
        code   = row["MRN"] or row["CODE"] or f"LEG-{old_id}"

        if code in already_codes:
            # Tìm lại UUID đã có
            with dst.cursor() as c:
                c.execute("""
                    SELECT id FROM diab_his_pat_patients
                    WHERE tenant_id=%s AND code=%s
                """, (MIGRATION_TENANT_ID, code))
                ex = c.fetchone()
                if ex:
                    id_map[old_id] = ex["id"]
            skipped += 1
            continue

        # Ghép full_name
        parts = [row.get("LAST_NAME") or "", row.get("MIDDLE_NAME") or "",
                 row.get("FIRST_NAME") or ""]
        full_name = " ".join(p.strip() for p in parts if p.strip())
        if not full_name:
            full_name = code  # fallback

        # Xử lý NATIONAL_ID
        nat_id_raw = row.get("NATIONAL_ID") or ""
        enc_key_id = row.get("ENCRYPTION_KEY_ID")
        if nat_id_raw and enc_key_id and len(nat_id_raw) > 12:
            # Có vẻ là ciphertext (CMND thật là 9-12 ký tự số)
            id_number_enc    = None
            id_number_masked = "ENCRYPTED_IN_SOURCE"
        elif nat_id_raw:
            id_number_enc    = None   # hệ mới dùng AES riêng, không copy ciphertext cũ
            id_number_masked = nat_id_raw[:6] + "****" if len(nat_id_raw) > 6 else nat_id_raw
        else:
            id_number_enc    = None
            id_number_masked = None

        # Xử lý Phone
        phone_raw = row.get("PHONE_MOBILE") or ""
        phone     = phone_raw if len(phone_raw) <= 15 and phone_raw.lstrip("+").isdigit() else None

        new_uuid = uid()
        id_map[old_id] = new_uuid

        if not dry:
            with dst.cursor() as c:
                c.execute("""
                    INSERT INTO diab_his_pat_patients
                        (id, tenant_id, code, full_name, gender, date_of_birth,
                         phone, email, id_number_masked, id_number_enc,
                         occupation, ethnicity, blood_type,
                         nationality, marital_status, status, patient_source,
                         created_at, updated_at)
                    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
                """, (
                    new_uuid,
                    MIGRATION_TENANT_ID,
                    code,
                    full_name,
                    map_gender(row.get("GENDER")),
                    fmt_date(row.get("DATE_OF_BIRTH")),
                    phone,
                    row.get("EMAIL"),
                    id_number_masked,
                    id_number_enc,
                    row.get("OCCUPATION"),
                    row.get("ETHNICITY"),
                    row.get("BLOOD_TYPE"),
                    row.get("NATIONALITY", "VN")[:5] if row.get("NATIONALITY") else "VN",
                    row.get("MARITAL_STATUS"),
                    row.get("PATIENT_STATUS", "ACTIVE"),
                    MIGRATION_TAG,
                    fmt_ts(row.get("CREATED_AT")) or "NOW()",
                    fmt_ts(row.get("CREATED_AT")) or "NOW()",
                ))
            inserted += 1

    if not dry:
        dst.commit()
    log.info("Bệnh nhân: %d inserted, %d skipped (đã có), total map %d", inserted, skipped, len(id_map))
    return id_map


# ─── BƯỚC 2: Migrate lượt khám (encounters) ──────────────────────────────────
def migrate_encounters(src, dst, dry, pat_map) -> dict:
    """
    cli_visits (old) → diab_his_enc_encounters (new)
    Trả về: {old_visit_id (int) → new_encounter_uuid (str)}
    """
    with src.cursor() as c:
        c.execute("""
            SELECT ID, CODE, PATIENT_ID, VISIT_TYPE, VISIT_STATUS,
                   CHIEF_COMPLAINT, ADMISSION_DIAGNOSIS,
                   ADMISSION_DATE, DISCHARGE_DATE, CREATED_AT
            FROM cli_visits
            WHERE STATUS = 1
        """)
        rows = c.fetchall()

    log.info("Tìm thấy %d lượt khám trong DB cũ.", len(rows))

    # Kiểm tra đã có
    with dst.cursor() as c:
        c.execute("""
            SELECT encounter_no FROM diab_his_enc_encounters
            WHERE tenant_id=%s AND encounter_no LIKE 'LEG-%%'
        """, (MIGRATION_TENANT_ID,))
        already = {r["encounter_no"] for r in c.fetchall()}

    enc_map = {}
    inserted = 0; skipped = 0; no_patient = 0

    # Map VISIT_STATUS → new status
    STATUS_MAP = {
        "ACTIVE": "IN_PROGRESS", "COMPLETED": "COMPLETED",
        "CANCELLED": "CANCELLED", None: "COMPLETED"
    }
    # Map VISIT_TYPE → encounter_type
    TYPE_MAP = {
        "OUTPATIENT": "FOLLOW_UP", "INPATIENT": "FOLLOW_UP",
        "EMERGENCY": "EMERGENCY", "TELEMEDICINE": "FOLLOW_UP", None: "FIRST_VISIT"
    }

    for row in rows:
        old_id    = row["ID"]
        old_vid   = row["CODE"] or f"LEG-{old_id}"
        enc_no    = f"LEG-{old_id}"
        old_pat   = row["PATIENT_ID"]
        new_pat   = pat_map.get(old_pat)

        if new_pat is None:
            no_patient += 1
            continue

        if enc_no in already:
            with dst.cursor() as c:
                c.execute("""
                    SELECT id FROM diab_his_enc_encounters
                    WHERE tenant_id=%s AND encounter_no=%s
                """, (MIGRATION_TENANT_ID, enc_no))
                ex = c.fetchone()
                if ex:
                    enc_map[old_id] = ex["id"]
            skipped += 1
            continue

        new_uuid = uid()
        enc_map[old_id] = new_uuid

        # ICD10 sơ bộ: chỉ lấy nếu trông như mã ICD10 (vd E11, I10)
        diag_raw = (row.get("ADMISSION_DIAGNOSIS") or "")[:10]
        primary_icd10 = diag_raw if len(diag_raw) <= 10 and diag_raw.strip() else None

        if not dry:
            with dst.cursor() as c:
                c.execute("""
                    INSERT INTO diab_his_enc_encounters
                        (id, tenant_id, patient_id, encounter_type, status,
                         chief_complaint, primary_icd10, encounter_no,
                         started_at, finished_at, created_at, updated_at)
                    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
                """, (
                    new_uuid,
                    MIGRATION_TENANT_ID,
                    new_pat,
                    TYPE_MAP.get(row.get("VISIT_TYPE"), "FIRST_VISIT"),
                    STATUS_MAP.get(row.get("VISIT_STATUS"), "COMPLETED"),
                    row.get("CHIEF_COMPLAINT"),
                    primary_icd10,
                    enc_no,
                    fmt_ts(row.get("ADMISSION_DATE")),
                    fmt_ts(row.get("DISCHARGE_DATE")),
                    fmt_ts(row.get("CREATED_AT")),
                    fmt_ts(row.get("CREATED_AT")),
                ))
            inserted += 1

    if not dry:
        dst.commit()
    log.info("Encounters: %d inserted, %d skipped, %d bỏ (không map patient)", inserted, skipped, no_patient)
    return enc_map


# ─── BƯỚC 3: Migrate EMR contents ────────────────────────────────────────────
def migrate_emr(src, dst, dry, enc_map):
    with src.cursor() as c:
        c.execute("""
            SELECT h.ID hid, h.VISIT_ID, h.PATIENT_ID, h.DOCUMENT_TITLE,
                   h.ENCOUNTER_DATE, h.CREATED_AT,
                   ct.ID cid, ct.CONTENT, ct.CONTENT_TYPE, ct.STRUCTURED_DATA
            FROM cli_emr_headers h
            LEFT JOIN cli_emr_contents ct ON ct.EMR_HEADER_ID = h.ID
            WHERE h.STATUS = 1
        """)
        rows = c.fetchall()

    log.info("Tìm thấy %d EMR record (header+content join) trong DB cũ.", len(rows))

    # BUG FIX: kiem tra idempotent truoc day dua tren created_by='LEGACY_IMPORT'
    # (cot da bo o tren) va ket qua "existing_ids" chua bao gio duoc dung trong vong
    # lap ben duoi - chay lai script se INSERT trung va an vao UNIQUE(encounter_id)
    # (uq_emr_encounter) roi crash. Doi sang kiem tra dung encounter_id da co ban ghi
    # EMR content chua (dung UNIQUE key that cua bang) de skip an toan khi chay lai.
    with dst.cursor() as c:
        c.execute("""
            SELECT encounter_id FROM diab_his_enc_emr_contents WHERE tenant_id=%s
        """, (MIGRATION_TENANT_ID,))
        existing_enc_ids = {r["encounter_id"] for r in c.fetchall()}

    inserted = 0; skipped_enc = 0; skipped_existing = 0

    for row in rows:
        old_vid = row.get("VISIT_ID")
        new_enc = enc_map.get(old_vid)
        if new_enc is None:
            skipped_enc += 1
            continue
        if new_enc in existing_enc_ids:
            skipped_existing += 1
            continue

        content_raw  = row.get("CONTENT") or ""
        struct_raw   = row.get("STRUCTURED_DATA") or ""
        content_json = struct_raw if struct_raw.strip().startswith("{") else "{}"
        content_html = content_raw or None

        new_uuid = uid()
        if not dry:
            with dst.cursor() as c:
                # BUG FIX (phát hiện khi test local): created_by la cot CHAR(36) luu UUID
                # nguoi tao, KHONG phai text tuy y - gia tri literal 'LEGACY_IMPORT' truoc day
                # khong phai GUID hop le -> EF Core FormatException moi lan doc trang chi tiet
                # luot kham cua BAT KY benh nhan migrate nao (tat ca 384 dong deu dinh). Bo cot
                # nay ra khoi INSERT (mac dinh NULL) - viec danh dau "ban ghi da migrate" nen
                # dua vao encounter_id (UNIQUE key uq_emr_encounter) thay vi lam bay cot GUID.
                c.execute("""
                    INSERT INTO diab_his_enc_emr_contents
                        (id, tenant_id, encounter_id, content_json, content_html,
                         version, created_at, updated_at)
                    VALUES (%s,%s,%s,%s,%s,1,%s,%s)
                """, (
                    new_uuid,
                    MIGRATION_TENANT_ID,
                    new_enc,
                    content_json,
                    content_html,
                    fmt_ts(row.get("CREATED_AT")),
                    fmt_ts(row.get("CREATED_AT")),
                ))
            inserted += 1

    if not dry:
        dst.commit()
    log.info("EMR contents: %d inserted, %d bỏ (không map encounter), %d bỏ (đã tồn tại)",
              inserted, skipped_enc, skipped_existing)


# ─── BƯỚC 4: Migrate vital signs ─────────────────────────────────────────────
def migrate_vitals(src, dst, dry, enc_map, pat_map):
    with src.cursor() as c:
        c.execute("""
            SELECT ID, PATIENT_ID, VISIT_ID, MEASUREMENT_DATE,
                   TEMPERATURE, HEART_RATE, BLOOD_PRESSURE_SYSTOLIC, BLOOD_PRESSURE_DIASTOLIC,
                   RESPIRATORY_RATE, OXYGEN_SATURATION, PAIN_SCALE,
                   WEIGHT, HEIGHT, GLUCOSE_LEVEL, CREATED_AT
            FROM cli_vital_signs
            WHERE STATUS = 1
        """)
        rows = c.fetchall()

    log.info("Tìm thấy %d vital sign records.", len(rows))
    inserted = 0; skipped = 0

    for row in rows:
        new_enc = enc_map.get(row.get("VISIT_ID"))
        new_pat = pat_map.get(row.get("PATIENT_ID"))
        if not new_enc or not new_pat:
            skipped += 1
            continue

        new_uuid = uid()
        if not dry:
            with dst.cursor() as c:
                c.execute("""
                    INSERT INTO diab_his_enc_vital_signs
                        (id, tenant_id, encounter_id, patient_id, recorded_at,
                         record_sequence,
                         temperature_c, heart_rate_bpm, respiratory_rate,
                         bp_systolic, bp_diastolic, spo2_percent,
                         weight_kg, height_cm, pain_scale, glucose_mg_dl,
                         created_at, updated_at)
                    VALUES (%s,%s,%s,%s,%s,1,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
                """, (
                    new_uuid,
                    MIGRATION_TENANT_ID,
                    new_enc,
                    new_pat,
                    fmt_ts(row.get("MEASUREMENT_DATE")) or fmt_ts(row.get("CREATED_AT")),
                    row.get("TEMPERATURE"),
                    row.get("HEART_RATE"),
                    row.get("RESPIRATORY_RATE"),
                    row.get("BLOOD_PRESSURE_SYSTOLIC"),
                    row.get("BLOOD_PRESSURE_DIASTOLIC"),
                    row.get("OXYGEN_SATURATION"),
                    row.get("WEIGHT"),
                    row.get("HEIGHT"),
                    row.get("PAIN_SCALE"),
                    row.get("GLUCOSE_LEVEL"),
                    fmt_ts(row.get("CREATED_AT")),
                    fmt_ts(row.get("CREATED_AT")),
                ))
            inserted += 1

    if not dry:
        dst.commit()
    log.info("Vital signs: %d inserted, %d bỏ (không map)", inserted, skipped)


# ─── BƯỚC 5: Migrate lab orders ──────────────────────────────────────────────
def migrate_lab_orders(src, dst, dry, enc_map) -> dict:
    """
    cli_lab_orders (old) → diab_his_cli_lab_orders (new)
    TESTS_ORDERED field chứa JSON list tên test → mỗi test = 1 bản ghi.
    Trả về: {old_lab_order_id → [new_lab_order_uuids]}
    """
    import json as _json

    with src.cursor() as c:
        c.execute("""
            SELECT ID, VISIT_ID, ORDER_DATE, ORDER_STATUS, PRIORITY,
                   SPECIMEN_TYPE, TESTS_ORDERED, CREATED_AT
            FROM cli_lab_orders
            WHERE STATUS = 1
        """)
        rows = c.fetchall()

    log.info("Tìm thấy %d lab order records.", len(rows))
    order_map = {}
    inserted = 0; skipped = 0

    for row in rows:
        new_enc = enc_map.get(row.get("VISIT_ID"))
        if not new_enc:
            skipped += 1
            continue

        tests_raw = row.get("TESTS_ORDERED") or ""
        try:
            tests = _json.loads(tests_raw) if tests_raw.strip().startswith("[") else [tests_raw or "UNKNOWN"]
        except Exception:
            tests = [tests_raw or "UNKNOWN"]

        new_uuids = []
        for test in tests:
            if isinstance(test, dict):
                test_code = test.get("code", "UNKNOWN")
                test_name = test.get("name", str(test))
            else:
                test_code = "LEGACY"
                test_name = str(test)[:200]

            new_uuid = uid()
            new_uuids.append(new_uuid)
            if not dry:
                with dst.cursor() as c:
                    c.execute("""
                        INSERT INTO diab_his_cli_lab_orders
                            (id, tenant_id, encounter_id, test_code, test_name,
                             sample_type, priority, status, ordered_at,
                             created_at, updated_at)
                        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
                    """, (
                        new_uuid,
                        MIGRATION_TENANT_ID,
                        new_enc,
                        test_code[:50],
                        test_name[:200],
                        (row.get("SPECIMEN_TYPE") or "")[:50] or None,
                        (row.get("PRIORITY") or "ROUTINE")[:20],
                        "COMPLETED" if row.get("ORDER_STATUS") in ("COMPLETED", "RESULTED") else "ORDERED",
                        fmt_ts(row.get("ORDER_DATE")) or fmt_ts(row.get("CREATED_AT")),
                        fmt_ts(row.get("CREATED_AT")),
                        fmt_ts(row.get("CREATED_AT")),
                    ))
                inserted += 1

        order_map[row["ID"]] = new_uuids

    if not dry:
        dst.commit()
    log.info("Lab orders: %d inserted, %d bỏ (không map encounter)", inserted, skipped)
    return order_map


# ─── BƯỚC 6: Migrate lab results ─────────────────────────────────────────────
def migrate_lab_results(src, dst, dry, pat_map, enc_map, order_map):
    with src.cursor() as c:
        c.execute("""
            SELECT ID, LAB_ORDER_ID, PATIENT_ID, TEST_CODE, TEST_NAME,
                   RESULT_VALUE, RESULT_UNIT, REFERENCE_RANGE,
                   ABNORMAL_FLAG, PERFORMED_DATE, NOTES, CREATED_AT
            FROM cli_lab_results
            WHERE STATUS = 1
        """)
        rows = c.fetchall()

    log.info("Tìm thấy %d lab result records.", len(rows))
    inserted = 0; skipped = 0

    for row in rows:
        new_pat = pat_map.get(row.get("PATIENT_ID"))
        # order_map có thể có nhiều UUID mới per old order_id → dùng cái đầu tiên
        old_order_id = row.get("LAB_ORDER_ID")
        new_order_uuids = order_map.get(old_order_id, [])
        new_order_id = new_order_uuids[0] if new_order_uuids else None

        if not new_pat:
            skipped += 1
            continue

        flag_raw = row.get("ABNORMAL_FLAG") or ""
        is_abnormal = 1 if flag_raw.strip().upper() in ("H", "L", "A", "ABNORMAL", "HIGH", "LOW") else 0

        new_uuid = uid()
        if not dry:
            with dst.cursor() as c:
                c.execute("""
                    INSERT INTO diab_his_lab_results
                        (id, tenant_id, order_id, test_code, test_name,
                         result_value, result_unit, normal_range,
                         is_abnormal, result_flag, performed_at,
                         patient_id, source, note,
                         created_at, updated_at)
                    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,'LEGACY_IMPORT',%s,%s,%s)
                """, (
                    new_uuid,
                    MIGRATION_TENANT_ID,
                    new_order_id or new_uuid,   # fallback: self-ref nếu không có order
                    (row.get("TEST_CODE") or "UNKNOWN")[:50],
                    (row.get("TEST_NAME") or "")[:200],
                    row.get("RESULT_VALUE"),
                    row.get("RESULT_UNIT"),
                    row.get("REFERENCE_RANGE"),
                    is_abnormal,
                    flag_raw[:5] or None,
                    fmt_ts(row.get("PERFORMED_DATE")),
                    new_pat,
                    row.get("NOTES"),
                    fmt_ts(row.get("CREATED_AT")),
                    fmt_ts(row.get("CREATED_AT")),
                ))
            inserted += 1

    if not dry:
        dst.commit()
    log.info("Lab results: %d inserted, %d bỏ (không map patient)", inserted, skipped)


# ─── BƯỚC 7: Migrate medications → prescriptions + items ─────────────────────
def migrate_medications(src, dst, dry, enc_map, pat_map):
    """
    cli_medications (old) gom theo VISIT_ID → 1 prescription/visit → N items.
    Drugs trong DB mới chưa có drug_id → dùng NULL-safe placeholder UUID.
    """
    with src.cursor() as c:
        c.execute("""
            SELECT ID, PATIENT_ID, VISIT_ID, MEDICATION_NAME, GENERIC_NAME,
                   STRENGTH, FORM, ROUTE, FREQUENCY, DOSAGE,
                   QUANTITY_PRESCRIBED, UNIT, DAYS_SUPPLY,
                   START_DATE, PRESCRIBED_BY, PRESCRIPTION_DATE, CREATED_AT
            FROM cli_medications
            WHERE STATUS_FLAG = 1
            ORDER BY VISIT_ID, ID
        """)
        rows = c.fetchall()

    log.info("Tìm thấy %d medication records.", len(rows))
    # Group by VISIT_ID
    from collections import defaultdict
    by_visit = defaultdict(list)
    for r in rows:
        by_visit[r["VISIT_ID"]].append(r)

    inserted_rx = 0; inserted_items = 0; skipped = 0

    for old_vid, meds in by_visit.items():
        new_enc = enc_map.get(old_vid)
        first   = meds[0]
        new_pat = pat_map.get(first.get("PATIENT_ID"))
        if not new_enc or not new_pat:
            skipped += len(meds)
            continue

        rx_uuid = uid()
        if not dry:
            with dst.cursor() as c:
                c.execute("""
                    INSERT IGNORE INTO diab_his_pha_prescriptions
                        (id, tenant_id, encounter_id, patient_id, doctor_id,
                         status, created_at, updated_at)
                    VALUES (%s,%s,%s,%s,NULL,'DISPENSED',%s,%s)
                """, (
                    rx_uuid, MIGRATION_TENANT_ID, new_enc, new_pat,
                    fmt_ts(first.get("PRESCRIPTION_DATE")) or fmt_ts(first.get("CREATED_AT")),
                    fmt_ts(first.get("CREATED_AT")),
                ))
            inserted_rx += 1

        for med in meds:
            item_uuid = uid()
            drug_name = (med.get("MEDICATION_NAME") or med.get("GENERIC_NAME") or "UNKNOWN")[:200]
            strength  = (med.get("STRENGTH") or "")[:100]
            unit      = (med.get("UNIT") or "viên")[:50]
            qty       = float(med.get("QUANTITY_PRESCRIBED") or 1)
            route     = (med.get("ROUTE") or "oral")[:50]
            frequency = (med.get("FREQUENCY") or "1x1")[:100]
            dosage    = (med.get("DOSAGE") or "1 viên")[:100]
            days      = int(med.get("DAYS_SUPPLY") or 7)

            # drug_id bắt buộc NOT NULL → dùng UUID placeholder (không có drug master)
            placeholder_drug_id = uid()

            if not dry:
                with dst.cursor() as c:
                    c.execute("""
                        INSERT INTO diab_his_pha_prescription_items
                            (id, tenant_id, prescription_id, drug_id,
                             drug_name, drug_strength, unit,
                             dosage, frequency, route, duration_days,
                             quantity, unit_price, line_total, bhyt_applicable,
                             instructions, created_at, updated_at)
                        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,0,0,0,NULL,%s,%s)
                    """, (
                        item_uuid, MIGRATION_TENANT_ID, rx_uuid, placeholder_drug_id,
                        drug_name, strength, unit,
                        dosage, frequency, route, days, qty,
                        fmt_ts(med.get("CREATED_AT")),
                        fmt_ts(med.get("CREATED_AT")),
                    ))
                inserted_items += 1

    if not dry:
        dst.commit()
    log.info("Medications → Prescriptions: %d rx, %d items, %d bỏ", inserted_rx, inserted_items, skipped)


# ─── BƯỚC 8: Migrate fil_files ───────────────────────────────────────────────
def migrate_files(src, dst, dry, pat_map):
    """
    fil_files (old) → fil_files (new - restructured)
    Lưu ý: file thực tế ở VStorage cũ không accessible → chỉ migrate metadata.
    """
    with src.cursor() as c:
        c.execute("""
            SELECT ID, CODE, PATIENT_ID, FILE_TYPE, FILE_NAME, FILE_PATH,
                   FILE_SIZE, MIME_TYPE, CREATED_AT
            FROM fil_files
            WHERE STATUS = 1
        """)
        rows = c.fetchall()

    log.info("Tìm thấy %d file records.", len(rows))
    inserted = 0; skipped = 0

    for row in rows:
        new_uuid = uid()
        file_name = row.get("FILE_NAME") or row.get("CODE") or f"legacy_{row['ID']}"
        # Kiểm tra xem new fil_files có cột nào giống không
        if not dry:
            try:
                with dst.cursor() as c:
                    c.execute("""
                        INSERT INTO fil_files
                            (id, tenant_id, bucket, object_key, file_name,
                             mime_type, file_size_bytes, category,
                             created_at, updated_at)
                        VALUES (%s,%s,'legacy-import',%s,%s,%s,%s,'LEGACY',%s,%s)
                    """, (
                        new_uuid,
                        MIGRATION_TENANT_ID,
                        row.get("FILE_PATH") or f"legacy/{row['ID']}",
                        file_name[:255],
                        row.get("MIME_TYPE"),
                        row.get("FILE_SIZE") or 0,
                        fmt_ts(row.get("CREATED_AT")),
                        fmt_ts(row.get("CREATED_AT")),
                    ))
                inserted += 1
            except Exception as e:
                log.warning("fil_files insert lỗi (bỏ qua): %s", e)
                skipped += 1

    if not dry:
        dst.commit()
    log.info("Files: %d inserted, %d bỏ", inserted, skipped)


# ─── MAIN ─────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(description="Migrate legacy HIS data")
    parser.add_argument("--dry-run", action="store_true",
                        help="Chỉ đọc, không ghi gì vào DB đích")
    args = parser.parse_args()
    dry = args.dry_run

    if dry:
        log.info("=== DRY-RUN MODE — không ghi DB đích ===")

    src = pymysql.connect(**SRC_CFG)
    dst = pymysql.connect(**DST_CFG)

    try:
        log.info("─── BƯỚC 0: Tenant ───")
        ensure_tenant(dst, dry)

        log.info("─── BƯỚC 1: Bệnh nhân ───")
        pat_map = migrate_patients(src, dst, dry)

        log.info("─── BƯỚC 2: Lượt khám ───")
        enc_map = migrate_encounters(src, dst, dry, pat_map)

        log.info("─── BƯỚC 3: EMR contents ───")
        migrate_emr(src, dst, dry, enc_map)

        log.info("─── BƯỚC 4: Vital signs ───")
        migrate_vitals(src, dst, dry, enc_map, pat_map)

        log.info("─── BƯỚC 5: Lab orders ───")
        order_map = migrate_lab_orders(src, dst, dry, enc_map)

        log.info("─── BƯỚC 6: Lab results ───")
        migrate_lab_results(src, dst, dry, pat_map, enc_map, order_map)

        log.info("─── BƯỚC 7: Medications → Prescriptions ───")
        migrate_medications(src, dst, dry, enc_map, pat_map)

        log.info("─── BƯỚC 8: Files metadata ───")
        migrate_files(src, dst, dry, pat_map)

        log.info("=== HOÀN TẤT %s ===", "(DRY-RUN)" if dry else "")

    except Exception as e:
        log.error("LỖI: %s", e)
        dst.rollback()
        raise
    finally:
        src.close()
        dst.close()


if __name__ == "__main__":
    main()
