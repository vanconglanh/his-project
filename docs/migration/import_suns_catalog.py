#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
import_suns_catalog.py
-----------------------
Nạp danh mục dịch vụ + giá từ SUNS (docs/migration/suns-catalog-20261015.json)
vào DB production pro-his-db (prodiab_his).

Chạy TRÊN SERVER (sau khi scp lên):
  python3 import_suns_catalog.py --dry-run
  python3 import_suns_catalog.py

Biến môi trường (có giá trị mặc định hợp lý cho môi trường production):
  DB_CONTAINER   : tên container MySQL (default: pro-his-db)
  DB_NAME        : tên database (default: prodiab_his)
  DB_USER        : user MySQL (default: root)
  DB_PASSWORD    : mật khẩu — BẮT BUỘC, không có default
  TENANT_ID      : tenant id (default: 1)
  CATALOG_FILE   : đường dẫn tới file JSON (default: /tmp/suns-catalog-20261015.json)
  BACKUP_DIR     : thư mục backup (default: /home/stackops/backups/pro-his)

Idempotent: an toàn chạy lại nhiều lần.
"""

import json
import os
import re
import subprocess
import sys
import unicodedata
from datetime import datetime

# ---------------------------------------------------------------------------
# Cấu hình từ env
# ---------------------------------------------------------------------------
DB_CONTAINER = os.environ.get("DB_CONTAINER", "pro-his-db")
DB_NAME      = os.environ.get("DB_NAME", "prodiab_his")
DB_USER      = os.environ.get("DB_USER", "root")
DB_PASSWORD  = os.environ.get("DB_PASSWORD", "")
TENANT_ID    = int(os.environ.get("TENANT_ID", "1"))
CATALOG_FILE = os.environ.get("CATALOG_FILE", "/tmp/suns-catalog-20261015.json")
BACKUP_DIR   = os.environ.get("BACKUP_DIR", "/home/stackops/backups/pro-his")

DRY_RUN = "--dry-run" in sys.argv or "-n" in sys.argv

# ---------------------------------------------------------------------------
# Trợ giúp màu terminal
# ---------------------------------------------------------------------------
def _c(code, text): return f"\033[{code}m{text}\033[0m"
def green(t): return _c("32", t)
def yellow(t): return _c("33", t)
def red(t): return _c("31", t)
def bold(t): return _c("1", t)

def log(msg):      print(f"[INFO]  {msg}")
def warn(msg):     print(yellow(f"[WARN]  {msg}"))
def error(msg):    print(red(f"[ERROR] {msg}"), file=sys.stderr)
def ok(msg):       print(green(f"[OK]    {msg}"))
def section(msg):  print(bold(f"\n=== {msg} ==="))

# ---------------------------------------------------------------------------
# Chạy lệnh mysql qua docker exec
# ---------------------------------------------------------------------------
def mysql(sql: str, fetch: bool = False) -> list[dict] | None:
    """Chạy một câu SQL qua docker exec. Trả về list[dict] nếu fetch=True."""
    if not DB_PASSWORD:
        error("DB_PASSWORD chưa được đặt. Chạy: export DB_PASSWORD='...'")
        sys.exit(1)
    cmd = [
        "docker", "exec", "-e", f"MYSQL_PWD={DB_PASSWORD}",
        DB_CONTAINER,
        "mysql", f"-u{DB_USER}", "--default-character-set=utf8mb4",
        "--batch", "--skip-column-names" if fetch else "--batch",
        DB_NAME,
        "-e", sql,
    ]
    if fetch:
        # dùng -e và parse kết quả tab-separated
        cmd2 = [
            "docker", "exec", "-e", f"MYSQL_PWD={DB_PASSWORD}",
            DB_CONTAINER,
            "mysql", f"-u{DB_USER}", "--default-character-set=utf8mb4",
            "--batch", DB_NAME, "-e", sql,
        ]
        result = subprocess.run(cmd2, capture_output=True, text=True, encoding="utf-8")
        if result.returncode != 0:
            error(f"MySQL error: {result.stderr.strip()}")
            return []
        lines = result.stdout.strip().split("\n")
        if len(lines) < 2:
            return []
        headers = lines[0].split("\t")
        rows = []
        for line in lines[1:]:
            if line:
                vals = line.split("\t")
                rows.append(dict(zip(headers, vals)))
        return rows
    else:
        if DRY_RUN:
            print(f"  [DRY-RUN SQL] {sql[:200]}{'...' if len(sql)>200 else ''}")
            return None
        result = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
        if result.returncode != 0:
            error(f"MySQL error: {result.stderr.strip()}")
            error(f"SQL: {sql[:300]}")
            raise RuntimeError("MySQL exec failed")
        return None


def mysql_many(statements: list[str]) -> None:
    """Chạy nhiều câu SQL cùng lúc."""
    combined = ";\n".join(s.strip().rstrip(";") for s in statements if s.strip()) + ";"
    if DRY_RUN:
        for s in statements[:5]:
            print(f"  [DRY-RUN SQL] {s[:180]}{'...' if len(s)>180 else ''}")
        if len(statements) > 5:
            print(f"  [DRY-RUN] ... và {len(statements)-5} câu SQL nữa")
        return
    if not DB_PASSWORD:
        error("DB_PASSWORD chưa được đặt.")
        sys.exit(1)
    cmd = [
        "docker", "exec", "-e", f"MYSQL_PWD={DB_PASSWORD}",
        "-i", DB_CONTAINER,
        "mysql", f"-u{DB_USER}", "--default-character-set=utf8mb4",
        DB_NAME,
    ]
    result = subprocess.run(cmd, input=combined, capture_output=True, text=True, encoding="utf-8")
    if result.returncode != 0:
        error(f"MySQL error: {result.stderr.strip()}")
        raise RuntimeError("MySQL exec failed")

# ---------------------------------------------------------------------------
# Chuẩn hóa tên để fuzzy match
# ---------------------------------------------------------------------------
def normalize_name(s: str) -> str:
    """Lowercase, bỏ dấu, bỏ khoảng trắng thừa, bỏ ký tự đặc biệt."""
    if not s:
        return ""
    # NFD → tách dấu → bỏ combining marks
    nfd = unicodedata.normalize("NFD", s.lower())
    no_accent = "".join(c for c in nfd if not unicodedata.combining(c))
    # bỏ ký tự không phải chữ/số/khoảng trắng
    cleaned = re.sub(r"[^\w\s]", " ", no_accent)
    return re.sub(r"\s+", " ", cleaned).strip()

# ---------------------------------------------------------------------------
# Escape SQL string
# ---------------------------------------------------------------------------
def esc(val) -> str:
    if val is None:
        return "NULL"
    s = str(val).replace("\\", "\\\\").replace("'", "\\'")
    return f"'{s}'"

def esc_dec(val) -> str:
    if val is None or val == "" or val == 0:
        return "0.00"
    try:
        return f"{float(val):.2f}"
    except (ValueError, TypeError):
        return "0.00"

# ---------------------------------------------------------------------------
# Backup
# ---------------------------------------------------------------------------
def backup_db():
    section("Backup DB trước khi ghi")
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_path = f"{BACKUP_DIR}/prodiab_his_before_suns_{ts}.sql.gz"
    cmd_mkdir = f"mkdir -p {BACKUP_DIR}"
    cmd_dump = (
        f"docker exec -e MYSQL_PWD={DB_PASSWORD} {DB_CONTAINER} "
        f"mysqldump -u{DB_USER} --default-character-set=utf8mb4 "
        f"--single-transaction --routines {DB_NAME} | gzip > {backup_path}"
    )
    if DRY_RUN:
        log(f"[DRY-RUN] Sẽ backup → {backup_path}")
        return backup_path
    subprocess.run(cmd_mkdir, shell=True, check=True)
    log(f"Đang dump → {backup_path} ...")
    subprocess.run(cmd_dump, shell=True, check=True)
    ok(f"Backup xong: {backup_path}")
    return backup_path

# ---------------------------------------------------------------------------
# Load catalog JSON
# ---------------------------------------------------------------------------
def load_catalog():
    with open(CATALOG_FILE, encoding="utf-8") as f:
        data = json.load(f)
    cols = data["columns"]
    rows = []
    for r in data["rows"]:
        rows.append(dict(zip(cols, r)))
    return rows

# ---------------------------------------------------------------------------
# 1. THUỐC
# ---------------------------------------------------------------------------
def migrate_drugs(catalog: list[dict]) -> dict:
    section("1. THUỐC (diab_his_pha_drugs)")
    TENANT = TENANT_ID

    # Lọc THUOC, bỏ rác
    suns_drugs = [
        r for r in catalog
        if r["nhom"] == "THUOC"
        and r["ma"] not in ("THUOC", "1", "")
        and r["ten"] not in ("1", "", None)
    ]
    log(f"SUNS THUOC sau lọc rác: {len(suns_drugs)} mục (tổng 162 - 1 rác = 161)")

    # Loại bỏ trùng tên trong SUNS — giữ bản mã số lớn nhất
    # Trích số từ mã THUOCXXX
    def suns_num(ma: str) -> int:
        m = re.search(r"\d+", ma)
        return int(m.group()) if m else 0

    suns_by_norm: dict[str, dict] = {}
    suns_dup_report: list[str] = []
    for r in suns_drugs:
        nkey = normalize_name(r["ten"])
        if nkey in suns_by_norm:
            existing = suns_by_norm[nkey]
            # giữ bản mã lớn hơn
            if suns_num(r["ma"]) > suns_num(existing["ma"]):
                suns_dup_report.append(
                    f"  TRÙNG: {r['ten']!r} → bỏ {existing['ma']}, giữ {r['ma']}"
                )
                suns_by_norm[nkey] = r
            else:
                suns_dup_report.append(
                    f"  TRÙNG: {r['ten']!r} → giữ {existing['ma']}, bỏ {r['ma']}"
                )
        else:
            suns_by_norm[nkey] = r

    if suns_dup_report:
        warn(f"Phát hiện {len(suns_dup_report)} trùng tên trong SUNS THUOC:")
        for line in suns_dup_report:
            print(line)

    # Lấy LEG-* hiện có
    existing_legs = mysql(
        f"SELECT id, code, name FROM diab_his_pha_drugs "
        f"WHERE tenant_id={TENANT} AND code LIKE 'LEG-%'",
        fetch=True,
    )
    log(f"LEG-* hiện có trong DB: {len(existing_legs)} dòng")

    # Lấy TẤT CẢ thuốc hiện có (để tránh trùng khi thêm mới)
    existing_all = mysql(
        f"SELECT id, code, name FROM diab_his_pha_drugs WHERE tenant_id={TENANT}",
        fetch=True,
    )
    existing_by_norm = {normalize_name(r["name"]): r for r in existing_all}
    existing_codes = {r["code"] for r in existing_all}

    matched = 0
    unmatched_leg: list[dict] = []
    update_sqls: list[str] = []
    insert_sqls: list[str] = []
    suns_inserted_names: set[str] = set()

    # Cập nhật LEG-*
    for leg in existing_legs:
        nkey = normalize_name(leg["name"])
        suns = suns_by_norm.get(nkey)
        if suns:
            matched += 1
            suns_code = suns["ma"]
            sell_p = esc_dec(suns["don_gia"])
            bhyt_p = esc_dec(suns["don_gia_bh"])
            unit    = esc(suns["don_vi"] or "viên")
            strength = esc(suns["ham_luong"] or None)
            hoat_chat = esc(suns["hoat_chat"] or None)
            note   = esc(f"SUNS:{suns_code}")
            so_dk  = esc(suns["so_dang_ky"] or None)
            update_sqls.append(
                f"UPDATE diab_his_pha_drugs SET "
                f"  sell_price={sell_p}, price={sell_p}, bhyt_price={bhyt_p}, "
                f"  unit={unit}, strength={strength}, generic_name={hoat_chat}, "
                f"  note={note}, so_dang_ky={so_dk}, "
                f"  updated_at=NOW() "
                f"WHERE tenant_id={TENANT} AND code={esc(leg['code'])}"
            )
            suns_inserted_names.add(nkey)
        else:
            unmatched_leg.append(leg)

    if unmatched_leg:
        warn(f"{len(unmatched_leg)} LEG-* không khớp tên SUNS (giữ nguyên, giá 0):")
        for r in unmatched_leg:
            print(f"  {r['code']}  {r['name']}")

    # Thêm thuốc SUNS chưa có trong DB
    import uuid as _uuid
    for nkey, suns in suns_by_norm.items():
        if nkey in existing_by_norm or nkey in suns_inserted_names:
            continue
        # thuốc mới từ SUNS
        new_code = suns["ma"]  # dùng mã SUNS làm code
        if new_code in existing_codes:
            new_code = suns["ma"] + "_SUNS"
        new_id = str(_uuid.uuid4())
        sell_p = esc_dec(suns["don_gia"])
        bhyt_p = esc_dec(suns["don_gia_bh"])
        unit    = esc(suns["don_vi"] or "viên")
        strength = esc(suns["ham_luong"] or None)
        hoat_chat = esc(suns["hoat_chat"] or None)
        note   = esc(f"SUNS:{suns['ma']}")
        so_dk  = esc(suns["so_dang_ky"] or None)
        name   = esc(suns["ten"])
        insert_sqls.append(
            f"INSERT INTO diab_his_pha_drugs "
            f"(id, tenant_id, code, name, generic_name, unit, strength, "
            f" sell_price, price, bhyt_price, note, so_dang_ky, is_active, created_at) "
            f"VALUES ({esc(new_id)}, {TENANT}, {esc(new_code)}, {name}, {hoat_chat}, "
            f" {unit}, {strength}, {sell_p}, {sell_p}, {bhyt_p}, {note}, {so_dk}, 1, NOW()) "
            f"ON DUPLICATE KEY UPDATE "
            f"  sell_price={sell_p}, price={sell_p}, bhyt_price={bhyt_p}, note={note}, updated_at=NOW()"
        )
        existing_codes.add(new_code)
        suns_inserted_names.add(nkey)

    log(f"Cập nhật LEG-* khớp: {len(update_sqls)} dòng")
    log(f"Thêm mới từ SUNS: {len(insert_sqls)} dòng")

    if update_sqls:
        mysql_many(update_sqls)
    if insert_sqls:
        mysql_many(insert_sqls)

    return {
        "updated": len(update_sqls),
        "inserted": len(insert_sqls),
        "unmatched_leg": len(unmatched_leg),
        "suns_dups": len(suns_dup_report),
    }

# ---------------------------------------------------------------------------
# 2. XÉT NGHIỆM
# ---------------------------------------------------------------------------
def migrate_lab(catalog: list[dict]) -> dict:
    section("2. XÉT NGHIỆM (diab_his_dict_lab_tests)")

    suns_xn = [r for r in catalog if r["nhom"] == "XN"]
    log(f"SUNS XN: {len(suns_xn)} mục")

    # Phát hiện trùng tên trong SUNS XN
    suns_xn_by_norm: dict[str, list[dict]] = {}
    for r in suns_xn:
        nkey = normalize_name(r["ten"])
        suns_xn_by_norm.setdefault(nkey, []).append(r)

    dup_xn = {k: v for k, v in suns_xn_by_norm.items() if len(v) > 1}
    if dup_xn:
        warn(f"Trùng tên trong SUNS XN ({len(dup_xn)} nhóm):")
        for k, vs in dup_xn.items():
            print(f"  {k!r}: " + ", ".join(r["ma"] for r in vs))

    # Lấy dict lab tests hiện có
    existing_labs = mysql(
        "SELECT code, name, default_price, bhyt_price FROM diab_his_dict_lab_tests",
        fetch=True,
    )
    existing_by_code = {r["code"]: r for r in existing_labs}
    existing_by_norm = {normalize_name(r["name"]): r for r in existing_labs}
    log(f"Lab tests hiện có: {len(existing_labs)} mục")

    updated = 0
    inserted = 0
    update_sqls: list[str] = []
    insert_sqls: list[str] = []

    for r in suns_xn:
        nkey = normalize_name(r["ten"])
        dp = esc_dec(r["don_gia"])
        bhp = esc_dec(r["don_gia_bh"])
        ma_bhyt = esc(r["ma_bhyt"] or None)
        code_suns = r["ma"]

        if nkey in existing_by_norm:
            # cập nhật giá
            ex = existing_by_norm[nkey]
            update_sqls.append(
                f"UPDATE diab_his_dict_lab_tests SET "
                f"  default_price={dp}, bhyt_price={bhp} "
                f"WHERE code={esc(ex['code'])}"
            )
            updated += 1
        elif code_suns in existing_by_code:
            # code trùng nhưng tên khác → cập nhật giá, không đổi tên
            update_sqls.append(
                f"UPDATE diab_his_dict_lab_tests SET "
                f"  default_price={dp}, bhyt_price={bhp} "
                f"WHERE code={esc(code_suns)}"
            )
            updated += 1
        else:
            # thêm mới — dùng mã SUNS làm code
            name = esc(r["ten"])
            don_vi = esc(r["don_vi"] or None)
            insert_sqls.append(
                f"INSERT INTO diab_his_dict_lab_tests "
                f"(code, name, default_price, bhyt_price, is_active, unit) "
                f"VALUES ({esc(code_suns)}, {name}, {dp}, {bhp}, 1, {don_vi}) "
                f"ON DUPLICATE KEY UPDATE "
                f"  default_price={dp}, bhyt_price={bhp}"
            )
            inserted += 1

    log(f"Cập nhật giá XN hiện có: {updated}")
    log(f"Thêm mới XN từ SUNS: {inserted}")

    if update_sqls:
        mysql_many(update_sqls)
    if insert_sqls:
        mysql_many(insert_sqls)

    return {"updated": updated, "inserted": inserted, "suns_dups": len(dup_xn)}

# ---------------------------------------------------------------------------
# 3. CĐHA / Siêu âm / Điện tim
# ---------------------------------------------------------------------------
def migrate_rad(catalog: list[dict]) -> dict:
    section("3. CĐHA + Siêu âm + Điện tim (diab_his_dict_rad_procedures)")

    # SA + DT đều là thủ thuật hình ảnh/điện
    suns_rad = [r for r in catalog if r["nhom"] in ("SA", "DT")]
    log(f"SUNS SA+DT: {len(suns_rad)} mục")

    existing_rads = mysql(
        "SELECT code, name, default_price, bhyt_price FROM diab_his_dict_rad_procedures",
        fetch=True,
    )
    existing_by_norm = {normalize_name(r["name"]): r for r in existing_rads}
    existing_by_code = {r["code"]: r for r in existing_rads}
    log(f"Rad procedures hiện có: {len(existing_rads)} mục")

    updated = 0
    inserted = 0
    update_sqls: list[str] = []
    insert_sqls: list[str] = []

    for r in suns_rad:
        nkey = normalize_name(r["ten"])
        dp = esc_dec(r["don_gia"])
        bhp = esc_dec(r["don_gia_bh"])
        code_suns = r["ma"]
        modality = "ECG" if r["nhom"] == "DT" else "US"

        if nkey in existing_by_norm:
            ex = existing_by_norm[nkey]
            update_sqls.append(
                f"UPDATE diab_his_dict_rad_procedures SET "
                f"  default_price={dp}, bhyt_price={bhp} "
                f"WHERE code={esc(ex['code'])}"
            )
            updated += 1
        elif code_suns in existing_by_code:
            update_sqls.append(
                f"UPDATE diab_his_dict_rad_procedures SET "
                f"  default_price={dp}, bhyt_price={bhp} "
                f"WHERE code={esc(code_suns)}"
            )
            updated += 1
        else:
            insert_sqls.append(
                f"INSERT INTO diab_his_dict_rad_procedures "
                f"(code, name, modality, default_price, bhyt_price, is_active) "
                f"VALUES ({esc(code_suns)}, {esc(r['ten'])}, {esc(modality)}, "
                f"  {dp}, {bhp}, 1) "
                f"ON DUPLICATE KEY UPDATE "
                f"  default_price={dp}, bhyt_price={bhp}"
            )
            inserted += 1

    log(f"Cập nhật giá RAD hiện có: {updated}")
    log(f"Thêm mới RAD từ SUNS: {inserted}")

    if update_sqls:
        mysql_many(update_sqls)
    if insert_sqls:
        mysql_many(insert_sqls)

    return {"updated": updated, "inserted": inserted}

# ---------------------------------------------------------------------------
# 4. Dịch vụ khám + thủ thuật (diab_his_bil_services)
# ---------------------------------------------------------------------------
def migrate_services(catalog: list[dict]) -> dict:
    section("4. Dịch vụ khám / thủ thuật / gói (diab_his_bil_services)")

    # Nhóm → category mapping
    GROUP_CAT = {
        "KB":     "CONSULTATION",   # khám bệnh
        "DVK":    "PROCEDURE",      # dịch vụ khác
        "NBK":    "PROCEDURE",      # NBK
        "THUOCDT":"OTHER",          # thuốc điều trị đặc biệt (không phải thuốc thông thường)
        "GDV":    "OTHER",          # gói dịch vụ
    }

    suns_svc = [r for r in catalog if r["nhom"] in GROUP_CAT]
    log(f"Dịch vụ cần nạp (KB+DVK+NBK+THUOCDT+GDV): {len(suns_svc)} mục")

    # Phát hiện trùng tên
    by_norm: dict[str, list[dict]] = {}
    for r in suns_svc:
        by_norm.setdefault(normalize_name(r["ten"]), []).append(r)
    dups = {k: v for k, v in by_norm.items() if len(v) > 1}
    if dups:
        warn(f"Trùng tên trong nhóm dịch vụ ({len(dups)} nhóm):")
        for k, vs in dups.items():
            print(f"  {k!r}: " + ", ".join(f"{r['nhom']}/{r['ma']}" for r in vs))

    # Lấy dịch vụ hiện có
    existing_svcs = mysql(
        f"SELECT code, name, price FROM diab_his_bil_services WHERE tenant_id={TENANT_ID}",
        fetch=True,
    )
    existing_by_code = {r["code"]: r for r in existing_svcs}
    log(f"Dịch vụ hiện có trong DB: {len(existing_svcs)} mục")

    import uuid as _uuid
    inserted = 0
    updated = 0
    sqls: list[str] = []

    for r in suns_svc:
        code = r["ma"]
        cat = GROUP_CAT[r["nhom"]]
        price = esc_dec(r["don_gia"])
        bhyt_code = esc(r["ma_bhyt"] or None)
        bhyt_max = esc_dec(r["don_gia_bh"])
        name = esc(r["ten"])
        new_id = str(_uuid.uuid4())

        if code in existing_by_code:
            # cập nhật giá
            sqls.append(
                f"UPDATE diab_his_bil_services SET "
                f"  price={price}, bhyt_code={bhyt_code}, bhyt_max_amount={bhyt_max}, "
                f"  updated_at=NOW() "
                f"WHERE tenant_id={TENANT_ID} AND code={esc(code)}"
            )
            updated += 1
        else:
            sqls.append(
                f"INSERT INTO diab_his_bil_services "
                f"(id, tenant_id, code, name, category, price, vat_rate, "
                f" bhyt_code, bhyt_max_amount, is_active, created_at) "
                f"VALUES ({esc(new_id)}, {TENANT_ID}, {esc(code)}, {name}, {esc(cat)}, "
                f"  {price}, 0, {bhyt_code}, {bhyt_max}, 1, NOW()) "
                f"ON DUPLICATE KEY UPDATE "
                f"  price={price}, bhyt_code={bhyt_code}, updated_at=NOW()"
            )
            inserted += 1

    log(f"Thêm mới dịch vụ: {inserted}")
    log(f"Cập nhật dịch vụ: {updated}")

    if sqls:
        mysql_many(sqls)

    return {"inserted": inserted, "updated": updated, "suns_dups": len(dups)}

# ---------------------------------------------------------------------------
# 5. Vật tư (VTYT) — bỏ qua, báo cáo
# ---------------------------------------------------------------------------
def skip_vtyt(catalog: list[dict]) -> dict:
    section("5. Vật tư VTYT — bỏ qua (không có bảng riêng)")
    vtyt = [r for r in catalog if r["nhom"] == "VTYT"]
    warn(f"Bỏ qua {len(vtyt)} mục VTYT (giá 0, chưa có bảng diab_his_pha_supplies).")
    warn("Cần thêm bảng hoặc dùng diab_his_bil_services nếu muốn track vật tư sau này.")
    return {"skipped": len(vtyt)}

# ---------------------------------------------------------------------------
# Kiểm tra sau khi nạp
# ---------------------------------------------------------------------------
def verify():
    section("Kiểm tra sau khi nạp")
    rows = mysql(
        f"SELECT COUNT(*) as cnt FROM diab_his_pha_drugs WHERE tenant_id={TENANT_ID} AND sell_price > 0",
        fetch=True,
    )
    ok(f"Thuốc có giá > 0: {rows[0]['cnt'] if rows else '?'}")

    rows2 = mysql(
        "SELECT COUNT(*) as cnt FROM diab_his_dict_lab_tests WHERE default_price > 0",
        fetch=True,
    )
    ok(f"Lab tests có giá > 0: {rows2[0]['cnt'] if rows2 else '?'}")

    rows3 = mysql(
        f"SELECT COUNT(*) as cnt FROM diab_his_bil_services WHERE tenant_id={TENANT_ID}",
        fetch=True,
    )
    ok(f"Bil services tổng: {rows3[0]['cnt'] if rows3 else '?'}")

    rows4 = mysql(
        "SELECT COUNT(*) as cnt FROM diab_his_dict_rad_procedures WHERE default_price > 0",
        fetch=True,
    )
    ok(f"Rad procedures có giá > 0: {rows4[0]['cnt'] if rows4 else '?'}")

    # Kiểm tra API nội bộ
    rows5 = mysql(
        f"SELECT name, sell_price FROM diab_his_pha_drugs "
        f"WHERE tenant_id={TENANT_ID} AND sell_price > 0 LIMIT 3",
        fetch=True,
    )
    ok("Mẫu thuốc có giá:")
    for r in (rows5 or []):
        print(f"  {r['name']} — {r['sell_price']} VNĐ")

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
def main():
    print(bold("=" * 60))
    print(bold(" Import SUNS Catalog → Pro-Diab HIS"))
    print(bold("=" * 60))
    if DRY_RUN:
        print(yellow("  CHẾ ĐỘ DRY-RUN: không ghi vào DB\n"))
    else:
        print(green("  CHẾ ĐỘ THỰC: SẼ GHI VÀO DB\n"))

    if not DB_PASSWORD:
        error("DB_PASSWORD chưa được đặt!")
        error("  Chạy: export DB_PASSWORD='<root_password>'")
        sys.exit(1)

    # Kiểm tra file catalog
    if not os.path.exists(CATALOG_FILE):
        error(f"Không tìm thấy file catalog: {CATALOG_FILE}")
        error(f"  Chạy: scp docs/migration/suns-catalog-20261015.json stackops@server:{CATALOG_FILE}")
        sys.exit(1)

    catalog = load_catalog()
    log(f"Đã load catalog: {len(catalog)} mục từ {CATALOG_FILE}")

    # Backup (chỉ thực khi không dry-run)
    if not DRY_RUN:
        backup_path = backup_db()
        log(f"Rollback: mysqldump restore từ {backup_path}")
    else:
        log("[DRY-RUN] Bỏ qua backup")

    # Migrate từng nhóm
    stats_drug = migrate_drugs(catalog)
    stats_lab  = migrate_lab(catalog)
    stats_rad  = migrate_rad(catalog)
    stats_svc  = migrate_services(catalog)
    stats_vtyt = skip_vtyt(catalog)

    # Verify
    if not DRY_RUN:
        verify()

    # Báo cáo tổng kết
    section("BÁO CÁO TỔNG KẾT")
    print(f"""
Nhóm              | Hành động              | Số lượng
------------------|------------------------|----------
THUOC (thuốc)     | Cập nhật LEG-* khớp   | {stats_drug['updated']}
                  | Thêm mới từ SUNS       | {stats_drug['inserted']}
                  | LEG-* không khớp       | {stats_drug['unmatched_leg']}  (giá vẫn 0)
                  | Trùng tên trong SUNS   | {stats_drug['suns_dups']}
XN (xét nghiệm)  | Cập nhật giá hiện có  | {stats_lab['updated']}
                  | Thêm mới               | {stats_lab['inserted']}
                  | Trùng tên trong SUNS   | {stats_lab['suns_dups']}
SA+DT (CĐHA)     | Cập nhật giá hiện có  | {stats_rad['updated']}
                  | Thêm mới               | {stats_rad['inserted']}
Dịch vụ (KB+DVK+)| Thêm mới               | {stats_svc['inserted']}
                  | Cập nhật               | {stats_svc['updated']}
                  | Trùng tên              | {stats_svc['suns_dups']}
VTYT (vật tư)    | Bỏ qua                 | {stats_vtyt['skipped']}
""")
    if DRY_RUN:
        print(yellow("Chạy lại KHÔNG có --dry-run để ghi thực vào DB."))
    else:
        ok("Migration hoàn tất.")
        print("Rollback: restore file backup đã tạo ở trên bằng:")
        print("  gunzip -c <backup_file> | docker exec -i -e MYSQL_PWD=... pro-his-db mysql -uroot prodiab_his")


if __name__ == "__main__":
    main()
