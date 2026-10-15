#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
import_suns_master.py — Nạp master data từ SUNS vào pro-his-db (tenant_id=1)
=============================================================================
Dữ liệu nguồn:
  - docs/migration/suns-master-20261016.json   (phòng, nhân sự, phác đồ, gói DV, ...)
  - docs/migration/suns-catalog-20261015.json  (danh mục dịch vụ + thuốc SUNS)

Sử dụng:
  export PRO_HIS_DB_HOST=127.0.0.1
  export PRO_HIS_DB_PORT=3316          # port forward hoặc nội bộ
  export PRO_HIS_DB_USER=root
  export PRO_HIS_DB_PASS=<mật khẩu>
  export PRO_HIS_DB_NAME=prodiab_his
  export PRO_HIS_TENANT_ID=1
  export PRO_HIS_BRANCH_ID=1
  python3 docs/migration/import_suns_master.py [--dry-run]

Hoặc chạy qua SSH tunnel:
  ssh -p 234 -i KEY -L 3316:127.0.0.1:3306 stackops@192.168.1.4 -N &
  PRO_HIS_DB_PORT=3316 python3 docs/migration/import_suns_master.py

Idempotent: có thể chạy lại. Insert dùng ON DUPLICATE KEY UPDATE hoặc
INSERT IGNORE; không xoá dữ liệu đang tồn tại.

Quy trình:
  1. Thêm cột title/license_number/suns_code (nếu chưa có) — migration SQL 9219
  2. Nạp phòng khám (diab_his_sys_rooms)
  3. Cập nhật nhân sự: title, license_number, suns_code cho 30 user đã có
  4. Tạo phác đồ chỉ định (diab_his_cls_order_templates, owner = admin user)
  5. Nạp gói dịch vụ (diab_his_bil_service_packages + items)
  6. Khớp giá LEG-* drugs từ SUNS catalog (hoạt chất + hàm lượng)
  7. Báo cáo
"""

import json
import os
import re
import sys
import uuid
from pathlib import Path
from datetime import datetime

# ------------------------------------------------------------------
# Dependency check
# ------------------------------------------------------------------
try:
    import mysql.connector
except ImportError:
    print("[ERROR] Cần cài mysql-connector-python: pip install mysql-connector-python")
    sys.exit(1)

DRY_RUN = "--dry-run" in sys.argv

# ------------------------------------------------------------------
# Config từ environment
# ------------------------------------------------------------------
DB_HOST   = os.environ.get("PRO_HIS_DB_HOST", "127.0.0.1")
DB_PORT   = int(os.environ.get("PRO_HIS_DB_PORT", "3316"))
DB_USER   = os.environ.get("PRO_HIS_DB_USER", "root")
DB_PASS   = os.environ.get("PRO_HIS_DB_PASS", "")
DB_NAME   = os.environ.get("PRO_HIS_DB_NAME", "prodiab_his")
TENANT_ID = int(os.environ.get("PRO_HIS_TENANT_ID", "1"))
BRANCH_ID = int(os.environ.get("PRO_HIS_BRANCH_ID", "1"))

SCRIPT_DIR  = Path(__file__).parent
MASTER_JSON = SCRIPT_DIR / "suns-master-20261016.json"
CATALOG_JSON = SCRIPT_DIR / "suns-catalog-20261015.json"

# ------------------------------------------------------------------
# Helpers
# ------------------------------------------------------------------
def log(msg):
    print(f"[{datetime.now().strftime('%H:%M:%S')}] {msg}")

def gen_uuid():
    return str(uuid.uuid4())

def connect():
    return mysql.connector.connect(
        host=DB_HOST, port=DB_PORT,
        user=DB_USER, password=DB_PASS,
        database=DB_NAME,
        charset="utf8mb4", collation="utf8mb4_0900_ai_ci",
        autocommit=False,
    )

# Thống kê
stats = {"added": {}, "updated": {}, "skipped": {}, "warnings": []}

def record(action, key, detail=""):
    stats[action].setdefault(key, 0)
    stats[action][key] += 1
    if detail:
        stats["warnings"].append(f"[{action.upper()}:{key}] {detail}")

# ------------------------------------------------------------------
# Bước 0: Ensure columns tồn tại (chạy migration 9219 nếu cần)
# ------------------------------------------------------------------
def ensure_columns(conn):
    log("Bước 0: Kiểm tra / thêm cột title, license_number, suns_code...")
    cur = conn.cursor()
    extra_cols = {
        "diab_his_sec_users": [
            ("title",          "VARCHAR(50) NULL COMMENT 'Chức danh: BS/ThS/TS/BSCKI/BSCKII'"),
            ("license_number", "VARCHAR(100) NULL COMMENT 'Số chứng chỉ hành nghề y'"),
            ("suns_code",      "VARCHAR(20) NULL COMMENT 'Mã nhân sự SUNS'"),
        ],
        "diab_his_sys_rooms": [
            ("suns_code",      "VARCHAR(20) NULL COMMENT 'Mã phòng SUNS'"),
        ],
    }
    for table, cols in extra_cols.items():
        for col, definition in cols:
            cur.execute("""
                SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA=%s AND TABLE_NAME=%s AND COLUMN_NAME=%s
            """, (DB_NAME, table, col))
            exists = cur.fetchone()[0]
            if not exists:
                if not DRY_RUN:
                    cur.execute(f"ALTER TABLE `{table}` ADD COLUMN `{col}` {definition}")
                    conn.commit()
                log(f"  + ADD COLUMN {table}.{col}")
            else:
                log(f"  = SKIP (đã có) {table}.{col}")
    cur.close()

# ------------------------------------------------------------------
# Bước 1: Nạp phòng khám
# ------------------------------------------------------------------
ROOM_TYPE_MAP = {
    "KHAM":  "EXAM",
    "CDHA":  "RADIOLOGY",
    "XN":    "LAB",
    "CASHIER": "CASHIER",
}

def import_rooms(conn, data):
    log("Bước 1: Nạp phòng khám...")
    cur = conn.cursor(dictionary=True)

    # Lấy các phòng demo mặc định (PK01, PK02, XN01, TC01) đang có
    cur.execute(
        "SELECT code, id FROM diab_his_sys_rooms WHERE tenant_id=%s AND deleted_at IS NULL",
        (TENANT_ID,)
    )
    existing = {r["code"]: r["id"] for r in cur.fetchall()}

    # Map mã phòng demo → mã SUNS tương ứng để đổi tên
    DEMO_REMAP = {
        "PK01": "PK001",   # đổi tên sang Phòng khám DiaB
        "PK02": "PK02",    # BSGĐ (trùng mã)
        "XN01": "PK05",    # Phòng Xét Nghiệm
    }
    # TC01 (quầy thu ngân) không có trong SUNS → giữ nguyên

    suns_rooms = data["phong_kham"]

    # Build map suns_code → room_type
    for pk in suns_rooms:
        suns_code = pk["ma"]
        ten       = pk["ten"]
        loai      = pk.get("loai", "KHAM")
        room_type = ROOM_TYPE_MAP.get(loai, "EXAM")

        # Tìm phòng demo tương ứng để ghi đè
        demo_code = None
        for dc, sc in DEMO_REMAP.items():
            if sc == suns_code:
                demo_code = dc
                break

        if demo_code and demo_code in existing:
            # Cập nhật phòng demo: đổi tên + gắn suns_code
            if not DRY_RUN:
                cur.execute(
                    """UPDATE diab_his_sys_rooms
                       SET name=%s, room_type=%s, suns_code=%s, updated_at=NOW()
                       WHERE id=%s""",
                    (ten, room_type, suns_code, existing[demo_code])
                )
                conn.commit()
            log(f"  ~ UPDATE {demo_code} → '{ten}' ({room_type}) suns_code={suns_code}")
            record("updated", "rooms")
        elif suns_code in existing or any(r.get("suns_code") == suns_code for r in []):
            # Đã có room với suns_code này (idempotent)
            # Kiểm tra bằng suns_code column
            cur.execute(
                "SELECT id FROM diab_his_sys_rooms WHERE tenant_id=%s AND suns_code=%s AND deleted_at IS NULL",
                (TENANT_ID, suns_code)
            )
            row = cur.fetchone()
            if row:
                log(f"  = SKIP (đã tồn tại suns_code={suns_code})")
                record("skipped", "rooms")
                continue
            # Insert mới
            _insert_room(cur, conn, suns_code, ten, room_type)
        else:
            # Kiểm tra suns_code đã tồn tại
            cur.execute(
                "SELECT id FROM diab_his_sys_rooms WHERE tenant_id=%s AND suns_code=%s AND deleted_at IS NULL",
                (TENANT_ID, suns_code)
            )
            if cur.fetchone():
                log(f"  = SKIP (suns_code={suns_code} đã có)")
                record("skipped", "rooms")
            else:
                _insert_room(cur, conn, suns_code, ten, room_type)

    cur.close()

def _insert_room(cur, conn, suns_code, ten, room_type):
    new_id = gen_uuid()
    if not DRY_RUN:
        cur.execute(
            """INSERT INTO diab_his_sys_rooms
               (id, tenant_id, branch_id, code, name, room_type, suns_code, is_active, created_at)
               VALUES (%s,%s,%s,%s,%s,%s,%s,1,NOW())""",
            (new_id, TENANT_ID, BRANCH_ID, suns_code, ten, room_type, suns_code)
        )
        conn.commit()
    log(f"  + INSERT {suns_code} '{ten}' ({room_type})")
    record("added", "rooms")

# ------------------------------------------------------------------
# Bước 2: Cập nhật nhân sự (title, license_number, suns_code)
# ------------------------------------------------------------------
TITLE_PATTERNS = [
    (r"^(BSCKII)\b",     "BSCKII"),
    (r"^(BSCKI)\b",      "BSCKI"),
    (r"^ThS BSCKI\b",    "ThS BSCKI"),
    (r"^ThS BS\b",       "ThS BS"),
    (r"^TS BS\b",        "TS BS"),
    (r"^BS\b",           "BS"),
    (r"^ThS\b",          "ThS"),
    (r"^TS\b",           "TS"),
]

def extract_title(ten: str) -> str:
    """Tách chức danh đứng đầu tên, ví dụ 'BSCKI. Trương Lan Đài' → 'BSCKI'"""
    clean = ten.strip().rstrip(".")
    for pattern, title in TITLE_PATTERNS:
        if re.match(pattern, clean, re.IGNORECASE):
            return title
    return ""

def import_nhan_su(conn, data):
    log("Bước 2: Cập nhật nhân sự (title, license_number, suns_code)...")
    cur = conn.cursor(dictionary=True)

    # Load user map từ email → id
    cur.execute(
        "SELECT id, email, full_name FROM diab_his_sec_users WHERE tenant_id=%s AND deleted_at IS NULL",
        (TENANT_ID,)
    )
    users_by_email = {r["email"]: r for r in cur.fetchall()}

    for nv in data["nhan_su"]:
        user_key = nv.get("user")
        if not user_key:
            log(f"  ~ SKIP NV {nv['ma']} {nv['ten']}: không có user → không tạo tài khoản đăng nhập")
            record("skipped", "nhan_su", f"{nv['ma']} không có user")
            continue

        email = f"{user_key}@diab.com.vn"
        user = users_by_email.get(email)
        if not user:
            log(f"  ! WARN NV {nv['ma']}: email {email} không tìm thấy trong DB")
            record("skipped", "nhan_su", f"{nv['ma']} email {email} không có trong DB")
            continue

        title = extract_title(nv["ten"])
        cchn  = nv.get("cchn") or None

        if not DRY_RUN:
            cur.execute(
                """UPDATE diab_his_sec_users
                   SET title=%s, license_number=%s, suns_code=%s, updated_at=NOW()
                   WHERE id=%s""",
                (title or None, cchn, nv["ma"], user["id"])
            )
            conn.commit()
        log(f"  ~ UPDATE {email}: title='{title}' cchn='{cchn}' suns={nv['ma']}")
        record("updated", "nhan_su")

    cur.close()

# ------------------------------------------------------------------
# Bước 3: Phác đồ chỉ định (order templates)
# API filter theo doctor_id = current user, nên cần tạo 1 bản cho mỗi BS.
# Chiến lược: tạo cho tất cả user có role bac_si (qua join role_permissions).
# Để giảm phức tạp: tạo cho tất cả user tenant (mỗi người khi login sẽ thấy
# đủ bộ phác đồ). Kiểm tra trùng bằng (tenant_id, doctor_id, name, deleted_at).
# ------------------------------------------------------------------
def import_phac_do(conn, data):
    log("Bước 3: Nạp phác đồ chỉ định (order templates, 1 bản/user)...")
    cur = conn.cursor(dictionary=True)

    # Lấy danh sách tất cả user có role bac_si (và admin để fallback)
    cur.execute(
        """SELECT DISTINCT u.id, u.email FROM diab_his_sec_users u
           JOIN diab_his_sec_user_roles ur ON ur.user_id = u.id
           JOIN diab_his_sec_roles r ON r.id = ur.role_id
           WHERE u.tenant_id=%s AND u.deleted_at IS NULL
             AND r.code IN ('bac_si','admin')""",
        (TENANT_ID,)
    )
    target_users = cur.fetchall()
    if not target_users:
        log("  ! WARN: không tìm thấy user bac_si/admin → bỏ qua phác đồ")
        record("skipped", "phac_do", "không có user bac_si")
        cur.close()
        return
    log(f"  Tạo template cho {len(target_users)} user (bac_si + admin)")

    # Dùng user đầu tiên để validate
    admin_id = target_users[0]["id"]

    # Load map suns_code → lab test code đã có trong DB
    cur.execute("SELECT code FROM diab_his_dict_lab_tests WHERE is_active=1")
    lab_codes_in_db = {r["code"] for r in cur.fetchall()}

    for pd in data["phac_do_chi_dinh"]:
        ten   = pd["ten"]
        items = pd["items"]

        # Lọc chỉ các mã XN tồn tại trong DB
        valid_items = [c for c in items if c in lab_codes_in_db]
        missing     = [c for c in items if c not in lab_codes_in_db]
        if missing:
            record("warnings", "phac_do", f"Phác đồ '{ten}': bỏ qua mã XN chưa có trong DB: {missing}")

        if not valid_items:
            log(f"  ! SKIP phác đồ '{ten}': không có mã XN hợp lệ nào")
            record("skipped", "phac_do")
            continue

        # items_json format: mảng {code, type:"LAB", is_free:false}
        items_json = json.dumps(
            [{"code": c, "type": "LAB", "is_free": False} for c in valid_items],
            ensure_ascii=False
        )

        # Tạo 1 bản cho mỗi user bac_si/admin
        for target_user in target_users:
            uid = target_user["id"]
            cur.execute(
                """SELECT id FROM diab_his_cls_order_templates
                   WHERE tenant_id=%s AND doctor_id=%s AND name=%s AND deleted_at IS NULL""",
                (TENANT_ID, uid, ten)
            )
            if cur.fetchone():
                record("skipped", "phac_do")
                continue

            new_id = gen_uuid()
            if not DRY_RUN:
                cur.execute(
                    """INSERT INTO diab_his_cls_order_templates
                       (id, tenant_id, doctor_id, name, items_json, created_at, created_by)
                       VALUES (%s,%s,%s,%s,%s,NOW(),%s)""",
                    (new_id, TENANT_ID, uid, ten, items_json, admin_id)
                )
                conn.commit()
            record("added", "phac_do")

        log(f"  + phác đồ '{ten}' ({len(valid_items)} XN) × {len(target_users)} users")

    cur.close()

# ------------------------------------------------------------------
# Bước 4: Gói dịch vụ
# ------------------------------------------------------------------
def import_goi_dv(conn, data):
    log("Bước 4: Nạp gói dịch vụ...")
    cur = conn.cursor(dictionary=True)

    # Load map service code → service id
    cur.execute(
        "SELECT id, code FROM diab_his_bil_services WHERE tenant_id=%s AND deleted_at IS NULL",
        (TENANT_ID,)
    )
    service_map = {r["code"]: r["id"] for r in cur.fetchall()}

    for goi in data["goi_dich_vu"]:
        ten     = goi["ten"]
        gia     = goi["gia"]
        giam    = goi.get("giam", 0)
        items   = goi.get("items", [])

        # Tính discount_percent
        discount_pct = round(giam / gia * 100, 2) if gia > 0 else 0

        # Tạo code từ tên (slug đơn giản)
        code = re.sub(r"[^A-Z0-9]", "_", ten.upper())[:45].strip("_")

        # Kiểm tra đã tồn tại
        cur.execute(
            "SELECT id FROM diab_his_bil_service_packages WHERE tenant_id=%s AND code=%s AND deleted_at IS NULL",
            (TENANT_ID, code)
        )
        existing_pkg = cur.fetchone()

        if existing_pkg:
            pkg_id = existing_pkg["id"]
            log(f"  = SKIP (đã có) gói '{ten}' (code={code})")
            record("skipped", "goi_dv")
        else:
            pkg_id = gen_uuid()
            if not DRY_RUN:
                cur.execute(
                    """INSERT INTO diab_his_bil_service_packages
                       (id, tenant_id, code, name, discount_percent, is_active, created_at)
                       VALUES (%s,%s,%s,%s,%s,1,NOW())""",
                    (pkg_id, TENANT_ID, code, ten, discount_pct)
                )
                conn.commit()
            log(f"  + INSERT gói '{ten}' discount={discount_pct}% (code={code})")
            record("added", "goi_dv")

        # Insert items
        for item in items:
            suns_item_code = item[0]
            qty           = item[1]

            svc_id = service_map.get(suns_item_code)
            if not svc_id:
                log(f"    ! WARN item {suns_item_code} không có trong diab_his_bil_services → skip")
                record("skipped", "goi_dv_items", f"{ten}: {suns_item_code} chưa có trong services")
                continue

            # Idempotent check
            cur.execute(
                "SELECT 1 FROM diab_his_bil_service_package_items WHERE package_id=%s AND service_id=%s",
                (pkg_id, svc_id)
            )
            if cur.fetchone():
                record("skipped", "goi_dv_items")
                continue

            if not DRY_RUN:
                cur.execute(
                    """INSERT INTO diab_his_bil_service_package_items
                       (id, package_id, service_id, quantity)
                       VALUES (%s,%s,%s,%s)""",
                    (gen_uuid(), pkg_id, svc_id, qty)
                )
                conn.commit()
            log(f"    + INSERT item {suns_item_code} qty={qty}")
            record("added", "goi_dv_items")

    cur.close()

# ------------------------------------------------------------------
# Bước 5: Khớp giá LEG-* từ SUNS catalog (hoạt chất + hàm lượng)
# Chỉ cập nhật khi khớp DUY NHẤT; ghi danh sách để user duyệt.
# ------------------------------------------------------------------
def normalize_str(s: str) -> str:
    """Chuẩn hóa tên hoạt chất để so sánh."""
    if not s:
        return ""
    s = s.strip().lower()
    # Bỏ ngoặc và nội dung bên trong
    s = re.sub(r"\(.*?\)", "", s)
    # Bỏ số mg/mcg/iu và các ký tự đặc biệt
    s = re.sub(r"\s*\d+\s*(mg|mcg|iu|g|ml|%|ui)\b", "", s)
    s = re.sub(r"[^a-z0-9\s]", " ", s)
    s = re.sub(r"\s+", " ", s).strip()
    return s

def import_drug_prices(conn, catalog_data):
    log("Bước 5: Khớp giá thuốc LEG-* từ SUNS catalog...")
    cur = conn.cursor(dictionary=True)

    # Thuốc SUNS
    suns_drugs = {}
    for row in catalog_data["rows"]:
        nhom = row[0]
        if nhom not in ("THUOC", "THUOCDT"):
            continue
        ma, ten, dvt, don_gia = row[1], row[2], row[3], row[4]
        hoat_chat = row[7] if len(row) > 7 else ""
        ham_luong = row[8] if len(row) > 8 else ""
        if not don_gia or don_gia <= 0:
            continue
        key_name  = normalize_str(ten)
        key_hc    = normalize_str(hoat_chat) if hoat_chat else normalize_str(ten)
        key_hl    = normalize_str(ham_luong)
        suns_drugs[ma] = {
            "ma": ma, "ten": ten, "hoat_chat": hoat_chat,
            "ham_luong": ham_luong, "don_gia": don_gia,
            "key_name": key_name, "key_hc": key_hc, "key_hl": key_hl,
        }

    # Thuốc LEG-* giá 0
    cur.execute(
        """SELECT id, code, name, generic_name, strength, sell_price
           FROM diab_his_pha_drugs
           WHERE code LIKE 'LEG-%' AND (sell_price=0 OR sell_price IS NULL)
           AND deleted_at IS NULL
           ORDER BY code"""
    )
    leg_drugs = cur.fetchall()
    log(f"  Tìm thấy {len(leg_drugs)} thuốc LEG-* giá 0")

    matched    = []  # danh sách để user duyệt
    updated    = 0

    for drug in leg_drugs:
        code      = drug["code"]
        name      = drug["name"] or ""
        gname     = drug["generic_name"] or ""
        strength  = drug["strength"] or ""

        key_name = normalize_str(name)
        key_gn   = normalize_str(gname) if gname else normalize_str(name)
        key_str  = normalize_str(strength)

        # Khớp: hoạt chất (generic_name) khớp với SUNS hoạt_chat hoặc tên
        # + (nếu có hàm lượng) hàm lượng cũng khớp
        candidates = []
        for sma, sd in suns_drugs.items():
            if key_gn and sd["key_hc"] and key_gn == sd["key_hc"]:
                # Khớp hoạt chất
                if key_str and sd["key_hl"] and key_str != sd["key_hl"]:
                    continue  # Hàm lượng không khớp
                candidates.append(sd)
            elif key_name and (key_name == sd["key_name"] or key_name == sd["key_hc"]):
                if key_str and sd["key_hl"] and key_str != sd["key_hl"]:
                    continue
                candidates.append(sd)

        # Bỏ trùng theo giá
        unique_prices = set(c["don_gia"] for c in candidates)

        if len(candidates) == 1 or len(unique_prices) == 1:
            best = candidates[0]
            matched.append({
                "leg_code":   code,
                "leg_name":   name,
                "suns_code":  best["ma"],
                "suns_name":  best["ten"],
                "don_gia":    best["don_gia"],
                "match_type": "unique",
            })
            if not DRY_RUN:
                cur.execute(
                    "UPDATE diab_his_pha_drugs SET sell_price=%s, updated_at=NOW() WHERE code=%s",
                    (best["don_gia"], code)
                )
                conn.commit()
            log(f"  ~ {code} '{name[:40]}' → SUNS {best['ma']} gia={best['don_gia']:,}")
            updated += 1
        elif len(candidates) > 1:
            matched.append({
                "leg_code":  code,
                "leg_name":  name,
                "suns_code": ",".join(c["ma"] for c in candidates[:3]),
                "suns_name": " | ".join(c["ten"] for c in candidates[:3]),
                "don_gia":   None,
                "match_type": "ambiguous",
            })
            log(f"  ? {code} '{name[:35]}' → khớp {len(candidates)} SUNS, cần user chọn")
        else:
            # Không khớp
            pass

    log(f"  Kết quả: {updated} cập nhật giá, {len([m for m in matched if m['match_type']=='ambiguous'])} cần duyệt, "
        f"{len(leg_drugs)-len(matched)} không khớp")

    # Xuất báo cáo khớp
    report_path = SCRIPT_DIR / "drug_price_match_report.json"
    with open(report_path, "w", encoding="utf-8") as f:
        json.dump(matched, f, ensure_ascii=False, indent=2)
    log(f"  Báo cáo khớp thuốc → {report_path}")

    stats["drug_matched"]    = [m for m in matched if m["match_type"] == "unique"]
    stats["drug_ambiguous"]  = [m for m in matched if m["match_type"] == "ambiguous"]
    stats["drug_no_match"]   = len(leg_drugs) - len(matched)
    record("updated", "drug_prices", f"{updated} thuốc LEG-*")

    cur.close()

# ------------------------------------------------------------------
# Main
# ------------------------------------------------------------------
def main():
    print("=" * 65)
    print("import_suns_master.py —", "DRY-RUN MODE" if DRY_RUN else "LIVE MODE")
    print(f"DB: {DB_HOST}:{DB_PORT}/{DB_NAME} tenant={TENANT_ID}")
    print("=" * 65)

    # Load JSON
    if not MASTER_JSON.exists():
        print(f"[ERROR] Không tìm thấy {MASTER_JSON}")
        sys.exit(1)
    if not CATALOG_JSON.exists():
        print(f"[ERROR] Không tìm thấy {CATALOG_JSON}")
        sys.exit(1)

    master_data  = json.loads(MASTER_JSON.read_text(encoding="utf-8"))
    catalog_data = json.loads(CATALOG_JSON.read_text(encoding="utf-8"))

    conn = connect()
    log("Kết nối DB thành công.")

    try:
        ensure_columns(conn)
        import_rooms(conn, master_data)
        import_nhan_su(conn, master_data)
        import_phac_do(conn, master_data)
        import_goi_dv(conn, master_data)
        import_drug_prices(conn, catalog_data)
    finally:
        conn.close()

    # Báo cáo cuối
    print()
    print("=" * 65)
    print("KẾT QUẢ")
    print("=" * 65)
    for action, data in stats.items():
        if action == "warnings" and isinstance(data, list):
            if data:
                print(f"\nCảnh báo ({len(data)}):")
                for w in data:
                    print(f"  {w}")
        elif action in ("drug_matched", "drug_ambiguous", "drug_no_match"):
            continue
        elif isinstance(data, dict):
            for key, cnt in data.items():
                print(f"  {action.upper():8s} {key:25s}: {cnt}")
    print()
    if "drug_matched" in stats:
        print(f"Thuốc LEG-* khớp giá (duy nhất): {len(stats.get('drug_matched', []))}")
        print(f"Thuốc LEG-* cần user duyệt (đa khớp): {len(stats.get('drug_ambiguous', []))}")
        print(f"Thuốc LEG-* không khớp: {stats.get('drug_no_match', 0)}")
    print()
    if DRY_RUN:
        print(">> DRY-RUN: chưa ghi gì vào DB. Chạy lại không có --dry-run để thực thi.")

if __name__ == "__main__":
    main()
